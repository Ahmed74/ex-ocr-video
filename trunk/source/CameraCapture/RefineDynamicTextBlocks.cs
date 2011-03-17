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
    /// Track the text among consecutive frames
    /// </summary>
    /// <remarks>
    /// Input: 
    ///  - 3 frame consecutive frames
    ///  - candicate dynamic text blocks in current frame
    /// Output:
    ///  - dynamic text blocks among these frames
    /// </remarks>
    public class RefineDynamicTextBlocks
    {
        List<DynamicTextDescriber> refinedDynamicImageList;

        public List<DynamicTextDescriber> RefinedDynamicImageList
        {
            get { return refinedDynamicImageList; }
            set { refinedDynamicImageList = value; }
        }

        public void VerifyDynamicTextBlocks(Image<Gray, byte> previous, Image<Gray, byte> next,
            List<Image<Gray, byte>> textImageList, List<Rectangle> regionList)
        {

            refinedDynamicImageList = new List<DynamicTextDescriber>();
            for (int i = 0; i < textImageList.Count; i++)
            {
                // here, we can add one more operation. It is to eleminate the static region which connect to the dynamic region
                // by divide the image into sub-image. Compare them with the previous image, and next image
                // only keep the sub-images which have the big difference.
                // it will code later.

                List<MotionVector> previousMotionVectorList, nextMotionVectorList;
                previousMotionVectorList = new List<MotionVector>();
                nextMotionVectorList = new List<MotionVector>();

                FindMotionVector findMotionVector = new FindMotionVector();
                findMotionVector.CalculateMotionVectorList(previous, textImageList[i], regionList[i], 15, 20, 1000);
                previousMotionVectorList = findMotionVector.MotionVectorList;

                findMotionVector.CalculateMotionVectorList(next, textImageList[i], regionList[i], 15, 20, 1000);
                nextMotionVectorList = findMotionVector.MotionVectorList;

                List<Image<Gray, byte>> subImageList = findMotionVector.SubImageList;
                List<Rectangle> subRegionList = findMotionVector.SubRegionList;

                // compare two motion vector lists
                // only keep the sub-block the same magnitude, and the contract direction

                List<RunLength> runningList = new List<RunLength>();
                int end = -1;
                bool same = false;
                for (int j = 0; j < previousMotionVectorList.Count-1; j++)
                {

                    if (((previousMotionVectorList[j].Direction == Direction.Left && nextMotionVectorList[j].Direction == Direction.Right)
                        || ((previousMotionVectorList[j].Direction == Direction.Right && nextMotionVectorList[j].Direction == Direction.Left)))
                        && Math.Abs(previousMotionVectorList[j].Magnitude + nextMotionVectorList[j].Magnitude) < 2)
                    {
                        if (same == false)
                        {
                            same = true;
                            RunLength runLength = new RunLength();
                            runLength.Start = j;
                            runningList.Add(runLength);
                        }
                        end = j;
                    }
                    else
                    {
                        if (end != -1 && same == true) // dang ton tai mot khoang chay giong nhau truoc do
                        {

                            if (!(j+1 < previousMotionVectorList.Count - 1 && ((previousMotionVectorList[j+1].Direction == Direction.Left && nextMotionVectorList[j+1].Direction == Direction.Right)
                        || ((previousMotionVectorList[j+1].Direction == Direction.Right && nextMotionVectorList[j+1].Direction == Direction.Left)))
                        && Math.Abs(previousMotionVectorList[j+1].Magnitude + nextMotionVectorList[j+1].Magnitude) < 2))
                            {
                                runningList[runningList.Count - 1].End = end;
                                end = -1;
                                same = false;
                            }
                            

                        }
                    }
                }

                if(end!=-1 && same==true)
                    runningList[runningList.Count - 1].End = end;

                if (runningList.Count > 0)
                {
                    foreach (RunLength runLenth in runningList)
                    {
                        List<Image<Gray, byte>> refinedSubImageList = new List<Image<Gray, byte>>();
                        List<Rectangle> refinedSubRegionList = new List<Rectangle>();
                        MotionVector motionVector = new MotionVector();
                        for (int j = runLenth.Start; j <= runLenth.End; j++)
                        {
                            refinedSubImageList.Add(subImageList[j]);
                            refinedSubRegionList.Add(subRegionList[j]);
                            if ((previousMotionVectorList[j].Magnitude + nextMotionVectorList[j].Magnitude)==0)
                            {
                                motionVector.Direction = nextMotionVectorList[j].Direction;
                                motionVector.Magnitude = nextMotionVectorList[j].Magnitude;
                            }
                        }

                        Image<Gray, byte> dynamicImage = Utilities.ConcatenateSubImages(refinedSubImageList);
                        Rectangle region = Utilities.ConcatenateSubRegion(refinedSubRegionList);

                        DynamicTextDescriber describer = new DynamicTextDescriber();
                        describer.MotionVector = motionVector;
                        describer.TextImage = dynamicImage;
                        describer.XCenter = region.X + region.Width / 2;
                        describer.YCenter = region.Y + region.Height / 2;
                        refinedDynamicImageList.Add(describer);
                    }
                }
            }
        }

        public class RunLength
        {
            int start;

            public int Start
            {
                get { return start; }
                set { start = value; }
            }
            int end;

            public int End
            {
                get { return end; }
                set { end = value; }
            }

        }

        
    }
}
