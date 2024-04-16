using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FileDownloader
{
    public partial class MainWindow : Window
    {
        private HttpClient httpClient;
        private string selectedFile;
        private StackPanel selectedPanel;
        private string selectedFolderPath = @"C:\Downloads\";
        private CancellationTokenSource cancellationTokenSource;
        private DownloadProgressWindow downloadProgressWindow;

        public MainWindow()
        {
            InitializeComponent();

            httpClient = new HttpClient();

            RefreshFileIcons();
        }

        private async void StartDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            string url = downloadUrlTextBox.Text;
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Please provide a download URL.");
                return;
            }

            downloadProgressWindow = new DownloadProgressWindow();
            downloadProgressWindow.CancelDownload += CancelDownload;
            downloadProgressWindow.Show();

            string savePath = selectedFolderPath;
            string fileName = Path.GetFileName(url);
            string filePath = Path.Combine(savePath, fileName);

            try
            {
                cancellationTokenSource = new CancellationTokenSource();
                await DownloadFile(url, filePath, cancellationTokenSource.Token);
                RefreshFileIcons();
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Downloading operation was cancelled.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error occured: {ex.Message}");
            }
        }

        private async Task DownloadFile(string url, string filePath, CancellationToken cancellationToken)
        {
            try
            {
                using (HttpResponseMessage response = await httpClient.GetAsync(
                    url, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
                {
                    response.EnsureSuccessStatusCode();
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync())
                    {
                        string directory = Path.GetDirectoryName(filePath);
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        string extension = Path.GetExtension(filePath);

                        int count = 1;
                        while (File.Exists(filePath))
                        {
                            string tempFileName = string.Format("{0}({1})", fileName, count++);
                            filePath = Path.Combine(directory, tempFileName + extension);
                        }

                        using (FileStream fileStream = new FileStream(
                            filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var totalBytes = response.Content.Headers.ContentLength.GetValueOrDefault();
                            var buffer = new byte[8192];
                            var bytesRead = await contentStream.ReadAsync(
                                buffer, 0, buffer.Length, cancellationToken);
                            var totalBytesRead = bytesRead;

                            while (bytesRead > 0)
                            {
                                await fileStream.WriteAsync(
                                    buffer, 0, bytesRead, cancellationToken);

                                double progress = (double)totalBytesRead / totalBytes * 100;
                                downloadProgressWindow.UpdateProgress(progress);

                                bytesRead = await contentStream.ReadAsync(
                                    buffer, 0, buffer.Length, cancellationToken);
                                totalBytesRead += bytesRead;
                            }
                        }
                        downloadProgressWindow.Close();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (File.Exists(filePath))
                {
                    try
                    {
                        File.Delete(filePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting file {filePath}: {ex.Message}");
                    }
                }
                throw;
            }
        }

        private void CancelDownload()
        {
            CancelDownloadButton_Click(this, new RoutedEventArgs());

            if (downloadProgressWindow != null)
            {
                downloadProgressWindow.Close();
                downloadProgressWindow = null;
            }
            if (downloadProgressWindow == null)
            {

            }
        }

        private void CancelDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource?.Cancel();
        }

        private void DeleteFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(selectedFile))
            {
                try
                {
                    File.Delete(selectedFile);
                    RefreshFileIcons();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting file {selectedFile}: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show($"The file {selectedFile} does not exist.");
            }
        }

        private void RenameFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(selectedFile))
            {
                string newFileName = Microsoft.VisualBasic.Interaction.InputBox(
                    "Enter new file name:", "Rename File", Path.GetFileName(selectedFile));
                if (!string.IsNullOrEmpty(newFileName))
                {
                    string newFilePath = Path.Combine(Path.GetDirectoryName(selectedFile), newFileName);
                    try
                    {
                        File.Move(selectedFile, newFilePath);
                        RefreshFileIcons();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error renaming file {selectedFile}: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show($"The file {selectedFile} does not exist.");
            }
        }

        private void ChooseFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.ValidateNames = false;
            dialog.CheckFileExists = false;
            dialog.CheckPathExists = true;
            dialog.FileName = "Folder Selection.";

            if (dialog.ShowDialog() == true)
            {
                selectedFolderPath = Path.GetDirectoryName(dialog.FileName);
                RefreshFileIcons();
            }
        }

        private void RefreshFileIcons()
        {
            if (!string.IsNullOrWhiteSpace(selectedFolderPath))
            {
                fileCanvas.Children.Clear();
                var files = Directory.GetFiles(selectedFolderPath);
                double posX = 10;
                double posY = 10;

                int columns = 5;
                double spacing = 10;

                for (int i = 0; i < files.Length; i++)
                {
                    string file = files[i];
                    var icon = System.Drawing.Icon.ExtractAssociatedIcon(file);
                    if (icon != null)
                    {
                        Image image = new Image
                        {
                            Source = ConvertIconToImageSource(icon),
                            Width = 32,
                            Height = 32,
                            Tag = file
                        };

                        TextBlock textBlock = new TextBlock
                        {
                            Text = Path.GetFileName(file),
                            Width = 100,
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center
                        };

                        StackPanel stackPanel = new StackPanel
                        {
                            Orientation = Orientation.Vertical,
                            Width = 100,
                            Height = 80,
                            Style = (Style)FindResource("file")
                        };
                        stackPanel.MouseLeftButtonDown += StackPanel_MouseLeftButtonDown;
                        stackPanel.Children.Add(image);
                        stackPanel.Children.Add(textBlock);

                        int row = i / columns;
                        int column = i % columns;

                        double posX2 = column * (stackPanel.Width + spacing);
                        double posY2 = row * (stackPanel.Height + spacing * 2);

                        Canvas.SetLeft(stackPanel, posX2);
                        Canvas.SetTop(stackPanel, posY2);
                        fileCanvas.Children.Add(stackPanel);
                    }
                }
            }
        }

        private void StackPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StackPanel panel = (StackPanel)sender;
            Image image = (Image)panel.Children.OfType<Image>().FirstOrDefault();

            if (selectedPanel != null)
                selectedPanel.Background = new SolidColorBrush(Colors.Transparent);

            panel.Background = new SolidColorBrush(Colors.LightBlue);

            selectedPanel = panel;

            if (image != null)
                selectedFile = (string)image.Tag;
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(selectedFile))
            {
                try
                {
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(selectedFile) {
                            UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file {selectedFile}: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show($"The file {selectedFile} does not exist.");
            }
        }

        private BitmapSource ConvertIconToImageSource(System.Drawing.Icon icon)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                icon.ToBitmap().Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
        }
    }
}
