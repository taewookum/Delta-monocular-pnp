using System;
using System.Drawing;
using System.Windows.Forms;

namespace Protocol_Test
{
    internal sealed class GripImagePreviewControl : UserControl
    {
        private readonly PictureBox yoloPictureBox;
        private readonly PictureBox depthPictureBox;
        private readonly Label yoloMissingLabel;
        private readonly Label depthMissingLabel;
        private readonly Font titleFont;

        public GripImagePreviewControl()
        {
            Size = new Size(620, 256);
            MinimumSize = new Size(500, 220);
            BackColor = Color.White;
            titleFont = new Font(
                SystemFonts.MessageBoxFont,
                FontStyle.Bold);

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Padding = new Padding(8),
                BackColor = Color.White
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            layout.Controls.Add(CreateTitleLabel("YOLO", titleFont), 0, 0);
            layout.Controls.Add(CreateTitleLabel("Depth (magma_r)", titleFont), 1, 0);

            Panel yoloPanel;
            yoloPictureBox = CreateImagePanel(out yoloPanel, out yoloMissingLabel);
            Panel depthPanel;
            depthPictureBox = CreateImagePanel(out depthPanel, out depthMissingLabel);

            layout.Controls.Add(yoloPanel, 0, 1);
            layout.Controls.Add(depthPanel, 1, 1);
            Controls.Add(layout);
        }

        internal bool YoloImageMissing
        {
            get
            {
                return yoloPictureBox.Image == null &&
                    yoloMissingLabel.Visible;
            }
        }

        internal bool DepthImageMissing
        {
            get
            {
                return depthPictureBox.Image == null &&
                    depthMissingLabel.Visible;
            }
        }

        internal void SetImages(Bitmap yoloImage, Bitmap depthImage)
        {
            if (InvokeRequired)
            {
                throw new InvalidOperationException(
                    "Grip preview images must be updated on the UI thread.");
            }

            ReplaceImage(yoloPictureBox, yoloMissingLabel, yoloImage);
            ReplaceImage(depthPictureBox, depthMissingLabel, depthImage);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposePictureBoxImage(yoloPictureBox);
                DisposePictureBoxImage(depthPictureBox);
            }

            base.Dispose(disposing);
            if (disposing)
            {
                titleFont.Dispose();
            }
        }

        private static Label CreateTitleLabel(string text, Font font)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = font,
                ForeColor = Color.Black,
                BackColor = Color.White,
                Margin = new Padding(4, 0, 4, 4)
            };
        }

        private static PictureBox CreateImagePanel(
            out Panel panel,
            out Label missingLabel)
        {
            panel = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4),
                BackColor = Color.FromArgb(32, 32, 32),
                BorderStyle = BorderStyle.FixedSingle
            };

            PictureBox pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Black
            };

            missingLabel = new Label
            {
                Text = "No image",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Gainsboro,
                BackColor = Color.FromArgb(32, 32, 32),
                Visible = true
            };

            panel.Controls.Add(pictureBox);
            panel.Controls.Add(missingLabel);
            missingLabel.BringToFront();
            return pictureBox;
        }

        private static void ReplaceImage(
            PictureBox pictureBox,
            Label missingLabel,
            Bitmap replacement)
        {
            Image previous = pictureBox.Image;
            pictureBox.Image = replacement;
            missingLabel.Visible = replacement == null;
            if (replacement == null)
            {
                missingLabel.BringToFront();
            }

            if (previous != null && !ReferenceEquals(previous, replacement))
            {
                previous.Dispose();
            }
        }

        private static void DisposePictureBoxImage(PictureBox pictureBox)
        {
            if (pictureBox == null)
            {
                return;
            }

            Image image = pictureBox.Image;
            pictureBox.Image = null;
            if (image != null)
            {
                image.Dispose();
            }
        }
    }
}
