using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.SlideratorStuff {
    public static class SliderPicturator {
        private const int OsupxBetweenRows = 960;
        private const double LightenAmount = 0.25;
        private const double DarkenAmount = 0.1;
        private const byte Alpha = 180;
        private static int Snaptol => (int) Math.Pow(2, 5) * 3;

        private static Color GetOpaqueColor(Color top, Color bottom) {
            const double gamma = 1;
            double topOpacity = top.A / 255.0;
            double bottomOpacity = bottom.A / 255.0;
            double totOpacity = topOpacity + bottomOpacity * (1 - topOpacity);
            return Color.FromArgb(255,
                (byte) Math.Round(Math.Pow((Math.Pow(bottom.R, gamma) * bottomOpacity * (1 - topOpacity) + Math.Pow(top.R, gamma) * topOpacity) / totOpacity, 1 / gamma)),
                (byte) Math.Round(Math.Pow((Math.Pow(bottom.G, gamma) * bottomOpacity * (1 - topOpacity) + Math.Pow(top.G, gamma) * topOpacity) / totOpacity, 1 / gamma)),
                (byte) Math.Round(Math.Pow((Math.Pow(bottom.B, gamma) * bottomOpacity * (1 - topOpacity) + Math.Pow(top.B, gamma) * topOpacity) / totOpacity, 1 / gamma)));
        }

        // TODO: update segment count after polishing sliderball control segments to reflect an upper bound (x segments per ms)
        public static (Bitmap, long) Recolor(Bitmap img, Color sliderColor, Color sliderBorder, Color backgroundColor, HitObject slider = null, bool blackOff = false,
            bool borderOff = false, bool opaqueOff = false, bool r = true, bool g = true, bool b = true, int quality = 101) {
            Color innerColor = Color.FromArgb(Alpha,
                (byte) Math.Min(255, sliderColor.R * (1 + 0.5 * LightenAmount) + 255 * LightenAmount),
                (byte) Math.Min(255, sliderColor.G * (1 + 0.5 * LightenAmount) + 255 * LightenAmount),
                (byte) Math.Min(255, sliderColor.B * (1 + 0.5 * LightenAmount) + 255 * LightenAmount));
            Color outerColor = Color.FromArgb(Alpha,
                (byte) Math.Min(255, sliderColor.R / (1 + DarkenAmount)),
                (byte) Math.Min(255, sliderColor.G / (1 + DarkenAmount)),
                (byte) Math.Min(255, sliderColor.B / (1 + DarkenAmount)));

            Color opaqueIC = GetOpaqueColor(innerColor, backgroundColor);
            Color opaqueOC = GetOpaqueColor(outerColor, backgroundColor);

            var projVec = new Vector3(opaqueIC.R - opaqueOC.R, opaqueIC.G - opaqueOC.G, opaqueIC.B - opaqueOC.B);
            double projVecLen = projVec.Length;
            var opaqueOCVec = new Vector3(opaqueOC.R, opaqueOC.G, opaqueOC.B);
            var opaqueICVec = new Vector3(opaqueIC.R, opaqueIC.G, opaqueIC.B);
            var sBColVec = new Vector3(sliderBorder.R, sliderBorder.G, sliderBorder.B);

            double gradientDist;
            var ret = (Bitmap) img.Clone();

            int imgWidth = img.Width;
            int imgHeight = img.Height;
            double[,] pixDist = new double[imgWidth, imgHeight];

            unsafe {
                BitmapData imgData = img.LockBits(new Rectangle(0, 0, imgWidth, imgHeight), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                BitmapData retData = ret.LockBits(new Rectangle(0, 0, imgWidth, imgHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                for (int i = 0; i < imgWidth; i++) {
                    for (int j = 0; j < imgHeight; j++) {
                        Color pixel = Color.FromArgb(((int*) imgData.Scan0)[j * imgWidth + i]);
                        if (!opaqueOff) {
                            pixel = GetOpaqueColor(pixel, backgroundColor);
                        }

                        var colorVec = new Vector3(r ? pixel.R : 0, g ? pixel.G : 0, b ? pixel.B : 0);
                        Vector3 proj = Vector3.Dot(colorVec - opaqueOCVec, projVec) / Vector3.Dot(projVec, projVec) * projVec + opaqueOCVec;
                        Vector3 closestGradientVec;
                        if (proj.X < opaqueOCVec.X) {
                            closestGradientVec = opaqueOCVec;
                        } else if (proj.X > opaqueICVec.X) {
                            closestGradientVec = opaqueICVec;
                        } else {
                            closestGradientVec = proj;
                        }

                        gradientDist = (colorVec - closestGradientVec).LengthSquared;
                        double borderDist = (colorVec - sBColVec).LengthSquared;
                        double blackDist = colorVec.LengthSquared;
                        // Test if border color would be better
                        if (borderOff || gradientDist < borderDist) {
                            // Test if black would be better
                            if (!blackOff && blackDist < gradientDist) {
                                pixDist[i, j] = 1.2;
                                ((uint*) retData.Scan0)[j * imgWidth + i] = 0xFF000000;
                            } else {
                                pixDist[i, j] = Math.Round(quality * Math.Clamp(1 - (closestGradientVec - opaqueOCVec).Length / projVecLen, 0, 1)) * (101d / quality) / 128;
                                Vector3 usedColor = opaqueICVec - pixDist[i, j] * projVec;
                                ((int*) retData.Scan0)[j * imgWidth + i] =
                                    Color.FromArgb((int) Math.Round(usedColor[0]), (int) Math.Round(usedColor[1]), (int) Math.Round(usedColor[2])).ToArgb();
                            }
                        } else {
                            // Test if black would be better
                            if (!blackOff && blackDist < borderDist) {
                                pixDist[i, j] = 1.2;
                                ((uint*) retData.Scan0)[j * imgWidth + i] = 0xFF000000;
                            } else {
                                pixDist[i, j] = 111.0 / 128;
                                ((int*) retData.Scan0)[j * imgWidth + i] = sliderBorder.ToArgb();
                            }
                        }
                    }
                }

                img.UnlockBits(imgData);
                ret.UnlockBits(retData);
            }

            // Count segments
            long numSegments = 0;
            int leftToRight = -1;
            // In the below loop, gradientDist means something completely different from what it means in the above loop. Here, it is being used to mean the distance in the gradient between two or more points that are evenly distributed along the slider body
            for (int i = 0; i < imgHeight; i++) {
                leftToRight = -leftToRight;
                int columnStartCoordinate = leftToRight == 1 ? 0 : imgWidth - 1;
                while (leftToRight == 1 ? columnStartCoordinate < imgWidth : columnStartCoordinate >= 0) {
                    // Look for gradients
                    int columnStartOffset = 0;
                    if (0 <= columnStartCoordinate + leftToRight && columnStartCoordinate + leftToRight < imgWidth) {
                        gradientDist = pixDist[columnStartCoordinate + leftToRight, i] - pixDist[columnStartCoordinate, i];
                        columnStartOffset += leftToRight;
                        while (0 <= columnStartCoordinate + columnStartOffset + leftToRight &&
                               columnStartCoordinate + columnStartOffset + leftToRight < imgWidth &&
                               Math.Abs(pixDist[columnStartCoordinate + columnStartOffset + leftToRight, i] - pixDist[columnStartCoordinate + columnStartOffset, i] - gradientDist) <= 0.001) {
                            columnStartOffset += leftToRight;
                        }
                    }

                    int columnEndCoordinate = columnStartCoordinate + columnStartOffset;
                    columnStartCoordinate = columnEndCoordinate + leftToRight;
                    numSegments += 2;
                }

                numSegments += 1;
            }

            if (slider is not { IsSlider: true }) {
                return (ret, numSegments);
            }

            int duration = (int) Math.Floor(slider.TemporalLength);

            // We make these assumptions to overestimate segment count. GPU probably cancels out
            const double circleSize = 10;
            const double objectRadius = 1.00041 * (54.4 - 4.48 * circleSize);
            const double gpu = 65536;
            var topLeftOsuPxImage = new Vector2(-104, -52);
            Vector2 topLeftOsuPxSlider = new Vector2(Math.Ceiling(objectRadius * 1.15)) + topLeftOsuPxImage;
            var startSliderCoordinate = new Vector2(topLeftOsuPxSlider.X, topLeftOsuPxSlider.Y);
            Vector2 bottomRightOsuPxSlider = new Vector2(Math.Floor(OsupxBetweenRows * gpu - 1.15 * objectRadius)) + topLeftOsuPxImage;

            var curPath = new List<Vector2> {
                startSliderCoordinate,
                new(startSliderCoordinate.X, topLeftOsuPxSlider.Y),
                new(bottomRightOsuPxSlider.X, topLeftOsuPxSlider.Y),
                bottomRightOsuPxSlider,
                new(bottomRightOsuPxSlider.X, topLeftOsuPxSlider.Y),
                topLeftOsuPxSlider
            };
            // This estimation seems pretty good for most use cases
            double frameDist = 2 * OsuStableDistance(curPath);
            // 700 is an upper bound on the x position of the sliderball
            double availableDist = 2 * (bottomRightOsuPxSlider.X - 700);
            numSegments += 2 * ((int) Math.Floor(frameDist / availableDist) + 1) * duration + duration;

            return (ret, numSegments);
        }

        public static (List<Vector2>, double) Picturate(Bitmap img, Color sliderColor, Color sliderBorder, Color backgroundColor, double circleSize, Vector2 startPos,
            Vector2 startPosPic, HitObject slider = null, double resY = 1080, long gpu = 16384, bool blackOff = false, bool borderOff = false, bool opaqueOff = false,
            bool r = true, bool g = true, bool b = true, int quality = 101) {
            Color innerColor = Color.FromArgb(Alpha,
                (byte) Math.Min(255, sliderColor.R * (1 + 0.5 * LightenAmount) + 255 * LightenAmount),
                (byte) Math.Min(255, sliderColor.G * (1 + 0.5 * LightenAmount) + 255 * LightenAmount),
                (byte) Math.Min(255, sliderColor.B * (1 + 0.5 * LightenAmount) + 255 * LightenAmount));
            Color outerColor = Color.FromArgb(Alpha,
                (byte) Math.Min(255, sliderColor.R / (1 + DarkenAmount)),
                (byte) Math.Min(255, sliderColor.G / (1 + DarkenAmount)),
                (byte) Math.Min(255, sliderColor.B / (1 + DarkenAmount)));

            Color opaqueIC = GetOpaqueColor(innerColor, backgroundColor);
            Color opaqueOC = GetOpaqueColor(outerColor, backgroundColor);

            // startPos, startPosPic are in osupx
            startPos.Round();
            startPosPic.Round();
            double objectRadius = 1.00041 * (54.4 - 4.48 * circleSize);

            var projVec = new Vector3(opaqueIC.R - opaqueOC.R, opaqueIC.G - opaqueOC.G, opaqueIC.B - opaqueOC.B);
            double projVecLen = projVec.Length;
            var opaqueOCVec = new Vector3(opaqueOC.R, opaqueOC.G, opaqueOC.B);
            var opaqueICVec = new Vector3(opaqueIC.R, opaqueIC.G, opaqueIC.B);
            var sBColVec = new Vector3(sliderBorder.R, sliderBorder.G, sliderBorder.B);

            int imgWidth = img.Width;
            int imgHeight = img.Height;
            double[,] pixDist = new double[imgWidth, imgHeight];
            double gradientDist;
            unsafe {
                BitmapData imgData = img.LockBits(new Rectangle(0, 0, imgWidth, imgHeight), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                for (int i = 0; i < imgWidth; i++) {
                    for (int j = 0; j < imgHeight; j++) {
                        Color pixel = Color.FromArgb(((int*) imgData.Scan0)[j * imgWidth + i]);
                        if (!opaqueOff) {
                            pixel = GetOpaqueColor(pixel, backgroundColor);
                        }

                        var colorVec = new Vector3(r ? pixel.R : 0, g ? pixel.G : 0, b ? pixel.B : 0);
                        Vector3 proj = Vector3.Dot(colorVec - opaqueOCVec, projVec) / Vector3.Dot(projVec, projVec) * projVec + opaqueOCVec;
                        Vector3 closestGradientVec;
                        if (proj.X < opaqueOCVec.X) {
                            closestGradientVec = opaqueOCVec;
                        } else if (proj.X > opaqueICVec.X) {
                            closestGradientVec = opaqueICVec;
                        } else {
                            closestGradientVec = proj;
                        }

                        gradientDist = (colorVec - closestGradientVec).LengthSquared;
                        double borderDist = (colorVec - sBColVec).LengthSquared;
                        double blackDist = colorVec.LengthSquared;
                        // Test if border color would be better
                        if (borderOff || gradientDist < borderDist) {
                            // Test if black would be better
                            if (!blackOff && blackDist < gradientDist) {
                                pixDist[i, j] = 1.2;
                            } else {
                                pixDist[i, j] = Math.Round(quality * Math.Clamp(1 - (closestGradientVec - opaqueOCVec).Length / projVecLen, 0, 1)) * (101d / quality) / 128;
                            }
                        } else {
                            // Test if black would be better
                            if (!blackOff && blackDist < borderDist) {
                                pixDist[i, j] = 1.2;
                            } else {
                                pixDist[i, j] = 111.0 / 128;
                            }
                        }
                    }
                }

                img.UnlockBits(imgData);
            }


            // (16+20n, 8+20m) for matching editor to gameplay. Require 8+20m>=-56 for the bounding box to not be cropped down to be entirely inside the playfield during gameplay (which changes how the distortion is applied).
            // 16+20n >= x is  required for some x depending on resolution. It's probably the case that x <= -104 for all resolutions. If resY is not a multiple of 60, the distortion will look different in gameplay and in editor.
            // Currently there are no plans to support resolutions whose heights are not multiples of 60.
            var topLeftOsuPxImage = new Vector2(-104, -52);
            Vector2 startSliderCoordinate = startPos;
            // For now we will ignore the fact that this may interfere with the sample points
            Vector2 topLeftOsuPxSlider = new Vector2(Math.Ceiling(objectRadius * 1.15)) + topLeftOsuPxImage;
            Vector2 bottomRightOsuPxSlider = new Vector2(Math.Floor(OsupxBetweenRows * gpu - 1.15 * objectRadius)) + topLeftOsuPxImage;
            // To get screenpx from osupx topLeftOsuPxImage to osupx startPosPic we do the following:
            // the game window is 480 osupx tall and resY-16 screenpx tall, so the ratio is (resY-16)/480 screenpx per osupx.
            startPosPic -= topLeftOsuPxImage;
            startPosPic *= (resY - 16) / 480;
            startPosPic.Round();
            Vector2 imageStartOsuPx = topLeftOsuPxImage + OsupxBetweenRows * startPosPic;


            // Handle sliderball control calculations
            Vector2[] sbPositions = null;
            Vector2[] msLastSegStart = null;
            int duration = 0;
            if (slider is { IsSlider: true }) {
                duration = (int) Math.Floor(slider.TemporalLength);
                sbPositions = new Vector2[duration + 1];
                for (int i = 0; i < duration + 1; i++) {
                    sbPositions[i] = slider.SliderPath.SliderballPositionAt(i, duration);
                }

                // Before rounding sbPositions, calculate starting coordinate for each ms' final segment to make the sliderball rotate appropriately
                msLastSegStart = new Vector2[duration + 1];
                // We don't care about msLastSegStart[0] so we'll leave it at 0. Technically we could save one Vector2's worth of space here but it would make indexing harder to read than necessary.
                // Find the first angle - we can't calculate the angle between points that are the same, but the sliderball's rotation should be the same as it was before.
                double savedAng = 0;
                for (int i = 1; i < duration + 1; i++) {
                    if (sbPositions[0] != sbPositions[i]) {
                        savedAng = Math.Atan2(sbPositions[i - 1].Y - sbPositions[i].Y, sbPositions[i - 1].X - sbPositions[i].X);
                    }
                }

                for (int i = 1; i < duration + 1; i++) {
                    double ang;
                    if (sbPositions[i - 1] == sbPositions[i]) {
                        ang = savedAng;
                    } else {
                        ang = Math.Atan2(sbPositions[i - 1].Y - sbPositions[i].Y, sbPositions[i - 1].X - sbPositions[i].X);
                        savedAng = ang;
                    }

                    msLastSegStart[i] = new Vector2((float) (Snaptol * Math.Cos(ang) + (float) sbPositions[i].Rounded().X),
                        (float) (Snaptol * Math.Sin(ang) + (float) sbPositions[i].Rounded().Y));
                }

                // Round all positions to float precision values
                for (int i = 0; i < duration + 1; i++) {
                    sbPositions[i].Round();
                    sbPositions[i] = new Vector2((float) sbPositions[i].X, (float) sbPositions[i].Y);
                    msLastSegStart[i].Round();
                    // Make the snapping segment straight to the right if it would otherwise go outside the bounding box
                    if (msLastSegStart[i].X < topLeftOsuPxSlider.X || msLastSegStart[i].Y < topLeftOsuPxSlider.Y) {
                        msLastSegStart[i] = new Vector2((float) (sbPositions[i].Rounded().X + 60), (float) sbPositions[i].Rounded().Y);
                    } else {
                        msLastSegStart[i] = new Vector2((float) msLastSegStart[i].X, (float) msLastSegStart[i].Y);
                    }
                }
            }


            int leftToRight = -1;

            var sliderPath = new List<Vector2>();
            // sliderPaths is the sliderPath broken up into segments that end nearest to the play area
            var sliderPaths = new List<List<Vector2>>();
            // curPath is the currently processing element of sliderPaths
            var curPath = new List<Vector2> {
                startSliderCoordinate,
                new(startSliderCoordinate.X, topLeftOsuPxSlider.Y),
                new(bottomRightOsuPxSlider.X, topLeftOsuPxSlider.Y),
                bottomRightOsuPxSlider,
                new(bottomRightOsuPxSlider.X, topLeftOsuPxSlider.Y),
                topLeftOsuPxSlider
            };
            sliderPaths.Add(curPath.Copy());
            curPath.Clear();
            // Move to the start of the image, avoiding sample points (could be done better)
            if (startPosPic.LengthSquared > 0) {
                curPath.Add(new Vector2(topLeftOsuPxSlider.X, imageStartOsuPx.Y));
                curPath.Add(imageStartOsuPx);
            }

            int absoluteStartX = 0;
            int columnStartOffset = 0;
            int absoluteStartY = 0;
            gradientDist = 0;
            // In the below loop, gradientDist means something completely different from what it means in the above loop. Here, it is being used to mean the distance in the gradient between two or more points that are evenly distributed along the slider body
            for (int i = 0; i < imgHeight; i++) {
                leftToRight = -leftToRight;
                int columnStartCoordinate = leftToRight == 1 ? 0 : imgWidth - 1;
                while (leftToRight == 1 ? columnStartCoordinate < imgWidth : columnStartCoordinate >= 0) {
                    // Look for gradients
                    columnStartOffset = 0;
                    gradientDist = 0;
                    if (0 <= columnStartCoordinate + leftToRight && columnStartCoordinate + leftToRight < imgWidth) {
                        gradientDist = pixDist[columnStartCoordinate + leftToRight, i] - pixDist[columnStartCoordinate, i];
                        columnStartOffset += leftToRight;
                        while (0 <= columnStartCoordinate + columnStartOffset + leftToRight && columnStartCoordinate + columnStartOffset + leftToRight < imgWidth
                                                                                            && Math.Abs(pixDist[columnStartCoordinate + columnStartOffset + leftToRight, i] -
                                                                                                        pixDist[columnStartCoordinate + columnStartOffset, i] - gradientDist) <=
                                                                                            0.001) {
                            columnStartOffset += leftToRight;
                        }
                    }

                    int columnEndCoordinate = columnStartCoordinate + columnStartOffset;
                    // First handle the case if columnStartCoordinate = columnEndCoordinate
                    // I belive this is being handled in the below case by simply setting gradientDist = 0

                    // Otherwise:
                    // Want to optimize gradientDist. We can control startPoint, but it should be between 55 and 65 away from columnStartCoordinate*OSUPX_BETWEEN_ROWS to avoid interfering
                    // 1. How close can we get to getting the actual slope? We want the slope to be gradientDist/OSUPX_BETWEEN_ROWS
                    // Suppose OSUPX_BETWEEN_ROWS*(columnStartCoordinate, i)+(OSUPX_BETWEEN_ROWS/2, OSUPX_BETWEEN_ROWS/2) is at (0,0). We need a radius of 55 around every sample point to avoid interfering. Given an x coordinate, the list of valid y coordinates are those such that
                    // need y such that x^2+y^2>55^2 but also (x+OSUPX_BETWEEN_ROWS)^2+y^2>55^2. To fix some problems on the edges we limit ourselves such that x>-OSUPX_BETWEEN_ROWS/2+55 and y>-OSUPX_BETWEEN_ROWS/2+55, and arbitrarily we choose y<0 since the region is symmetric.
                    // x>0 or x<0 depends on leftToRight.

                    // A "best rational approximation" algorithm is not very functional here because the denominator needs to be in a specific range,
                    // and there's no guarantee that the best rational approximation will be a factor of a number in the range. Instead, we impose a stronger restriction, and just say that
                    // the slope's denominator is going to have a fixed size. The starting x coordinate will be at ((-55)+(-OSUPX_BETWEEN_ROWS/2+55))/2 = -OSUPX_BETWEEN_ROWS/4 relative to the sample point (i, columnStartCoordinate),
                    // and putting the sample point (i, columnEndCoordinate) at (0,0), the ending x coordinate will be at ((55)+(OSUPX_BETWEEN_ROWS/2-55))/2 = OSUPX_BETWEEN_ROWS/4.
                    // This means that the x-length of the slider segment is columnStartOffset*OSUPX_BETWEEN_ROWS+OSUPX_BETWEEN_ROWS/2 = (columnStartOffset+1/2)*OSUPX_BETWEEN_ROWS.

                    // Therefore the height is given by round(gradientDist/OSUPX_BETWEEN_ROWS * (columnStartOffset*OSUPX_BETWEEN_ROWS + OSUPX_BETWEEN_ROWS/2)) = round(gradientDist*(columnStartOffset+1/2))

                    // We get the starting location by calculating a linear regression with fixed slope. At x=OSUPX_BETWEEN_ROWS*(columnStartOffset+j), we want y=pixDist[columnStartCoordinate+j, i]*objectRadius, for all j in [0, columnStartOffset]\cap Z.
                    // Using https://www.mathworks.com/matlabcentral/answers/67434-how-can-i-do-a-linear-fit-with-forced-slope, we get the y-intercept as:
                    // mean([pixDist[columnStartCoordinate+j, i]*objectRadius - round(gradientDist*(columnStartOffset+1/2))/((columnStartOffset+1/2)*OSUPX_BETWEEN_ROWS)*j*OSUPX_BETWEEN_ROWS for j in range(0, columnStartOffset+1)])
                    // Writing that a bit more succinctly,
                    // mean([pixDist[columnStartCoordinate+j, i]*objectRadius - round(gradientDist*(columnStartOffset+1/2))/(columnStartOffset+1/2)*j for j in range(0, columnStartOffset+1)])
                    // In fact, we can simplify this further by separating the two terms.
                    // mean([pixDist[columnStartCoordinate+j, i]*objectRadius for j in range(0, columnStartOffset+1)]) = objectRadius*mean([pixDist[columnStartCoordinate, i] + gradientDist*j for j in range(0, columnStartOffset+1)]) = objectRadius*(pixDist[columnStartCoordinate, i] + gradientDist*(columnStartOffset+1)/2)
                    // mean([round(gradientDist*(columnStartOffset+1/2))/(columnStartOffset+1/2)*j for j in range(0, columnStartOffset+1)]) = round(gradientDist*(columnStartOffset+1/2))/(columnStartOffset+1/2)*(columnStartOffset+1)/2
                    // Therefore we have the y-intercept as objectRadius*(pixDist[columnStartCoordinate, i] + gradientDist*(columnStartOffset+1)/2)-round(gradientDist*(columnStartOffset+1/2))/(columnStartOffset+1/2)*(columnStartOffset+1)/2

                    // Actually, the vertical distance to the color we want (in units of the object radius) is affected by the slope we use. In particular, the slope scales the distance by a factor of sqrt(1+m^2) where m is the slope we use.
                    // This means that the slope we should use is the one such that mx+c=sqrt(1+m^2)(nx+b). This has a solution m=n/sqrt(1-n^2) when |n|<1. We set flatSlope = n and segmentSlope = m.
                    // Rewriting the above calculation of the y intercept, we get:
                    // mean([Math.Pow(1+segmentSlope*segmentSlope, 0.5)*pixDist[columnStartCoordinate+j, i]*objectRadius - segmentSlope*j*OSUPX_BETWEEN_ROWS for j in range(0, columnStartOffset+1)])
                    // which is simplified to:
                    // Math.Pow(1+segmentSlope*segmentSlope, 0.5)*objectRadius*(pixDist[columnStartCoordinate, i] + gradientDist*(columnStartOffset+1)/2)-round(gradientDist*(columnStartOffset+1/2))/(columnStartOffset+1/2)*(columnStartOffset+1)/2
                    double flatSlope = Math.Round(gradientDist * (columnStartOffset + 0.5)) / ((columnStartOffset + 0.5) * OsupxBetweenRows);
                    double segmentSlope;
                    if (flatSlope == 0) {
                        segmentSlope = 0;
                    } else {
                        segmentSlope = flatSlope / Math.Pow(1 - flatSlope * flatSlope, 0.5); // This works because flatSlope <= 1/OSUPX_BETWEEN_ROWS << 1
                    }

                    int relativeStartX = -leftToRight * OsupxBetweenRows / 4;
                    int relativeStartY = (int) (segmentSlope * relativeStartX +
                                                Math.Pow(1 + segmentSlope * segmentSlope, 0.5) * objectRadius *
                                                (pixDist[columnStartCoordinate, i] + gradientDist * (columnStartOffset + 1) / 2) -
                                                segmentSlope * OsupxBetweenRows * (columnStartOffset + 1) / 2);
                    absoluteStartX = (int) (relativeStartX + OsupxBetweenRows * (columnStartCoordinate + 0.5) + imageStartOsuPx.X);
                    absoluteStartY = (int) (relativeStartY + OsupxBetweenRows * (i + 0.5) + imageStartOsuPx.Y);
                    curPath.Add(new Vector2(absoluteStartX, absoluteStartY));
                    curPath.Add(new Vector2(absoluteStartX + (columnStartOffset + leftToRight * 0.5) * OsupxBetweenRows,
                        Math.Round(absoluteStartY + gradientDist * columnStartOffset)));

                    columnStartCoordinate = columnEndCoordinate + leftToRight;
                }

                curPath.Add(new Vector2(absoluteStartX + (columnStartOffset + 0.5) * OsupxBetweenRows, absoluteStartY + gradientDist * columnStartOffset + OsupxBetweenRows));
                if (leftToRight == 1) {
                    sliderPaths.Add(curPath.Copy());
                    curPath.Clear();
                }
            }

            sliderPaths.Add(curPath.Copy());

            // The first element of sliderPaths is going to be the longest, probably by a lot
            // We need the smallest length such that the duration exceeds the total length divided by the framedist
            // so that we don't have to worry about running out of duration and needing to draw more image
            var totalPath = new List<Vector2>();
            foreach (List<Vector2> path in sliderPaths) {
                totalPath.AddRange(path);
            }

            if (duration == 0) {
                return (totalPath, 0);
            }

            double totalDist = OsuStableDistance(totalPath);
            double frameDist = 0;
            const int pathIdx = 0;
            while (duration * frameDist < totalDist) {
                if (frameDist > 0) {
                    sliderPath.RemoveRange(sliderPath.Count - 2, 2);
                }

                sliderPath.AddRange(sliderPaths[pathIdx]);
                sliderPath.AddRange(sliderPaths[0]);
                sliderPath.Add(msLastSegStart[1]);
                sliderPath.Add(sbPositions[1]);

                frameDist = OsuStableDistance(sliderPath) - Snaptol / 2d;
            }

            double curMsDist = 0;
            double correction = 0;
            double availableDist;
            int totRepeats;
            int curMs = 2;
            var curMsPath = new List<Vector2> { sliderPath.Last() };
            // v is just a Vector2 that I can use to briefly store vector differences
            Vector2 v;
            for (int i = 1; i < sliderPaths.Count && curMs < duration; i++) {
                // Add sliderball path while we can make use of the image to get distance
                curPath.Clear();
                curPath.Add(curMsDist > 0 ? sliderPaths[i - 1].Last() : sliderPath.Last());

                curPath.AddRange(sliderPaths[i]);
                double curPathDist = OsuStableDistance(curPath);
                v = curPath.Last() - msLastSegStart[curMs];
                if (curMsDist + curPathDist + Math.Abs(v.X) + Math.Abs(v.Y) + Snaptol > frameDist) {
                    // We can't go directly from curMsPath.Last() to msLastSegStart[curMs] because this could run over sample points.
                    // Instead we use an additional segment to avoid any sample points between the two anchors.
                    v = curMsPath.Last() - msLastSegStart[curMs];
                    curMsDist += Math.Abs(v.X) + Math.Abs(v.Y);
                    // We have that curMsPath.Last() is -OSUPX_BETWEEN_ROWS/4 units away from a sample point, so we can
                    // go straight up without intersecting any sample points. Because msLastSegStart is (assumed to be)
                    // in the upper left quadrant of the box centered around the top left sample point, we can go up to its
                    // y coordinate and then over to its x coordinate.
                    curMsPath.Add(new Vector2(curMsPath.Last().X, msLastSegStart[curMs].Y));
                    curMsPath.Add(msLastSegStart[curMs]);
                    // We know we will be adding this at the end, need it now for calculation
                    curMsDist += Snaptol;
                    // TODO: We could get more distance per segment than this but I can't be bothered
                    availableDist = 2 * (bottomRightOsuPxSlider.X - msLastSegStart[curMs].X);
                    totRepeats = (int)Math.Floor((frameDist - curMsDist) / availableDist);
                    for (int j = 0; j < totRepeats; j++) {
                        // We are assuming all slider positions are in the top left quadrant of the box centered on
                        // the top left sample point, so adding segments like this does not interfere with the picture.
                        curMsPath.Add(new Vector2(bottomRightOsuPxSlider.X, msLastSegStart[curMs].Y));
                        curMsPath.Add(msLastSegStart[curMs]);
                        curMsDist += availableDist;
                    }

                    curMsPath.Add(new Vector2(msLastSegStart[curMs].X + Math.Round((frameDist - curMsDist + correction) / 2), msLastSegStart[curMs].Y));
                    curMsPath.Add(msLastSegStart[curMs]);
                    curMsPath.Add(sbPositions[curMs]);
                    curMs++;
                    correction += frameDist - OsuStableDistance(curMsPath);
                    curMsPath.RemoveAt(0);
                    sliderPath.AddRange(curMsPath.Copy());
                    curMsPath.Clear();
                    curMsPath.Add(sliderPath.Last());
                    curMsPath.AddRange(sliderPaths[i]);
                    curMsDist = OsuStableDistance(curMsPath);
                } else {
                    curMsDist += curPathDist;
                    curMsPath.AddRange(sliderPaths[i]);
                }
            }

            // Add sliderball path once (if) you run out of image to get distance
            if (curMs < duration) {
                // First use remainder of image (code duplication)
                v = curMsPath.Last() - msLastSegStart[curMs];
                curMsDist += Math.Abs(v.X) + Math.Abs(v.Y);
                curMsPath.Add(new Vector2(curMsPath.Last().X, msLastSegStart[curMs].Y));
                curMsPath.Add(msLastSegStart[curMs]);
                curMsDist += Snaptol;
                availableDist = 2 * (bottomRightOsuPxSlider.X - msLastSegStart[curMs].X);
                totRepeats = (int) Math.Floor((frameDist - curMsDist) / availableDist);
                for (int j = 0; j < totRepeats; j++) {
                    curMsPath.Add(new Vector2(bottomRightOsuPxSlider.X, msLastSegStart[curMs].Y));
                    curMsPath.Add(msLastSegStart[curMs]);
                    curMsDist += availableDist;
                }

                curMsPath.Add(new Vector2(msLastSegStart[curMs].X + Math.Round((frameDist - curMsDist + correction) / 2), msLastSegStart[curMs].Y));
                curMsPath.Add(msLastSegStart[curMs]);
                curMsPath.Add(sbPositions[curMs]);
                curMs++;
                correction += frameDist - OsuStableDistance(curMsPath);
                curMsPath.RemoveAt(0);
                sliderPath.AddRange(curMsPath.Copy());
                curMsPath.Clear();
                curMsPath.Add(sliderPath.Last());
                // Next just spam segments to get length
                while (curMs < duration) {
                    v = curMsPath[0] - msLastSegStart[curMs];
                    double generousLength = Math.Abs(v.X) + Math.Abs(v.Y);
                    availableDist = 2 * (bottomRightOsuPxSlider.X - msLastSegStart[curMs].X);
                    // Why does generousLength exist? I want to account for the additional length that I know will occur
                    // getting from the endpoint of the previous ms's path to msLastSegStart[curMs]. The additional length over
                    // first moving to msLastSegStart (which is what the below calculation otherwise assumes is happening)
                    // is at most generousLength due to the triangle inequality.
                    for (int j = 0; j < Math.Floor((frameDist - Snaptol - generousLength) / availableDist); j++) {
                        curMsPath.Add(new Vector2(bottomRightOsuPxSlider.X, msLastSegStart[curMs].Y));
                        curMsPath.Add(msLastSegStart[curMs]);
                    }

                    curMsPath.Add(new Vector2(msLastSegStart[curMs].X + Math.Round((frameDist - OsuStableDistance(curMsPath) - Snaptol + correction) / 2),
                        msLastSegStart[curMs].Y));
                    curMsPath.Add(msLastSegStart[curMs]);
                    curMsPath.Add(sbPositions[curMs]);
                    curMs++;
                    correction += frameDist - OsuStableDistance(curMsPath);
                    curMsPath.RemoveAt(0);
                    sliderPath.AddRange(curMsPath.Copy());
                    curMsPath.Clear();
                    curMsPath.Add(sliderPath.Last());
                }
            }

            return (sliderPath, frameDist);
        }

        private static double OsuStableDistance(List<Vector2> controlPoints) {
            double length = 0;
            for (int i = 1; i < controlPoints.Count; i++) {
                Vector2 lp = controlPoints.ElementAt(i - 1);
                Vector2 cp = controlPoints.ElementAt(i);
                float num1 = (float) Math.Round(lp.X) - (float) Math.Round(cp.X);
                float num2 = (float) Math.Round(lp.Y) - (float) Math.Round(cp.Y);
                float num3 = num1 * num1 + num2 * num2;

                length += (float) Math.Sqrt(num3);
            }

            return length;
        }
    }
}