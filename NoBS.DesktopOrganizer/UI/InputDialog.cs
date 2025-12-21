using System.Drawing;
using System.Windows.Forms;

namespace NoBS.DesktopOrganizer.UI
{
    public class InputDialog : Form
    {
        private TextBox txtValue;
        private Button btnOk;
        private Button btnCancel;

        public string Value => txtValue.Text;

        public InputDialog(string title, string label, string defaultValue = "")
        {
            AutoScaleMode = AutoScaleMode.Dpi;

            Text = title;
            Width = 360;
            Height = 160;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(24, 24, 28);
            ForeColor = Color.White;

            var lbl = new Label
            {
                Text = label,
                Left = 12,
                Top = 15,
                Width = 320
            };

            txtValue = new TextBox
            {
                Left = 12,
                Top = 40,
                Width = 320,
                Text = defaultValue
            };

            btnOk = new Button
            {
                Text = "OK",
                Left = 172,
                Top = 80,
                Width = 75,
                Height = 32,
                DialogResult = DialogResult.OK
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Left = 257,
                Top = 80,
                Width = 75,
                Height = 32,
                DialogResult = DialogResult.Cancel
            };

            Controls.AddRange(new Control[] { lbl, txtValue, btnOk, btnCancel });

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }
}
