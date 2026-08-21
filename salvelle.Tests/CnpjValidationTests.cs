using Helpers;
using Xunit;

namespace salvelle.Tests;

/// <summary>
/// Casos obrigatórios da adequação ao CNPJ alfanumérico
/// (IN RFB nº 2.229/2024 e NT Conjunta 2025.001), seção 6 do documento oficial.
/// Cobre a rotina única <see cref="DocumentValidator.IsValidCnpj"/> e a
/// normalização <see cref="DocumentValidator.NormalizeCnpj"/>.
/// </summary>
public class CnpjValidationTests
{
    [Theory]
    // Alfanuméricos
    [InlineData("12ABC34501DE35")]      // exemplo oficial da Receita Federal
    [InlineData("12.ABC.345/01DE-35")]  // o mesmo, com máscara
    [InlineData("12abc34501de35")]      // minúsculo - deve ser normalizado e aceito
    [InlineData("AB12C3D40E9F41")]      // letra na 1a posição
    // Numéricos legados (retrocompatibilidade)
    [InlineData("11222333000181")]
    [InlineData("12345678000195")]
    [InlineData("11.222.333/0001-81")]
    public void IsValidCnpj_Validos_RetornaTrue(string cnpj)
    {
        Assert.True(DocumentValidator.IsValidCnpj(cnpj));
    }

    [Theory]
    [InlineData("12ABC34501DE34")]      // DV1 correto, DV2 incorreto
    [InlineData("12ABC34501DE45")]      // DV1 incorreto
    [InlineData("11222333000180")]      // DV alterado em CNPJ numérico
    [InlineData("12ABC34501DEAB")]      // posições 13-14 devem ser numéricas
    [InlineData("12ABC34501D")]         // tamanho menor que 14
    [InlineData("12ABC34501DE355")]     // tamanho maior que 14
    [InlineData("12@BC34501DE35")]      // caractere fora do alfabeto permitido
    [InlineData("00000000000000")]      // sequência repetida
    [InlineData("AAAAAAAAAAAA00")]      // sequência repetida alfanumérica
    [InlineData("")]                    // entrada vazia
    [InlineData(null)]                  // entrada nula - não pode lançar exceção
    public void IsValidCnpj_Invalidos_RetornaFalse(string? cnpj)
    {
        Assert.False(DocumentValidator.IsValidCnpj(cnpj!));
    }

    [Theory]
    [InlineData("12.ABC.345/01DE-35", "12ABC34501DE35")]
    [InlineData("12abc34501de35", "12ABC34501DE35")]
    [InlineData("11.222.333/0001-81", "11222333000181")]
    [InlineData("  12ABC34501DE35  ", "12ABC34501DE35")]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void NormalizeCnpj_RemoveMascaraEMaiusculiza(string? entrada, string esperado)
    {
        Assert.Equal(esperado, DocumentValidator.NormalizeCnpj(entrada));
    }

    [Fact]
    public void FormatCnpj_Alfanumerico_AplicaMascara()
    {
        Assert.Equal("12.ABC.345/01DE-35", DocumentValidator.FormatCnpj("12ABC34501DE35"));
    }

    [Fact]
    public void FormatCnpj_Numerico_AplicaMascara()
    {
        Assert.Equal("11.222.333/0001-81", DocumentValidator.FormatCnpj("11222333000181"));
    }
}
