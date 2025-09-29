using System.Drawing;
using System.Windows.Forms;
using SQLUserForge.Services;

namespace SQLUserForge.Forms
{
    public class LoadingDialog : Form
    {
        public LoadingDialog(string? message = null)
        {
            Text = TranslationProvider.T("about_title"); // titre neutre ; ou "Veuillez patienter"
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            ShowInTaskbar = false;
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.White;
            Width = 380;
            Height = 140;
            this.Icon = Properties.Resources.AppIcon;

            var lbl = new Label
            {
                AutoSize = false,
                Text = message ?? TranslationProvider.T("loading_discovering"),
                Left = 16,
                Top = 16,
                Width = ClientSize.Width - 32,
                Height = 28
            };
            Controls.Add(lbl);

            var bar = new ProgressBar
            {
                Left = 16,
                Width = ClientSize.Width - 32,
                Top = 56,
                Height = 20,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 25
            };
            Controls.Add(bar);
        }
    }
}