using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FileDownloader
{
    /// <summary>
    /// Interaction logic for DownloadProgressWindow.xaml
    /// </summary>
    public partial class DownloadProgressWindow : Window
    {
        public event Action CancelDownload;

        public DownloadProgressWindow()
        {
            InitializeComponent();
        }

        public void UpdateProgress(double progress)
        {
            progressBar.Value = progress;
            progressText.Text = $"{progress:F2}%";
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CancelDownload?.Invoke();
        }
    }
}
