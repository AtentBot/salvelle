using System.Text.RegularExpressions;

namespace Helpers;

public static class DocumentValidator
{
    /// <summary>
    /// Valida se um CPF é válido
    /// </summary>
    public static bool IsValidCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return false;

        // Remove caracteres não numéricos
        cpf = Regex.Replace(cpf, @"[^\d]", "");

        // CPF deve ter 11 dígitos
        if (cpf.Length != 11)
            return false;

        // Verifica se todos os dígitos são iguais (ex: 111.111.111-11)
        if (cpf.Distinct().Count() == 1)
            return false;

        // Validação do primeiro dígito verificador
        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += int.Parse(cpf[i].ToString()) * (10 - i);

        int remainder = sum % 11;
        int digit1 = remainder < 2 ? 0 : 11 - remainder;

        if (int.Parse(cpf[9].ToString()) != digit1)
            return false;

        // Validação do segundo dígito verificador
        sum = 0;
        for (int i = 0; i < 10; i++)
            sum += int.Parse(cpf[i].ToString()) * (11 - i);

        remainder = sum % 11;
        int digit2 = remainder < 2 ? 0 : 11 - remainder;

        return int.Parse(cpf[10].ToString()) == digit2;
    }

    /// <summary>
    /// Valida se um CNPJ é válido
    /// </summary>
    public static bool IsValidCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return false;

        // Remove caracteres não numéricos
        cnpj = Regex.Replace(cnpj, @"[^\d]", "");

        // CNPJ deve ter 14 dígitos
        if (cnpj.Length != 14)
            return false;

        // Verifica se todos os dígitos são iguais
        if (cnpj.Distinct().Count() == 1)
            return false;

        // Validação do primeiro dígito verificador
        int[] multiplier1 = { 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int sum = 0;

        for (int i = 0; i < 12; i++)
            sum += int.Parse(cnpj[i].ToString()) * multiplier1[i];

        int remainder = sum % 11;
        int digit1 = remainder < 2 ? 0 : 11 - remainder;

        if (int.Parse(cnpj[12].ToString()) != digit1)
            return false;

        // Validação do segundo dígito verificador
        int[] multiplier2 = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        sum = 0;

        for (int i = 0; i < 13; i++)
            sum += int.Parse(cnpj[i].ToString()) * multiplier2[i];

        remainder = sum % 11;
        int digit2 = remainder < 2 ? 0 : 11 - remainder;

        return int.Parse(cnpj[13].ToString()) == digit2;
    }

    /// <summary>
    /// Valida se um PIS/PASEP é válido
    /// </summary>
    public static bool IsValidPis(string pis)
    {
        if (string.IsNullOrWhiteSpace(pis))
            return false;

        // Remove caracteres não numéricos
        pis = Regex.Replace(pis, @"[^\d]", "");

        // PIS deve ter 11 dígitos
        if (pis.Length != 11)
            return false;

        // Verifica se todos os dígitos são iguais
        if (pis.Distinct().Count() == 1)
            return false;

        // Validação do dígito verificador
        int[] multiplier = { 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };
        int sum = 0;

        for (int i = 0; i < 10; i++)
            sum += int.Parse(pis[i].ToString()) * multiplier[i];

        int remainder = sum % 11;
        int digit = remainder < 2 ? 0 : 11 - remainder;

        return int.Parse(pis[10].ToString()) == digit;
    }

    /// <summary>
    /// Formata CPF (apenas dígitos) para o padrão XXX.XXX.XXX-XX
    /// </summary>
    public static string FormatCpf(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
            return string.Empty;

        cpf = Regex.Replace(cpf, @"[^\d]", "");

        if (cpf.Length != 11)
            return cpf;

        return $"{cpf.Substring(0, 3)}.{cpf.Substring(3, 3)}.{cpf.Substring(6, 3)}-{cpf.Substring(9, 2)}";
    }

    /// <summary>
    /// Formata CNPJ (apenas dígitos) para o padrão XX.XXX.XXX/XXXX-XX
    /// </summary>
    public static string FormatCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return string.Empty;

        cnpj = Regex.Replace(cnpj, @"[^\d]", "");

        if (cnpj.Length != 14)
            return cnpj;

        return $"{cnpj.Substring(0, 2)}.{cnpj.Substring(2, 3)}.{cnpj.Substring(5, 3)}/{cnpj.Substring(8, 4)}-{cnpj.Substring(12, 2)}";
    }

    /// <summary>
    /// Remove formatação de documento (deixa apenas dígitos)
    /// </summary>
    public static string RemoveFormatting(string document)
    {
        if (string.IsNullOrWhiteSpace(document))
            return string.Empty;

        return Regex.Replace(document, @"[^\d]", "");
    }
}
