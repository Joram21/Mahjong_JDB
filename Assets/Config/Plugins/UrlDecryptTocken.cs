using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class UrlDecryptTocken : MonoBehaviour
{
    public string originalText = "Naftali";
    public string encryptedText;
    public string decryptedText;
    public string key = "your-encryption-key-123"; // 16, 24, or 32 bytes key
    public string iv = "your-initial-vector"; // 16 bytes IV (initialization vector)

    // Encrypt a string
    public string EncryptString(string plainText)
    {
        using (Aes aesAlg = Aes.Create())
        {
            //Debug.Log(aesAlg.BlockSize/8);
            // Debug.Log(Encoding.UTF8.GetBytes(key).Length);
            aesAlg.Key = Encoding.UTF8.GetBytes(key); // Convert key to byte array
            //Debug.Log(Encoding.UTF8.GetBytes(iv).Length);
            aesAlg.IV = Encoding.UTF8.GetBytes(iv); // Convert IV to byte array

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                return Convert.ToBase64String(msEncrypt.ToArray()); // Return as base64 string
            }
        }
    }

    // Decrypt a string
    public string DecryptString(string cipherText)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = Encoding.UTF8.GetBytes(key); // Convert key to byte array
            aesAlg.IV = Encoding.UTF8.GetBytes(iv); // Convert IV to byte array

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd(); // Return the decrypted string
            }
        }
    }

    [ContextMenu("Encrypt")]
    public void Encrypt()
    {
        encryptedText = EncryptString(originalText);
        // Debug.Log("Encrypted Text: " + encryptedText);

    }
    [ContextMenu("Decrypt")]
    public void Decrypt()
    {
        decryptedText = DecryptString(encryptedText);
        // Debug.Log("Decrypted Text: " + decryptedText);
    }

    [ContextMenu("GenerateKey")]
    public void GenerateKey()
    {
        key = _KeyGenerator.GenerateRandomKey();
        //Debug.Log(key.Length);
    }
    [ContextMenu("Generateiv")]
    public void GenerateIv()
    {
        iv = _IVGenerator.GenerateRandomIV();
    }
}
public class _KeyGenerator : MonoBehaviour
{
    // Generate a random 16-byte key
    public static string GenerateRandomKey()
    {
        byte[] key = new byte[12]; // AES-128 requires a 16-byte key
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(key); // Fill the key with random bytes
        }
        return Convert.ToBase64String(key); // Return the key as a base64 string
    }

}

public class _IVGenerator : MonoBehaviour
{
    // Generate a random 16-byte IV
    public static string GenerateRandomIV()
    {
        byte[] iv = new byte[12]; // AES block size is 16 bytes
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv); // Fill the IV with random bytes
        }
        return Convert.ToBase64String(iv); // Return the IV as a base64 string
    }
}
