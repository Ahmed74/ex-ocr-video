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
    public class Utilities
    {
        /// <summary>
        /// Create grayscale image to array 2d
        /// </summary>
        /// <remarks>
        /// Input: float array 2d
        /// Output: grayscale image
        /// </remarks>
        /// <returns>A grayscale image</returns>
        public static Image<Gray, float> CreateImageFromArray2D(float[,] data)
        {
            int height, width;
            height = data.GetLength(0);
            width = data.GetLength(1);
            float[, ,] temp = new float[height, width, 1];
            int i, j;
            for (i = 0; i < height; i++)
                for (j = 0; j < width; j++)
                {
                    temp[i, j, 0] = data[i, j];
                }
            return new Image<Gray, float>(temp);
        }

        /// <summary>
        /// Convert Image to Array 2d
        /// </summary>
        /// <remarks>This function get a input image which can be color or grayscale image. Then, it converts color data of image into an array 2d</remarks>
        /// <returns>Array 2d</returns>
        public static int[,] ConvertImageToArray2D(Image<Gray, byte> img)
        {
            int height, width;
            height = img.Height; width = img.Width;

            int[,] data = new int[height, width];
            int i, j;
            Bitmap image = img.Bitmap;
            BitmapData bitmapData1 = image.LockBits(new Rectangle(0, 0, width, height),
                                     ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            unsafe
            {
                byte* imagePointer1 = (byte*)bitmapData1.Scan0;

                for (i = 0; i < height; i++)
                {
                    for (j = 0; j < width; j++)
                    {
                        data[i, j] = (int)(imagePointer1[0] / 3.0);
                        //4 bytes per pixel
                        imagePointer1 += 4;
                    }//end for j
                    //4 bytes per pixel
                    imagePointer1 += bitmapData1.Stride - (bitmapData1.Width * 4);
                }//end for i
            }//end unsafe
            image.UnlockBits(bitmapData1);
            return data;
        }

        public static int[,] ConvertImageToArray2D(Image<Bgr, byte> img)
        {
            Image<Gray, byte> gray = img.Convert<Gray, byte>();
            return ConvertImageToArray2D(gray);
        }



        /// <summary>
        /// Export the images in the list into the image file
        /// </summary>
        public static void ExportImageListUnderFile(string dir, List<Image<Bgr, byte>> imageList)
        {

            foreach (Image<Bgr, byte> img in imageList)
            {
                string filename = dir + count.ToString() + ".jpg";
                img.Save(filename);
                count++;
            }
        }

        public static int count = 1;
        public static void ExportImageListUnderFile(string dir, List<Image<Gray, byte>> imageList)
        {

            foreach (Image<Gray, byte> img in imageList)
            {
                string filename = dir + count.ToString() + ".jpg";
                img.Save(filename);
                count++;
            }
        }


        public static List<Image<Bgr, byte>> CreateImageListsFromROIList(Image<Bgr, byte> img, List<Rectangle> roiList)
        {
            List<Image<Bgr, byte>> imageList = new List<Image<Bgr, byte>>();
            foreach (Rectangle rect in roiList)
            {
                Image<Bgr, byte> subimg = img.Copy(rect);
                imageList.Add(subimg);
            }
            return imageList;
        }

        /// <summary>
        /// Build the sub-images list from the input image based on the available boxes list
        /// </summary>
        /// <returns>List of sub-images</returns>
        public static List<Image<Gray, byte>> CreateImageListsFromROIList(Image<Gray, byte> img, List<Rectangle> roiList)
        {
            List<Image<Gray, byte>> imageList = new List<Image<Gray, byte>>();
            foreach (Rectangle rect in roiList)
            {
                Image<Gray, byte> subimg = img.Copy(rect);
                imageList.Add(subimg);
            }
            return imageList;
        }

        /// <summary>
        /// Attach textRegion blocks on the image
        /// </summary>
        /// <remarks>Create a black image, use the textRegion blocks list, and the textRegion region to attach them to this image.</remarks>
        /// <returns>The image is attached textRegion blocks</returns>
        public static Image<Gray, byte> AttachTextBlocksOnImage(int height, int width, List<Image<Gray, byte>> textImageList, List<Rectangle> textRegionList)
        {
            Image<Gray, byte> blackImage = new Image<Gray, byte>(width, height);
            Rectangle originalROI = blackImage.ROI;
            for (int i = 0; i < textImageList.Count; i++)
            {
                Image<Gray, byte> image = textImageList[i];
                Rectangle region = textRegionList[i];
                // set ROI on the image
                // place image on the selected ROI
                blackImage.ROI = region;
                image.CopyTo(blackImage);
                blackImage.ROI = Rectangle.Empty;
            }
            return blackImage;
        }

        public static Image<Bgr, byte> AttachTextBlocksOnImage(int height, int width, List<Image<Bgr, byte>> textImageList, List<Rectangle> textRegionList)
        {
            Image<Bgr, byte> colorImage = new Image<Bgr, byte>(width, height);
            Rectangle originalROI = colorImage.ROI;
            for (int i = 0; i < textImageList.Count; i++)
            {
                Image<Bgr, byte> image = textImageList[i];
                Rectangle region = textRegionList[i];
                // set ROI on the image
                // place image on the selected ROI
                colorImage.ROI = region;
                image.CopyTo(colorImage);
                colorImage.ROI = Rectangle.Empty;
            }
            return colorImage;
        }

        /// <summary>
        /// Create a larger image from the sub-images list
        /// </summary>
        /// <remarks>
        /// Input:
        ///    - sub-images list
        /// Output
        ///    - concatenated image
        /// </remarks>
        public static Image<Gray, byte> ConcatenateSubImages(List<Image<Gray, byte>> subImageList)
        {
            int noSubImage, width, height;
            noSubImage = subImageList.Count;
            Image<Gray, byte> concatenatedImage = null;
            if (noSubImage > 0)
            {
                width = subImageList[0].Width; height = subImageList[0].Height;
                concatenatedImage = new Image<Gray, byte>(width * noSubImage, height);
                for (int i = 0; i < noSubImage; i++)
                {
                    Rectangle roi = new Rectangle(new Point(i * width, 0), new Size(width, height));
                    concatenatedImage.ROI = roi;
                    subImageList[i].CopyTo(concatenatedImage);
                    concatenatedImage.ROI = Rectangle.Empty;
                }
            }

            return concatenatedImage;
        }

        /// <summary>
        /// The same as concatenating sub-images, but concatenate on sub-regions
        /// </summary>
        public static Rectangle ConcatenateSubRegion(List<Rectangle> subRegion)
        {
            int noSubRegion, width, height, X, Y;
            noSubRegion = subRegion.Count;
            Rectangle region = Rectangle.Empty;
            if (noSubRegion > 0)
            {
                width = subRegion[0].Width; height = subRegion[0].Height;
                X = subRegion[0].X; Y = subRegion[0].Y;
                region = new Rectangle(new Point(X, Y), new Size(width*noSubRegion, height));
            }
            return region;

        }

    }

}
