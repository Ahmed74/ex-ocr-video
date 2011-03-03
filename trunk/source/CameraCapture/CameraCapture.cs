using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.Util;

namespace CameraCapture
{
    public partial class CameraCapture : Form
    {
        private Capture _capture;
        private bool _captureInProgress;

        public CameraCapture()
        {
            InitializeComponent();
        }

        private void ProcessFrame(object sender, EventArgs arg)
        {
            Image<Bgr, Byte> frame = _capture.QueryFrame();

            //Image<Bgr, Byte> temp = frame.PyrDown();
            //CannyEdgeDetector.SetInputInfo(frame, 5, 40f, 100f);
            //CannyEdgeDetector.Canny();

            ExtractCandicateTextBlock.SetInputInfo(temp);
            //ExtractCandicateTextBlock.DilateVerticalEdgeImage();
            //ExtractCandicateTextBlock.DilateHorizontalEdgeImage();

            
            captureImageBox.Image = frame;
            grayscaleImageBox.Image = CannyEdgeDetector.grayscaleImage;
            //smoothedGrayscaleImageBox.Image = CannyEdgeDetector.DisplayImage(CannyEdgeDetector.horizontalEdge);
            //cannyImageBox.Image = CannyEdgeDetector.DisplayImage(CannyEdgeDetector.verticalEdge);

            smoothedGrayscaleImageBox.Image = ExtractCandicateTextBlock.verticalEdgeImage;
            cannyImageBox.Image = ExtractCandicateTextBlock.horizontalEdgeImage;
        }

        private void captureButtonClick(object sender, EventArgs e)
        {
            #region if capture is not created, create it now
            if (_capture == null)
            {
                try
                {
                    _capture = new Capture("F:\\THAO\\Ban_tin_kinh_te_-_tai_chinh.avi");
                }
                catch (NullReferenceException excpt)
                {
                    MessageBox.Show(excpt.Message);
                }
            }
            #endregion

            if (_capture != null)
            {
                if (_captureInProgress)
                {  //stop the capture
                    captureButton.Text = "Start Capture";
                    Application.Idle -= ProcessFrame;
                }
                else
                {
                    //start the capture
                    captureButton.Text = "Stop";
                    Application.Idle += ProcessFrame;
                }

                _captureInProgress = !_captureInProgress;
            }
        }

        private void ReleaseData()
        {
            if (_capture != null)
                _capture.Dispose();
        }

        private void FlipHorizontalButtonClick(object sender, EventArgs e)
        {
            if (_capture != null) _capture.FlipHorizontal = !_capture.FlipHorizontal;
        }

        private void FlipVerticalButtonClick(object sender, EventArgs e)
        {
            if (_capture != null) _capture.FlipVertical = !_capture.FlipVertical;
        }
    }
}
