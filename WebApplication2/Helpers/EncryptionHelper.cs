using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace WebApplication2.Helpers
{
    /// <summary>
    /// LEGACY: the old reversible 3DES password format. Only PasswordHelper uses it, to recognise
    /// not-yet-upgraded passwords at login (they are then re-saved as PBKDF2 hashes).
    /// Safe to delete, together with the EncryptionKey setting, once every user has logged in once.
    /// </summary>
    public static class EncryptionHelper
    {
        private static readonly string _key = LoadKey();

        private static string LoadKey()
        {
            var key = ConfigurationManager.AppSettings["EncryptionKey"];
            if (string.IsNullOrWhiteSpace(key))
                throw new ConfigurationErrorsException("Missing 'EncryptionKey' in Web.config <appSettings>.");
            return key;
        }

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using (var des = new TripleDESCryptoServiceProvider())
            {
                des.Key = Encoding.UTF8.GetBytes(_key.PadRight(24).Substring(0, 24));
                des.IV = Encoding.UTF8.GetBytes(_key.Substring(0, 8));
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.PKCS7;

                var transform = des.CreateEncryptor();
                var bytes = Encoding.UTF8.GetBytes(plainText);
                var result = transform.TransformFinalBlock(bytes, 0, bytes.Length);
                return Convert.ToBase64String(result);
            }
        }

    }
}