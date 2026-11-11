using System.IO;
using System.Windows.Forms;

namespace SoftSolute.Plugin.Installer.InstallerForms;
public class PathInputForm : Form
{
    private TextBox txtPath;
    private Button btnOk;
    private Button btnBrowse;
    private Label lblMessage;

    public string SelectedPath { get; private set; }

    public PathInputForm(string message)
    {
        this.Text = "Path Required";
        this.Width = 500;
        this.Height = 160;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        lblMessage = new Label { Text = message, Left = 10, Top = 15, Width = 460 };
        txtPath = new TextBox { Left = 10, Top = 40, Width = 360 };
        btnBrowse = new Button { Text = "Browse...", Left = 380, Top = 38, Width = 80 };
        btnOk = new Button { Text = "OK", Left = 380, Top = 80, Width = 80, Enabled = false };

        btnBrowse.Click += (s, e) =>
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the Security Center folder";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = dialog.SelectedPath;
                    btnOk.Enabled = Directory.Exists(txtPath.Text);
                }
            }
        };

        btnOk.Click += (s, e) =>
        {
            if (!Directory.Exists(txtPath.Text))
            {
                MessageBox.Show("The path does not exist. Please enter another one.",
                                "Invalid Path",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }

            if (!txtPath.Text.Contains(AppConstants.TARGET_VERSION))
            {
                MessageBox.Show($"The folder must contain version {AppConstants.TARGET_VERSION}.",
                                "Invalid Version",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                return;
            }


            SelectedPath = txtPath.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        };

        this.FormClosing += (s, e) =>
        {
            if (this.DialogResult != DialogResult.OK)
            {
                e.Cancel = true;
                MessageBox.Show("You must select a valid folder before continuing.",
                                "Path Required",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
        };

        this.Controls.Add(lblMessage);
        this.Controls.Add(txtPath);
        this.Controls.Add(btnBrowse);
        this.Controls.Add(btnOk);
    }
}
