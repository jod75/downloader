using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace Downloader
{
    public partial class MainForm : Form
    {
        private string destinationFolder = @"f:\temp";

        public MainForm()
        {
            InitializeComponent();

            destinationFolder = Properties.Settings.Default["DestinationLocation"].ToString();
            if (!Directory.Exists(destinationFolder))
                SaveDestinationFolder(Path.GetTempPath());
            
            DataGridViewProgressColumn column = new DataGridViewProgressColumn();

            dataGridView.ColumnCount = 1;
            dataGridView.Columns[0].Name = "URL";
            dataGridView.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;                        
            dataGridView.Columns.Add(column);
            dataGridView.Columns[1].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            column.HeaderText = "Progress";            
        }

        private void SaveDestinationFolder(string folder)
        {
            destinationFolder = folder;
            Properties.Settings.Default["DestinationLocation"] = destinationFolder;
            Properties.Settings.Default.Save();
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
                e.Effect = DragDropEffects.All;            
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                string[] droppedStrings = e.Data.GetData(DataFormats.Text).ToString().Split(
                    new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var droppedString in droppedStrings)
                {
                    try
                    {
                        var url = droppedString;
                        Trace.WriteLine(url);
                        if (!droppedString.StartsWith("http"))
                            url = $"http://{droppedString}"; // add http in case of dropped urls from browsers                        

                        if (CustomWebClient.RemoteUrlExists(url))
                        {
                            var uri = new Uri(url);
                            var filename = Path.GetFileName(uri.LocalPath);
                            if (string.IsNullOrWhiteSpace(filename))
                                filename = Path.ChangeExtension(Path.GetRandomFileName(), "html");

                            if (string.IsNullOrWhiteSpace(Path.GetExtension(filename)))
                                filename = Path.ChangeExtension(filename, "html");

                            filename = Path.Combine(destinationFolder, filename);

                            var rowIndex = dataGridView.Rows.Add(new object[] { url, 0 });
                            StartDownload(uri, filename, rowIndex);
                        }
                        else
                        {
                            dataGridView.Rows.Add(new object[] { $"Error: \"{url}\" is invalid.", 0 });                            
                        }
                    }
                    catch (Exception exx)
                    {
                        Trace.WriteLine($"Error: (exx) = {exx.ToString()}");                                                
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"Error: {ex.ToString()}");
            }
        }

        private void StartDownload(Uri uri, string filename, int rowIndex)
        {
            Thread thread = new Thread(() => {
                CustomWebClient client = new CustomWebClient
                {
                    XRowIndex = rowIndex,
                    XFileName = filename
                };
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(Client_DownloadProgressChanged);
                client.DownloadFileCompleted += new AsyncCompletedEventHandler(Client_DownloadFileCompleted);                
                client.DownloadFileAsync(uri, filename);
            });
            thread.Start();
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var customWebClient = sender as CustomWebClient;

            if (customWebClient != null) {
                this.BeginInvoke((MethodInvoker)delegate {
                double bytesIn = double.Parse(e.BytesReceived.ToString());
                double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
                    double percentage = Math.Max(bytesIn / totalBytes * 100, 0); // adjust case when bytes to receive is -1, the percentage = 0
                    dataGridView[0, customWebClient.XRowIndex].Value = $"Downloaded {e.BytesReceived}{(e.TotalBytesToReceive > 0 ? " of " + e.TotalBytesToReceive : "bytes")}";                        
                    dataGridView[1, customWebClient.XRowIndex].Value = int.Parse(Math.Truncate(percentage).ToString());
                });
            }
        }
        private void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var customWebClient = sender as CustomWebClient;

            if (customWebClient != null)
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    dataGridView[0, customWebClient.XRowIndex].Value = customWebClient.XFileName;
                    dataGridView[1, customWebClient.XRowIndex].Value = 100;                    
                });
            }
        }

        private void DataGridView_CellContentDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            var filename = dataGridView[e.ColumnIndex, e.RowIndex].Value.ToString();            

            if (!File.Exists(filename))
            {
                // try next bet, directory
                var dir = Path.GetDirectoryName(filename);
                if (Directory.Exists(dir))
                    filename = dir;
                else
                    filename = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(filename))
                Process.Start("explorer.exe");
            else
                Process.Start("explorer.exe", $"/select, \"{filename}\"");
        }       

        private void DestinationFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var folderDialog = new FolderBrowserDialog
            {
                SelectedPath = destinationFolder,
                Description = "Select destination location",
                ShowNewFolderButton = true
            };

            if (folderDialog.ShowDialog() == DialogResult.OK)
                SaveDestinationFolder(folderDialog.SelectedPath);
        }
    }
}
