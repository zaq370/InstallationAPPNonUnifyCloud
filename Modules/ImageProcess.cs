using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;

namespace InstallationAPPNonUnify.Modules
{
    public class ImageProcess
    {
        public byte[] DownSizingImage(byte[] data, int targetWidth = 1600, int targetHeight = 1200)
        {
            double ratio = 1;
            using (var ms = new MemoryStream(data))
            {
                try
                {
                    var oriImage = Image.FromStream(ms);
                    var result = GetOrientation(oriImage);

                    //有錯誤，不再轉，直接回傳
                    if (!result.Item1) return data;

                    var image = result.Item2;
                    if (image.Width > targetWidth)
                    {
                        ratio = (double)targetWidth / (double)image.Width;
                    }
                    else if (image.Height > targetHeight)
                    {
                        ratio = (double)targetHeight / (double)image.Height;
                    }

                    targetWidth = (int)((double)image.Width * ratio);
                    targetHeight = (int)((double)image.Height * ratio);

                    if (targetWidth < 1 || targetHeight < 1) return data;

                    var newImage = new Bitmap(image, new Size(targetWidth, targetHeight));

                    MemoryStream returnMs = new MemoryStream();
                    newImage.Save(returnMs, ImageFormat.Jpeg);

                    return returnMs.ToArray();
                }
                catch (Exception ex)
                {
                    //Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                }
                return data;
            }
        }

        public Tuple<bool,Image> GetOrientation(Image image)
        {
            try
            {
                var orientationId = image.GetPropertyItem(0x0112);
                var imageOrientation = BitConverter.ToInt16(orientationId.Value, 0);
                var rotateFlip = RotateFlipType.RotateNoneFlipNone;
                switch (imageOrientation)
                {
                    case 1: rotateFlip = RotateFlipType.RotateNoneFlipNone; break;
                    case 2: rotateFlip = RotateFlipType.RotateNoneFlipX; break;
                    case 3: rotateFlip = RotateFlipType.Rotate180FlipNone; break;
                    case 4: rotateFlip = RotateFlipType.Rotate180FlipX; break;
                    case 5: rotateFlip = RotateFlipType.Rotate90FlipX; break;
                    case 6: rotateFlip = RotateFlipType.Rotate90FlipNone; break;
                    case 7: rotateFlip = RotateFlipType.Rotate270FlipX; break;
                    case 8: rotateFlip = RotateFlipType.Rotate270FlipNone; break;
                    default: rotateFlip = RotateFlipType.RotateNoneFlipNone; break;
                }
                image.RotateFlip(rotateFlip);
            }
            catch (ArgumentException ae)
            {
                //這種不記錄
                return new Tuple<bool, Image>(true, image);
            }
            catch (Exception ex)
            {
                Elmah.ErrorSignal.FromCurrentContext().Raise(ex);
                return new Tuple<bool, Image>(false, image);
            }
            return new Tuple<bool, Image>(true, image);
        }
    }
}