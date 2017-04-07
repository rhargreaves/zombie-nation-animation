using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ZombieNation
{
    class VidUtil
    {
        public static RenderTargetBitmap SaveCanvas(Window window, Canvas canvas, int dpi)
        {
           // Size size = new Size(window.Width, window.Height);
           // canvas.Measure(size);
            //canvas.Arrange(new Rect(size));

            var rtb = new RenderTargetBitmap(
                (int)canvas.ActualWidth, //width
                (int)canvas.ActualHeight, //height
                dpi, //dpi x
                dpi, //dpi y 
                PixelFormats.Pbgra32 // pixelformat
                );
             rtb.Render(canvas);
             return rtb;
         //   SaveRTBAsPNG(rtb, filename);
        }

        //public static void SaveRTBAsPNG(RenderTargetBitmap bmp, string filename)
        //{
        //    var enc = new System.Windows.Media.Imaging.PngBitmapEncoder();
        //    enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));
        //    using (var stm = System.IO.File.Create(filename))
        //    {
        //        enc.Save(stm);
        //    }
        //}

        public static void SaveRTBAsPNG(RenderTargetBitmap bmp, Stream stream)
        {
            var enc = new System.Windows.Media.Imaging.JpegBitmapEncoder();
            enc.QualityLevel = 100;
            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));
            //using (var stm = System.IO.File.Create(filename))
            //{
            enc.Save(stream);
            //}      
        }
    }
}
