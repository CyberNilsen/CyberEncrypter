using Microsoft.Win32;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace CyberEncrypter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            SetSizeAndPositionOnPrimaryScreen();
        }

        public class EncryptionKey
        {
            private byte[] _key;

            public void EncryptionService(string key)
            { 
                using var sha256 = SHA256.Create();

                _key = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                Console.WriteLine($"Generated Key:{Convert.ToHexString(_key)}");
            }
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

        private void Encryptfile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.DefaultExt = ".txt";

            bool? result = op.ShowDialog();
            
        }
    }
}