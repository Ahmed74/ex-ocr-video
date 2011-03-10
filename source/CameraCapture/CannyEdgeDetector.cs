using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

namespace CameraCapture
{
    class CannyEdgeDetector
    {
        private int width;
        private int height;

        private int[,] data;

        public int[,] Data
        {
            get { return data; }
        }
        private float[,] horizontalGradient;
        private float[,] verticalGradient;

        
        private float[,] nonmaxHorizontalGradient;
        private float[,] nonmaxVerticalGradient;

        private float[,] horizontalEdge;

        public float[,] HorizontalEdge
        {
            get { return horizontalEdge; }
        }
        private float[,] verticalEdge;

        public float[,] VerticalEdge
        {
            get { return verticalEdge; }
        }

        private float lowThresh, highThresh;
        private int sizeofGaussianKernel;


        // define two Soble mask
        // the mask to detect Vertical Edges
        private float[,] horizontalMask = new float[3, 3]{
            {-1, 0, 1}, {-2, 0, 2}, {-1, 0, 1}};
        // the mask to detect Horizontal Edge
        private float[,] verticalMask = new float[3, 3] {
            {1, 2, 1}, {0, 0, 0}, {-1, -2, -1}};

        /// <summary>
        /// Perform the Canny edge detector
        /// </summary>
        /// <remarks>
        /// Input: 
        /// - color input image
        /// - Size of Gaussian kernel which support the Smooth Gaussian operation
        /// - lowThreshold: to link the weak edge points to the strong edge points 
        /// - high Threshold: to determine the strong edge points in input image
        /// Describe the main steps in Canny edge detector
        /// 1. smooth the Image with Gausian distribution
        /// 2. calculate the gradient in the horizontal/vertical direction
        /// 3. non-maximum suppression is used to trace along the edge in the edge direction and suppress any pixel value 
        /// 4. perform segmentation with two thresholds (lowThresh, highThresh)
        /// </remarks>
        public void Canny(Image<Bgr, byte> img, int sizeofGaussianKernel, float lowThresh, float highThresh)
        {
            this.lowThresh = lowThresh;
            this.highThresh = highThresh;
            this.sizeofGaussianKernel = sizeofGaussianKernel;
            height = img.Height;
            width = img.Width;

            // convert input image to grayscale input
            Image<Gray, byte> grayscale = img.Convert<Gray, byte>();
            // smooth the image with the Gaussian Mask, to reduce the noise
            Image<Gray,byte> smoothGaussianImage = grayscale.SmoothGaussian(sizeofGaussianKernel);

            // get image data under array 2d
            this.data = Utilities.ConvertImageToArray2D(smoothGaussianImage);

            
            ComputeGradient();
            NonMaximaSuppression();
            PerformHysteresis();
        }

        /// <summary>
        /// Get the horizontal edge image
        /// </summary>
        /// <returns>Grayscale image contain the horizontal edges</returns>
        public Image<Gray, byte> GetHorizontalEdgeImage()
        {
            Image<Gray,float> temp = Utilities.CreateImageFromArray2D(horizontalEdge);
            return temp.Convert<Gray, byte>();           
        }

        /// <summary>
        /// Get the vertical edge image
        /// </summary>
        /// <returns>Grayscale image contains the horizontal edges</returns>
        public Image<Gray, byte> GetVerticalEdgeImage()
        {
            Image<Gray, float> temp = Utilities.CreateImageFromArray2D(verticalEdge);           
            return temp.Convert<Gray, byte>();
        }


        // Compute the gradient in the horizontal and vertical direction by using the Sobel operator
        // and find the magnitude (EDGE STRENGTH) of gradient
        // Besides that, set the direction for each point
        /// <summary>
        /// Compute gradient at each pixel in the vertical/horizontal direction
        /// </summary>
        private void ComputeGradient()
        {
            horizontalGradient = new float[height, width];
            verticalGradient = new float[height, width];

            int i, j;
            float sum1 = 0, sum2=0;
            for(i=1; i<height-1; i++)
                for (j = 1; j < width-1; j++)
                {

                    // to calculate the gradient in the vertical direction
                    sum1 = data[i - 1, j - 1] * horizontalMask[0, 0]
                        + data[i - 1, j] * horizontalMask[0, 1]
                        + data[i - 1, j + 1] * horizontalMask[0, 2]

                        + data[i, j - 1] * horizontalMask[1, 0]
                        + data[i, j] * horizontalMask[1, 1]
                        + data[i, j + 1] * horizontalMask[1, 2]

                        + data[i + 1, j - 1] * horizontalMask[2, 0]
                        + data[i + 1, j] * horizontalMask[2, 1]
                        + data[i + 1, j + 1] * horizontalMask[2, 2];
                    // to calculate the gradient in the horizontal direction
                    sum2 = data[i - 1, j - 1] * verticalMask[0, 0]
                        + data[i - 1, j] * verticalMask[0, 1]
                        + data[i - 1, j + 1] * verticalMask[0, 2]

                        + data[i, j - 1] * verticalMask[1, 0]
                        + data[i, j] * verticalMask[1, 1]
                        + data[i, j + 1] * verticalMask[1, 2]

                        + data[i + 1, j - 1] * verticalMask[2, 0]
                        + data[i + 1, j] * verticalMask[2, 1]
                        + data[i + 1, j + 1] * verticalMask[2, 2];

                    horizontalGradient[i, j] = sum2;
                    verticalGradient[i, j] = sum1; 
                }            
        }
        
        private void NonMaximaSuppression()
        {

            nonmaxHorizontalGradient = new float[height, width];
            nonmaxVerticalGradient = new float[height, width];

            int i, j;
            for(i = 0; i<height; i++)
                for (j = 0; j < width; j++)
                {
                    nonmaxHorizontalGradient[i, j] = Math.Abs(horizontalGradient[i, j]);
                    nonmaxVerticalGradient[i, j] = Math.Abs(verticalGradient[i, j]);
                }

            int Limit = sizeofGaussianKernel / 2;            
            float Tangent;
            
             for (i = Limit; i < height - Limit; i++)
             {
                 for (j = Limit; j < width - Limit; j++)
                 {
                    
                     float gradX, gradY; //, gradient;
                     gradX = horizontalGradient[i, j];
                     gradY = verticalGradient[i, j];
                     //gradient = gradientImage[i, j];
                    
                     if (gradX == 0F)
                         Tangent = 90F;
                     else
                         Tangent = (float)(Math.Atan(gradY/ gradX) * 180 / Math.PI); //rad to degree

                     //Horizontal Edge
                     if (((-22.5 < Tangent) && (Tangent <= 22.5)) || ((157.5 < Tangent) && (Tangent <= -157.5)))
                     {
                         // for horizontal gradient
                         if ((horizontalGradient[i, j]< horizontalGradient[i, j + 1])
                             || (horizontalGradient[i, j]< horizontalGradient[i, j - 1]))
                             nonmaxHorizontalGradient[i, j] = 0f;

                         // for vertical gradient
                         if ((verticalGradient[i, j]< verticalGradient[i, j + 1])
                             || (verticalGradient[i, j]< verticalGradient[i, j - 1]))
                             nonmaxVerticalGradient[i, j] = 0f;
                     }

                     //Vertical Edge
                     if (((-112.5 < Tangent) && (Tangent <= -67.5)) || ((67.5 < Tangent) && (Tangent <= 112.5)))
                     {
                         if ((horizontalGradient[i, j] < horizontalGradient[i + 1, j])
                             || (horizontalGradient[i, j] < horizontalGradient[i - 1, j]))
                             nonmaxHorizontalGradient[i, j] = 0F;

                         if ((verticalGradient[i, j] < verticalGradient[i + 1, j])
                             || (verticalGradient[i, j] < verticalGradient[i - 1, j]))
                             nonmaxVerticalGradient[i, j] = 0f;
                     }

                     //+45 Degree Edge
                     if (((-67.5 < Tangent) && (Tangent <= -22.5)) || ((112.5 < Tangent) && (Tangent <= 157.5)))
                     {
                         if ((horizontalGradient[i, j] < horizontalGradient[i + 1, j - 1])
                             || (horizontalGradient[i, j] < horizontalGradient[i - 1, j + 1]))
                             nonmaxHorizontalGradient[i, j] = 0F;

                         if ((verticalGradient[i, j] < verticalGradient[i + 1, j - 1])
                             || (verticalGradient[i, j] < verticalGradient[i - 1, j + 1]))
                             nonmaxVerticalGradient[i, j] = 0F;
                     }

                     //-45 Degree Edge
                     if (((-157.5 < Tangent) && (Tangent <= -112.5)) || ((67.5 < Tangent) && (Tangent <= 22.5)))
                     {
                         if ((horizontalGradient[i, j] < horizontalGradient[i + 1, j + 1])
                             || (horizontalGradient[i, j] < horizontalGradient[i - 1, j - 1]))
                             nonmaxHorizontalGradient[i, j] = 0F;

                         if ((verticalGradient[i, j]< verticalGradient[i + 1, j + 1])
                             || (verticalGradient[i, j]< verticalGradient[i - 1, j - 1]))
                             nonmaxVerticalGradient[i, j] = 0F;
                     }
                     
                 }
             }
            
        }

        private void PerformHysteresis()
        {
            horizontalEdge = new float[height, width];
            verticalEdge = new float[height, width];
            int i, j;
            for (i = 0; i < height; i++)
                for (j = 0; j < width; j++)
                {
                    horizontalEdge[i, j] = verticalEdge[i, j] = 0;
                }
            PerformHystersisHorizontal();
            PerformHystersisVertical();
        }

        // perform hysteris in the horizontal direction
        private void PerformHystersisHorizontal()
        {
            for (int x = 0; x < height; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    if (horizontalEdge[x, y] == 0 && nonmaxHorizontalGradient[x, y] >= highThresh)
                    {
                        FollowHorizontal(x, y, lowThresh);
                    }
                }
            }
        }

        // perform hysteris in the vertical direction
        private void PerformHystersisVertical()
        {
            for (int x = 0; x < height; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    if (verticalEdge[x, y] == 0 && nonmaxVerticalGradient[x, y] >= highThresh)
                    {
                        FollowVertical(x, y, lowThresh);
                    }
                }
            }
        }

        private void FollowHorizontal(int x1, int y1, float threshold)
        {

            int x0 = x1 == 0 ? x1 : x1 - 1;

            int x2 = x1 == height - 1 ? x1 : x1 + 1;

            int y0 = y1 == 0 ? y1 : y1 - 1;

            int y2 = y1 == width - 1 ? y1 : y1 + 1;

            horizontalEdge[x1, y1] = 255;

            for (int x = x0; x <= x2; x++)
            {
                for (int y = y0; y <= y2; y++)
                {
                    if ((y != y1 || x != x1)
                        && horizontalEdge[x, y]== 0
                        && nonmaxHorizontalGradient[x, y]>= threshold)
                    {
                        FollowHorizontal(x, y, threshold);
                    }
                }
            }
        }

        private void FollowVertical(int x1, int y1, float threshold)
        {

            int x0 = x1 == 0 ? x1 : x1 - 1;

            int x2 = x1 == height - 1 ? x1 : x1 + 1;

            int y0 = y1 == 0 ? y1 : y1 - 1;

            int y2 = y1 == width - 1 ? y1 : y1 + 1;

            verticalEdge[x1, y1] = 255;

            for (int x = x0; x <= x2; x++)
            {

                for (int y = y0; y <= y2; y++)
                {
                    if ((y != y1 || x != x1)
                        && verticalEdge[x, y]== 0
                        && nonmaxVerticalGradient[x, y]>= threshold)
                    {
                        FollowVertical(x, y, threshold);
                    }
                }
            }
        }
        
    }
}
