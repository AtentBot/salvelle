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

    // Pesos do Módulo 11 para o cálculo dos dígitos verificadores do CNPJ.
    // Distribuídos da direita para a esquerda (2..9, reiniciando em 2). Para uma
    // base de N caracteres tomam-se os últimos N pesos deste vetor.
    // Ref.: IN RFB nº 2.229/2024 e NT Conjunta 2025.001 (CNPJ alfanumérico).
    private static readonly int[] CnpjPesos = { 6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2 };

    // Regex canônica do CNPJ já normalizado (sem máscara, maiúsculo):
    // 12 posições alfanuméricas (raiz + ordem) + 2 dígitos verificadores numéricos.
    private static readonly Regex CnpjCanonico = new(@"^[A-Z0-9]{12}[0-9]{2}$", RegexOptions.Compiled);

    /// <summary>
    /// Normaliza um CNPJ para o formato canônico sem máscara: remove qualquer
    /// caractere fora de [A-Z0-9] e converte para MAIÚSCULO. É a normalização de
    /// borda que deve ser aplicada antes de validar, comparar ou persistir.
    /// Retrocompatível com CNPJ numérico (só afeta letras).
    /// </summary>
    public static string NormalizeCnpj(string? cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return string.Empty;

        return Regex.Replace(cnpj.ToUpperInvariant(), "[^A-Z0-9]", "");
    }

    // Calcula um dígito verificador (Módulo 11) para uma base de 12 ou 13
    // caracteres. Cada caractere é convertido pela tabela ASCII: valor = char - 48
    // ('0'..'9' => 0..9, 'A'..'Z' => 17..42). Resto 0 ou 1 => DV = 0.
    private static char CnpjDv(string baseCnpj)
    {
        int offset = CnpjPesos.Length - baseCnpj.Length;
        int soma = 0;
        for (int i = 0; i < baseCnpj.Length; i++)
            soma += (baseCnpj[i] - 48) * CnpjPesos[offset + i];

        int resto = soma % 11;
        return (char)('0' + (resto < 2 ? 0 : 11 - resto));
    }

    /// <summary>
    /// Valida um CNPJ (numérico legado OU alfanumérico). Rotina única e
    /// retrocompatível conforme IN RFB nº 2.229/2024: a mesma regra de DV
    /// aplicada a um CNPJ numérico antigo produz o mesmo dígito verificador.
    /// </summary>
    public static bool IsValidCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return false;

        // Normaliza na borda: remove máscara e coloca em maiúsculo.
        cnpj = NormalizeCnpj(cnpj);

        // 12 posições [A-Z0-9] + 2 DVs numéricos.
        if (!CnpjCanonico.IsMatch(cnpj))
            return false;

        // Rejeita sequências repetidas (regra de negócio herdada da validação numérica).
        var baseCnpj = cnpj.Substring(0, 12);
        if (baseCnpj.Distinct().Count() == 1)
            return false;

        var d1 = CnpjDv(baseCnpj);
        var d2 = CnpjDv(baseCnpj + d1);

        return cnpj[12] == d1 && cnpj[13] == d2;
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
    /// Formata CNPJ (numérico ou alfanumérico) para o padrão XX.XXX.XXX/XXXX-XX.
    /// Preserva letras: normaliza para o formato canônico e aplica a máscara.
    /// </summary>
    public static string FormatCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
            return string.Empty;

        cnpj = NormalizeCnpj(cnpj);

        if (cnpj.Length != 14)
            return cnpj;

        return $"{cnpj.Substring(0, 2)}.{cnpj.Substring(2, 3)}.{cnpj.Substring(5, 3)}/{cnpj.Substring(8, 4)}-{cnpj.Substring(12, 2)}";
    }

    /// <summary>
    /// Remove formatação de documento (deixa apenas dígitos). Use para CPF/PIS.
    /// Para CNPJ prefira <see cref="NormalizeCnpj"/>, que preserva letras.
    /// </summary>
    public static string RemoveFormatting(string document)
    {
        if (string.IsNullOrWhiteSpace(document))
            return string.Empty;

        return Regex.Replace(document, @"[^\d]", "");
    }
}
