using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Mapping_Tools.classes.BeatmapHelper;

namespace Mapping_Tools.views {
    /// <summary>
    /// Interaktionslogik für UserControl1.xaml
    /// </summary>
    public partial class SliderMergerView :UserControl {
        private BackgroundWorker backgroundWorker;

        public SliderMergerView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            backgroundWorker =
                        ( (BackgroundWorker) this.FindResource("backgroundWorker") );
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Merge_Sliders((string) e.Argument, bgw, e);
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if( e.Error != null ) {
                MessageBox.Show(e.Error.Message);
            }
            else {
                MessageBox.Show(e.Result.ToString());
                progress.Value = 0;
            }
            start.IsEnabled = true;
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progress.Value = e.ProgressPercentage;
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            DateTime now = DateTime.Now;
            string fileToCopy = MainWindow.AppWindow.currentMap.Text;
            string destinationDirectory = System.Environment.CurrentDirectory + "\\Backups\\";
            try {
                File.Copy(fileToCopy, destinationDirectory + now.ToString("yyyy-MM-dd HH-mm-ss") + "___" + System.IO.Path.GetFileName(fileToCopy));
            }
            catch( Exception ex ) {
                MessageBox.Show(ex.Message);
                return;
            }
            backgroundWorker.RunWorkerAsync(fileToCopy);
            start.IsEnabled = false;
        }

        private string Merge_Sliders(string path, BackgroundWorker worker, DoWorkEventArgs e) {
            //Get and edit whole contents and rewrite after
            string[] linesz = new string[0];
            try {
                linesz = System.IO.File.ReadAllLines(path);
            }
            catch( Exception ex ) {
                MessageBox.Show(ex.Message);
                return "File could not be found.";
            }
            var lines = new List<string>(linesz);


            //edit
            bool atObjects = false;
            int currentLine = 0;
            int Sliders_Merged = 0;

            while( currentLine + 1 < lines.Count ) {
                if( atObjects ) {
                    string[] values = lines[currentLine].Split(',');
                    string[] values2 = lines[currentLine + 1].Split(',');
                    //Check if 2 concurrent sliders
                    if( IsSlider(values) && IsSlider(values2) ) {
                        //Check if the 2 slider on eachothers dick
                        string[] sliderData = values[5].Split('|');
                        string[] sliderData2 = values2[5].Split('|');

                        //collect all their anchors
                        string[] lastAnchor = sliderData[sliderData.Length - 1].Split(':');

                        if( lastAnchor[0] == values2[0] && lastAnchor[1] == values2[1] ) {
                            //check if anyone is Circular and convert them
                            string anchors = null;
                            string anchors2 = null;

                            if( sliderData[0] == "P" ) {
                                anchors = ConvertSlider(values, sliderData, worker, e);
                            }
                            else {
                                anchors = values[5].Substring(1);
                            }
                            if( sliderData2[0] == "P" ) {
                                anchors2 = ConvertSlider(values2, sliderData2, worker, e);
                            }
                            else {
                                anchors2 = values2[5].Substring(1);
                            }

                            //anchors would be like B|0:0|0:0|0:0
                            values[5] = "B" + anchors + "|" + values2[0] + ":" + values2[1] + anchors2;

                            //add the pixellengths
                            double length = double.Parse(values[7], CultureInfo.InvariantCulture) + double.Parse(values2[7], CultureInfo.InvariantCulture);
                            values[7] = length.ToString(CultureInfo.InvariantCulture);

                            //make the new line and remove the other
                            lines[currentLine] = String.Join(",", values);
                            lines.RemoveAt(currentLine + 1);
                            currentLine -= 1;
                            Sliders_Merged += 1;
                        }
                    }
                }
                else {
                    if( lines[currentLine] == "[HitObjects]" ) {
                        atObjects = true;
                    }
                }
                currentLine += 1;
            }

            //foreach (var item in lines)
            //    Debug.WriteLine(item);

            // Complete progressbar
            if( worker != null && worker.WorkerReportsProgress ) {
                worker.ReportProgress(100);
            }

            try {
                System.IO.File.WriteAllLines(path, lines);
            }
            catch( Exception ex ) {
                MessageBox.Show(ex.Message);
                return "ERROR: File could not be saved!";
            }
            
            return "Succesfully merged " + Sliders_Merged + " sliders!";
        }


        private bool IsSlider(string[] values) {
            BitArray b = new BitArray(new int[] { int.Parse(values[3]) });
            return b[1] == true;
        }

        private List<double> GetBookmarks(List<String> lines) {
            foreach( string line in lines ) {
                // Bookmarks: 233929,256422,501083,503779,505970,549875
                if( line.Split(':')[0] == "Bookmarks" ) {
                    string[] bookmarksString = line.Split(':')[1].Split(',');
                    List<double> bookmarks = new List<double>();
                    foreach( string bookmark in bookmarksString ) {
                        bookmarks.Add(double.Parse(bookmark));
                    }
                    return bookmarks;
                }
            }
            // No bookmarks found. Return empty list
            return new List<double>();
        }

        private string ConvertSlider(string[] values, string[] sliderData, BackgroundWorker worker, DoWorkEventArgs e) {
            //get the anchors and middle of circle
            Poi p1 = new Poi(double.Parse(values[0]), double.Parse(values[1]));
            Poi p2 = new Poi(double.Parse(sliderData[1].Split(':')[0]), double.Parse(sliderData[1].Split(':')[1]));
            Poi p3 = new Poi(double.Parse(sliderData[2].Split(':')[0]), double.Parse(sliderData[2].Split(':')[1]));

            const int bezier_steps = 20;
            const int looky_steps = 500;
            const int num_steps = 20000;
            const double loss_goal = 0.0015;

            Poi pc = CircleCenter(p1, p2, p3);
            double r = pc.GetDistance(p1);

            Poi d1 = p1 - pc;
            Poi d2 = p2 - pc;
            Poi d3 = p3 - pc;

            double a1 = d1.GetAngle();
            double a2 = d2.GetAngle();
            double a3 = d3.GetAngle();

            double da1 = getSmallestAngle(a1, a2);
            double da2 = getSmallestAngle(a2, a3);

            if( da1 * da2 > 0 ) {
                a2 = ( da1 + da2 ) / 2 + a1;
            }
            else {
                a2 = ( da1 + da2 ) / 2 + Math.PI + a1;
            }
            p2 = new Poi(r * Math.Cos(a2), r * Math.Sin(a2)) + pc;

            /*Debug.WriteLine("x " + p1.X + " y " + p1.Y);
            Debug.WriteLine("x " + p2.X + " y " + p2.Y);
            Debug.WriteLine("x " + p3.X + " y " + p3.Y);*/

            double da = getSmallestAngle(a1, a2);
            Tuple<double, double, double> abc = MakeLine(p2, pc);

            // Get the tangents for snapping later
            Tuple<double, double, double> firstTangent = PointAngleToLine(p1, a1 + 0.5 * Math.PI);
            Tuple<double, double, double> lastTangent = PointAngleToLine(p3, a3 + 0.5 * Math.PI);

            double lrr = 0;
            if( Math.Abs(da) > Math.PI / 4 ) {
                lrr = 0.2;
            }
            else {
                lrr = 0.35;
            }

            int num_anchors = (int) Math.Ceiling(Math.Abs(da * 1.1));
            //Debug.WriteLine("Number of anchors: " + num_anchors);

            double dv = da / ( num_anchors + 1 );

            List<Poi> prev_anchors = MakeAnchors(pc, r, dv, num_anchors, a1);
            SnapTangents(prev_anchors, firstTangent, lastTangent);
            List<Poi> anchors = new List<Poi>();



            List<Poi> Points = MakePoints(p1, prev_anchors, p3, abc);
            double prev_loss = TestPoints(Points, pc, r, bezier_steps);
            double loss = new double();

            int step = 0;
            Random random = new Random();

            while( prev_loss > loss_goal ) {
                double lr = r * prev_loss * lrr;

                double bl = prev_loss;
                List<Poi> next_anchors = new List<Poi>();
                for( int i = 0; i < Math.Ceiling(prev_loss * looky_steps); i++ ) {
                    anchors = Mutate(prev_anchors, lr, random);
                    SnapTangents(anchors, firstTangent, lastTangent);
                    Points = MakePoints(p1, anchors, p3, abc);
                    loss = TestPoints(Points, pc, r, bezier_steps);

                    if( loss < bl ) {
                        bl = loss;
                        next_anchors = new List<Poi>(anchors);
                    }
                }

                if( bl < prev_loss ) {
                    prev_anchors = new List<Poi>(next_anchors);
                    prev_loss = bl;
                    if( worker != null ) {
                        if( worker.WorkerReportsProgress ) {
                            int percentComplete = (int) ( ( 1 - prev_loss ) * 100 );
                            worker.ReportProgress(percentComplete);
                        }
                    }

                }

                step += 1;
                if( step > num_steps ) {
                    //Debug.WriteLine("resetting " + prev_loss);
                    prev_anchors = MakeAnchors(pc, r, dv, num_anchors, a1);
                    SnapTangents(prev_anchors, firstTangent, lastTangent);
                    Points = MakePoints(p1, prev_anchors, p3, abc);
                    prev_loss = TestPoints(Points, pc, r, bezier_steps);
                    step = 0;
                }
            }

            //Debug.WriteLine("Steps took " + step);
            //Debug.WriteLine("Loss: ", prev_loss);
            Points = MakePoints(p1, prev_anchors, p3, abc);

            string ret = "";

            for( int i = 1; i < Points.Count; i++ ) {
                ret += "|" + Math.Round(Points[i].X);
                ret += ":" + Math.Round(Points[i].Y);
            }

            return ret;

        }












        private double Modulo(double a, double n) {
            return a - Math.Floor(a / n) * n;
        }

        private double getSmallestAngle(double a1, double a2) {
            return Modulo(( a2 - a1 + Math.PI ), ( 2 * Math.PI )) - Math.PI;
        }

        private Poi CircleCenter(Poi p1, Poi p2, Poi p3) {
            double t = p2.X * p2.X + p2.Y * p2.Y;
            double bc = ( p1.X * p1.X + p1.Y * p1.Y - t ) / 2.0;
            double cd = ( t - p3.X * p3.X - p3.Y * p3.Y ) / 2.0;
            double det = ( p1.X - p2.X ) * ( p2.Y - p3.Y ) - ( p2.X - p3.X ) * ( p1.Y - p2.Y );

            det = 1 / det;
            // Avoid NaN
            if( det > 99 ) {
                det = 99;
            }
            double x = ( bc * ( p2.Y - p3.Y ) - cd * ( p1.Y - p2.Y ) ) * det;
            double y = ( ( p1.X - p2.X ) * cd - ( p2.X - p3.X ) * bc ) * det;

            Poi Centre = new Poi(x, y);
            return Centre;
        }

        private Tuple<double, double, double> MakeLine(Poi p1, Poi p2) {
            if( p1.X == p2.X ) {
                return new Tuple<double, double, double>(1, 0, p1.X);
            }
            double a = -1 * ( p2.Y - p1.Y ) / ( p2.X - p1.X );
            double c = p1.Y + a * p1.X;
            return new Tuple<double, double, double>(a, 1, c);
        }

        private Tuple<double, double, double> PointAngleToLine(Poi p1, double angle) {
            if( Math.Abs(angle) == 0.5 * Math.PI ) {
                return new Tuple<double, double, double>(1, 0, p1.X);
            }
            double a = -1 * Math.Tan(angle);
            double c = p1.Y + a * p1.X;
            return new Tuple<double, double, double>(a, 1, c);
        }

        private List<Poi> MakeAnchors(Poi pc, double r, double dv, double num_anchors, double a1) {
            List<Poi> anchors = new List<Poi>();
            for( int n = 1; n <= num_anchors; n++ ) {
                double theta = n * dv + a1;
                double co = Math.Cos(theta);
                double si = Math.Sin(theta);
                anchors.Add(pc + new Poi(co, si) * r);
            }
            return anchors;
        }

        private List<Poi> MakePoints(Poi p1, List<Poi> anchors, Poi p2, Tuple<double, double, double> abc) {
            List<Poi> fullanchors = anchors.Concat(MirrorPoints(anchors, abc)).ToList();
            fullanchors.Insert(0, p1);
            fullanchors.Insert(fullanchors.Count, p2);
            return fullanchors;
        }

        private List<Poi> MirrorPoints(List<Poi> points, Tuple<double, double, double> line) {
            List<Poi> newPoints = new List<Poi>();
            foreach( Poi p in points ) {
                newPoints.Add(p.MirrorPoint(line, 2));
            }
            newPoints.Reverse();
            return newPoints;
        }

        private double TestPoints(List<Poi> points, Poi pc, double r, int bezier_steps) {
            double hLoss = 0;
            for( int n = 0; n < bezier_steps; n++ ) {
                double t = ( n + 0.2 ) / ( bezier_steps - 0.8 ) * 0.5;
                Poi p = Bezier(points, t);
                double loss = Math.Abs(p.GetDistance(pc) - r) / r;
                if( loss > hLoss ) {
                    hLoss = loss;
                }
            }
            return hLoss;
        }

        private Poi Bezier(List<Poi> points, double t) {
            List<Poi> bPoints = new List<Poi>(points);
            int Order = bPoints.Count - 1;
            int Calc_Order = Order;
            int at = 0;
            for( int i = 0; i < Order; i++ ) {
                for( int n = 0; n < Calc_Order; n++ ) {
                    bPoints.Add(bPoints[at] * ( 1 - t ) + bPoints[at + 1] * t);
                    at += 1;
                }
                Calc_Order -= 1;
                at += 1;
            }
            return bPoints[bPoints.Count - 1];
        }

        private List<Poi> Mutate(List<Poi> points, double lr, Random random) {
            List<Poi> newPoints = new List<Poi>(points);
            for( int i = 0; i < newPoints.Count; i++ ) {
                double randomx = random.NextDouble() - 0.5;
                double randomy = random.NextDouble() - 0.5;
                newPoints[i] = new Poi(newPoints[i].X + lr * randomx, newPoints[i].Y + lr * randomy);
            }
            return newPoints;
        }

        private void SnapTangents(List<Poi> points, Tuple<double, double, double> firstLine, Tuple<double, double, double> lastLine) {
            points[0] = points[0].MirrorPoint(firstLine, 1);
            //points[points.Count - 1] = points[points.Count - 1].MirrorPoint(lastLine, 1);
        }

        private void PrintListP(List<Poi> points) {
            for( int n = 0; n < points.Count; n++ ) {
                Debug.WriteLine("X: " + points[n].X.ToString() + " Y: " + points[n].Y.ToString());
            }
        }

        private void Print(string str) {
            Debug.WriteLine(str);
        }
    }
}
