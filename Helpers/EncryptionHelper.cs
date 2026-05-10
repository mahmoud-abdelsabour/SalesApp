using System;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace WebApplication2.Helpers
{
    public static class EncryptionHelper
    {
        private static readonly string _key = "VTSSales@SecureKey#2024!";

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

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            using (var des = new TripleDESCryptoServiceProvider())
            {
                des.Key = Encoding.UTF8.GetBytes(_key.PadRight(24).Substring(0, 24));
                des.IV = Encoding.UTF8.GetBytes(_key.Substring(0, 8));
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.PKCS7;

                var transform = des.CreateDecryptor();
                var bytes = Convert.FromBase64String(cipherText);
                var result = transform.TransformFinalBlock(bytes, 0, bytes.Length);
                return Encoding.UTF8.GetString(result);
            }
        }
    }
}