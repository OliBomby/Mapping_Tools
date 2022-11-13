using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.Tools.SlideratorStuff
{
    public static class SliderPicturator
    {

        private const double LIGHTEN_AMOUNT = 0.25;
        private const double DARKEN_AMOUNT = 0.1;
        private const byte ALPHA = 180;
        public static int SNAPTOL => (int)Math.Pow(2, 5) * 3;
        private static Color getOpaqueColor(Color top, Color bottom)
        {
            double GAMMA = 1;
            double topOpacity = top.A / 255.0;
            double bottomOpacity = bottom.A / 255.0;
            double totOpacity = topOpacity + bottomOpacity * (1 - topOpacity);
            return Color.FromArgb(255,
                (byte)Math.Round(Math.Pow((Math.Pow(bottom.R, GAMMA) * bottomOpacity * (1 - topOpacity) + Math.Pow(top.R, GAMMA) * topOpacity) / totOpacity, 1 / GAMMA)),
                (byte)Math.Round(Math.Pow((Math.Pow(bottom.G, GAMMA) * bottomOpacity * (1 - topOpacity) + Math.Pow(top.G, GAMMA) * topOpacity) / totOpacity, 1 / GAMMA)),
                (byte)Math.Round(Math.Pow((Math.Pow(bottom.B, GAMMA) * bottomOpacity * (1 - topOpacity) + Math.Pow(top.B, GAMMA) * topOpacity) / totOpacity, 1 / GAMMA)));
        }
        public static Bitmap Recolor(Bitmap img, Color sliderColor, Color sliderBorder, Color backgroundColor, bool BLACK_OFF = false, bool BORDER_OFF = false, bool OPAQUE_OFF = false, bool R = true, bool G = true, bool B = true)
        {
            Color innerColor = Color.FromArgb(ALPHA,
                (byte)Math.Min(255, sliderColor.R * (1 + 0.5 * LIGHTEN_AMOUNT) + 255 * LIGHTEN_AMOUNT),
                (byte)Math.Min(255, sliderColor.G * (1 + 0.5 * LIGHTEN_AMOUNT) + 255 * LIGHTEN_AMOUNT),
                (byte)Math.Min(255, sliderColor.B * (1 + 0.5 * LIGHTEN_AMOUNT) + 255 * LIGHTEN_AMOUNT));
            Color outerColor = Color.FromArgb(ALPHA,
                (byte)Math.Min(255, sliderColor.R / (1 + DARKEN_AMOUNT)),
                (byte)Math.Min(255, sliderColor.G / (1 + DARKEN_AMOUNT)),
                (byte)Math.Min(255, sliderColor.B / (1 + DARKEN_AMOUNT)));

            Color opaqueIC = getOpaqueColor(innerColor, backgroundColor);
            Color opaqueOC = getOpaqueColor(outerColor, backgroundColor);

            Vector3 projVec = new Vector3(opaqueIC.R - opaqueOC.R, opaqueIC.G - opaqueOC.G, opaqueIC.B - opaqueOC.B);
            double projVecLen = projVec.Length;
            Vector3 opaqueOCVec = new Vector3(opaqueOC.R, opaqueOC.G, opaqueOC.B);
            Vector3 opaqueICVec = new Vector3(opaqueIC.R, opaqueIC.G, opaqueIC.B);
            Vector3 sBColVec = new Vector3(sliderBorder.R, sliderBorder.G, sliderBorder.B);

            Color pixel;
            Vector3 colorVec, proj, closestGradientVec, usedColor;
            double gradientDist, borderDist, blackDist;
            Bitmap ret = (Bitmap)img.Clone();
            for (int i = 0; i < img.Width; i++) {
                for (int j = 0; j < img.Height; j++) {
                    pixel = img.GetPixel(i, j);
                    colorVec = new Vector3(R ? pixel.R : 0, G ? pixel.G : 0, B ? pixel.B : 0);
                    proj = Vector3.Dot(colorVec - opaqueOCVec, projVec) / Vector3.Dot(projVec, projVec) * projVec + opaqueOCVec;
                    if (proj.X < opaqueOCVec.X) {
                        closestGradientVec = opaqueOCVec;
                    }
                    else if (proj.X > opaqueICVec.X) {
                        closestGradientVec = opaqueICVec;
                    }
                    else {
                        closestGradientVec = proj;
                    }
                    gradientDist = (colorVec - closestGradientVec).LengthSquared;
                    borderDist = (colorVec - sBColVec).LengthSquared;
                    blackDist = colorVec.LengthSquared;
                    // Test if border color would be better
                    if (BORDER_OFF || gradientDist < borderDist) {
                        // Test if black would be better
                        if (!BLACK_OFF && blackDist < gradientDist) {
                            ret.SetPixel(i, j, Color.Black);
                        }
                        else {
                            usedColor = Math.Round(101 * Math.Clamp(1 - (closestGradientVec - opaqueOCVec).Length / projVecLen, 0, 1)) / 128 * projVec + opaqueOCVec;
                            ret.SetPixel(i, j, Color.FromArgb((int)Math.Round(usedColor[0]), (int)Math.Round(usedColor[1]), (int)Math.Round(usedColor[2])));
                        }
                    }
                    else {
                        // Test if black would be better
                        if (!BLACK_OFF && blackDist < borderDist) {
                            ret.SetPixel(i, j, Color.Black);
                        }
                        else {
                            ret.SetPixel(i, j, sliderBorder);
                        }
                    }
                }
            }
            return ret;
        }

        // TODO: use color opacity per pixel, pass in background color, calculate opaqueIC and opaqueOC here
        public static List<Vector2> Picturate(Bitmap img, Color sliderColor, Color sliderBorder, Color backgroundColor, double circleSize, Vector2 startPos, Vector2 startPosPic, double resY = 1080, long GPU = 16384, bool BLACK_OFF = false, bool BORDER_OFF = false, bool OPAQUE_OFF = false, bool R = true, bool G = true, bool B = true)
        {
            Color innerColor = Color.FromArgb(ALPHA,
                (byte) Math.Min(255, sliderColor.R * (1 + 0.5 * LIGHTEN_AMOUNT) + 255 * LIGHTEN_AMOUNT),
                (byte) Math.Min(255, sliderColor.G * (1 + 0.5 * LIGHTEN_AMOUNT) + 255 * LIGHTEN_AMOUNT),
                (byte) Math.Min(255, sliderColor.B * (1 + 0.5 * LIGHTEN_AMOUNT) + 255 * LIGHTEN_AMOUNT));
            Color outerColor = Color.FromArgb(ALPHA,
                (byte) Math.Min(255, sliderColor.R / (1 + DARKEN_AMOUNT)),
                (byte) Math.Min(255, sliderColor.G / (1 + DARKEN_AMOUNT)),
                (byte) Math.Min(255, sliderColor.B / (1 + DARKEN_AMOUNT)));

            Color opaqueIC = getOpaqueColor(innerColor, backgroundColor);
            Color opaqueOC = getOpaqueColor(outerColor, backgroundColor);

            // startPos, startPosPic are in osupx
            startPos.Round();
            startPosPic.Round();
            const int OSUPX_BETWEEN_ROWS = 240;
            double objectRadius = 1.00041 * (54.4 - 4.48 * circleSize);

            Vector3 projVec = new Vector3(opaqueIC.R - opaqueOC.R, opaqueIC.G - opaqueOC.G, opaqueIC.B - opaqueOC.B);
            double projVecLen = projVec.Length;
            Vector3 opaqueOCVec = new Vector3(opaqueOC.R, opaqueOC.G, opaqueOC.B);
            Vector3 opaqueICVec = new Vector3(opaqueIC.R, opaqueIC.G, opaqueIC.B);
            Vector3 sBColVec = new Vector3(sliderBorder.R, sliderBorder.G, sliderBorder.B);

            double[,] pixDist = new double[img.Width, img.Height];
            Color pixel;
            Vector3 colorVec, proj, closestGradientVec;
            double gradientDist, borderDist, blackDist;
            for (int i = 0; i < pixDist.GetLength(0); i++) {
                for (int j = 0; j < pixDist.GetLength(1); j++) {
                    pixel = img.GetPixel(i, j);
                    colorVec = new Vector3(R ? pixel.R : 0, G ? pixel.G : 0, B ? pixel.B : 0);
                    proj = Vector3.Dot(colorVec - opaqueOCVec, projVec) / Vector3.Dot(projVec, projVec) * projVec + opaqueOCVec;
                    if (proj.X < opaqueOCVec.X) {
                        closestGradientVec = opaqueOCVec;
                    }
                    else if (proj.X > opaqueICVec.X) {
                        closestGradientVec = opaqueICVec;
                    }
                    else {
                        closestGradientVec = proj;
                    }
                    gradientDist = (colorVec - closestGradientVec).LengthSquared;
                    borderDist = (colorVec - sBColVec).LengthSquared;
                    blackDist = colorVec.LengthSquared;
                    // Test if border color would be better
                    if (BORDER_OFF || gradientDist < borderDist) {
                        // Test if black would be better
                        if (!BLACK_OFF && blackDist < gradientDist) {
                            pixDist[i, j] = 1;
                        }
                        else {
                            pixDist[i, j] = Math.Round(101 * Math.Clamp(1 - (closestGradientVec - opaqueOCVec).Length / projVecLen, 0, 1)) / 128;
                        }
                    }
                    else {
                        // Test if black would be better
                        if (!BLACK_OFF && blackDist < borderDist) {
                            pixDist[i, j] = 1;
                        }
                        else {
                            pixDist[i, j] = 111.0 / 128;
                        }
                    }

                }
            }

            Vector2 topLeftOsuPxImage = new Vector2(-64, -72); // (16+20n, 8+20m) for matching editor to gameplay. Further than cs0 slider placed at (0,0)'s bounding box so we should be set.
            Vector2 startSliderCoordinate = startPos;
            // For now we will ignore the fact that this may interfere with the sample points
            Vector2 topLeftOsuPxSlider = new Vector2(Math.Ceiling(objectRadius * 1.15)) + topLeftOsuPxImage;
            Vector2 bottomRightOsuPxSlider = new Vector2(Math.Floor(OSUPX_BETWEEN_ROWS * GPU - 1.15 * objectRadius)) + topLeftOsuPxImage;
            // To get screenpx from osupx topLeftOsuPxImage to osupx startPosPic we do the following:
            // the game window is 480 osupx tall and resY-16 screenpx tall, so the ratio is (resY-16)/480 screenpx per osupx.
            startPosPic -= topLeftOsuPxImage;
            startPosPic *= (resY - 16) / 480;
            startPosPic.Round();
            Vector2 imageStartOsuPx = topLeftOsuPxImage + OSUPX_BETWEEN_ROWS * startPosPic;
            int columnStartCoordinate, columnEndCoordinate, columnStartOffset, relativeStartX, relativeStartY, absoluteStartX, absoluteStartY;
            int leftToRight = -1;
            double segmentSlope;
            // In the below loop, gradientDist means something completely different from what it means in the above loop. Here, it is being used to mean the distance in the gradient between two or more points that are evenly distributed along the slider body
            List<Vector2> sliderPath = new List<Vector2>();
            sliderPath.Add(startSliderCoordinate);
            sliderPath.Add(new Vector2(startSliderCoordinate.X, topLeftOsuPxSlider.Y));
            sliderPath.Add(new Vector2(bottomRightOsuPxSlider.X, topLeftOsuPxSlider.Y));
            sliderPath.Add(bottomRightOsuPxSlider);
            sliderPath.Add(new Vector2(bottomRightOsuPxSlider.X, topLeftOsuPxSlider.Y));
            sliderPath.Add(topLeftOsuPxSlider);
            // Move to the start of the image, avoiding sample points (could be done better)
            if (startPosPic.LengthSquared > 0) {
                sliderPath.Add(new Vector2(topLeftOsuPxSlider.X, imageStartOsuPx.Y));
                sliderPath.Add(imageStartOsuPx);
            }
            absoluteStartX = 0;
            columnStartOffset = 0;
            absoluteStartY = 0;
            gradientDist = 0;
            for (int i = 0; i < img.Height; i++) {
                leftToRight = -leftToRight;
                columnStartCoordinate = (leftToRight == 1) ? 0 : (img.Width - 1);
                columnEndCoordinate = columnStartCoordinate;
                while ((leftToRight == 1) ? (columnStartCoordinate < img.Width) : (columnStartCoordinate >= 0)) {
                    // Look for gradients
                    columnStartOffset = 0;
                    gradientDist = 0;
                    if (0 <= columnStartCoordinate + leftToRight && columnStartCoordinate + leftToRight < img.Width) {
                        gradientDist = pixDist[columnStartCoordinate + leftToRight, i] - pixDist[columnStartCoordinate, i];
                        columnStartOffset += leftToRight;
                        while (0 <= columnStartCoordinate + columnStartOffset + leftToRight && columnStartCoordinate + columnStartOffset + leftToRight < img.Width
                            && pixDist[columnStartCoordinate + columnStartOffset + leftToRight, i] - pixDist[columnStartCoordinate + columnStartOffset, i] == gradientDist) {
                            columnStartOffset += leftToRight;
                        }
                    }
                    columnEndCoordinate = columnStartCoordinate + columnStartOffset;
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
                    double flatSlope = Math.Round(gradientDist * (columnStartOffset + 0.5)) / ((columnStartOffset + 0.5) * OSUPX_BETWEEN_ROWS);
                    if (flatSlope == 0) {
                        segmentSlope = 0;
                    }
                    else {
                        segmentSlope = flatSlope / Math.Pow(1 - flatSlope * flatSlope, 0.5); // This works because flatSlope <= 1/OSUPX_BETWEEN_ROWS << 1
                    }

                    relativeStartX = -leftToRight * OSUPX_BETWEEN_ROWS / 4; // This only works because OSUPX_BETWEEN_ROWS is a multiple of 4
                    relativeStartY = (int)(segmentSlope * relativeStartX + Math.Pow(1 + segmentSlope * segmentSlope, 0.5) * objectRadius * (pixDist[columnStartCoordinate, i] + gradientDist * (columnStartOffset + 1) / 2) - segmentSlope * OSUPX_BETWEEN_ROWS * (columnStartOffset + 1) / 2);
                    absoluteStartX = (int)(relativeStartX + OSUPX_BETWEEN_ROWS * (columnStartCoordinate + 0.5) + imageStartOsuPx.X);
                    absoluteStartY = (int)(relativeStartY + OSUPX_BETWEEN_ROWS * (i + 0.5) + imageStartOsuPx.Y);
                    sliderPath.Add(new Vector2(absoluteStartX, absoluteStartY));
                    sliderPath.Add(new Vector2(absoluteStartX + (columnStartOffset + leftToRight * 0.5) * OSUPX_BETWEEN_ROWS, Math.Round(absoluteStartY + gradientDist * columnStartOffset)));

                    columnStartCoordinate = columnEndCoordinate + leftToRight;

                }

                sliderPath.Add(new Vector2(absoluteStartX + (columnStartOffset + 0.5) * OSUPX_BETWEEN_ROWS, absoluteStartY + gradientDist * columnStartOffset + OSUPX_BETWEEN_ROWS));
            }



            return sliderPath;
        }
    }


}