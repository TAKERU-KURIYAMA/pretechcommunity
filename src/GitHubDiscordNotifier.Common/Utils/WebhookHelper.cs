using System;
using System.Security.Cryptography;
using System.Text;

namespace GitHubDiscordNotifier.Common.Utils
{
    public static class WebhookHelper
    {
        public static bool VerifyGitHubWebhookSignature(string payload, string signature, string secret)
        {
            if (string.IsNullOrEmpty(signature) || !signature.StartsWith("sha256="))
                return false;

            var expectedSignature = signature.Substring(7); // Remove "sha256="
            
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            var computedSignature = BitConverter.ToString(computedHash).Replace("-", "").ToLower();
            
            return string.Equals(expectedSignature, computedSignature, StringComparison.OrdinalIgnoreCase);
        }

        public static string GenerateWebhookSecret()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}