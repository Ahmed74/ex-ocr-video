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
        
        // This splitting only apply for detecting the text moving in the horizontal
        /// <summary>
        /// Split the Text image into fixedWidthxHeight sub-Images
        /// </summary>
        /// <remarks>Often, fixedWidth is 15 pixels</remarks>
        public void SplitImageIntoSubImages(Image<Gray, byte> image, Rectangle region, int fixWidth)
        {

            // split images into widthxHeight sub-images in the imge height 
            subImageList = new List<Image<Gray, byte>>();
            subRegionList = new List<Rectangle>();

            // get the information about the Image size, and its position on the original image
            int height, width;
            int x, y;
            height = image.Height; width = image.Width; // the Image size
            x = region.X; y = region.Y;                     // its position on the actual image

            // this is to assure that the sub-image always have the odd size.
            if (height % 2 == 0)      
                height = height - 1; 

            int mod = width % fixWidth;
            int div = width / fixWidth;
            int i;


            for (i = 0; i < div; i++)
            {
                Rectangle subRegion = new Rectangle(new Point(x + i * fixWidth, y), new Size(fixWidth, height));  // to calculate the sub-Image position on the original image
                Rectangle rect = new Rectangle(new Point(i * fixWidth, 0), new Size(fixWidth, height));  // to calculate the region to cut the image
                subImageList.Add(image.Copy(rect));
                subRegionList.Add(subRegion);
            }
            if (mod != 0)
            {
                Rectangle subRegion = new Rectangle(new Point(x + i * fixWidth, y), new Size(width - i * fixWidth, height));
                Rectangle rect = new Rectangle(new Point(i * fixWidth, 0), new Size(width - i * fixWidth, height));
                Image<Gray, byte> temp = image.Copy(rect);
                subImageList.Add(image.Copy(rect));
                subRegionList.Add(subRegion);
            }
        }


        /// <summary>
        /// Calculate motion vector of Text Image on the original Image
        /// </summary>
        /// <remarks>Assume that the text moving in the horizontal direction </remarks>
        /// <returns>A motion vector</returns>
        public MotionVector CalculateMotionVector(Image<Gray, byte> originalImage, Image<Gray, byte> image, 
            Rectangle region, int maxShift, int sadThresh)
        {
            // First, extract the necessary subImage on the the original Image
            // and use it compare with the Text image
            int originalHeight, originalWidth; 
            int height, width, x, y;
            originalHeight = originalImage.Height; originalWidth = originalImage.Width;
            height = image.Height; width = image.Width;
            x = region.X; y = region.Y;

            Image<Gray, byte> leftSubImage, rightSubImage;
            Rectangle leftRegion, rightRegion;

            // to calculate the xLeft, and wLeft of the Left image region
            int xLeft, wLeft;
            if(x-maxShift < 0){
                xLeft = 0; 
                wLeft = width + (x-xLeft);
            }
            else {
                xLeft = x-maxShift;
                wLeft = width + maxShift;
            }
            leftRegion = new Rectangle(new Point(xLeft, y), new Size(wLeft, height));
            leftSubImage = originalImage.Copy(leftRegion);

            // to calculate the wRight of the Right image region
            int wRight;
            if (x + width + maxShift > originalWidth)
            {
                wRight = originalWidth - x;
            }
            else
            {
                wRight = width + maxShift;
            }
            rightRegion = new Rectangle(new Point(x, y), new Size(wRight, height));
            rightSubImage = originalImage.Copy(rightRegion);

            // Find the motion vector of the image block
            MotionVector motionVector = new MotionVector();

            // Case 1: the image block shift from the right to the left
            motionVector.Direction = Direction.None;
            motionVector.Magnitude = 0; 

            int minSAD = int.MaxValue, posMinSAD;
            for (int xi = 0; xi<(leftSubImage.Width-image.Width) ; xi++)
            {
                Rectangle rect = new Rectangle(new Point(xi, 0), new Size(width, height));
                Image<Gray, byte> temp = leftSubImage.Copy(rect);
                // to the SAD metric betweeen temp, image
                int curSAD = CalculateSAD(image, temp);
                if (curSAD < minSAD && curSAD < sadThresh)
                {
                    motionVector.Direction = Direction.Left;
                    minSAD = curSAD;
                    if ((leftSubImage.Width - image.Width) == maxShift)
                        posMinSAD = -maxShift + xi; // if 
                    else posMinSAD = -(leftSubImage.Width - image.Width) + xi;
                    motionVector.Magnitude = posMinSAD;
                }
            }            

            // Case 2: the image block shift from the left to the right
            for (int xi = 0; xi < (rightSubImage.Width - image.Width); xi++)
            {
                Rectangle rect = new Rectangle(new Point(xi, 0), new Size(width, height));
                Image<Gray, byte> temp = rightSubImage.Copy(rect);
                // to the SAD metric betweeen temp, image
                int curSAD = CalculateSAD(image, temp);
                if (curSAD < minSAD && curSAD < sadThresh)
                {
                    motionVector.Direction = Direction.Right; // shift the right
                    minSAD = curSAD;
                    posMinSAD = xi;
                    motionVector.Magnitude = posMinSAD;

                }
            }
            return motionVector;

        }

        private int CalculateSAD(Image<Gray, byte> img1, Image<Gray, byte> img2)
        {
            
            Image<Gray, byte> diffImage = img1.AbsDiff(img2);
            //Gray color = diffImage.GetSum();

           
            int[,] data = Utilities.ConvertImageToArray2D(diffImage);
            int height, width;
            height = diffImage.Height; width = diffImage.Width;
            int SAD = 0;
            for(int i=0; i<height; i++)
                for (int j = 0; j < width; j++)
                {
                    SAD += data[i, j];
                }
            
            return SAD;
        }

        public void CalculateMotionVectorList(Image<Gray, byte> originalImage, Image<Gray, byte> textImage, 
            Rectangle region, int fixedWidth, int maxShift, int sadThresh)
        {
            motionVectorList = new List<MotionVector>();
            // divide the Text image int Text sub-Imge

            SplitImageIntoSubImages(textImage, region, fixedWidth);
            
            for (int i = 0; i < subImageList.Count; i++)
            {
                // First, check the block is uniform color ?
                double [] minValues, maxValues;
                Point [] minLocations, maxLocations;
                subImageList[i].MinMax(out minValues, out maxValues, out minLocations, out maxLocations);


                if ((maxValues[0] - minValues[0]) > 15)
                {
                    motionVectorList.Add(CalculateMotionVector(originalImage, subImageList[i], subRegionList[i], maxShift, sadThresh));    
                }
                else motionVectorList.Add(new MotionVector());
               
            }

        }
        
    }
}
