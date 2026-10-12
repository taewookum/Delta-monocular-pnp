using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Protocol_Test
{
    internal sealed class GripImageCaptureStore
    {
        private const int Margin = 8;
        private const int ColumnWidth = CameraResponseProtocol.ExpectedImageWidth;
        private const int TitleHeight = 34;
        private const int ImageHeight = CameraResponseProtocol.ExpectedImageHeight;
        private const int ColumnGap = 8;
        public const int CompositeWidth =
            Margin + ColumnWidth + ColumnGap + ColumnWidth + Margin;
        public const int CompositeHeight =
            Margin + TitleHeight + ImageHeight + Margin;
        private int captureSequence;

        public Task<string> SaveCompositeAsync(
            Bitmap yoloImage,
            Bitmap depthImage,
            DateTime capturedAt)
        {
            // Render synchronously while the caller still owns the source
            // Bitmaps. The disk write then uses an independent Bitmap.
            Bitmap composite = RenderComposite(yoloImage, depthImage);
            int sequence = Interlocked.Increment(ref captureSequence);
            string directory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "GripImageCaptures",
                capturedAt.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
            string fileName = string.Format(
                CultureInfo.InvariantCulture,
                "GripImage_{0}_{1:D6}.png",
                capturedAt.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture),
                sequence);
            string path = Path.Combine(directory, fileName);

            return Task.Run(() =>
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    using (FileStream output = new FileStream(
                        path,
                        FileMode.CreateNew,
                        FileAccess.Write,
                        FileShare.Read))
                    {
                        composite.Save(output, ImageFormat.Png);
                    }

                    return path;
                }
                finally
                {
                    composite.Dispose();
                }
            });
        }

        internal static Bitmap RenderComposite(
            Bitmap yoloImage,
            Bitmap depthImage)
        {
            Bitmap composite = new Bitmap(
                CompositeWidth,
                CompositeHeight,
                PixelFormat.Format24bppRgb);
            try
            {
                using (Graphics graphics = Graphics.FromImage(composite))
                using (Font titleFont = new Font(
                    SystemFonts.MessageBoxFont,
                    FontStyle.Bold))
                using (Font missingFont = new Font(SystemFonts.MessageBoxFont, FontStyle.Regular))
                using (StringFormat centered = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                {
                    graphics.Clear(Color.White);

                    Rectangle yoloTitle = new Rectangle(
                        Margin,
                        Margin,
                        ColumnWidth,
                        TitleHeight);
                    Rectangle depthTitle = new Rectangle(
                        Margin + ColumnWidth + ColumnGap,
                        Margin,
                        ColumnWidth,
                        TitleHeight);
                    using (Brush titleBrush = new SolidBrush(Color.Black))
                    {
                        graphics.DrawString(
                            "YOLO",
                            titleFont,
                            titleBrush,
                            yoloTitle,
                            centered);
                        graphics.DrawString(
                            "Depth (magma_r)",
                            titleFont,
                            titleBrush,
                            depthTitle,
                            centered);
                    }

                    Rectangle yoloBounds = new Rectangle(
                        Margin,
                        Margin + TitleHeight,
                        ColumnWidth,
                        ImageHeight);
                    Rectangle depthBounds = new Rectangle(
                        Margin + ColumnWidth + ColumnGap,
                        Margin + TitleHeight,
                        ColumnWidth,
                        ImageHeight);
                    DrawImageOrMissing(
                        graphics,
                        yoloImage,
                        yoloBounds,
                        missingFont,
                        centered);
                    DrawImageOrMissing(
                        graphics,
                        depthImage,
                        depthBounds,
                        missingFont,
                        centered);
                }

                return composite;
            }
            catch
            {
                composite.Dispose();
                throw;
            }
        }

        private static void DrawImageOrMissing(
            Graphics graphics,
            Bitmap image,
            Rectangle bounds,
            Font missingFont,
            StringFormat centered)
        {
            if (image == null)
            {
                using (Brush background = new SolidBrush(Color.FromArgb(32, 32, 32)))
                using (Brush foreground = new SolidBrush(Color.Gainsboro))
                {
                    graphics.FillRectangle(background, bounds);
                    graphics.DrawString(
                        "No image",
                        missingFont,
                        foreground,
                        bounds,
                        centered);
                }
            }
            else
            {
                graphics.DrawImage(
                    image,
                    bounds,
                    0,
                    0,
                    image.Width,
                    image.Height,
                    GraphicsUnit.Pixel);
            }

            graphics.DrawRectangle(
                Pens.Black,
                bounds.X,
                bounds.Y,
                bounds.Width - 1,
                bounds.Height - 1);
        }
    }
}
