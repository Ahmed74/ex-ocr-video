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
    /// <summary>
    /// Classify candicate textRegion lines into two classes: static textRegion class and dynamic textRegion class
    /// </summary>
    /// <remarks>
    /// In this class, we will get 
    /// Input: 
    /// 1. three consecutive frames (previous, current, next)
    /// 2. list of textRegion lines on the current frame
    /// 3. list of textRegion region on the current frame
    /// Output: 
    ///      which textRegion lines are static ?
    ///      which textRegion lines are dynamic ?
    /// </remarks>
    public class ClassifyTextLines
    {
        
        private List<Rectangle> staticTextRegionList;

        public List<Rectangle> StaticTextRegionList
        {
            get { return staticTextRegionList; }
            set { staticTextRegionList = value; }
        }
        private List<Rectangle> dynamicTextRegionList;

        public List<Rectangle> DynamicTextRegionList
        {
            get { return dynamicTextRegionList; }
            set { dynamicTextRegionList = value; }
        }

        public bool CheckColorConsistencyOnColorImage(Image<Bgr, byte> previous, Image<Bgr, byte> current, Image<Bgr, byte> next,
            Rectangle textRegion, Image<Gray, byte> resultTextRegion, int threshEpsilon)
        {
            Image<Bgr, byte> prevTextImage, curTextImage, nextTextImage;
            prevTextImage = previous.Copy(textRegion);
            curTextImage = current.Copy(textRegion);
            nextTextImage = next.Copy(textRegion);

            // split color image into three color channel
            Image<Gray,byte> [] prevTextChannelImages = prevTextImage.Split();
            Image<Gray, byte>[] curTextChannelImages = curTextImage.Split();
            Image<Gray, byte>[] nextTextChannelImages = nextTextImage.Split();

            // use the method Otsu, to find a proper threshold on grayscale image
            
            int [] prevThresholds = new int [3]; 
            int [] curThresholds = new int [3];
            int [] nextThresholds = new int [3];
            
            
            OtsuThreshold  otsuThresh = new OtsuThreshold();
            int i;
            for (i = 0; i < 3; i++)
            {
                prevThresholds[i] = otsuThresh.getOtsuThreshold(prevTextChannelImages[i].Bitmap);
                curThresholds[i] = otsuThresh.getOtsuThreshold(curTextChannelImages[i].Bitmap);
                nextThresholds[i] = otsuThresh.getOtsuThreshold(nextTextChannelImages[i].Bitmap);
            }
            
            // compare pairs of corresponding thresholds, ig, (prevBlueThreshold, curBlueThreshold)
            bool isColorConsistency = true;
            for (i = 0; i < 3; i++)
            {
                if (Math.Abs(prevThresholds[i] - curThresholds[i]) > threshEpsilon
                    || Math.Abs(curThresholds[i] - nextThresholds[i]) > threshEpsilon
                    || Math.Abs(prevThresholds[i] - nextThresholds[i]) > threshEpsilon)
                    isColorConsistency = false;
            }


            /*
            // if consistent, find the consistent pixels
            if (isColorConsistency == true)
            {
                // convert color image samples into grayscale
                Image<Gray, byte> prevGrayImage = prevTextImage.Convert<Gray, byte>();
                Image<Gray, byte> curGrayImage = curTextImage.Convert<Gray, byte>();
                Image<Gray, byte> nextGrayImage = nextTextImage.Convert<Gray, byte>();

                Image<Gray, byte> prevThresholdImage, curThresholdImage, nextThresholdImage;
                prevThresholdImage = prevGrayImage.CopyBlank();
                curThresholdImage = curGrayImage.CopyBlank();
                nextThresholdImage = nextGrayImage.CopyBlank();

                CvInvoke.cvThreshold(prevGrayImage, prevThresholdImage, 160F, 255F, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY | Emgu.CV.CvEnum.THRESH.CV_THRESH_OTSU);
                CvInvoke.cvThreshold(curGrayImage, curThresholdImage, 160F, 255F, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY | Emgu.CV.CvEnum.THRESH.CV_THRESH_OTSU);
                CvInvoke.cvThreshold(nextGrayImage, nextThresholdImage, 160F, 255F, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY | Emgu.CV.CvEnum.THRESH.CV_THRESH_OTSU);


                // temporarily, get the intersection of three binary images.
                resultTextRegion = prevThresholdImage.Copy();
                resultTextRegion._And(curThresholdImage);
                resultTextRegion._And(nextThresholdImage);
            }
            */

            return true;
        }

        public bool CheckColorConsistencyOnGrayImage( Image<Gray, byte> previous, Image<Gray, byte> current, Image<Gray, byte> next,
            Rectangle textRegion, Image<Gray, byte> resultTextRegion, int threshEpsilon)
        {
            Image<Gray, byte> prevGrayImage, curGrayImage, nextGrayImage;
            prevGrayImage = previous.Copy(textRegion);
            curGrayImage = current.Copy(textRegion);
            nextGrayImage = next.Copy(textRegion);

            
            
            OtsuThreshold otsuThresh = new OtsuThreshold();
            int prevThreshold, curThreshold, nextThreshold;

            prevThreshold = otsuThresh.getOtsuThreshold(prevGrayImage.Bitmap);
            curThreshold = otsuThresh.getOtsuThreshold(curGrayImage.Bitmap);
            nextThreshold = otsuThresh.getOtsuThreshold(nextGrayImage.Bitmap);
            

            // compare pairs of corresponding thresholds, ig, (prevBlueThreshold, curBlueThreshold)
            bool isColorConsistency = true;

            if (Math.Abs(prevThreshold - curThreshold) > threshEpsilon
                    || Math.Abs(curThreshold - nextThreshold) > threshEpsilon
                    || Math.Abs(prevThreshold - nextThreshold) > threshEpsilon)          
                    
            {
           
                // convert color image samples into grayscale
                Image<Gray, byte> prevThresholdImage, curThresholdImage, nextThresholdImage;

                prevThresholdImage = prevGrayImage.ThresholdBinary(new Gray(prevThreshold), new Gray(255F));
                curThresholdImage = curGrayImage.ThresholdBinary(new Gray(curThreshold), new Gray(255F));
                nextThresholdImage = nextGrayImage.ThresholdBinary(new Gray(curThreshold), new Gray(255F));

                // temporarily, get the intersection of three binary images.
                

                resultTextRegion.Sub(curThresholdImage);
                resultTextRegion._And(nextThresholdImage);
                if ((resultTextRegion.CountNonzero()[0]*1.0F/curThresholdImage.CountNonzero()[0]) < 0.98F)
                    isColorConsistency = false;
            }
            return isColorConsistency;
        }

        // use Kim's method
        // its drawback: will fail when the background changes.
        public bool CheckColorConsistencyOnGrayImageKim(Image<Gray, byte> previous, Image<Gray, byte> current, Image<Gray, byte> next,
            Rectangle textRegion, Image<Gray, byte> resultTextRegion, float threshEpsilon)
        {

            Image<Gray, byte> prevGrayImage, curGrayImage, nextGrayImage;
            prevGrayImage = previous.Copy(textRegion);
            curGrayImage = current.Copy(textRegion);
            nextGrayImage = next.Copy(textRegion);
            
            // set the same ROI for each image (previous, current, next)
            previous.ROI = textRegion;
            current.ROI = textRegion;
            next.ROI = textRegion;

            Image<Gray, byte> diff1, diff2; // diff1 = |current-previous|; diff2 = |current-next|;
            diff1 = current.AbsDiff(previous).ThresholdBinaryInv(new Gray(10), new Gray(255));
            diff2 = current.AbsDiff(next).ThresholdBinaryInv(new Gray(10), new Gray(255));


            //diff1.ThresholdBinaryInv(new Gray(20), new Gray(255));
            //diff2.ThresholdBinaryInv(new Gray(20), new Gray(255));

            Image<Gray, byte> diff = diff1.And(diff2);
            int count = diff.CountNonzero()[0];
            int height, width;
            height = textRegion.Height; width = textRegion.Width;            

            bool isColorConsitency = true;
            float ratio = count * 1.0F / (height * width);
            if (ratio < threshEpsilon)
            {
                isColorConsitency = false;
            }
            // reset ROI 
            previous.ROI = Rectangle.Empty;
            current.ROI = Rectangle.Empty;
            next.ROI = Rectangle.Empty;


            return isColorConsitency;
        }


        public bool CheckOrientationConsistencyOnGrayImage(Image<Gray, byte> previous, Image<Gray, byte> current, Image<Gray, byte> next,
            Rectangle textRegion,  float gradVariance, float orientVariance, float variance)
        {
            Image<Gray, byte> prevGrayImage, curGrayImage, nextGrayImage;
            prevGrayImage = previous.Copy(textRegion);
            curGrayImage = current.Copy(textRegion);
            nextGrayImage = next.Copy(textRegion);


            // calculate the gradient, its direction on each pixel in images (prevTextImage, curTextImage, nextTextImage)
            DerivativeImage derivativeImage = new DerivativeImage();

            // derivative in the horizontal direction, to detect the vertical edge
            int[,] kernelX = new int[3, 3]{
            {-1, 0, 1}, {-2, 0, 2}, {-1, 0, 1}};
            // derivative in the vertical direction, to detect the horizontal edge
            int[,] kernelY = new int[3, 3] {
            {1, 2, 1}, {0, 0, 0}, {-1, -2, -1}};

            float[,] gradientPrevImage, gradientCurImage, gradientNextImage;
            float[,] gradientOrientationPrevImage, gradientOrientationCurImage, gradientOrientationNextImage;

            // calculate the derivative of previous Image
            derivativeImage.ComputeDerivative(prevGrayImage, kernelX, kernelY);
            // get the info Gradient and its Direction
            gradientPrevImage = derivativeImage.Grad;
            gradientOrientationPrevImage = derivativeImage.GradientOrientation;

            // repeat it on current Image, next Image
            // Current Image
            derivativeImage.ComputeDerivative(curGrayImage, kernelX, kernelY);
            // get the info Gradient and its Direction
            gradientCurImage = derivativeImage.Grad;
            gradientOrientationCurImage = derivativeImage.GradientOrientation;
            // Next Image
            derivativeImage.ComputeDerivative(nextGrayImage, kernelX, kernelY);
            // get the info Gradient and its Direction
            gradientNextImage = derivativeImage.Grad;
            gradientOrientationNextImage = derivativeImage.GradientOrientation;

            int i, j;
            int height, width;
            height = prevGrayImage.Height; width = prevGrayImage.Width;
            int count = 0;
            for (i = 0; i < height; i++)
                for (j = 0; j < width; j++)
                {
                    if (Math.Abs(gradientPrevImage[i, j] - gradientCurImage[i, j]) <= gradVariance
                        && Math.Abs(gradientCurImage[i, j] - gradientNextImage[i, j]) <= gradVariance
                        && Math.Abs(gradientOrientationPrevImage[i, j] - gradientOrientationCurImage[i, j]) <= orientVariance
                        && Math.Abs(gradientOrientationCurImage[i, j] - gradientOrientationNextImage[i, j]) <= orientVariance)
                    {
                        count++;
                    }
                }

            bool isOrientationConsistency = false;
            float  temp = count*1.0F/(height*width);
            if ( temp >= variance)
                isOrientationConsistency = true;
            return isOrientationConsistency;
        }

        public bool CheckOrientationConsistencyOnGrayImageLiu(Image<Gray, byte> previous, Image<Gray, byte> current, Image<Gray, byte> next,
            Rectangle textRegion, float threshold)
        {
            Image<Gray, byte> prevGrayImage, curGrayImage, nextGrayImage;
            prevGrayImage = previous.Copy(textRegion);
            curGrayImage = current.Copy(textRegion);
            nextGrayImage = next.Copy(textRegion);

                        
            // calculate the gradient, its direction on each pixel in images (prevTextImage, curTextImage, nextTextImage)
            DerivativeImage derivativeImage = new DerivativeImage();

            // derivative in the horizontal direction, to detect the vertical edge
            int[,] kernelX = new int[3, 3]{
            {-1, 0, 1}, {-2, 0, 2}, {-1, 0, 1}};
            // derivative in the vertical direction, to detect the horizontal edge
            int[,] kernelY = new int[3, 3] {
            {1, 2, 1}, {0, 0, 0}, {-1, -2, -1}};

            float[] prevHistogram, curHistogram, nextHistogram;
            // calculate the derivative of previous Image
            derivativeImage.ComputeDerivative(prevGrayImage, kernelX, kernelY);
            prevHistogram = derivativeImage.HistogramOfGradientDirection();
            derivativeImage.ComputeDerivative(curGrayImage, kernelX, kernelY);
            curHistogram = derivativeImage.HistogramOfGradientDirection();
            derivativeImage.ComputeDerivative(nextGrayImage, kernelX, kernelY);
            nextHistogram = derivativeImage.HistogramOfGradientDirection();

            
            float diffPrevCur = 0, diffCurNext = 0;
            for (int i = 0; i < prevHistogram.GetLength(0); i++)
            {
                diffPrevCur += (curHistogram[i] - prevHistogram[i]) * (curHistogram[i] - prevHistogram[i]);
                diffPrevCur += (curHistogram[i] - nextHistogram[i]) * (curHistogram[i] - nextHistogram[i]);
            }

            bool isOrientationConsistency = false;
            float temp1, temp2;
            temp1 = (float)Math.Sqrt(diffPrevCur);
            temp2 = (float)Math.Sqrt(diffCurNext);
            if (temp1 < threshold && temp2 < threshold)
            {
                isOrientationConsistency = true;
            }
            return isOrientationConsistency;
        }

        public void ClassfifyTextLines(Image<Gray, byte> previous, Image<Gray, byte> current, Image<Gray, byte> next,
            List<Image<Gray,byte>> textImageList, List<Rectangle> textRegionList, float threshEpsilon,
            float gradVariance, float orientVariance, float variance)
        {
            staticTextRegionList = new List<Rectangle>();
            dynamicTextRegionList = new List<Rectangle>();

            for (int i=0; i< textRegionList.Count; i++)
            {
                
                int widthRegion = textRegionList[i].Width;
                int heightRegion = textRegionList[i].Height;
                Point location = textRegionList[i].Location;
                int div, mod ;

                div = widthRegion / 15; mod = widthRegion % 15;
                if (mod > 8 & ((textRegionList[i].X + 15 * (div + 1)) < previous.Width))
                    widthRegion = 15 * (div + 1);
                else
                    widthRegion = 15 * div;
                if (heightRegion % 2 == 0)
                {
                    heightRegion = heightRegion - 1;
                    location.Y++;
                    
                }

                Rectangle region = new Rectangle(location, new Size(widthRegion, heightRegion));

                if (widthRegion >= 15)
                {
                    //Rectangle region = textRegionList[i];
                    if (CheckColorConsistencyOnGrayImageKim(previous, current, next, region, null, threshEpsilon)
                       && CheckOrientationConsistencyOnGrayImage(previous, current, next, region, gradVariance, orientVariance, variance))
                    {
                        staticTextRegionList.Add(region);
                    }
                    else
                    {
                        dynamicTextRegionList.Add(region);
                    }
                }
            }
        }

        /*
        public void ClassfifyTextLines(List<Image<Gray, byte>> textImageList, List<Rectangle> textRegionList, float thresh)
        {
            staticTextRegionList = new List<Rectangle>();
            dynamicTextRegionList = new List<Rectangle>();

            for (int i = 0; i < textRegionList.Count; i++)
            {
                Rectangle region = textRegionList[i];

                
                if (//CheckColorConsistencyOnGrayImageKim(region, null, threshEpsilon) &&
                     CheckOrientationConsistencyOnGrayImageLiu(previous, current, next, region, thresh))
                {
                    staticTextRegionList.Add(region);
                }
                else
                {
                    dynamicTextRegionList.Add(region);
                }
            }
        }
        */

        public List<Image<Gray, byte>> BinarizeTextImageList(Image<Gray, byte> currentFrame, List<Rectangle> textRegionList)
        {
            /*
            Image<Bgr, byte> prevTextImage, curTextImage, nextTextImage;
            prevTextImage = previous.Copy(textRegion);
            curTextImage = current.Copy(textRegion);
            nextTextImage = next.Copy(textRegion);

            // convert them into grayscale images
            Image<Gray, byte> prevGrayImage = prevTextImage.Convert<Gray, byte>().SmoothGaussian(5);
            Image<Gray, byte> curGrayImage = curTextImage.Convert<Gray, byte>().SmoothGaussian(5);
            Image<Gray, byte> nextGrayImage = nextTextImage.Convert<Gray, byte>().SmoothGaussian(5);
            */
            List<Image<Gray, byte>> binaryImageList = new List<Image<Gray, byte>>();

            for (int i = 0; i < textRegionList.Count; i++)
            {
                Image<Gray, byte> gray = currentFrame.Copy(textRegionList[i]);                 
                Image<Gray, byte> binary = gray.CopyBlank();
                CvInvoke.cvThreshold(gray, binary, 100F, 255F, THRESH.CV_THRESH_BINARY | THRESH.CV_THRESH_OTSU);
                binaryImageList.Add(binary);
            }

            return binaryImageList;


            

        }


    }
}
