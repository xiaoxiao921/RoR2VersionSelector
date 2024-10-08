using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RoR2VersionSelector
{
    public partial class RoR2VersionSelector : Form
    {
        private Process DepotDownloaderProcess;

        private string[] AllRoR2VersionsStringSorted;

        public RoR2VersionSelector()
        {
            InitializeComponent();
        }

        private bool IsValidDownloadedDepot(string folderName)
        {
            if (!Directory.Exists(folderName))
            {
                return false;
            }

            foreach (var filePath in Directory.EnumerateFiles(folderName, "Risk of Rain 2.exe", SearchOption.AllDirectories))
            {
                return true;
            }

            return false;
        }

        private List<string> GetRiskofRain2Folders(string rootPath)
        {
            var res = new List<string>();

            foreach (var filePath in Directory.EnumerateFiles(rootPath, "Risk of Rain 2.exe", SearchOption.AllDirectories))
            {
                res.Add(Path.GetDirectoryName(filePath));
            }

            return res;
        }

        private void RoR2VersionSelector_Load(object sender, EventArgs e)
        {
            new Thread(() =>
            {
                while (true)
                {
                    ButtonCopyDownloadedVersionToSteamInstall.InvokeIfRequired((ctrl) =>
                    {
                        ctrl.Enabled = IsValidDownloadedDepot("./depots");
                    });

                    Thread.Sleep(3000);
                }
            }).Start();

            AllRoR2VersionsStringSorted = Enum.GetNames(typeof(RoR2Versions));
            Array.Sort(AllRoR2VersionsStringSorted);
            ComboBoxVersionSelector.DataSource = AllRoR2VersionsStringSorted;

            TextBoxDepotDownloaderResult.ReadOnly = true;

            TextBoxDepotDownloaderResult.Text = "Download the version of your choice through the interface above." +
                Environment.NewLine +
                "You may need to enter a Steam Guard code for DepotDownloader to download the version correctly." +
                Environment.NewLine +
                "Once manifest depot download has reached 100%:" +
                Environment.NewLine +
                "If you wish to use this version via Steam / r2modman / Thunderstore Mod Manager:" +
                Environment.NewLine +
                "Press the Copy Downloaded Version To Steam Install button," +
                Environment.NewLine +
                "A File dialog box will then open, allowing you to select the folder to which the ror2 version just downloaded is to be copied." +
                Environment.NewLine +
                "Otherwise, you can find the downloaded depots here: " + Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "depots");
        }

        private async void ButtonDownloadDepot_Click(object sender, EventArgs e)
        {
            ButtonDownloadDepot.Enabled = false;

            var allRoR2VersionsStringNotSorted = Enum.GetNames(typeof(RoR2Versions));
            var ror2Versions = (RoR2Versions[])Enum.GetValues(typeof(RoR2Versions));
            long manifestId = 0;
            for (var i = 0; i < AllRoR2VersionsStringSorted.Length; i++)
            {
                if (allRoR2VersionsStringNotSorted[i] == AllRoR2VersionsStringSorted[ComboBoxVersionSelector.SelectedIndex])
                {
                    manifestId = (long)ror2Versions[i];
                }
            }

            var manifestArg = manifestId != -1 ? $" -manifest {manifestId} " : " ";
            var fileListArg = " ";
            if (CheckBoxDownloadOnlyDLLFiles.Checked)
            {
                var dllOnlyFilePath = "dllonly.txt";
                fileListArg = $" -filelist {dllOnlyFilePath} ";

                if (!File.Exists(dllOnlyFilePath))
                {
                    File.WriteAllText(dllOnlyFilePath, @"regex:.*\.dll$");
                }
            }

            var outputfolderPath = Path.Combine("./depots", AllRoR2VersionsStringSorted[ComboBoxVersionSelector.SelectedIndex]);

            var startInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/k \".\\DepotDownloader.exe " +
                $"-app 632360 " +
                $"-depot 632361 " +
                $"{manifestArg} " +
                $"-username {TextBoxUsername.Text} " +
                $"-password {TextBoxPassword.Text} " +
                $"{fileListArg} " +
                $"-dir {outputfolderPath}\"",
                UseShellExecute = true
            };

            try
            {
                DepotDownloaderProcess = new Process { StartInfo = startInfo };

                DepotDownloaderProcess.Start();

                DepotDownloaderProcess.WaitForExit();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
            finally
            {
                ButtonDownloadDepot.Enabled = true;
            }
        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath)
        {
            foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }

        private void ButtonCopyDownloadedVersionToSteamInstall_Click(object sender, EventArgs e)
        {
            var sourceFolders = GetRiskofRain2Folders("./depots");
            if (sourceFolders.Count <= 0)
            {
                MessageBox.Show("Folder copy operation invalid.", "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var fileDialog = new FileSelectionDialog(sourceFolders);
            if (fileDialog.ShowDialog() != DialogResult.OK)
            {
                MessageBox.Show("Folder copy operation invalid (2).", "Invalid", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var openFileDialog = new OpenFileDialog();
            // Set to filter out all files, but allow folder selection
            openFileDialog.Filter = "Folders|*.none";
            openFileDialog.CheckFileExists = false;
            openFileDialog.CheckPathExists = true;
            openFileDialog.FileName = "Risk of Rain 2 Folder";

            openFileDialog.Title = "Select the destination folder where the depot will be copied to.";

            const string defaultSteamRoR2InstallFolderPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Risk of Rain 2";
            if (Directory.Exists(defaultSteamRoR2InstallFolderPath))
            {
                openFileDialog.InitialDirectory = defaultSteamRoR2InstallFolderPath;
            }

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                var selectedFolderPath = Path.GetDirectoryName(openFileDialog.FileName);

                // Get the selected file path
                string selectedFilePath = fileDialog.SelectedFilePath;

                // Prompt the user for confirmation
                var result = MessageBox.Show(
                    $"Do you want to copy the downloaded RoR2 Version from\n\n{selectedFilePath}\n\nto\n\n{selectedFolderPath}",
                    "Confirm Folder Copy",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    var destinationFolder = selectedFolderPath;

                    try
                    {
                        CopyFilesRecursively(selectedFilePath, destinationFolder);

                        MessageBox.Show("Folder copied successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error copying folder: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Folder copy operation canceled.", "Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("No destination folder selected.", "Operation Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
