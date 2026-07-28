using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Service;

/// <summary>
/// Serviço de criptografia usando AES-256-GCM
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly ILogger<AesEncryptionService> _logger;
    
    // Prefixo para identificar dados criptografados
    private const string EncryptedPrefix = "ENC:";
    private const int NonceSize = 12;  // 96 bits para AES-GCM
    private const int TagSize = 16;    // 128 bits para tag de autenticação

    public AesEncryptionService(IConfiguration configuration, ILogger<AesEncryptionService> logger)
    {
        _logger = logger;
        
        // Busca a chave de criptografia das configurações
        var keyString = configuration["Encryption:Key"] 
            ?? configuration["ENCRYPTION_KEY"]
            ?? throw new InvalidOperationException(
                "Chave de criptografia não configurada. Configure 'Encryption:Key' no appsettings ou 'ENCRYPTION_KEY' nas variáveis de ambiente.");

        // A chave deve ter 32 bytes (256 bits) para AES-256
        // Pode ser fornecida como Base64 ou como string que será hasheada
        if (IsBase64String(keyString) && Convert.FromBase64String(keyString).Length == 32)
        {
            _key = Convert.FromBase64String(keyString);
        }
        else
        {
            // Deriva uma chave de 256 bits usando SHA-256 (compativel com dados existentes)
            // RECOMENDADO: Gerar chave Base64 de 32 bytes e configurar diretamente para evitar derivacao
            _key = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
            _logger.LogWarning("Usando derivacao de chave via SHA-256. Gere uma chave Base64 de 32 bytes para maior seguranca: openssl rand -base64 32");
        }
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            var cipherText = new byte[plainBytes.Length];
            var tag = new byte[TagSize];

            using var aesGcm = new AesGcm(_key, TagSize);
            aesGcm.Encrypt(nonce, plainBytes, cipherText, tag);

            // Formato: nonce + tag + ciphertext
            var result = new byte[NonceSize + TagSize + cipherText.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
            Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
            Buffer.BlockCopy(cipherText, 0, result, NonceSize + TagSize, cipherText.Length);

            return EncryptedPrefix + Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criptografar dados");
            throw new CryptographicException("Falha ao criptografar dados", ex);
        }
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        // Verifica se está criptografado
        if (!cipherText.StartsWith(EncryptedPrefix))
        {
            _logger.LogError("Tentativa de descriptografar dados sem prefixo ENC: - dados podem nao estar criptografados");
            throw new CryptographicException("Dados nao estao no formato criptografado esperado (prefixo ENC: ausente).");
        }

        try
        {
            var encryptedData = Convert.FromBase64String(cipherText[EncryptedPrefix.Length..]);

            if (encryptedData.Length < NonceSize + TagSize)
                throw new CryptographicException("Dados criptografados inválidos");

            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];
            var cipherBytes = new byte[encryptedData.Length - NonceSize - TagSize];

            Buffer.BlockCopy(encryptedData, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(encryptedData, NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(encryptedData, NonceSize + TagSize, cipherBytes, 0, cipherBytes.Length);

            var plainBytes = new byte[cipherBytes.Length];

            using var aesGcm = new AesGcm(_key, TagSize);
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (CryptographicException)
        {
            _logger.LogError("Falha na descriptografia - chave inválida ou dados corrompidos");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao descriptografar dados");
            throw new CryptographicException("Falha ao descriptografar dados", ex);
        }
    }

    public bool IsEncrypted(string text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        return text.StartsWith(EncryptedPrefix);
    }

    public string MaskKey(string key, int visibleChars = 4)
    {
        if (string.IsNullOrEmpty(key))
            return "***";

        if (key.Length <= visibleChars)
            return new string('*', key.Length);

        // Encontra o prefixo (ex: sk_live_, pk_test_)
        var prefixEnd = key.IndexOf('_', key.IndexOf('_') + 1);
        
        if (prefixEnd > 0 && prefixEnd < key.Length - visibleChars)
        {
            var prefix = key[..(prefixEnd + 1)];
            var suffix = key[^visibleChars..];
            var maskedLength = key.Length - prefix.Length - visibleChars;
            return $"{prefix}{new string('*', Math.Min(maskedLength, 10))}{suffix}";
        }

        // Fallback: mostra apenas os últimos caracteres
        var maskedPart = new string('*', Math.Min(key.Length - visibleChars, 20));
        return $"{maskedPart}{key[^visibleChars..]}";
    }

    private static bool IsBase64String(string base64)
    {
        if (string.IsNullOrEmpty(base64))
            return false;

        try
        {
            Convert.FromBase64String(base64);
            return base64.Length % 4 == 0;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Extensão para registrar o serviço de criptografia
/// </summary>
public static class EncryptionServiceExtensions
{
    public static IServiceCollection AddEncryptionService(this IServiceCollection services)
    {
        services.AddSingleton<IEncryptionService, AesEncryptionService>();
        return services;
    }
}
