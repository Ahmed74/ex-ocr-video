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
    class DetermineIndividualTextLines
    {
        private List<Image<Gray, byte>> filteredTextBlockImageList;
        private List<Rectangle> filteredTextRegionList;

        public List<Image<Gray, byte>> FilteredTextBlockImageList
        {
            get { return filteredTextBlockImageList; }
            set { filteredTextBlockImageList = value; }
        }

        public List<Rectangle> FilteredTextRegionList
        {
            get { return filteredTextRegionList; }
            set { filteredTextRegionList = value; }
        }

        /// <summary>
        /// Deprecated method
        /// </summary>
        private void FilterBasedOnFillFactor(List<Image<Gray, byte>> imagesList, float fillFactorThreshold)
        {
            List<Image<Gray, byte>> filteredImagesList = new List<Image<Gray, byte>>();

            foreach (Image<Gray, byte> img in imagesList)
            {
                float fillFactor = 0;
                int height, width;
                height = img.Height; width = img.Width;
                int[] numberOfNonZeroPixels = img.CountNonzero();
                fillFactor = numberOfNonZeroPixels[0] * 1.0F / (height * width);
                if (fillFactor >= fillFactorThreshold)
                    filteredImagesList.Add(img);
            }
            imagesList = filteredImagesList;
        }

        /// <summary>
        /// Calculate the image projection the Y-axis
        /// </summary>
        /// <returns>A sequence of values projected on Y-axis</returns>
        private int[] yAxisProjection(Image<Gray, byte> grayImg)
        {
            int height, width;
            int i, j, count;
            height = grayImg.Height;
            width = grayImg.Width;
            int[,] data = Utilities.ConvertImageToArray2D(grayImg);
            int[] hy = new int[height];
            for (i = 0; i < height; i++)
            {
                count = 0;
                for (j = 0; j < width; j++)
                    if (data[i, j] != 0)
                        count++;
                hy[i] = count;
            }
            return hy;
        }


        /// <summary>
        /// Calculate the derivatives of y-axis projection
        /// </summary>
        private int[] CalculateDerivatives(int[] hy)
        {
            int length = hy.GetLength(0);

            // the derivative value of the first element is alway equal to zero
            int[] dev = new int[length];
            dev[0] = 0;
            for (int i = 1; i < length; i++)
                dev[i] = hy[i] - hy[i - 1]; // temporaryly, to calculate the derivative at i via hy[i] - hy[i-1]
            return dev;
        }

        /// <summary>
        /// Search and return the position which the absolute derivative at that is maximum.
        /// </summary>
        /// <returns>Position which the absolute derivative is  maximum</returns>
        private int GetMaxDerivativePosition(int[] dev)
        {

            int i, length;
            i = 0; length = dev.GetLength(0);

            int[] indexArray = new int[length];
            for (i = 0; i < length; i++)
                indexArray[i] = i;
            for (i = 0; i < length - 1; i++)
                for (int j = i + 1; j < length; j++)
                {
                    if (Math.Abs(dev[indexArray[i]]) < Math.Abs(dev[indexArray[j]]))
                    {
                        // swap between two index.
                        int temp = indexArray[i];
                        indexArray[i] = indexArray[j];
                        indexArray[j] = temp;
                    }
                }
            return indexArray[0];
        }

        /// <summary>
        /// Search and return the longest line position
        /// </summary>
        /// <returns>Position which the line is the longest</returns>
        private int GetLongestLinePosition(int[] hy)
        {
            int i, length;
            i = 0; length = hy.GetLength(0);

            int[] indexArray = new int[length];
            for (i = 0; i < length; i++)
                indexArray[i] = i;
            for (i = 0; i < length - 1; i++)
                for (int j = i + 1; j < length; j++)
                {
                    if (hy[indexArray[i]] < hy[indexArray[j]])
                    {
                        // swap between two index.
                        int temp = indexArray[i];
                        indexArray[i] = indexArray[j];
                        indexArray[j] = temp;
                    }
                }
            return indexArray[0];
        }


        private int absoluteGreaterThan(int x, int y)
        {
            int result;
            if (Math.Abs(y) > Math.Abs(x))
                result = 1;
            else if (Math.Abs(y) == Math.Abs(x))
                result = 0;
            else result = -1;
            return result;
        }

        /// <summary>
        /// Search and return the positions list which the given image can be splitted
        /// </summary>
        /// <remarks>The variable splitPositionList contain the possible split position on the given image.</remarks>
        private void SplitVaryingLengthTextLines(Image<Gray, byte> img, int originalPosition, int varianceThreshold, float coef, List<int> splitPositionList)
        {
            if (img.Height >= 8)
            {
                int[] hy = yAxisProjection(img);
                int length = hy.GetLength(0);
                int[] dev = CalculateDerivatives(hy);
                int maxDevPos = GetMaxDerivativePosition(dev);
                int longestLinePos = GetLongestLinePosition(hy);

                if (dev[maxDevPos] > varianceThreshold && (hy[maxDevPos] * 1.0F / hy[longestLinePos] < coef))
                {
                    // call this function recursively
                    // Split the given image into two sub-images.
                    // Apply this function on two sub-images
                    int height, width;
                    height = img.Height; width = img.Width;
                    Rectangle rect1 = new Rectangle(new Point(0, 0), new Size(width, maxDevPos));
                    Rectangle rect2 = new Rectangle(new Point(0, maxDevPos), new Size(width, height - maxDevPos));

                    Image<Gray, byte> subImage1 = img.Copy(rect1); // sub-image region is above split position, original position is equal to 0
                    Image<Gray, byte> subImage2 = img.Copy(rect2); // sub-image region is below split position, original position is split position (maxDevPos)

                    SplitVaryingLengthTextLines(subImage1, 0, varianceThreshold, coef, splitPositionList);
                    SplitVaryingLengthTextLines(subImage2, maxDevPos, varianceThreshold, coef, splitPositionList);
                    splitPositionList.Add(originalPosition + maxDevPos);


                }
            }
            // otherwise, nothing do.
        }

        /// <summary>
        /// Install based on Datong's algorithm "equal length textRegion line split"
        /// </summary>
        private void SplitEqualLengthTextLines(Image<Gray, byte> img, int originalPosition, float coef, List<int> splitPositionList)
        {
            if (img.Height >= 8)
            {
                OtsuThreshold otsu = new OtsuThreshold();
                int[] hy = yAxisProjection(img);
                int length = hy.GetLength(0);
                int longestLinePos = GetLongestLinePosition(hy);
                int otsuThreshPos = otsu.getOtsuThresholdFromHistogram(hy);

                if (hy[otsuThreshPos] * 1.0F / hy[longestLinePos] < coef)
                {
                    // call this function recursively
                    // Split the given image into two sub-images.
                    // Apply this function on two sub-images
                    int height, width;
                    height = img.Height; width = img.Width;
                    Rectangle rect1 = new Rectangle(new Point(0, 0), new Size(width, otsuThreshPos + 1));
                    Rectangle rect2 = new Rectangle(new Point(0, otsuThreshPos), new Size(width, height - otsuThreshPos));

                    Image<Gray, byte> subImage1 = img.Copy(rect1);
                    Image<Gray, byte> subImage2 = img.Copy(rect2);
                    SplitEqualLengthTextLines(subImage1, 0, coef, splitPositionList);
                    SplitEqualLengthTextLines(subImage2, otsuThreshPos, coef, splitPositionList);
                    splitPositionList.Add(originalPosition + otsuThreshPos);
                }
            }
        }

        /// <summary>
        /// Refine the baseline of textRegion block
        /// </summary>
        /// <returns>
        /// whether there is the change of baseline or not and new positions of top line and bottom line
        /// true: if change
        /// false: nothing
        /// </returns>
        private bool RefineBaseline(Image<Gray, byte> img, float coef, ref int topLine, ref int bottomLine)
        {
            bool isChange = false;
            // calculate the central line position of textRegion region
            if (img.Height >= 8)
            {
                int[] hy = yAxisProjection(img);
                int length = hy.GetLength(0);
                int ycenter;
                int i, sum1, sum2;
                sum1 = 0; sum2 = 0;

                for (i = 0; i < length; i++)
                {
                    sum1 += (i + 1) * hy[i];
                    sum2 += hy[i];
                }
                ycenter = (int)(sum1 * 1.0F / sum2);

                // because the minmum target textRegion height is 8 pixels;

                int topInitialLine, bottomInitialLine;
                topLine = topInitialLine = ycenter - 3;
                bottomLine = bottomInitialLine = ycenter + 3;

                FindOptimalBaseline(img, coef, ref topLine, ref bottomLine);

                // this flag have a meaning as follow
                // true: if it changes, ie. this image block is textRegion
                // false: otherwise, it is non-textRegion.                

                if (topLine != topInitialLine && bottomLine != bottomInitialLine)
                {
                    isChange = true;
                    topLine++; bottomLine--;
                }                
            }
            return isChange;
        }

        /// <summary>
        /// Optimize the top and bottom baseline
        /// </summary>
        /// <remarks>The algorithm details can see in Datong Chen's thesis, page 56</remarks>
        private void FindOptimalBaseline(Image<Gray, byte> img, float coef, ref int topLine, ref int bottomLine)
        {
            int afterSetROI;
            int height, width;
            width = img.Width;
            height = bottomLine - topLine;

            Rectangle roi = new Rectangle(new Point(0, topLine), new Size(width, height));
            img.ROI = roi;
            afterSetROI = img.CountNonzero()[0];            

            if (afterSetROI * 1.0F / (height * width) > coef)
            {
                topLine--; bottomLine++;
                img.ROI = System.Drawing.Rectangle.Empty;
                if (topLine >= 0 && bottomLine <= img.Height)
                    FindOptimalBaseline(img, coef, ref topLine, ref bottomLine);
                else return;
            }
        }

        /// <summary>
        /// Determine whether the textRegion line satisfy heuristic constraints or not
        /// There are total three heuristic constraints
        /// </summary>
        /// <remarks>
        /// 1. The number of pixels contains in this textRegion line is greater than a specific value
        /// 2. The horizontal-vertical aspect ratio of the textRegion line is greater than a certain threshold 
        /// 3. The height of the textRegion line is greater than 8 pixels
        /// </remarks>
        /// <returns>
        /// true: if choosing
        /// false: not choose
        /// </returns>
        private bool SelectTextLineBasedHeuristicConstraints(Image<Gray, byte> img, int minimumNumberOfPixels,
            int minimumNumberOfCharacters, float minimumHeight )
        {
            bool isChoose = false;
            int numberOfNonZeroPixels = img.CountNonzero()[0];
            float widthHeightRatio = img.Width * 1.0F / img.Height;
            int height = img.Height;
            if (height > minimumHeight && numberOfNonZeroPixels >= minimumNumberOfPixels
                && widthHeightRatio > 0.6 * minimumNumberOfCharacters)
                isChoose = true;
            return isChoose;
        }

        /// <summary>
        /// Filter candicate textRegion blocks and only keep blocks which satisfy heuristics constraints
        /// </summary>
        public void ExtractTextLines(List<Image<Gray, byte>> textBlockImagesList, List<Rectangle> textRegionsList)
        {
            filteredTextBlockImageList = new List<Image<Gray, byte>>();
            filteredTextRegionList = new List<Rectangle>();
            /*  We must perform several following steps
             * 1: Split varying length textRegion lines
             * 2: Split equal length textRegion lines
             * 3: Refine baselines
             * 4: select textRegion line using heuristic constraints
             */


            List<Image<Gray, byte>> subImageList = new List<Image<Gray, byte>>();
            List<Rectangle> subRegionList = new List<Rectangle>();

            // Run step 1
            for (int i = 0; i < textBlockImagesList.Count; i++)
            {
                Image<Gray, byte> image = textBlockImagesList[i];
                Rectangle region = textRegionsList[i];
                List<int> splitPositionList = new List<int>();
                SplitVaryingLengthTextLines(image, 0, 25, 0.5F, splitPositionList);
                // split image into sub-images, textRegion region into textRegion sub-region; place them the corresponding lists
                SplitIntoSubImagesAndPlaceIntoList(subImageList, subRegionList, image, region, splitPositionList);
            }
            // Run step 2
            List<Image<Gray, byte>> subImageList2 = new List<Image<Gray, byte>>();
            List<Rectangle> subRegionList2 = new List<Rectangle>();
            for (int i = 0; i < subImageList.Count; i++)
            {
                Image<Gray, byte> image = subImageList[i];
                Rectangle region = subRegionList[i];
                List<int> splitPositionList = new List<int>();
                SplitEqualLengthTextLines(image, 0, 0.5F, splitPositionList);
                // split image into sub-images, textRegion region into textRegion sub-region; place them the corresponding lists
                SplitIntoSubImagesAndPlaceIntoList(subImageList2, subRegionList2, image, region, splitPositionList);                
            }

            // Run step 3
            // First, clear subImageList, subRegionList so that it can contain new element
            subImageList.Clear();
            subRegionList.Clear();

            for (int i = 0; i < subImageList2.Count; i++)
            {
                Image<Gray, byte> image = subImageList2[i];
                Rectangle region = subRegionList2[i];

                int top = 0, bottom = 0;
                bool isChange = RefineBaseline(image, 0.5F, ref top, ref bottom);
                // refine the size of textRegion image, and size of textRegion region
                Rectangle imageRect = new Rectangle(new Point(0, top), new Size(image.Width, bottom - top));
                Rectangle regionRect = new Rectangle(new Point(region.X, region.Y + top ), new Size(region.Width, bottom-top));

                if (i == 22)
                {
                    subImageList.Add(image.Copy(imageRect));
                    subRegionList.Add(regionRect);
                }
                subImageList.Add(image.Copy(imageRect));
                subRegionList.Add(regionRect);

            }
            
            // Run step 4
            subImageList2.Clear();
            subRegionList2.Clear();

            for (int i = 0; i < subImageList.Count; i++)
            {
                Image<Gray, byte> image = subImageList[i];
                Rectangle region = subRegionList[i];
                bool isChoose = SelectTextLineBasedHeuristicConstraints(image, 75, 2, 8);
                if (isChoose == true)
                {
                    filteredTextBlockImageList.Add(image);
                    filteredTextRegionList.Add(region);
                }
            }

            subImageList.Clear();
            subRegionList.Clear();
        }

        /// <summary>
        /// Split the image into sub-images based on the possible split positions list
        /// </summary>
        private void SplitIntoSubImagesAndPlaceIntoList(List<Image<Gray, byte>> subImageList, List<Rectangle> subRegionList, 
            Image<Gray, byte> image, Rectangle region, List<int> splitPositionList)
        {
            if (splitPositionList.Count != 0)
            {
                // find some positions which can split
                // split the image into smaller sub-images based the split positions and set them into filtered
                // first, sort splitPositionList in the ascending order
                splitPositionList.Sort(); // check the defaul comparer is sorted in which order (ASC/DESC)
                // assume it is sorted in ASC order
                int prevPosition = 0, currentPosition;

                for (int j = 0; j < splitPositionList.Count; j++)
                {
                    currentPosition = splitPositionList[j] + 1;
                    Rectangle rect = new Rectangle(new Point(0, prevPosition),
                        new Size(image.Width, currentPosition - prevPosition));
                    // to calculate the position of the textRegion sub-region on the original image
                    Rectangle subRegion = new Rectangle(new Point(region.X, region.Y + prevPosition),
                        new Size(region.Width, currentPosition - prevPosition));

                    prevPosition = currentPosition;
                    Image<Gray, byte> subImage = image.Copy(rect);
                    subImageList.Add(subImage);
                    subRegionList.Add(subRegion);
                    // how to insert textRegion region ??
                }

                Rectangle finalRect = new Rectangle(new Point(0, prevPosition),
                    new Size(image.Width, image.Height - prevPosition));
                Rectangle finalSubRegion = new Rectangle(new Point(region.X, region.Y + prevPosition),
                    new Size(region.Width, region.Height - prevPosition));
                subImageList.Add(image.Copy(finalRect));
                subRegionList.Add(finalSubRegion);
            }
            else
            {
                // if it doesn't satisfy the condition, then set it subImageList, subRegionList
                // and proceed further.
                subImageList.Add(image);
                subRegionList.Add(region);
            }
        }







    }
}
