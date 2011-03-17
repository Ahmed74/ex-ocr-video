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
    public class ConcatenateTwoImages
    {
        private Image<Gray, byte> resultImage;

        // This splitting only apply for detecting the text moving in the horizontal
        public void spliteImageIntoSubImages(Image<Gray, byte> image, Rectangle region, int fixWidth,
            List<Image<Gray, byte>> subImageList, List<Rectangle> subRegionList)
        {

            // split images into widthxHeight sub-images in the imge height 
            subImageList = new List<Image<Gray, byte>>();
            subRegionList = new List<Rectangle>();

            // get the information about the Image size, and its position on the original image
            int height, width;
            int x, y; 
            height = image.Height; width = image.Width; // the Image size
            x = region.X; y = region.Y;                     // its position on the actual image

            int mod = width % fixWidth;
            int div = width / fixWidth;
            int i;
            
            
            for (i = 0; i < div; i++)
            {                
                Rectangle subRegion = new Rectangle(new Point(x*i, y), new Size(fixWidth, height));  // to calculate the sub-Image position on the original image
                Rectangle rect = new Rectangle(new Point(i * fixWidth, 0), new Size(fixWidth, height));  // to calculate the region to cut the image
                subImageList.Add(image.Copy(rect));
                subRegionList.Add(subRegion);
            }
            if (mod != 0)
            {
                Rectangle subRegion = new Rectangle(new Point(x*i, y), new Size(width - i * fixWidth, height));
                Rectangle rect = new Rectangle(new Point(i * fixWidth, 0), new Size(width - i * fixWidth, height));
                Image<Gray, byte> temp = image.Copy(rect);
                subImageList.Add(image.Copy(rect));
                subRegionList.Add(subRegion);
            }            
        }        

        
        //public void FindMotionVector(Image<Gray, byte> subImge, Image<Gray, byte> originalImage, int shift)

    }
}
