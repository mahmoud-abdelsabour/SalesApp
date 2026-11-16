using System;
using System.Security.Cryptography;

namespace WebApplication2.Helpers
{
    /// <summary>
    /// One-way password hashing using PBKDF2 (HMAC-SHA256, per-user random salt).
    /// Stored format: PBKDF2$iterations$saltBase64$hashBase64
    /// </summary>
    public static class PasswordHelper
    {
        private const string Prefix = "PBKDF2";
        private const int Iterations = 100000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public static string Hash(string password)
        {
            var salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            var hash = Derive(password, salt, Iterations);
            return string.Join("$", Prefix, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
        }

        public static bool IsHashed(string stored)
        {
            return !string.IsNullOrEmpty(stored) && stored.StartsWith(Prefix + "$", StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies a password. Legacy 3DES-encrypted values are still accepted
        /// so existing users can log in; callers should re-hash them (see NeedsUpgrade).
        /// </summary>
        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored))
                return false;

            if (!IsHashed(stored))
                return ConstantTimeEquals(
                    System.Text.Encoding.UTF8.GetBytes(EncryptionHelper.Encrypt(password)),
                    System.Text.Encoding.UTF8.GetBytes(stored));

            var parts = stored.Split('$');
            if (parts.Length != 4) return false;

            int iterations;
            if (!int.TryParse(parts[1], out iterations)) return false;

            try
            {
                var salt = Convert.FromBase64String(parts[2]);
                var expected = Convert.FromBase64String(parts[3]);
                return ConstantTimeEquals(Derive(password, salt, iterations), expected);
            }
            catch (FormatException)
            {
                return false;
            }
        }

        public static bool NeedsUpgrade(string stored)
        {
            return !IsHashed(stored);
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
                return pbkdf2.GetBytes(HashSize);
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
