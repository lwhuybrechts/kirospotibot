namespace KiroSpotiBot.Infrastructure.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext string.
    /// </summary>
    string Encrypt(string plaintext);
    
    /// <summary>
    /// Decrypts an encrypted string.
    /// </summary>
    string Decrypt(string ciphertext);
}
