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
    class ExtractCandicateTextBlock
    {
        public static int[,] verticalStructElement = new int[1,5]{
            {1,1, 1, 1, 1 }};
        public static float[,] horizontalStructElement = new float[7, 3] {
            {1,1,1},
            {1,1,1},
            {1,1,1},
            {1,1,1},
            {1,1,1},
            {1,1,1},
            {1,1,1}};
        public static Image<Gray, byte> horizontalEdgeImage = null;
        public static Image<Gray, byte> verticalEdgeImage = null;



        public static void SetInputInfo(Image<Bgr, byte> source)
        {
            CannyEdgeDetector.SetInputInfo(source, 5, 40f, 100f);
            CannyEdgeDetector.Canny();
            horizontalEdgeImage = CannyEdgeDetector.GetHorizontalEdge();
            verticalEdgeImage = CannyEdgeDetector.GetVerticalEdge();
        }

        public static void DilateVerticalEdgeImage()
        {
            //ConvolutionKernelF kernel = new ConvolutionKernelF(
            //        new Matrix<float>(verticalStructElement), new Point(0, 2));

            int width, height;
            width = CannyEdgeDetector.width;
            height = CannyEdgeDetector.height;
            for(int i=0; i< height; i++)
                for (int j = 2; j < width - 2; j++)
                {
                    float flag = CannyEdgeDetector.horizontalEdge[i, j - 2] * verticalStructElement[0,0]
                        + CannyEdgeDetector.horizontalEdge[i, j - 1] * verticalStructElement[0,1]
                        + CannyEdgeDetector.horizontalEdge[i, j] * verticalStructElement[0,2]
                        + CannyEdgeDetector.horizontalEdge[i, j + 1] * verticalStructElement[0,3]
                        + CannyEdgeDetector.horizontalEdge[i, j + 2] * verticalStructElement[0,4];
                    if (flag != 0)
                        CannyEdgeDetector.horizontalEdge[i, j] = 255;
                }
            verticalEdgeImage = CannyEdgeDetector.GetVerticalEdge();
            //Image<Gray, float> temp = verticalEdgeImage.Convolution(kernel);
            //verticalEdgeImage = temp.Convert<Gray, byte>();


        }

        public static void DilateHorizontalEdgeImage()
        {
            ConvolutionKernelF kernel = new ConvolutionKernelF(
                    new Matrix<float>(horizontalStructElement), new Point(3, 1));
             Image<Gray, float> temp  = horizontalEdgeImage.Convolution(kernel);
             horizontalEdgeImage = temp.Convert<Gray, byte>();
        }


    }
}
