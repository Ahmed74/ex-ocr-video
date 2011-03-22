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
    /// <remarks>
    /// Input
    ///    - the original image
    ///    - a Text image and its position on the original image
    ///    
    /// Output: 
    ///    List of Motion Vectors, include the direction of each vector, and its magnitude.
    /// Main task: 
    ///     - Calculate the motion vector of a image block in the horiztonal direction.
    /// </remarks>
    public class FindMotionVector
    {
        private List<MotionVector> motionVectorList;

        public List<MotionVector> MotionVectorList
        {
            get { return motionVectorList; }
            set { motionVectorList = value; }
        }

        /*
         * deprecated
        List<Image<Gray, byte>> subImageList;


        public List<Image<Gray, byte>> SubImageList
        {
            get { return subImageList; }
            set { subImageList = value; }
        }
        
        List<Rectangle> subRegionList;

        public List<Rectangle> SubRegionList
        {
            get { return subRegionList; }
            set { subRegionList = value; }
        }
        */

        List<Point> splitPositionList;

        public List<Point> SplitPositionList
        {
            get { return splitPositionList; }
            set { splitPositionList = value; }
        }

        
        // This splitting only apply for detecting the text moving in the horizontal
        /// <summary>
        /// Determine the list of split positions when dividing image into sub-images. Each sub-image has a given fixed width.
        /// </summary>
        /// <remarks>Often, fixedWidth is 15 pixels</remarks>
        public void SplitImageIntoSubImages(Image<Gray, byte> image, int fixWidth)
        {
            
            // the list to save the split positions
            splitPositionList = new List<Point>();

            
            int height, width;
            height = image.Height; width = image.Width; // get the size of input image
            
            
            int div = width / fixWidth;

            int i;
            for (i = 0; i < div; i++)
            {
                //Rectangle subRegion = new Rectangle(new Point(x + i * fixWidth, y), new Size(fixWidth, height));  // to calculate the sub-Image position on the original image
                Point splitPosition = new Point(i * fixWidth, 0);
                splitPositionList.Add(splitPosition);
            }
            
        }


        /// <summary>
        /// Calculate motion vector of Text Image on the original Image
        /// </summary>
        /// <remarks>Assume that the text moving in the horizontal direction </remarks>
        /// <returns>A motion vector</returns>
        /// Note: the param "image" must have already been set ROI before calling this funtion
        public MotionVector CalculateMotionVector(Image<Gray, byte> originalImage, Image<Gray, byte> image, 
            Rectangle originalRegion, int maxShift, int sadThresh)
        {
            // First, extract the necessary subImage on the the original Image
            // and use it compare with the Text image
            int WIDTH, HEIGHT, X, Y;    // get the original image Size and the location of image
            X = originalRegion.X; Y = originalRegion.Y;            
            HEIGHT = originalImage.Height; WIDTH = originalImage.Width;

            int height, width;
            height = image.ROI.Height; width = image.ROI.Width;

            //Image<Gray, byte> leftSubImage, rightSubImage;
            Rectangle leftRegion, rightRegion;

            // to calculate the xLeft, and wLeft of the Left image region
            int xLeft, wLeft;
            if(X-maxShift < 0){
                xLeft = 0; 
                wLeft = width + (X-xLeft);
            }
            else {
                xLeft = X-maxShift;
                wLeft = width + maxShift;
            }
            leftRegion = new Rectangle(new Point(xLeft, Y), new Size(wLeft, height));
            //leftSubImage = originalImage.Copy(leftRegion);

            // to calculate the wRight of the Right image region
            int wRight;
            if (X + width + maxShift > WIDTH)
            {
                wRight = WIDTH - X;
            }
            else
            {
                wRight = width + maxShift;
            }
            rightRegion = new Rectangle(new Point(X, Y), new Size(wRight, height));
            //rightSubImage = originalImage.Copy(rightRegion);

            // Find the motion vector of the image block
            MotionVector motionVector = new MotionVector();

            // Case 1: the image block shift from the right to the left

            motionVector.Direction = Direction.None;
            motionVector.Magnitude = 0; 

            int minSAD = int.MaxValue, posMinSAD = 0;
            
            for (int xi = 0; xi<(leftRegion.Width-width) ; xi++)
            {
                Rectangle rect = new Rectangle(new Point(leftRegion.X + xi, leftRegion.Y), new Size(width, height));
                
                // set ROI on the original Image
                originalImage.ROI = rect;
                // to the SAD metric betweeen temp, image                
               
                int curSAD = CalculateSAD(image, originalImage);
               
                if (curSAD < minSAD)
                {
                    motionVector.Direction = Direction.Left;
                    minSAD = curSAD;
                    if ((leftRegion.Width - width) == maxShift)
                        posMinSAD = -maxShift + xi; // if 
                    else posMinSAD = -(leftRegion.Width - width) + xi;                    
                }
               
                // reset ROI on the original image
                originalImage.ROI = Rectangle.Empty;
                
            }            
            
            // Case 2: the image block shift from the left to the right
            for (int xi = 0; xi <(rightRegion.Width - width); xi++)
            {
                Rectangle rect = new Rectangle(new Point(rightRegion.X + xi, rightRegion.Y), new Size(width, height));
                
                // set ROI on the original Image
                originalImage.ROI = rect;
                // to the SAD metric betweeen temp, image
                int curSAD = CalculateSAD(image, originalImage);
                if (curSAD < minSAD)
                {
                    motionVector.Direction = Direction.Right; // shift the right
                    minSAD = curSAD;
                    posMinSAD = xi;
                    
                }
                // reset ROI on the original image
                originalImage.ROI = Rectangle.Empty;
            }
            motionVector.Magnitude = posMinSAD;
            /*
            if(minSAD < sadThresh)
                motionVector.Magnitude = posMinSAD;
            */
            return motionVector;

        }

        private int CalculateSAD(Image<Gray, byte> img1, Image<Gray, byte> img2)
        {
            Image<Gray, byte> diffImage = img1.AbsDiff(img2);
            Gray sum = diffImage.GetSum();
            return (int)sum.Intensity;
        }

        public void CalculateMotionVectorList(Image<Gray, byte> originalImage, Image<Gray, byte> textImage, 
            Rectangle region, int fixedWidth, int maxShift, int sadThresh)
        {
            motionVectorList = new List<MotionVector>();
            // divide the Text image int Text sub-Imge

            SplitImageIntoSubImages(textImage, fixedWidth);

            Size size = new Size(fixedWidth, textImage.Height);
            int i = 0;
            for (; i < splitPositionList.Count; i++)
            {
                // To calculate the ROI of text Image, the correspondent region on the original image
                Rectangle roiOnTextImage = new Rectangle(splitPositionList[i], size);
                Rectangle rect = new Rectangle( new Point(region.X + splitPositionList[i].X, region.Y + splitPositionList[i].Y), size);
                textImage.ROI = roiOnTextImage;

                // First, check the block is uniform color ?
                double [] minValues, maxValues;
                Point [] minLocations, maxLocations;
                textImage.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

                
                if ((maxValues[0] - minValues[0])> 50)
                {
                    motionVectorList.Add(CalculateMotionVector(originalImage, textImage, rect, maxShift, sadThresh));    
                }
                else motionVectorList.Add(new MotionVector());
                
                textImage.ROI = Rectangle.Empty;
            }

            /*
            // ie. exist the ... region
            if (textImage.Width % fixedWidth != 0) 
            {
                Size smallSize = new Size(textImage.Width - splitPositionList[i].X, height);
                Rectangle roiOnTextImage = new Rectangle(splitPositionList[i], smallSize);
                Rectangle rect = new Rectangle(new Point(region.X + splitPositionList[i].X, region.Y + splitPositionList[i].Y), smallSize);
                textImage.ROI = roiOnTextImage;

                // First, check the block is uniform color ?
                double[] minValues, maxValues;
                Point[] minLocations, maxLocations;
                textImage.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);


                if ((maxValues[0] - minValues[0]) > 25)
                {
                    motionVectorList.Add(CalculateMotionVector(originalImage, textImage, rect, maxShift, sadThresh));
                }
                else motionVectorList.Add(new MotionVector());

                textImage.ROI = Rectangle.Empty;
            }
            */
            
        }
        
    }
}
