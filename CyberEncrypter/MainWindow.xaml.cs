using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberEncrypter
{
    public partial class MainWindow : Window
    {
        // List to store files to be encrypted
        private List<string> filesToEncrypt = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
            SetSizeAndPositionOnPrimaryScreen();

            // Wire up navigation buttons to switch between pages
            SetupNavigation();
        }

        /// <summary>
        /// Sets up the navigation button click handlers
        /// </summary>
        private void SetupNavigation()
        {
            // Find all buttons in the navigation panel and hook up their click events
            var navButtons = FindVisualChildren<Button>(this).Where(b => b.Style == FindResource("NavButton"));
            int index = 0;

            foreach (var button in navButtons)
            {
                int capturedIndex = index;
                button.Click += (s, e) => {
                    NavigateToPage(capturedIndex);
                };
                index++;
            }
        }

        /// <summary>
        /// Helper method to find all visual children of a specified type
        /// </summary>
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                if (child != null && child is T)
                    yield return (T)child;

                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }

        /// <summary>
        /// Navigates to the specified page by index
        /// </summary>
        private void NavigateToPage(int index)
        {
            // Hide all pages
            Dashboard.Visibility = Visibility.Collapsed;
            EncryptFilesPage.Visibility = Visibility.Collapsed;
            DecryptFilesPage.Visibility = Visibility.Collapsed;
            KeyManagementPage.Visibility = Visibility.Collapsed;
            SettingsPage.Visibility = Visibility.Collapsed;

            // Show the selected page
            switch (index)
            {
                case 0:
                    Dashboard.Visibility = Visibility.Visible;
                    break;
                case 1:
                    EncryptFilesPage.Visibility = Visibility.Visible;
                    break;
                case 2:
                    DecryptFilesPage.Visibility = Visibility.Visible;
                    break;
                case 3:
                    KeyManagementPage.Visibility = Visibility.Visible;
                    break;
                case 4:
                    SettingsPage.Visibility = Visibility.Visible;
                    break;
            }
        }

        /// <summary>
        /// Service class for handling encryption operations
        /// </summary>
        public class EncryptionService
        {
            // Initialization vector size for AES encryption
            private const int IV_SIZE = 16;

            /// <summary>
            /// Derives an encryption key from the provided password
            /// </summary>
            public static byte[] DeriveKeyFromPassword(string password, byte[] salt)
            {
                using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256);
                return pbkdf2.GetBytes(32); // 256 bits for AES-256
            }

            /// <summary>
            /// Encrypts a file using AES-256 with the provided password
            /// </summary>
            public static void EncryptFile(string sourceFile, string destinationFile, string password)
            {
                byte[] salt = new byte[16];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(salt);
                }

                // Derive key from password
                byte[] key = DeriveKeyFromPassword(password, salt);

                // Generate random IV
                byte[] iv = new byte[IV_SIZE];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(iv);
                }

using (var aes = System.Security.Cryptography.Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                    {
                        using (var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write))
                        {
                            // Write salt
                            destinationStream.Write(salt, 0, salt.Length);

                            // Write IV
                            destinationStream.Write(iv, 0, iv.Length);

                            // Add file signature
                            byte[] signature = Encoding.UTF8.GetBytes("CYBER");
                            destinationStream.Write(signature, 0, signature.Length);

                            // Store original file extension
                            string extension = Path.GetExtension(sourceFile);
                            byte[] extensionBytes = Encoding.UTF8.GetBytes(extension.PadRight(10));
                            destinationStream.Write(extensionBytes, 0, 10);

                            // Encrypt the file content
                            using (var encryptor = aes.CreateEncryptor())
                            {
                                using (var cryptoStream = new CryptoStream(destinationStream, encryptor, CryptoStreamMode.Write))
                                {
                                    sourceStream.CopyTo(cryptoStream);
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Decrypts a .cyber file using the provided password
            /// </summary>
            public static bool DecryptFile(string sourceFile, string destinationFile, string password)
            {
                try
                {
                    using (var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                    {
                        // Read salt
                        byte[] salt = new byte[16];
                        sourceStream.Read(salt, 0, salt.Length);

                        // Read IV
                        byte[] iv = new byte[IV_SIZE];
                        sourceStream.Read(iv, 0, iv.Length);

                        // Validate file signature
                        byte[] signature = new byte[5];
                        sourceStream.Read(signature, 0, signature.Length);
                        string signatureStr = Encoding.UTF8.GetString(signature);

                        if (signatureStr != "CYBER")
                        {
                            return false; // Not a valid .cyber file
                        }

                        // Read original extension
                        byte[] extensionBytes = new byte[10];
                        sourceStream.Read(extensionBytes, 0, extensionBytes.Length);
                        string extension = Encoding.UTF8.GetString(extensionBytes).Trim();

                        // If no destination specified, use original name with extension
                        if (string.IsNullOrEmpty(destinationFile))
                        {
                            destinationFile = Path.ChangeExtension(sourceFile.Replace(".cyber", ""), extension);
                        }

                        // Derive key from password
                        byte[] key = DeriveKeyFromPassword(password, salt);

                        using (var aes = System.Security.Cryptography.Aes.Create())
                        {
                            aes.Key = key;
                            aes.IV = iv;
                            aes.Mode = CipherMode.CBC;
                            aes.Padding = PaddingMode.PKCS7;

                            using (var decryptor = aes.CreateDecryptor())
                            {
                                using (var cryptoStream = new CryptoStream(sourceStream, decryptor, CryptoStreamMode.Read))
                                {
                                    using (var destinationStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write))
                                    {
                                        cryptoStream.CopyTo(destinationStream);
                                    }
                                }
                            }
                        }

                        return true;
                    }
                }
                catch (CryptographicException)
                {
                    // Incorrect password or corrupted file
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Sets the window size and position based on the primary screen resolution
        /// </summary>
        private void SetSizeAndPositionOnPrimaryScreen()
        {
            var primaryScreen = System.Windows.SystemParameters.WorkArea;

            double widthRatio = 0.75;
            double heightRatio = 0.75;

            this.Width = primaryScreen.Width * widthRatio;
            this.Height = primaryScreen.Height * heightRatio;
        }

        /// <summary>
        /// Closes the application
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Maximizes or restores the window
        /// </summary>
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == System.Windows.WindowState.Normal)
            {
                this.WindowState = System.Windows.WindowState.Maximized;
                MaximizeIcon.Text = "\uE923"; // Change to restore icon
            }
            else
            {
                this.WindowState = System.Windows.WindowState.Normal;
                MaximizeIcon.Text = "\uE922"; // Change back to maximize icon
            }
        }

        /// <summary>
        /// Minimizes the window
        /// </summary>
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Opens the file selection dialog for encryption
        /// </summary>
        private void Encryptfile_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to the encrypt files page
            NavigateToPage(1);

            // Setup buttons on that page
            SetupEncryptionButtons();
        }

        /// <summary>
        /// Sets up the encryption page buttons
        /// </summary>
        private void SetupEncryptionButtons()
        {
            // Find buttons in the encryption page and hook up their events
            var selectFilesButton = FindVisualChildren<Button>(EncryptFilesPage)
                .FirstOrDefault(b => b.Content is StackPanel sp &&
                                  (sp.Children[1] as TextBlock)?.Text == "Select Files");

            var selectFolderButton = FindVisualChildren<Button>(EncryptFilesPage)
                .FirstOrDefault(b => b.Content is StackPanel sp &&
                                  (sp.Children[1] as TextBlock)?.Text == "Select Folder");

            var startEncryptionButton = FindVisualChildren<Button>(EncryptFilesPage)
                .FirstOrDefault(b => b.Content is StackPanel sp &&
                                  (sp.Children[1] as TextBlock)?.Text == "Start Encryption");

            if (selectFilesButton != null)
            {
                selectFilesButton.Click += SelectFiles_Click;
            }

            if (selectFolderButton != null)
            {
                selectFolderButton.Click += SelectFolder_Click;
            }

            if (startEncryptionButton != null)
            {
                startEncryptionButton.Click += StartEncryption_Click;
            }
        }

        /// <summary>
        /// Handles the file selection dialog for encryption
        /// </summary>
        private void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "All Files (*.*)|*.*",
                Title = "Select Files to Encrypt"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    if (!filesToEncrypt.Contains(filename))
                    {
                        filesToEncrypt.Add(filename);
                    }
                }

                // Update the ListView
                UpdateFileListView();
            }
        }

        /// <summary>
        /// Handles folder selection dialog for encryption
        /// </summary>
        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select a folder to encrypt all contained files",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string[] files = Directory.GetFiles(dialog.SelectedPath, "*.*", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    if (!filesToEncrypt.Contains(file) && !file.EndsWith(".cyber"))
                    {
                        filesToEncrypt.Add(file);
                    }
                }

                // Update the ListView
                UpdateFileListView();
            }
        }

        /// <summary>
        /// Updates the file list view with the current files to encrypt
        /// </summary>
        private void UpdateFileListView()
        {
            // Find the ListView control
            var listView = FindVisualChildren<ListView>(EncryptFilesPage).FirstOrDefault();

            if (listView != null)
            {
                listView.Items.Clear();

                foreach (string file in filesToEncrypt)
                {
                    listView.Items.Add(new FileInfo(file).Name);
                }
            }
        }

        /// <summary>
        /// Starts the encryption process for the selected files
        /// </summary>
        private void StartEncryption_Click(object sender, RoutedEventArgs e)
        {
            // Find the password box
            var passwordBox = FindVisualChildren<PasswordBox>(EncryptFilesPage).FirstOrDefault();

            if (passwordBox == null || string.IsNullOrEmpty(passwordBox.Password))
            {
                MessageBox.Show("Please enter an encryption password.", "Password Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (filesToEncrypt.Count == 0)
            {
                MessageBox.Show("Please select at least one file to encrypt.", "No Files Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string password = passwordBox.Password;
            int successCount = 0;

            foreach (string file in filesToEncrypt)
            {
                try
                {
                    string outputFile = file + ".cyber";
                    EncryptionService.EncryptFile(file, outputFile, password);
                    successCount++;

                    // Check if we should delete original
                    var deleteOriginalCheckbox = FindVisualChildren<CheckBox>(SettingsPage)
                        .FirstOrDefault(cb => cb.Content.ToString() == "Securely delete original files");

                    if (deleteOriginalCheckbox != null && deleteOriginalCheckbox.IsChecked == true)
                    {
                        // Securely delete the original file
                        SecureDeleteFile(file);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error encrypting file {Path.GetFileName(file)}: {ex.Message}",
                                    "Encryption Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Show completion message
            MessageBox.Show($"Successfully encrypted {successCount} of {filesToEncrypt.Count} files.",
                            "Encryption Complete", MessageBoxButton.OK, MessageBoxImage.Information);

            // Clear the files list
            filesToEncrypt.Clear();
            UpdateFileListView();

            // Update statistics on dashboard
            UpdateDashboardStats(successCount);
        }

        /// <summary>
        /// Updates the dashboard statistics after encryption
        /// </summary>
        private void UpdateDashboardStats(int filesEncrypted)
        {
            // Find the stats TextBlocks on the dashboard
            var statsTextBlocks = FindVisualChildren<TextBlock>(Dashboard)
                .Where(tb => tb.Text == "0" || tb.Text == "0 MB");

            // Update files encrypted counter
            var filesEncryptedTextBlock = statsTextBlocks.FirstOrDefault();
            if (filesEncryptedTextBlock != null)
            {
                int currentCount = int.Parse(filesEncryptedTextBlock.Text);
                filesEncryptedTextBlock.Text = (currentCount + filesEncrypted).ToString();
            }
        }

        /// <summary>
        /// Securely deletes a file by overwriting it with random data before deletion
        /// </summary>
        private void SecureDeleteFile(string filePath)
        {
            try
            {
                // Get file size
                FileInfo fileInfo = new FileInfo(filePath);
                long fileSize = fileInfo.Length;

                // Overwrite file with random data 3 times
                using (var rng = RandomNumberGenerator.Create())
                {
                    for (int i = 0; i < 3; i++)
                    {
                        using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
                        {
                            byte[] buffer = new byte[8192]; // 8KB buffer
                            long bytesRemaining = fileSize;

                            while (bytesRemaining > 0)
                            {
                                int bytesToWrite = (int)Math.Min(buffer.Length, bytesRemaining);
                                rng.GetBytes(buffer, 0, bytesToWrite);
                                fs.Write(buffer, 0, bytesToWrite);
                                bytesRemaining -= bytesToWrite;
                            }
                        }
                    }
                }

                // Finally delete the file
                File.Delete(filePath);
            }
            catch
            {
                // If secure delete fails, try simple delete
                try
                {
                    File.Delete(filePath);
                }
                catch
                {
                    // Ignore if we can't delete
                }
            }
        }
    }
}