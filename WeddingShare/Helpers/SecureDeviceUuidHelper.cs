using System;
using System.Security.Cryptography;
using System.Text;

namespace WeddingShare.Helpers
{
    /// <summary>
    /// Helper class for generating and validating cryptographically signed device UUIDs.
    /// This prevents tampering and ensures device UUID authenticity.
    /// </summary>
    public class SecureDeviceUuidHelper
    {
        private static readonly byte[] SecretKey;

        static SecureDeviceUuidHelper()
        {
            // Generate a consistent secret key from environment variable or use a default
            // In production, this should come from a secure configuration source
            var secretKeyString = Environment.GetEnvironmentVariable("WEDDINGSHARE_UUID_SECRET")
                ?? "WeddingShare-Default-Secret-Key-CHANGE-IN-PRODUCTION-2024";
            SecretKey = Encoding.UTF8.GetBytes(secretKeyString);
        }

        /// <summary>
        /// Generates a new device UUID with cryptographic signature
        /// </summary>
        /// <returns>Signed UUID in format: {uuid}.{signature}</returns>
        public static string GenerateSignedUuid()
        {
            var uuid = Guid.NewGuid().ToString();
            var signature = ComputeHmac(uuid);
            return $"{uuid}.{signature}";
        }

        /// <summary>
        /// Validates a signed UUID and extracts the actual UUID if valid
        /// </summary>
        /// <param name="signedUuid">The signed UUID to validate</param>
        /// <param name="uuid">The extracted UUID if valid, null otherwise</param>
        /// <returns>True if signature is valid, false otherwise</returns>
        public static bool ValidateSignedUuid(string? signedUuid, out string? uuid)
        {
            uuid = null;

            if (string.IsNullOrWhiteSpace(signedUuid))
                return false;

            var parts = signedUuid.Split('.');
            if (parts.Length != 2)
                return false;

            // Validate UUID format
            if (!Guid.TryParse(parts[0], out _))
                return false;

            uuid = parts[0];
            var expectedSignature = ComputeHmac(uuid);

            // Constant-time comparison to prevent timing attacks
            return CryptographicEquals(parts[1], expectedSignature);
        }

        /// <summary>
        /// Computes HMAC-SHA256 signature for the given data
        /// </summary>
        private static string ComputeHmac(string data)
        {
            using (var hmac = new HMACSHA256(SecretKey))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Constant-time string comparison to prevent timing attacks
        /// </summary>
        private static bool CryptographicEquals(string a, string b)
        {
            if (a == null || b == null)
                return false;

            if (a.Length != b.Length)
                return false;

            var result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }
            return result == 0;
        }
    }
}
