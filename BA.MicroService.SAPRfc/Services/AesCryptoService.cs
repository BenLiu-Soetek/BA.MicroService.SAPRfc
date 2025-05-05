using SapRfcMicroservice.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SapRfcMicroservice
{
    public class AesCryptoService
    {
        private const string AesKey = "12345678901212345678901212345678";

        public string EncryptConnection(SapConnectionInfo conn)
        {
            var json = JsonSerializer.Serialize(conn);
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(AesKey);
            aes.GenerateIV();
            var encryptor = aes.CreateEncryptor();
            var plain = Encoding.UTF8.GetBytes(json);
            var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
            return Convert.ToBase64String(aes.IV.Concat(cipher).ToArray());
        }

        public SapConnectionInfo DecryptConnection(string encrypted)
        {
            var fullBytes = Convert.FromBase64String(encrypted);
            using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(AesKey);
            aes.IV = fullBytes.Take(16).ToArray();
            var decryptor = aes.CreateDecryptor();
            var cipher = fullBytes.Skip(16).ToArray();
            var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return JsonSerializer.Deserialize<SapConnectionInfo>(Encoding.UTF8.GetString(plain));
        }
    }
}
