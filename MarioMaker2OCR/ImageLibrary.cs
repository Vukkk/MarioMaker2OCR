﻿using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Drawing;
using System.Linq;

namespace MarioMaker2OCR
{
    public static class ImageLibrary
    {
        /// <summary>
        /// <para>Grayscale, increase gamma, binarization & size of the image.</para>
        /// <para>https://github.com/tesseract-ocr/tesseract/wiki/ImproveQuality</para>
        /// 
        /// <para>Optimal pixel height, depends on font & tess version but somewhere between 30px - 70px</para>
        /// <para>https://groups.google.com/forum/#!msg/tesseract-ocr/Wdh_JJwnw94/24JHDYQbBQAJ</para>
        /// </summary>
        public static Image<Gray, byte> PrepareImageForOCR(Image<Bgr, byte> image)
        {
            double scale = (image.Data.GetLength(0) < 720) ? 3d : 2d;
            Image<Gray, byte> grayScaleImage = image.Convert<Gray, byte>();
            grayScaleImage = grayScaleImage.Resize(scale, Inter.Cubic);
            grayScaleImage._GammaCorrect(3.5d);

            // Invert image, then use an inverse binary threshold - this increases font weight and gives better OCR result
            grayScaleImage._Not();
            grayScaleImage._ThresholdBinaryInv(new Gray(180), new Gray(255));
            
            return grayScaleImage;
        }

        /// <summary>
        /// Compare two images - return the percentage the images match
        /// </summary>
        public static double CompareImages(Image<Bgr, byte> firstImage, Mat secondImage)
        {
            using (Image<Bgr, byte> diffImage = new Image<Bgr, byte>(firstImage.Size))
            {
                // OpenCV method to produce an image which is the difference of the 2.
                CvInvoke.AbsDiff(firstImage, secondImage, diffImage);

                // Threshold to filter out pixels that are basically a match.
                // Count remaining black pixels. 
                var nonZeroPixels = diffImage.ThresholdBinary(new Bgr(20, 20, 20), new Bgr(255d, 255d, 255d)).CountNonzero().Average();

                // Divide by total pixels in resolution for total percentage images match
                return 1 - (nonZeroPixels / (firstImage.Height * firstImage.Width));
            }
        }

        public static Rectangle ChangeSize(Rectangle rectangle, Size oldResolution, Size newResolution)
        {
            if (oldResolution.Equals(newResolution)) return rectangle;

            float widthDifference = (newResolution.Width) / (float)oldResolution.Width;
            float heightDifference = (newResolution.Height) / (float)oldResolution.Height;

            Rectangle newRectangle = new Rectangle(rectangle.Location, rectangle.Size);
            newRectangle.Height = (int)(newRectangle.Height * heightDifference);
            newRectangle.Y = (int)(newRectangle.Y * heightDifference);

            newRectangle.Width = (int)(newRectangle.Width * widthDifference);
            newRectangle.X = (int)(newRectangle.X*widthDifference);

            return newRectangle;
        }

        public static Mat ChangeSize(Mat image, Size oldResolution, Size newResolution)
        {
            if (oldResolution.Equals(newResolution)) return image;

            double widthDifference = (newResolution.Width) / (double)oldResolution.Width;
            double heightDifference = (newResolution.Height) / (double)oldResolution.Height;

            Mat newImage = new Mat();
            CvInvoke.Resize(image, newImage, new Size(), widthDifference, heightDifference);

            return newImage;
        }

        /// <summary>
        /// Scans the provided frame for the template.
        /// </summary>
        /// <param name="frame">Grayscale Image that potentially contains the template</param>
        /// <param name="template">Grayscale template to be scanned for</param>
        /// <param name="threshold">Minimum match percent required to match (0.0-1.0)</param>
        /// <returns>The Top-Left point of the location the template matched, or null of no acceptable match was found.</returns>
        public static Point? IsTemplatePresent(Image<Gray, byte> frame, Image<Gray, byte> template, double threshold=0.9)
        {
            Image<Gray, float> match = frame.MatchTemplate(template, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed);
            match.MinMax(out _, out double[] max, out _, out Point[] maxLoc);
            if (max[0] < threshold) return null;
            return maxLoc[0];
        }

    }
}
