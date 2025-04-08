using System.IO;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using Forms = System.Windows.Forms;
using Path = System.IO.Path;

namespace CyberEncrypter.View
{
    public partial class Encrypt : System.Windows.Controls.UserControl
    {
        private List<string> selectedFilePaths = new List<string>();
        private bool isFolder = false;
        private string folderPath = string.Empty;

        //Strings ovenfor. selectedFilePaths er for å lagre filstier. isFolder bool der den sier om det er en mappe eller en fil.
        //Folderpath kan oppdatere stringen og si hva folder stien er.
        public Encrypt()
        {
            InitializeComponent();
            SetupEventHandlers();
        }

        //Setter opp programmet og event listeners for knappene files/folders.

        private void SetupEventHandlers()
        {
            SelectFilesButton.Click += SelectFilesButton_Click;
            SelectFolderButton.Click += SelectFolderButton_Click;
        }

        private void SelectFilesButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "Select Files to Encrypt";
            openFileDialog.Filter = "All Files (*.*)|*.*";

            //Hvis du trykker på knappen select file så popper det
            //opp file explorer/dialog der det er lov å selecte flere filer, tittel og filtrer til alle filer.

            if (openFileDialog.ShowDialog() == true)
            {
                selectedFilePaths = openFileDialog.FileNames.ToList();
                isFolder = false;
                PathTextBox.Text = $"{selectedFilePaths.Count} file(s) selected";

                //Hvis file explorer/dialog åpner seg så kommer selectedFilePaths til å bli vist i boksen slik at du ser at du har valgt noe.
                //Sier også at stringen isFolder er falskt.
                //Deretter så endrer vi på boksen slik at den viser hvor mange filer som er valgt.
            }
        }

        private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            using (var folderDialog = new Forms.FolderBrowserDialog())
            {
                folderDialog.Description = "Select a folder to encrypt";
                Forms.DialogResult result = folderDialog.ShowDialog();

                //Her åpnes file explorer/dialog og det står også select a folder to encrypt.

                if (result == Forms.DialogResult.OK)
                {
                    folderPath = folderDialog.SelectedPath;
                    isFolder = true;
                    PathTextBox.Text = folderPath;

                    //Hvis du åpner file explorer/dialog så endres folder path, isFolder settes til sant og boksen endrer seg til
                    //å inneholde stien.
                }
            }
        }

        private void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            if ((isFolder && string.IsNullOrEmpty(folderPath)) ||
                (!isFolder && selectedFilePaths.Count == 0))
            {
                System.Windows.MessageBox.Show("Please select files or a folder to encrypt", "No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(PasswordBox.Password))
            {
                System.Windows.MessageBox.Show("Please enter an encryption password", "No Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            //Her er det som skjer når du trykker encrypt og hvis det er tomt så kommer det error melding
            //og hvis det ikke er tomt så sendes dataen videre.

            try
            {
                if (isFolder)
                {
                    EncryptFolder(folderPath, PasswordBox.Password);
                }
                else
                {
                    foreach (var filePath in selectedFilePaths)
                    {
                        EncryptFile(filePath, PasswordBox.Password);
                    }
                }

                System.Windows.MessageBox.Show("Encryption completed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

                //Hvis du enkrypterer noe så kommer det en melding opp.
                //Før dette skjer så blir dataen sendt videre til metoden avhening av det du vil enkryptere.
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Encryption failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //Hvis du får feilmelding så kommer dette opp.

        }

        private void EncryptFolder(string folderPath, string password)
        {
            string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                EncryptFile(file, password);
            }

            //Her så sier vi at stringen kan ta alle folders, deretter tar den alle filer og sender dette videre til metoden EncryptFile.

        }

        private void EncryptFile(string filePath, string password)
        {
            if (Path.GetExtension(filePath).Equals(".cyber", StringComparison.OrdinalIgnoreCase))
                return;

            //Hvis filen du har valgt allerede har .cyber som slutt så enkrypterer den ikke filen.

            string outputPath = filePath + ".cyber";
            byte[] salt = GenerateRandomSalt();

            //Her så lager vi en string som er filestien .cyber. Dette skal vi bruke som output og endre på slutten til filen til .cyber
            //her så kaller vi en metode som salter hver eneste fil.

            using (var keyDerivation = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
            {
                byte[] key = keyDerivation.GetBytes(32); 
                byte[] iv = keyDerivation.GetBytes(16);

                //Her lager vi en metode som heter keyDerivation som lager en encryption nøkkel
                //og deretter gjør at hvis du enkrypter samme fil igjen så er det forskjell på inneholdet.

                using (FileStream outputFileStream = new FileStream(outputPath, FileMode.Create))
                {
                    outputFileStream.Write(salt, 0, salt.Length);

                    //Her så lager vi en ny fil som vi skal skrive til.
                    //Her så lager vi en salt til starten den enkryptere filen.

                    using (Aes aes = Aes.Create())
                    {
                        aes.Key = key;
                        aes.IV = iv;
                        aes.Padding = PaddingMode.PKCS7;

                        //Først så lager vi en ny AES objekt, så setter vi stringen til key og iv.
                        //Deretter padding som sørger for at hvis vi trenger flere bytes for å enkryptere så har vi dette.

                        using (CryptoStream cryptoStream = new CryptoStream(
                            outputFileStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            using (FileStream inputFileStream = new FileStream(filePath, FileMode.Open))
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

        private byte[] GenerateRandomSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            return salt;
        }
    }
}