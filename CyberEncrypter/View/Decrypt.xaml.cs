using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;
using Path = System.IO.Path;

namespace CyberEncrypter.View
{
    public partial class Decrypt : UserControl
    {
        private List<string> selectedFilePaths = new List<string>();
        private bool isFolder = false;
        private string folderPath = string.Empty;

        public Decrypt()
        {
            InitializeComponent();
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            SelectFilesButton.Click += SelectFilesButton_Click;
            SelectFolderButton.Click += SelectFolderButton_Click;
            DecryptButton.Click += DecryptButton_Click;
        }

        private void SelectFilesButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "Select Files to Decrypt";
            openFileDialog.Filter = "Encrypted Files (*.cyber)|*.cyber|All Files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePaths = openFileDialog.FileNames.ToList();
                isFolder = false;
                PathTextBox.Text = $"{selectedFilePaths.Count} file(s) selected";
            }
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new Forms.FolderBrowserDialog())
            {
                folderDialog.Description = "Select a folder to decrypt";
                Forms.DialogResult result = folderDialog.ShowDialog();

                if (result == Forms.DialogResult.OK)
                {
                    folderPath = folderDialog.SelectedPath;
                    isFolder = true;
                    PathTextBox.Text = folderPath;
                }
            }
        }

        private void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            if ((isFolder && string.IsNullOrEmpty(folderPath)) ||
                (!isFolder && selectedFilePaths.Count == 0))
            {
                MessageBox.Show("Please select files or a folder to decrypt", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(PasswordBox.Password))
            {
                MessageBox.Show("Please enter a decryption password", "No Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (isFolder)
                {
                    DecryptFolder(folderPath, PasswordBox.Password);
                }
                else
                {
                    foreach (var filePath in selectedFilePaths)
                    {
                        DecryptFile(filePath, PasswordBox.Password);
                    }
                }

                MessageBox.Show("Decryption completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Decryption failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DecryptFolder(string folderPath, string password)
        {
            string[] files = Directory.GetFiles(folderPath, "*.cyber", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                DecryptFile(file, password);
            }
        }

        private void DecryptFile(string filePath, string password)
        {
            if (!Path.GetExtension(filePath).Equals(".cyber", StringComparison.OrdinalIgnoreCase))
                return;

            string outputPath = filePath.Substring(0, filePath.Length - ".cyber".Length);

            using (FileStream inputFileStream = new FileStream(filePath, FileMode.Open))
            {
                byte[] salt = new byte[16];
                inputFileStream.Read(salt, 0, salt.Length);

                using (var keyDerivation = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
                {
                    byte[] key = keyDerivation.GetBytes(32);
                    byte[] iv = keyDerivation.GetBytes(16);

                    using (FileStream outputFileStream = new FileStream(outputPath, FileMode.Create))
                    {
                        using (Aes aes = Aes.Create())
                        {
                            aes.Key = key;
                            aes.IV = iv;
                            aes.Padding = PaddingMode.PKCS7;

                            using (CryptoStream cryptoStream = new CryptoStream(
                                outputFileStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                            {
                                byte[] buffer = new byte[4096];
                                int bytesRead;
                                while ((bytesRead = inputFileStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    cryptoStream.Write(buffer, 0, bytesRead);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}