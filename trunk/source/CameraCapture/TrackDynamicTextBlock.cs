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
    public class TrackDynamicTextBlock
    {
        /// <summary>
        /// It's used to contain the dynamic text list at status "End"
        /// </summary>
        private List<DynamicTextTracker> finalDynamicTextImageList;

        public List<DynamicTextTracker> FinalDynamicTextImageList
        {
            get { return finalDynamicTextImageList; }
            set { finalDynamicTextImageList = value; }
        }

        /// <summary>
        /// It's used to contain the dynamic text list at the status "Start", "Tracking'
        /// </summary>
        /// <remarks>When the dynamic text change into status "End", it will be placed into dynamicTextList</remarks>
        private List<DynamicTextTracker> trackingDynamicTextList;

        public List<DynamicTextTracker> TrackingDynamicTextList
        {
            get { return trackingDynamicTextList; }
            set { trackingDynamicTextList = value; }
        }

        public TrackDynamicTextBlock()
        {
            finalDynamicTextImageList = new List<DynamicTextTracker>();
            trackingDynamicTextList = new List<DynamicTextTracker>();
        }


        /// <summary>
        /// Initilize the dynamic text blocks for the tracking process
        /// </summary>
        public void InitializeTrackingDynamicTextList(List<DynamicTextDescriber> dynamicTextList, int width)
        {
            if (trackingDynamicTextList.Count == 0)
            {
                for (int i = 0; i < dynamicTextList.Count; i++)
                {
                    DynamicTextDescriber describer = dynamicTextList[i];
                    if (describer.MotionVector.Direction == Direction.Left &&
                        describer.XCenter > (int)(0.75F * width))
                    {
                        // insert it into trackingDynamicTextList
                        DynamicTextTracker dynamicTextTracker = new DynamicTextTracker();
                        dynamicTextTracker.Describer = describer;
                        dynamicTextTracker.StatusTracking = StatusTracking.Start;
                        dynamicTextTracker.Periodicity = 0;
                        trackingDynamicTextList.Add(dynamicTextTracker);
                    }
                    /*
                    else if (describer.MotionVector.Direction == Direction.Left&&
                        describer.XCenter > (int)(0.25F * width))
                    {
                        // insert it into trackingDynamicTextList
                    }
                    */
                }
            }
        }

        /// <summary>
        /// Perform the tracking process
        /// </summary>
        /// <remarks>
        /// Input:
        ///   - new dynamic text blocks
        ///   - old dynamic text blocks
        /// Compare block-wise among them, and determine which new text block uniforms the old text block.
        /// If any, concatenate these two blocks
        /// Output:
        ///     - expand the content the old text block
        /// </remarks>
        public void TrackingDynamicTextProcess(List<DynamicTextDescriber> dynamicTextImageList, int width)
        {
            if (trackingDynamicTextList.Count == 0)
            {  // call the function InitializeTrackingDynamicTextList
                InitializeTrackingDynamicTextList(dynamicTextImageList, width);
            }
            else
            {
                // proceed the tracking process
                for(int i=0; i< trackingDynamicTextList.Count; i++)
                {
                    int j = 0;
                    foreach( DynamicTextDescriber newDescriber in dynamicTextImageList )
                    {
                        StatusTracking status = StatusTracking.None;
                        bool matching = IsMatchingBetweenTwoBlocks(trackingDynamicTextList[i].Describer, newDescriber, ref status, width);
                        if(matching== true)
                        { 
                            // proceed to merge two text blocks 
                            // How can we merge two text blocks?
                            dynamicTextImageList.RemoveAt(j);
                            break;
                        }
                        j++;

                    }
                }

            }
        }

        /// <summary>
        /// Check the matching condition between two text blocks
        /// </summary>
        /// <remarks>Read more the file Dieu kien so khop hai khoi van ban dong</remarks>
        /// <returns>
        /// - true:   matching
        /// - false:  no matching
        /// </returns>
        private bool IsMatchingBetweenTwoBlocks(DynamicTextDescriber oldDescriber, DynamicTextDescriber newDescriber,
            ref StatusTracking status, int width)
        {
            bool isMatching = false;
            if (Math.Abs((oldDescriber.YCenter - newDescriber.YCenter)) < 2                
                && (oldDescriber.MotionVector.Direction == newDescriber.MotionVector.Direction)
                && Math.Abs((oldDescriber.MotionVector.Magnitude - newDescriber.MotionVector.Magnitude)) < 2)
            {
                int diffCenter = newDescriber.MotionVector.Magnitude - oldDescriber.MotionVector.Magnitude;
                if (Math.Abs(diffCenter) < 2)
                {   // Possibly, this text has the length greather than the length of the display window
                    isMatching = true;
                    status = StatusTracking.Tracking;
                }

                if (Math.Abs(diffCenter - oldDescriber.MotionVector.Magnitude) < 2)
                {   // Possibly, this text is appearing or disappearing
                    // Need to determine it is appearing or disappearing

                    isMatching = true;
                    // Here, we have two cases: 
                    // Case 1: the text is moving to the right
                    if (oldDescriber.MotionVector.Direction == Direction.Right)
                    {
                        if (newDescriber.XCenter < width / 2) // appearing
                            status = StatusTracking.Appearing;
                        else // disappearing
                            status = StatusTracking.Disappearing;
                    }
                    // Case 2: the text is moving to the left
                    else if (oldDescriber.MotionVector.Direction == Direction.Left)
                    {
                        if (newDescriber.XCenter > width / 2)
                            status = StatusTracking.Appearing;
                        else status = StatusTracking.Disappearing;
                    }
                }

                if (Math.Abs(diffCenter - 2 * oldDescriber.MotionVector.Magnitude) < 2)
                {   //  Possibly, this text is short and appearing fully on the consecutive frames
                    isMatching = true;
                    status = StatusTracking.Tracking;
                }
            }
            return isMatching;
        }

        private void MergeTwoTextBlocks(DynamicTextDescriber oldDescriber, DynamicTextDescriber newDescriber)
        {
            // compare the height of two text blocks
            if (oldDescriber.TextImage.Height == newDescriber.TextImage.Height)
            { 
                // find the 
            }
            else if (oldDescriber.TextImage.Height < newDescriber.TextImage.Height)
            {
                // chuan bi 
            }
            else
            { 

            }
        }




    }
}
