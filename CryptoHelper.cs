using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BeiShuiCS2
{
    public static class CryptoHelper
    {
        // 生成 AES 密钥和 IV
        public static (byte[] key, byte[] iv) GenerateAesKey()
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.GenerateKey();
            aes.GenerateIV();
            return (aes.Key, aes.IV);
        }

        // AES 加密
        public static byte[] EncryptAes(byte[] data, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
            return ms.ToArray();
        }

        // AES 解密
        public static byte[] DecryptAes(byte[] encryptedData, byte[] key, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            using var ms = new MemoryStream(encryptedData);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new MemoryStream();
            cs.CopyTo(sr);
            return sr.ToArray();
        }

        // 生成 RSA 公钥/私钥对（客户端只保存公钥，私钥仅用于解密服务器返回的密钥，但实际我们可以让服务器用客户端的公钥加密，客户端用私钥解密）
        // 更简单的方式：客户端生成 RSA 密钥对，将公钥发给服务器，服务器用该公钥加密会话密钥后返回。
        public static (string publicKeyXml, string privateKeyXml) GenerateRsaKeys()
        {
            using var rsa = new RSACryptoServiceProvider(2048);
            string publicKey = rsa.ToXmlString(false);
            string privateKey = rsa.ToXmlString(true);
            return (publicKey, privateKey);
        }

        // 用 RSA 公钥加密数据
        public static byte[] EncryptRsa(byte[] data, string publicKeyXml)
        {
            using var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKeyXml);
            return rsa.Encrypt(data, false);
        }

        // 用 RSA 私钥解密数据
        public static byte[] DecryptRsa(byte[] data, string privateKeyXml)
        {
            using var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKeyXml);
            return rsa.Decrypt(data, false);
        }
    }
}