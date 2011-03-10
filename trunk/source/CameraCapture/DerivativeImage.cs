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
    public class DerivativeImage
    {
        private float[,] gradX;

        public float[,] GradX
        {
            get { return gradX; }
            set { gradX = value; }
        }
        private float[,] gradY;

        public float[,] GradY
        {
            get { return gradY; }
            set { gradY = value; }
        }
        private float[,] grad;

        public float[,] Grad
        {
            get { return grad; }
            set { grad = value; }
        }
        private float[,] gradientOrientation;

        public float[,] GradientOrientation
        {
            get { return gradientOrientation; }
            set { gradientOrientation = value; }
        }

        public void ComputeDerivative(Image<Gray, byte> img, int[,] kernelX, int[,] kernelY)
        {
            int height, width;
            height = img.Height; width = img.Width;

            grad = new float[height, width];
            gradX = new float[height, width];
            gradY = new float[height, width];
            gradientOrientation = new float[height, width];

            int[,] Data = Utilities.ConvertImageToArray2D(img);

            gradX = Differentiate(Data, kernelX);
            gradY = Differentiate(Data, kernelY);

            //Compute the gradient magnitude based on derivatives in x and y:
            int i, j;
            for (i = 0; i < height; i++)
            {
                for (j = 0; j < width; j++)
                {
                    grad[i, j] = (float)Math.Sqrt((gradX[i, j] * gradX[i, j]) + (gradY[i, j] * gradY[i, j]));
                    gradientOrientation[i, j] = (float)(Math.Atan(gradY[i, j] / gradX[i, j]) * 180 / Math.PI); //rad to degree
                }
            }
        }

        public float[,] Differentiate (int[,] Data, int[,] Filter)
        {
            int i, j, k, l, Fh, Fw;
            int Height, Width;
            Height = Data.GetLength(0);  Width = Data.GetLength(1);


            Fw = Filter.GetLength(0);
            Fh = Filter.GetLength(1);
            float sum = 0;
            float[,] Output = new float[ Height, Width];

            for (i = Fh / 2; i < (Height - Fh / 2) ; i++)
            
            {
                for (j = Fw / 2; j < (Width - Fw / 2); j++)    
                {
                    sum = 0;
                    for (k = -Fh / 2; k <= Fh / 2; k++)
                    {
                        for (l = -Fw / 2; l <= Fw / 2; l++)
                        {
                            sum = sum + Data[i + k, j + l] * Filter[Fh / 2 + k, Fw / 2 + l];
                        }
                    }
                    Output[i, j] = sum;

                }

            }
            return Output;
        }

        public float [] HistogramOfGradientDirection()
        {
            int [] histogram = new int[8];
            int height, width;
            height = gradientOrientation.GetLength(0); width = gradientOrientation.GetLength(1);
            for(int i=0; i < height; i++)
                for (int j = 0; j < width; j++)
                {
                    if ((-22.5 <= gradientOrientation[i, j]) && (gradientOrientation[i, j] < 22.5))
                        histogram[0]++;
                    if((22.5 <= gradientOrientation[i, j]) && (gradientOrientation[i, j]< 67.5))
                        histogram[1]++;
                    if((67.5<=gradientOrientation[i, j])&&(gradientOrientation[i, j]< 112.5))
                        histogram[2]++;
                    if((112.5<=gradientOrientation[i, j])&&(gradientOrientation[i, j] <157.5))
                        histogram[3]++;
                    if(((157.5<=gradientOrientation[i, j])&&(gradientOrientation[i, j]<180)) ||
                        ((-180 <= gradientOrientation[i, j])&&(gradientOrientation[i, j] < -157.5)))
                        histogram[4]++;
                    if((-157.5 <=gradientOrientation[i, j])&& (gradientOrientation[i, j]<-112.5))
                        histogram[5]++;
                    if((-112.5<=gradientOrientation[i, j])&&(gradientOrientation[i, j] < -67.5))
                        histogram [6]++;
                    if((-67.5<=gradientOrientation[i, j])&&(gradientOrientation[i, j]<-22.5))
                        histogram[7]++;

                }
            float []normalizeHistogram = new float[8];
            for(int i=0; i<8; i++)
                normalizeHistogram[i] = histogram[i]*1.0F/(height*width);
            return normalizeHistogram;
        }

    }

}
