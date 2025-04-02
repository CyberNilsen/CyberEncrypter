using System;
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

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
    }
}