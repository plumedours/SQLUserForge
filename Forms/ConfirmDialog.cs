using System;
using System.Drawing;
using System.Windows.Forms;
using SQLUserForge.Models;
using SQLUserForge.Services;

namespace SQLUserForge.Forms
{
    public class ConfirmDialog : Form
    {
        private readonly Label _lblSummary;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;

        public ConfirmDialog(UserRequest request)
        {
            Text = TranslationProvider.T("confirm_title");
            StartPosition = FormStartPosition.CenterParent;
            this.Icon = Properties.Resources.AppIcon;
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Width = 620;
            Height = 460;

            _lblSummary = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 20,
                Width = ClientSize.Width - 40,
                Height = ClientSize.Height - 100,
                Text = request.ToString(),
                Font = new Font("Segoe UI", 10F),
            };
            Controls.Add(_lblSummary);

            _btnOk = new Button
            {
                Text = TranslationProvider.T("confirm_validate"),
                DialogResult = DialogResult.OK,
                Width = 120,
                Height = 34,
                Left = ClientSize.Width - 280,
                Top = ClientSize.Height - 60
            };
            Controls.Add(_btnOk);

            _btnCancel = new Button
            {
                Text = TranslationProvider.T("confirm_cancel"),
                DialogResult = DialogResult.Cancel,
                Width = 120,
                Height = 34,
                Left = ClientSize.Width - 140,
                Top = ClientSize.Height - 60
            };
            Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }
    }
}