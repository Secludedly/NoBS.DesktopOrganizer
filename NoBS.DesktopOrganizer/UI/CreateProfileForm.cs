using NoBS.Core.Profiles;
using System.Drawing;
using System.Windows.Forms;

namespace NoBS.DesktopOrganizer.UI
{
    public class CreateProfileForm : Form
    {
        private TextBox txtName;
        private Button btnCreate;

        public WorkspaceProfile CreatedProfile { get; private set; } = null!;

        public CreateProfileForm()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            Text = "Create Profile";
            Width = 300;
            Height = 150;
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            txtName = new TextBox
            {
                Left = 20,
                Top = 20,
                Width = 240
            };

            btnCreate = new Button
            {
                Text = "Create",
                Left = 180,
                Top = 60,
                Width = 80,
                Height = 32, // IMPORTANT
                FlatStyle = FlatStyle.Flat
            };

            btnCreate.Click += CreateClicked;

            Controls.Add(txtName);
            Controls.Add(btnCreate);
        }

        private void CreateClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
                return;

            CreatedProfile = new WorkspaceProfile
            {
                Name = txtName.Text.Trim()
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
