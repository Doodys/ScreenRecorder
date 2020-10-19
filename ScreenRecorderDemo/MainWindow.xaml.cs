using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace ScreenRecorderDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool folderSelected = false;
        string outputPath = string.Empty;

        ScreenRecorder recorder = new ScreenRecorder(new System.Drawing.Rectangle(), string.Empty);
        System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            foreach (var screen in Screen.AllScreens)
                screenCmbo.Items.Add(screen.DeviceName);

            screenCmbo.SelectedIndex = 0;
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            recorder.RecordAudio();
            recorder.RecordVideo();

            lblTime.Content = recorder.GetElapsed();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowser = new FolderBrowserDialog();
            folderBrowser.Description = "Select output folder";

            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var s = Screen.AllScreens.First(ss => ss.DeviceName.Equals(screenCmbo.SelectedItem));
                outputPath = folderBrowser.SelectedPath;
                folderSelected = true;

                System.Drawing.Rectangle bounds = s.WorkingArea; //implement screen selection

                recorder = new ScreenRecorder(bounds, outputPath);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            recorder.CreateFolderIfNotExists();

            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);

            if (folderSelected)
                dispatcherTimer.Start();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
            recorder.Stop();

            System.Windows.Forms.Application.Restart();
            System.Windows.Application.Current.Shutdown();
        }
    }
}
