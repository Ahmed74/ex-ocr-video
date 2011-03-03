using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;


enum DirectionType
{
    Horizontal = 0,
    Vertical = 1
}

namespace CameraCapture
{
    class CannyEdgeDetector
    {
        public static int width;
        public static int height;
        public static Image<Bgr, Byte> inputImage = null;
        public static Image<Gray, Byte> grayscaleImage = null;
        public static Image<Gray, Byte> smoothGaussianImage = null;
        public static int[,] data;
        public static float[,] horizontalGradient = null;
        public static float[,] verticalGradient;
        //public static Image<Gray, float> gradientImage = null;
        public static float[,] horizontalEdge;
        public static float[,] verticalEdge;

        public static float[,] nonmaxHorizontalGradient;
        public static float[,] nonmaxVerticalGradient;

        public static float lowThresh, highThresh;

        //public static float[,] directionMatrix;
        private static float[,] horizontalMask = new float[3, 3]{
            {-1, 0, 1}, {-2, 0, 2}, {-1, 0, 1}};
        private static float[,] verticalMask = new float[3, 3] {
            {1, 2, 1}, {0, 0, 0}, {-1, -2, -1}};

        // this is the Gaussian mask which correspond to the Gaussian function with sigma = 1.4f;
        public static int size;

        public static void SetInputInfo(Image<Bgr, Byte> source, int s, float low, float high)
        {
            if (source != null)
            {
                width = source.Width;
                height = source.Height;
                inputImage = source;
                size = s;
                lowThresh = low;
                highThresh = high;
                grayscaleImage = inputImage.Convert<Gray, Byte>();
            }

        }

        // perform the Canny Edge Detector
        // 1. smooth the Image with Gausian distribution
        // 2. calculat the gradient in the horizontal/vertical direction
        // 3. non-maximum suppression is used to trace along the edge in the edge direction and suppress any pixel value 
        // 4. perform segmentation with two thresholds (lowThresh, highThresh)

        public static void Canny()
        {
            // smooth the image with the Gaussian Mask, to reduce the noise
            smoothGaussianImage = grayscaleImage.SmoothGaussian(size);
            ReadImage();
            ComputeGradient();
            NonMaximaSuppression();
            PerformHysteresis();
        }

        public static Image<Gray, byte> GetHorizontalEdge()
        {
            Image<Gray,float> temp = DisplayImage(verticalEdge);
            return temp.Convert<Gray, byte>();           
        }

        public static Image<Gray, byte> GetVerticalEdge()
        {
            Image<Gray, float> temp = DisplayImage(horizontalEdge);
            return temp.Convert<Gray, byte>();
        }


        // Compute the gradient in the horizontal and vertical direction by using the Sobel operator
        // and find the magnitude (EDGE STRENGTH) of gradient
        // Besides that, set the direction for each point
        public static void ComputeGradient()
        {
            horizontalGradient = new float[height, width];
            verticalGradient = new float[height, width];

            int i, j;
            float sum1 = 0, sum2=0;
            for(i=1; i<height-1; i++)
                for (j = 1; j < width-1; j++)
                {
                    sum1 = data[i - 1, j - 1] * horizontalMask[0, 0]
                        + data[i - 1, j] * horizontalMask[0, 1]
                        + data[i - 1, j + 1] * horizontalMask[0, 2]

                        + data[i, j - 1] * horizontalMask[1, 0]
                        + data[i, j] * horizontalMask[1, 1]
                        + data[i, j + 1] * horizontalMask[1, 2]

                        + data[i + 1, j - 1] * horizontalMask[2, 0]
                        + data[i + 1, j] * horizontalMask[2, 1]
                        + data[i + 1, j + 1] * horizontalMask[2, 2];

                    sum2 = data[i - 1, j - 1] * verticalMask[0, 0]
                        + data[i - 1, j] * verticalMask[0, 1]
                        + data[i - 1, j + 1] * verticalMask[0, 2]

                        + data[i, j - 1] * verticalMask[1, 0]
                        + data[i, j] * verticalMask[1, 1]
                        + data[i, j + 1] * verticalMask[1, 2]

                        + data[i + 1, j - 1] * verticalMask[2, 0]
                        + data[i + 1, j] * verticalMask[2, 1]
                        + data[i + 1, j + 1] * verticalMask[2, 2];

                    horizontalGradient[i, j] = sum1;
                    verticalGradient[i, j] = sum2; 
                }            
        }

        
        public static Image<Gray,float> DisplayImage(float [,]data)
        {
            float[, ,] temp = new float[height, width, 1];
            int i, j;
            for(i=0; i<height; i++)
                for (j = 0; j < width; j++)
                {
                    temp[i, j, 0] = data[i, j];
                }
            return new Image<Gray, float>(temp);
        }

        // get data from smoothGaussianImage
        private static void ReadImage()
        {
            int i, j;
            data = new int[height, width];  //[Row,Column]
            // convert smoothGaussianImage into Bitmap
            Bitmap image = smoothGaussianImage.ToBitmap();
            BitmapData bitmapData1 = image.LockBits(new Rectangle(0, 0, width, height),
                                     ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* imagePointer1 = (byte*)bitmapData1.Scan0;

                for (i = 0; i < height; i++)
                {
                    for (j = 0; j < width; j++)
                    {
                        data[i, j] = (int)((imagePointer1[0] + imagePointer1[1] + imagePointer1[2]) / 3.0);
                        //4 bytes per pixel
                        imagePointer1 += 4;
                    }//end for j
                    //4 bytes per pixel
                    imagePointer1 += bitmapData1.Stride - (bitmapData1.Width * 4);
                }//end for i
            }//end unsafe
            image.UnlockBits(bitmapData1);

        }


        public static void NonMaximaSuppression()
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

            
            
            int Limit = size / 2;            
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

        
        
        public static void PerformHysteresis()
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
        public static void PerformHystersisHorizontal()
        {
            for (int x = 0; x < height; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    if (horizontalEdge[x, y] == 0 && nonmaxHorizontalGradient[x, y] >= highThresh)
                    {
                        followHorizontal(x, y, lowThresh);
                    }
                }
            }
        }

        // perform hysteris in the vertical direction
        public static void PerformHystersisVertical()
        {
            for (int x = 0; x < height; x++)
            {
                for (int y = 0; y < width; y++)
                {
                    if (verticalEdge[x, y] == 0 && nonmaxVerticalGradient[x, y] >= highThresh)
                    {
                        followVertical(x, y, lowThresh);
                    }
                }
            }
        }


        private static void followHorizontal(int x1, int y1, float threshold)
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
                        followHorizontal(x, y, threshold);
                    }
                }
            }
        }

        private static void followVertical(int x1, int y1, float threshold)
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
                        followVertical(x, y, threshold);
                    }
                }
            }
        }
    }
}
