using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace GPlus.Base.Helpers
{
    public static class CryptoUtils
    {
        public static string Encrypt(string plainText, string key)
        {
            using var aes = Aes.Create();
            var pdb = new Rfc2898DeriveBytes(key, Encoding.UTF8.GetBytes("FixedSalt123"), 10000);
            aes.Key = pdb.GetBytes(32);
            aes.IV = pdb.GetBytes(16);

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs))
                sw.Write(plainText);

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText, string key)
        {
            using var aes = Aes.Create();
            var pdb = new Rfc2898DeriveBytes(key, Encoding.UTF8.GetBytes("FixedSalt123"), 10000);
            aes.Key = pdb.GetBytes(32);
            aes.IV = pdb.GetBytes(16);

            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
    }

}
