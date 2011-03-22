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
        private RefineDynamicTextBlocks refineDynamicTextBlocks;

        Image<Gray, byte> prev, cur, next, grayTemp ;
        Image<Bgr, byte> temp; 
        
        public CameraCapture()
        {
            InitializeComponent();
            cannyDetector = new CannyEdgeDetector();
            determineTextBlocks = new DetermineCandicateTextBlocks();
            determineTextLines = new DetermineIndividualTextLines();
            classifyTextLines = new ClassifyTextLines();
            refineDynamicTextBlocks = new RefineDynamicTextBlocks();
            prev = null;
        }

        private void ProcessFrame(object sender, EventArgs arg)
        {
            //Image<Bgr, Byte> frame = _capture.QueryFrame();
            //TestOtsuThresh(frame);

            temp = _capture.QueryFrame();
            grayTemp = temp.Convert<Gray, byte>();//.SmoothMedian(3);
            
            if (temp != null)
            {

                int height, width;
                height = temp.Height; width = temp.Width;
                
                //Rectangle rect = new Rectangle(new Point(0, (int)(0.75F * height)), new Size(width, (int)(0.25F * height)));
                //previous = temp.Copy(rect);
                //temp = _capture.QueryFrame();            
                //previous = temp.Copy(rect);
                //temp = _capture.QueryFrame();
                //next = temp.Copy(rect);

                if (prev == null)
                {
                    prev = grayTemp.Copy();
                    temp = _capture.QueryFrame();
                    grayTemp = temp.Convert<Gray, byte>();//.SmoothMedian(3);
                }
                else prev = next.Copy();
                
                cur = grayTemp.Copy();
                temp = _capture.QueryFrame();
                grayTemp = temp.Convert<Gray, byte>();//.SmoothMedian(3);
                next = grayTemp.Copy();
                
                if (next != null)
                {
                    captureImageBox.Image = cur;
                    // Step 1: dectect candicate blocks and extract individual lines.
                    cannyDetector.Canny(cur, 5, 5f, 15f);
                    determineTextBlocks.DilateEdgeImage(cannyDetector.VerticalEdge, cannyDetector.HorizontalEdge);                    
                    determineTextBlocks.ExtractCandicateTextBlocks();
                    determineTextLines.ExtractTextLines(determineTextBlocks.CandicateTextBlocksImagesList, determineTextBlocks.CandicateTextRegionList);


                    
                    List<Image<Gray, byte>> textImageList = new List<Image<Gray, byte>>();
                    
                    textImageList = Utilities.CreateImageListsFromROIList(cur, determineTextLines.FilteredTextRegionList);
                    grayscaleImageBox.Image = Utilities.AttachTextBlocksOnImage(cur.Height, cur.Width,
                        textImageList, determineTextLines.FilteredTextRegionList);

                    
                    

                    classifyTextLines.ClassfifyTextLines(prev, cur, next, determineTextLines.FilteredTextBlockImageList,
                        determineTextLines.FilteredTextRegionList, 0.90F, 20F, 10F, 0.5F);
                    
                    /*

                    List<Image<Gray, byte>> staticTextImageList, dynamicTextImageList;
                    staticTextImageList = new List<Image<Gray, byte>>();
                    dynamicTextImageList = new List<Image<Gray, byte>>();
                    staticTextImageList = Utilities.CreateImageListsFromROIList(cur.Convert<Gray, byte>(), classifyTextLines.StaticTextRegionList);
                    dynamicTextImageList = Utilities.CreateImageListsFromROIList(cur.Convert<Gray, byte>(), classifyTextLines.DynamicTextRegionList);

                    Image<Gray, byte> staticTextImage, dynamicTextImage;
                    staticTextImage = Utilities.AttachTextBlocksOnImage(cur.Height, cur.Width, staticTextImageList, classifyTextLines.StaticTextRegionList);
                    dynamicTextImage = Utilities.AttachTextBlocksOnImage(cur.Height, cur.Width, dynamicTextImageList, classifyTextLines.DynamicTextRegionList);

                    verticalEdgeImageBox.Image = dynamicTextImage;
                    horizontalEdgeImageBox.Image = staticTextImage;
                    
                    /*
                    refineDynamicTextBlocks.VerifyDynamicTextBlocks(prev.Convert<Gray, byte>(), next.Convert<Gray, byte>(),
                    dynamicTextImageList, classifyTextLines.DynamicTextRegionList);

                    List<Image<Gray, byte>> imageList = new List<Image<Gray, byte>>();
                    List<Rectangle> regionList = new List<Rectangle>();
                    for (int i = 0; i < refineDynamicTextBlocks.RefinedDynamicImageList.Count; i++)
                    {
                        imageList.Add(refineDynamicTextBlocks.RefinedDynamicImageList[i].TextImage);
                        Rectangle rect1 = new Rectangle(new Point(refineDynamicTextBlocks.RefinedDynamicImageList[i].X,
                            refineDynamicTextBlocks.RefinedDynamicImageList[i].Y),
                            refineDynamicTextBlocks.RefinedDynamicImageList[i].TextImage.Size);
                        regionList.Add(rect1);
                    }

                    if (imageList.Count > 0)
                    {
                        staticTextImage = Utilities.AttachTextBlocksOnImage(cur.Height, cur.Width, imageList, regionList);
                        horizontalEdgeImageBox.Image = staticTextImage;
                    }
                    
                    */


                    //List<Image<Gray,byte>> binaryImageList = classifyTextLines.BinarizeTextImageList(classifyTextLines.StaticTextRegionList);
                    //Utilities.ExportImageListUnderFile("F:\\THAO\\Output\\Binary Static Text Block\\", binaryImageList);

                    //List<Image<Gray, byte>> binaryImageList = classifyTextLines.BinarizeTextImageList(classifyTextLines.DynamicTextRegionList);
                    //Utilities.ExportImageListUnderFile("F:\\THAO\\Output\\Binary Dynamic Text Block\\", dynamicTextImageList);

                    //++;

                }

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

        
        private void captureButtonClick(object sender, EventArgs e)
        {
            #region if capture is not created, create it now
            if (_capture == null)
            {
                try
                {
                    //_capture = new Capture("F:\\sample1.avi");
                    _capture = new Capture(@"F:\THAO\Output\SampleVideos\sample2.avi");
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
