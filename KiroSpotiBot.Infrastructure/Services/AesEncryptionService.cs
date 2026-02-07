using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace KiroSpotiBot.Infrastructure.Services;

/// <summary>
/// AES-256 encryption service for sensitive data.
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(IOptions<Options.EncryptionOptions> options)
    {
        var encryptionKey = options.Value.Key;
        
        if (string.IsNullOrWhiteSpace(encryptionKey))
        {
            throw new InvalidOperationException("Encryption key not configured. Please set Encryption:Key in configuration.");
        }
        
        // Derive key and IV from the encryption key
        using var sha256 = SHA256.Create();
        var keyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(encryptionKey));
        _key = keyBytes;
        
        // Use first 16 bytes of key hash as IV
        _iv = keyBytes.Take(16).ToArray();
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return plaintext;
        }

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertextBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);
        
        return Convert.ToBase64String(ciphertextBytes);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext))
        {
            return ciphertext;
        }

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var ciphertextBytes = Convert.FromBase64String(ciphertext);
        var plaintextBytes = decryptor.TransformFinalBlock(ciphertextBytes, 0, ciphertextBytes.Length);
        
        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
