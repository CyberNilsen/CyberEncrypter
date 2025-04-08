using System.Windows;
using System.Windows.Controls;
using CyberEncrypter;


namespace CyberEncrypter.View
{
    /// <summary>
    /// Interaction logic for Dashboard.xaml
    /// </summary>
    public partial class Dashboard : UserControl
    {
        public Dashboard()
        {
            InitializeComponent();
        }

        private void Encryptfiles_Click(object sender, RoutedEventArgs e)
        {
            var MainWindow = (MainWindow)Application.Current.MainWindow;
            MainWindow.DashboardContent.Content = new Encrypt();
        }

        private void Decrypt(object sender, RoutedEventArgs e)
        {
            var MainWindow = (MainWindow)Application.Current.MainWindow;
            MainWindow.DashboardContent.Content = new Decrypt();
        }
    }
}
