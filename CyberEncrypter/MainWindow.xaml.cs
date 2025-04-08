using CyberEncrypter.View;
using Microsoft.Win32;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace CyberEncrypter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetSizeAndPositionOnPrimaryScreen();
            DashboardContent.Content = new Dashboard();

        }

        private void SetSizeAndPositionOnPrimaryScreen()
        {
     
            var primaryScreen = System.Windows.SystemParameters.WorkArea;
            
            
            double widthRatio = 0.75;
            double heightRatio = 0.75;
            
            this.Width = primaryScreen.Width * widthRatio;
            this.Height = primaryScreen.Height * heightRatio;
         
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                this.WindowState = System.Windows.WindowState.Maximized;
            }
            else
            {
                this.WindowState = System.Windows.WindowState.Normal;
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            DashboardContent.Content = new Dashboard();
        }

        private void Encryptfiles_Click(object sender, RoutedEventArgs e)
        {
            DashboardContent.Content = new Encrypt();
        }

        private void Decrypt_Click(object sender, RoutedEventArgs e)
        {
            DashboardContent.Content = new Decrypt();
        }
    }
}