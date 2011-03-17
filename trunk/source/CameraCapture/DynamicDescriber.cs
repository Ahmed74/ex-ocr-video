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
    /// Describe the information of a dynamic text block
    /// </summary>
    /// <remarks>
    /// Include
    /// 1. dynamic text image
    /// 2. its motion vector
    /// 3. the center position (xCenter, yCenter)
    /// </remarks>
    public class DynamicTextDescriber
    {
        Image<Gray, byte> textImage;  // save the text dynamic image in the time.

        public Image<Gray, byte> TextImage
        {
            get { return textImage; }
            set { textImage = value; }
        }

        MotionVector motionVector;  // allow its direction and its magnitude

        public MotionVector MotionVector
        {
            get { return motionVector; }
            set { motionVector = value; }
        }
        int xCenter, yCenter;  // the center 

        public int YCenter
        {
            get { return yCenter; }
            set { yCenter = value; }
        }

        public int XCenter
        {
            get { return xCenter; }
            set { xCenter = value; }
        }
        
    }

    /// <summary>
    /// Record the information of the states of dynamic text block in the tracking process
    /// </summary>
    /// <remarks>
    /// Following states:
    /// 0. None:            indeterminate
    /// 1. Start:           when it just appears 
    /// 2. Appearing:       when it's appearing more
    /// 3. Tracking:        when it's already appeared fully
    /// 4. Disappearing:    when it's disappearing 
    /// 5. End:             when it's alreay disappearing
    /// </remarks>

    public enum StatusTracking { None, Start, Appearing, Tracking, Disappearing, Ended};
    /// <summary>
    /// Record the information of dynamic text block in the tracking process
    /// </summary>
    /// <remarks>
    /// Include
    /// 1. dynamic text describer
    /// 2. status tracking
    /// 3. its period, ie. this text appearing in how many frame since it appears and until it disappears and ends
    /// </remarks>
    public class DynamicTextTracker
    {
        private DynamicTextDescriber describer;

        public DynamicTextDescriber Describer
        {
            get { return describer; }
            set { describer = value; }
        }
        private StatusTracking statusTracking;

        public StatusTracking StatusTracking
        {
            get { return statusTracking; }
            set { statusTracking = value; }
        }
        private int periodicity; // the period which the text re-appear, number of frames/period       

        public int Periodicity
        {
            get { return periodicity; }
            set { periodicity = value; }
        }
    }

}
