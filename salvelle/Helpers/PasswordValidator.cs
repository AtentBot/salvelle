using System.Text.RegularExpressions;

namespace Helpers;

public static class PasswordValidator
{
    /// <summary>
    /// Valida se a senha atende aos requisitos mínimos de segurança
    /// </summary>
    public static (bool isValid, List<string> errors) ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("A senha não pode ser vazia");
            return (false, errors);
        }

        // Mínimo de 8 caracteres
        if (password.Length < 8)
            errors.Add("A senha deve ter no mínimo 8 caracteres");

        // Máximo de 128 caracteres
        if (password.Length > 128)
            errors.Add("A senha deve ter no máximo 128 caracteres");

        // Deve conter pelo menos uma letra maiúscula
        if (!Regex.IsMatch(password, @"[A-Z]"))
            errors.Add("A senha deve conter pelo menos uma letra maiúscula");

        // Deve conter pelo menos uma letra minúscula
        if (!Regex.IsMatch(password, @"[a-z]"))
            errors.Add("A senha deve conter pelo menos uma letra minúscula");

        // Deve conter pelo menos um número
        if (!Regex.IsMatch(password, @"[0-9]"))
            errors.Add("A senha deve conter pelo menos um número");

        // Deve conter pelo menos um caractere especial
        if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>_\-+=\[\]\\\/]"))
            errors.Add("A senha deve conter pelo menos um caractere especial (!@#$%^&*...)");

        // Não pode conter espaços
        if (password.Contains(' '))
            errors.Add("A senha não pode conter espaços");

        // Não pode conter sequências óbvias
        if (HasCommonSequence(password))
            errors.Add("A senha não pode conter sequências óbvias (123, abc, etc)");

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Verifica se a senha contém sequências comuns óbvias
    /// </summary>
    private static bool HasCommonSequence(string password)
    {
        var commonSequences = new[]
        {
            "123", "234", "345", "456", "567", "678", "789",
            "abc", "bcd", "cde", "def", "efg", "fgh",
            "qwerty", "asdf", "zxcv",
            "password", "senha", "admin"
        };

        return commonSequences.Any(seq => 
            password.ToLower().Contains(seq));
    }

    /// <summary>
    /// Calcula a força da senha (0-100)
    /// </summary>
    public static (int strength, string level) CalculatePasswordStrength(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return (0, "Muito Fraca");

        int score = 0;

        // Comprimento
        if (password.Length >= 8) score += 10;
        if (password.Length >= 12) score += 10;
        if (password.Length >= 16) score += 10;
        if (password.Length >= 20) score += 10;

        // Variedade de caracteres
        if (Regex.IsMatch(password, @"[a-z]")) score += 10;
        if (Regex.IsMatch(password, @"[A-Z]")) score += 10;
        if (Regex.IsMatch(password, @"[0-9]")) score += 10;
        if (Regex.IsMatch(password, @"[!@#$%^&*(),.?""':{}|<>_\-+=\[\]\\\/]")) score += 15;

        // Diversidade
        int uniqueChars = password.Distinct().Count();
        if (uniqueChars >= 8) score += 10;
        if (uniqueChars >= 12) score += 5;

        // Penalidades
        if (HasCommonSequence(password)) score -= 20;
        if (Regex.IsMatch(password, @"(.)\1{2,}")) score -= 10; // Caracteres repetidos

        score = Math.Max(0, Math.Min(100, score));

        string level = score switch
        {
            >= 80 => "Muito Forte",
            >= 60 => "Forte",
            >= 40 => "Média",
            >= 20 => "Fraca",
            _ => "Muito Fraca"
        };

        return (score, level);
    }

    /// <summary>
    /// Verifica se a senha está na lista de senhas mais comuns (top 1000)
    /// </summary>
    public static bool IsCommonPassword(string password)
    {
        // Lista simplificada das senhas mais comuns
        // Em produção, usar lista completa ou serviço externo
        var commonPasswords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "123456", "password", "12345678", "qwerty", "123456789",
            "12345", "1234", "111111", "1234567", "dragon",
            "123123", "baseball", "iloveyou", "trustno1", "1234567890",
            "senha", "admin", "root", "master", "senha123"
        };

        return commonPasswords.Contains(password);
    }

    /// <summary>
    /// Gera uma senha forte aleatória
    /// </summary>
    public static string GenerateStrongPassword(int length = 16)
    {
        const string lowercase = "abcdefghijklmnopqrstuvwxyz";
        const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numbers = "0123456789";
        const string special = "!@#$%^&*()_-+=[]{}|:;,.<>?";

        var random = new Random();
        var password = new char[length];

        // Garante pelo menos um de cada tipo
        password[0] = lowercase[random.Next(lowercase.Length)];
        password[1] = uppercase[random.Next(uppercase.Length)];
        password[2] = numbers[random.Next(numbers.Length)];
        password[3] = special[random.Next(special.Length)];

        // Preenche o resto aleatoriamente
        string allChars = lowercase + uppercase + numbers + special;
        for (int i = 4; i < length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }

        // Embaralha
        return new string(password.OrderBy(x => random.Next()).ToArray());
    }

    /// <summary>
    /// Verifica se a senha atual expirou (padrão: 90 dias)
    /// </summary>
    public static bool IsPasswordExpired(DateTime passwordCreatedAt, int expirationDays = 90)
    {
        return DateTime.UtcNow > passwordCreatedAt.AddDays(expirationDays);
    }

    /// <summary>
    /// Verifica se a nova senha é diferente das últimas N senhas
    /// </summary>
    public static bool IsDifferentFromHistory(string newPasswordHash, List<string> passwordHistory, int historyCount = 5)
    {
        if (passwordHistory == null || passwordHistory.Count == 0)
            return true;

        // Compara apenas com as últimas N senhas
        var recentPasswords = passwordHistory.Take(historyCount);
        
        return !recentPasswords.Contains(newPasswordHash);
    }

    /// <summary>
    /// Mensagens de ajuda para criação de senha forte
    /// </summary>
    public static List<string> GetPasswordRequirements()
    {
        return new List<string>
        {
            "Mínimo de 8 caracteres (recomendado: 12 ou mais)",
            "Pelo menos uma letra maiúscula (A-Z)",
            "Pelo menos uma letra minúscula (a-z)",
            "Pelo menos um número (0-9)",
            "Pelo menos um caractere especial (!@#$%^&*...)",
            "Não use sequências óbvias (123, abc, etc)",
            "Não use senhas comuns ou facilmente adivinháveis",
            "Não compartilhe sua senha com ninguém",
            "Use senhas diferentes para cada serviço"
        };
    }
}
