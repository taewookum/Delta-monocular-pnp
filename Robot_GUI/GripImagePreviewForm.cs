using System.Drawing;
using System.Windows.Forms;

namespace Protocol_Test
{
    internal sealed class GripImagePreviewForm : Form
    {
        private readonly GripImagePreviewControl previewControl;

        internal GripImagePreviewForm()
        {
            Text = "Grip Image Preview";
            ClientSize = new Size(620, 256);
            MinimumSize = new Size(520, 260);
            StartPosition = FormStartPosition.CenterScreen;
            ShowInTaskbar = true;

            previewControl = new GripImagePreviewControl
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(previewControl);
        }

        internal void SetImages(Bitmap yoloImage, Bitmap depthImage)
        {
            previewControl.SetImages(yoloImage, depthImage);
        }
    }
}
