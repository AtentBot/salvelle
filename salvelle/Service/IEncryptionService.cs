namespace Service;

/// <summary>
/// Interface para serviço de criptografia de dados sensíveis
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Criptografa um texto plano
    /// </summary>
    /// <param name="plainText">Texto a ser criptografado</param>
    /// <returns>Texto criptografado em Base64</returns>
    string Encrypt(string plainText);

    /// <summary>
    /// Descriptografa um texto criptografado
    /// </summary>
    /// <param name="cipherText">Texto criptografado em Base64</param>
    /// <returns>Texto plano</returns>
    string Decrypt(string cipherText);

    /// <summary>
    /// Verifica se um texto está criptografado
    /// </summary>
    /// <param name="text">Texto a verificar</param>
    /// <returns>True se parece estar criptografado</returns>
    bool IsEncrypted(string text);

    /// <summary>
    /// Mascara uma chave para exibição segura
    /// Ex: sk_live_abc123xyz -> sk_live_***xyz
    /// </summary>
    /// <param name="key">Chave original</param>
    /// <param name="visibleChars">Quantidade de caracteres visíveis no final</param>
    /// <returns>Chave mascarada</returns>
    string MaskKey(string key, int visibleChars = 4);
}
