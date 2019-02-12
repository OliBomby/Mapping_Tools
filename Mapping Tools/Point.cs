using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools {
    class Poi {
        public double X { get; set; }
        public double Y { get; set; }
        public Poi(double X, double Y) {
            this.X = X;
            this.Y = Y;
        }

        public static Poi operator -(Poi p1, Poi p2) {
            return new Poi(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static Poi operator +(Poi p1, Poi p2) {
            return new Poi(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Poi operator *(Poi p1, double s) {
            return new Poi(p1.X * s, p1.Y * s);
        }

        public static Poi operator /(Poi p1, double s) {
            return new Poi(p1.X / s, p1.Y / s);
        }

        public static bool operator ==(Poi p1, Poi p2) {
            return p1.X == p2.X && p1.Y == p2.Y;
        }

        public static bool operator !=(Poi p1, Poi p2) {
            return p1.X != p2.X || p1.Y != p2.Y;
        }

        public Poi Rotate(double theta) {
            return new Poi(X * Math.Cos(theta) - Y * Math.Sin(theta), Y * Math.Cos(theta) + X * Math.Sin(theta));
        }

        public Poi Round() {
            return new Poi(Math.Round(X), Math.Round(Y));
        }

        public double GetAngle() {
            if (Y < 0) {
                return -Math.Acos(X / GetDistance());
            }
            else {
                return Math.Acos(X / GetDistance());
            }
        }

        public double GetDistance(Poi p2) {
            return Math.Sqrt((p2.X - X) * (p2.X - X) + (p2.Y - Y) * (p2.Y - Y));
        }

        public double GetDistance() {
            return Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2));
        }

        public string StringX() {
            return Math.Round(X).ToString();
        }

        public string StringY() {
            return Math.Round(Y).ToString();
        }

        public string GetString() {
            return Math.Round(X) + ", " + Math.Round(Y);
        }

        internal Poi MirrorPoint(Tuple<double, double, double> line, int v) {
            throw new NotImplementedException();
        }
    }
}
