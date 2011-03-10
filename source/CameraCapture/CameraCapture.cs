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
        private string filename;

        private CannyEdgeDetector cannyDetector;
        private DetermineCandicateTextBlocks determineTextBlocks;
        private DetermineIndividualTextLines determineTextLines;
        private ClassifyTextLines classifyTextLines;

        Image<Bgr, byte> prev, cur, next ;
        
        public CameraCapture()
        {
            InitializeComponent();
            cannyDetector = new CannyEdgeDetector();
            determineTextBlocks = new DetermineCandicateTextBlocks();
            determineTextLines = new DetermineIndividualTextLines();
            classifyTextLines = new ClassifyTextLines();
        }

        private void ProcessFrame(object sender, EventArgs arg)
        {
            //Image<Bgr, Byte> frame = _capture.QueryFrame();
            //TestOtsuThresh(frame);

            Image <Bgr, byte> temp = _capture.QueryFrame();
            prev = temp.Copy();
            temp = _capture.QueryFrame();
            cur = temp.Copy();
            temp = _capture.QueryFrame();
            next = temp.Copy();
            
            if (next!= null)
                {

                    cannyDetector.Canny(cur, 5, 5f, 20f);
                    determineTextBlocks.DilateEdgeImage(cannyDetector.VerticalEdge, cannyDetector.HorizontalEdge);

                    captureImageBox.Image = cur;
                    //grayscaleImageBox.Image = determineTextBlocks.DilateEdgeImg;
                    //horizontalEdgeImageBox.Image = determineTextBlocks.DilateHorizontalEdgeImg;
                    //verticalEdgeImageBox.Image = determineTextBlocks.DilateVerticalEdgeImg;

                    determineTextBlocks.ExtractCandicateTextBlocks();
                    //Image<Gray, byte> beforeExtractTextLines = Utilities.AttachTextBlocksOnImage(cur.Height, cur.Width,
                    //    determineTextBlocks.CandicateTextBlocksImagesList, determineTextBlocks.CandicateTextRegionList);
                    //horizontalEdgeImageBox.Image = beforeExtractTextLines;


                    determineTextLines.ExtractTextLines(determineTextBlocks.CandicateTextBlocksImagesList, determineTextBlocks.CandicateTextRegionList);
                    //Image<Gray, byte> afterExtractTextLines = Utilities.AttachTextBlocksOnImage(cur.Height, cur.Width,
                    //    determineTextLines.FilteredTextBlockImageList, determineTextLines.FilteredTextRegionList);
                    //verticalEdgeImageBox.Image = afterExtractTextLines;
                    List<Image<Bgr, byte>> textImageList = new List<Image<Bgr, byte>>();
                    textImageList = Utilities.CreateImageListsFromROIList(cur, determineTextLines.FilteredTextRegionList);
                    grayscaleImageBox.Image = Utilities.AttachTextBlocksOnImage(cur.Height, cur.Width,
                        textImageList, determineTextLines.FilteredTextRegionList);
                    

                    classifyTextLines.PreviousFrame = prev;
                    classifyTextLines.CurrentFrame = cur;
                    classifyTextLines.NextFrame = next;
                    classifyTextLines.ClassfifyTextLines(determineTextLines.FilteredTextBlockImageList,
                        determineTextLines.FilteredTextRegionList, 0.70F, 20F, 10F, 0.5F);

                    // Liu's method is too sensitive to threshold
                    //classifyTextLines.ClassfifyTextLines(determineTextLines.FilteredTextBlockImageList,
                    //    determineTextLines.FilteredTextRegionList, 0.02F);

                    List<Image<Bgr, byte>> staticTextImageList, dynamicTextImageList;
                    staticTextImageList = new List<Image<Bgr, byte>>();
                    dynamicTextImageList = new List<Image<Bgr, byte>>();
                    staticTextImageList = Utilities.CreateImageListsFromROIList(cur, classifyTextLines.StaticTextRegionList);
                    dynamicTextImageList = Utilities.CreateImageListsFromROIList(cur, classifyTextLines.DynamicTextRegionList);

                    Image<Bgr, byte> staticTextImage, dynamicTextImage;
                    staticTextImage = Utilities.AttachTextBlocksOnImage(cur.Height, cur.Width, staticTextImageList, classifyTextLines.StaticTextRegionList);
                    dynamicTextImage = Utilities.AttachTextBlocksOnImage(cur.Height, cur.Width, dynamicTextImageList, classifyTextLines.DynamicTextRegionList);


                    horizontalEdgeImageBox.Image = staticTextImage;
                    verticalEdgeImageBox.Image = dynamicTextImage;

                    //List<Image<Gray,byte>> binaryImageList = classifyTextLines.BinarizeTextImageList(classifyTextLines.StaticTextRegionList);
                    //Utilities.ExportImageListUnderFile("F:\\THAO\\Output\\Binary Static Text Block\\", binaryImageList);

                    //List<Image<Gray, byte>> binaryImageList = classifyTextLines.BinarizeTextImageList(classifyTextLines.StaticTextRegionList);
                    //Utilities.ExportImageListUnderFile("F:\\THAO\\Output\\Binary Dynamic Text Block\\", binaryImageList);
                    
                    //++;

                }


                       

        }

        private void TestOtsuThresh(Image<Bgr, Byte> frame)
        {
            Image<Gray, Byte> gray = frame.Convert<Gray, byte>();
            Image<Gray, byte> otsuOpenCV, otsuTolga;
            otsuOpenCV = gray.CopyBlank();
            otsuTolga = gray.CopyBlank();

            CvInvoke.cvThreshold(gray, otsuOpenCV, 160F, 255F, Emgu.CV.CvEnum.THRESH.CV_THRESH_BINARY_INV | Emgu.CV.CvEnum.THRESH.CV_THRESH_OTSU);

            OtsuThreshold otsu = new OtsuThreshold();
            int otsuThresh = otsu.getOtsuThreshold(gray.Bitmap);
            otsuTolga = gray.ThresholdBinaryInv(new Gray(otsuThresh), new Gray(255));


            Image<Gray, byte> subImage = gray.CopyBlank();
            subImage = otsuOpenCV.Sub(otsuTolga);

            captureImageBox.Image = frame;
            grayscaleImageBox.Image = subImage;
            horizontalEdgeImageBox.Image = otsuOpenCV;
            verticalEdgeImageBox.Image = otsuTolga;

        }

        private void SubstractTwoConsecutiveFrames()
        {
            Image<Bgr, Byte> prev, current;
            Image<Gray, byte> grayPrev, grayCur;
            Image<Gray, byte> cannyPrev, cannyCur;
            current = _capture.QueryFrame();

            current = current.SmoothGaussian(5);
            prev = current.Copy();
            current = _capture.QueryFrame();
            string path = "F:\\THAO\\Output\\Dau tu kinh te\\";
            int count = 1;
            while (true)
            {
                if (current != null)
                {
                    captureImageBox.Image = current;
                    current = current.SmoothGaussian(5);

                    grayPrev = prev.Convert<Gray, byte>();
                    cannyPrev = grayPrev.Canny(new Gray(100), new Gray(60));

                    grayCur = current.Convert<Gray, byte>();
                    cannyCur = grayCur.Canny(new Gray(100), new Gray(60));

                    Image<Gray, byte> diff = grayCur.Sub(grayPrev);
                    string filename = path + "diff\\" + count.ToString() + ".jpg";
                    diff.Save(filename);

                    diff = cannyCur.Sub(cannyPrev);
                    filename = path + "canny\\" + count.ToString() + ".jpg";
                    diff.Save(filename);



                    prev = current.Copy();
                    current = _capture.QueryFrame();
                    count++;
                }
                else
                {
                    MessageBox.Show("Meet a null image !!");
                    break;
                }

            }
        }

        private void captureButtonClick(object sender, EventArgs e)
        {
            #region if capture is not created, create it now
            if (_capture == null)
            {
                try
                {
                    //_capture = new Capture("F:\\THAO\\Output\\Tai chinh\\Ban tin tai chinh-02.avi");
                    _capture = new Capture("F:\\THAO\\Ban tin dau tu kinh te.avi");
                    //_capture = new Capture("F:\\THAO\\Ban tin tai chinh.avi");
                    
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

        private void loadButton_Click(object sender, EventArgs e)
        {
            
        }

        private void CameraCapture_Load(object sender, EventArgs e)
        {

        }
    }
}
