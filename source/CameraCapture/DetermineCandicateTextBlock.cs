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
    /// Extract Candicate Text Blocks
    /// </summary>
    /// <remarks>
    /// This class performs the Dilate morphology operation on the horizontal edge image and the vertical edge image with two structure elements. 
    /// Then, the intersection between two dilated edge image in both direction give us the dilated edge image DI which contain textRegion blocks
    /// Finally, candicate textRegion blocks are extract based on getting contours on the DI image.
    /// </remarks>
    class DetermineCandicateTextBlocks
    {

        // define two structure elements for the Dilate operation on both horziontal and vertical edge image
        public int[] verticalStructElement = new int[5] { 1, 1, 1, 1, 1 };

        public int[,] horizontalStructElement = new int[7,3] {
            { 1, 1, 1},
            { 1, 1, 1},
            { 1, 1, 1},
            { 1, 1, 1},
            { 1, 1, 1},
            { 1, 1, 1},
            { 1, 1, 1}            
        };

        private float[,] dilateHorizontalEdge;
        private float[,] dilateVerticalEdge;
        private float[,] dilateEdge;

        private Image<Gray, byte> dilateHorizontalEdgeImg;        
        private Image<Gray, byte> dilateVerticalEdgeImg;               
        private Image<Gray, byte> dilateEdgeImg;


        private List<Image<Gray, Byte>> candicateTextBlocksImagesList; // of course, these block are on the dilated edge image
        private List<Rectangle> candicateTextRegionList;

        # region define the properties of this class

        public List<Image<Gray, Byte>> CandicateTextBlocksImagesList
        {
            get { return candicateTextBlocksImagesList; }
        }


        public List<Rectangle> CandicateTextRegionList
        {
            get { return candicateTextRegionList; }
        }

        public Image<Gray, byte> DilateHorizontalEdgeImg
        {
            get { return dilateHorizontalEdgeImg; }
        }
        public Image<Gray, byte> DilateVerticalEdgeImg
        {
            get { return dilateVerticalEdgeImg; }
        }

        public Image<Gray, byte> DilateEdgeImg
        {
            get { return dilateEdgeImg; }
        }

        public float[,] DilateHorizontalEdge
        {
            get { return dilateHorizontalEdge; }
        }

        public float[,] DilateVerticalEdge
        {
            get { return dilateVerticalEdge; }
        }

        public float[,] DilateEdge
        {
            get { return dilateEdge; }
        }

        # endregion 

        /// <summary>
        /// Dilate on the vertical edge image
        /// </summary>
        private void DilateVerticalEdgeImage(float[,] verticalEdge, int height, int width)
        {
            // to dilate the vertical image manually                          
            dilateVerticalEdge = new float[height, width];

            int halfOfSize = verticalStructElement.GetLength(0) / 2;
            for (int i = 0; i < height; i++)
                for (int j = halfOfSize; j < width - halfOfSize; j++)
                {
                    if (verticalEdge[i, j] != 0)
                    {
                        for (int k = -halfOfSize; k <= halfOfSize; k++)
                            dilateVerticalEdge[i, j + k] = 255;
                    }
                }
            dilateVerticalEdgeImg = Utilities.CreateImageFromArray2D(dilateVerticalEdge).Convert<Gray, byte>();
        }

        /// <summary>
        /// Dilate on the horizontal edge image
        /// </summary>
        private void DilateHorizontalEdgeImage(float[,] horizontalEdge, int height, int width)
        {
            // to dilate the vertical image manually              
            
            dilateHorizontalEdge = new float[height, width];

            int halfOfRow= horizontalStructElement.GetLength(0) / 2;
            int halfOfColumn = horizontalStructElement.GetLength(1) / 2;

            for (int i = halfOfRow; i < height - halfOfRow; i++)
                for (int j = halfOfColumn; j < width - halfOfColumn; j++)
                {
                    if (horizontalEdge[i, j] != 0)
                    {
                        for (int k = -halfOfRow; k <= halfOfRow; k++)
                            for (int l = -halfOfColumn; l <= halfOfColumn; l++)
                            {
                                dilateHorizontalEdge[i + k, j + l] = 255;
                            }
                    }
                }
            dilateHorizontalEdgeImg = Utilities.CreateImageFromArray2D(dilateHorizontalEdge).Convert<Gray, byte>();
        }

        /// <summary>
        /// Combine both two dilated horizontal and vertical images to  create the final dilated edge image which contains the candicate textRegion blocks
        /// </summary>
        public void DilateEdgeImage(float [,] verticalEdge, float [,] horizontalEdge)
        {
            int width, height;
            
            height = verticalEdge.GetLength(0);
            width = verticalEdge.GetLength(1);

            DilateHorizontalEdgeImage(horizontalEdge, height, width);
            DilateVerticalEdgeImage(verticalEdge, height, width);

            dilateEdge = new float[height, width];

            for (int i = 0; i < height; i++)
                for (int j = 0; j < width; j++)
                {
                    if (dilateHorizontalEdge[i, j] != 0 && dilateVerticalEdge[i, j] != 0)
                        dilateEdge[i, j] = 255;
                }
            dilateEdgeImg = Utilities.CreateImageFromArray2D(dilateEdge).Convert<Gray,byte> ();
            
            
        }

        /// <summary>
        /// Extract candicate textRegion blocks on the final edge image
        /// </summary>
        /// <remarks>Set these blocks and their corresponding region into two lists</remarks>
        public void ExtractCandicateTextBlocks()
        {

            candicateTextBlocksImagesList = new List<Image<Gray, byte>>();
            candicateTextRegionList = new List<Rectangle>();

            using (MemStorage storage = new MemStorage()) //allocate storage for contour approximation
            {
                Contour<Point> contours = dilateEdgeImg.FindContours(
                 Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                 Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_CCOMP, storage);
                
                for (; contours != null; contours = contours.HNext)
                {
                    //Contour<Point> currentContour = contours.ApproxPoly(contours.Perimeter*0.05, storage);
                    // Here,we can use more the ApproxPoly to 
                    if (contours.Area > 30)
                    {
                        Rectangle rect = contours.BoundingRectangle;                     
                        candicateTextBlocksImagesList.Add(dilateEdgeImg.Copy(rect));
                        candicateTextRegionList.Add(rect);                        
                    }
                }
            }             
        }
    }
}
