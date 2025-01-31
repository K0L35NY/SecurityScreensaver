using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;

namespace FileDecryptor
{
    public partial class DecryptForm : Form
    {
        public DecryptForm()
        {
            InitializeComponent();
        }

        private void btnDecrypt_Click(object sender, EventArgs e)
        {
            if (txtKey.Text == "90473")
            {
                DecryptFiles();
                MessageBox.Show("Files decrypted successfully!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Invalid decryption key!", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DecryptFiles()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string[] files = Directory.GetFiles(desktopPath, "*.encrypted");

            foreach (string file in files)
            {
                try
                {
                    DecryptFile(file, "90473");
                    File.Delete(file);
                }
                catch { /* Skip problematic files */ }
            }
        }

        private void DecryptFile(string inputFile, string password)
        {
            byte[] salt = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };

            using (Aes aes = Aes.Create())
            {
                Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(password, salt, 1000);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);

                using (FileStream fsCrypt = new FileStream(inputFile, FileMode.Open))
                {
                    using (CryptoStream cs = new CryptoStream(fsCrypt,
                        aes.CreateDecryptor(),
                        CryptoStreamMode.Read))
                    {
                        string outputFile = inputFile.Replace(".encrypted", "");
                        using (FileStream fsOut = new FileStream(outputFile, FileMode.Create))
                        {
                            cs.CopyTo(fsOut);
                        }
                    }
                }
            }
        }
    }
}