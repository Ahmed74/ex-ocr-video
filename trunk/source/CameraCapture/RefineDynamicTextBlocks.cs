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

        public void VerifyDynamicTextBlocks(Image<Gray, byte> previousImage, Image<Gray, byte> nextImage,
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
                findMotionVector.CalculateMotionVectorList(previousImage, textImageList[i], regionList[i], 15, 20, 3000);
                previousMotionVectorList = findMotionVector.MotionVectorList;

                
                findMotionVector.CalculateMotionVectorList(nextImage, textImageList[i], regionList[i], 15, 20, 3000);
                nextMotionVectorList = findMotionVector.MotionVectorList;
                
                List<Point> splitPositionList = findMotionVector.SplitPositionList;
                

                
                
                // compare two motion vector lists
                // only keep the sub-block the same magnitude, and the contract direction

                List<RunLength> runningList = new List<RunLength>();
                int end = -1;
                bool same = false;
                for (int j = 0; j < previousMotionVectorList.Count; j++)
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
                                runningList[runningList.Count - 1].End = end;
                                end = -1;
                                same = false;
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
                            if ((previousMotionVectorList[j].Magnitude + nextMotionVectorList[j].Magnitude)==0)
                            {
                                motionVector.Direction = nextMotionVectorList[j].Direction;
                                motionVector.Magnitude = nextMotionVectorList[j].Magnitude;
                            }
                        }


                        Point upperLeftPoint = splitPositionList[runLenth.Start];
                        Size size = new Size(splitPositionList[runLenth.End].X-splitPositionList[runLenth.Start].X, textImageList[i].Height);
                        Rectangle rect = new Rectangle(upperLeftPoint, size );

                        Image<Gray, byte> dynamicImage = textImageList[i].Copy(rect);                        
                        DynamicTextDescriber describer = new DynamicTextDescriber();
                        describer.MotionVector = motionVector;
                        describer.TextImage = dynamicImage;
                        describer.X = regionList[i].X + upperLeftPoint.X;
                        describer.Y = regionList[i].Y + upperLeftPoint.Y;
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
