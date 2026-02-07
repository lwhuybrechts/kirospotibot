namespace KiroSpotiBot.Infrastructure.Options;

/// <summary>
/// Configuration options for encryption service.
/// </summary>
public class EncryptionOptions
{
    public const string SectionName = "Encryption";
    
    /// <summary>
    /// The encryption key used for AES-256 encryption.
    /// Should be stored in Azure Key Vault or environment variables.
    /// </summary>
    public string Key { get; set; } = string.Empty;
}
