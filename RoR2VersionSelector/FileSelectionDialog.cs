using System.Collections.Generic;
using System.Windows.Forms;
using System;

namespace RoR2VersionSelector
{
    public partial class FileSelectionDialog : Form
    {
        public string SelectedFilePath { get; private set; }

        public FileSelectionDialog(List<string> filePaths)
        {
            InitializeComponent();

            // Populate the ListBox with file paths
            foreach (var path in filePaths)
            {
                listBox1.Items.Add(path);
            }
        }

        private void ButtonOk_Click(object sender, EventArgs e)
        {
            // Get the selected file path
            SelectedFilePath = listBox1.SelectedItem?.ToString();

            // Close the dialog with OK result
            if (SelectedFilePath != null)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select a file.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            // Close the dialog with Cancel result
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }

}

