using System;

namespace Helpers;

public static class LaborLawCalculator
{
    /// <summary>
    /// Calcula o valor de férias (1/12 por mês trabalhado)
    /// </summary>
    public static decimal CalculateVacationPay(decimal salary, int monthsWorked)
    {
        if (monthsWorked < 1) return 0;
        
        // Férias = salário + 1/3 do salário
        decimal vacationBase = salary / 12 * Math.Min(monthsWorked, 12);
        decimal constitutionalBonus = vacationBase / 3; // 1/3 constitucional
        
        return vacationBase + constitutionalBonus;
    }

    /// <summary>
    /// Calcula o 13º salário (1/12 por mês trabalhado)
    /// </summary>
    public static decimal Calculate13thSalary(decimal salary, int monthsWorked)
    {
        if (monthsWorked < 1) return 0;
        
        // 13º = (salário / 12) * meses trabalhados
        // Considera mês completo se trabalhou 15 dias ou mais
        return salary / 12 * Math.Min(monthsWorked, 12);
    }

    /// <summary>
    /// Calcula o FGTS (8% sobre o salário)
    /// </summary>
    public static decimal CalculateFGTS(decimal salary)
    {
        return salary * 0.08m;
    }

    /// <summary>
    /// Calcula o INSS de acordo com a tabela progressiva 2025
    /// </summary>
    public static (decimal inssValue, decimal effectiveRate) CalculateINSS(decimal salary)
    {
        // Tabela INSS 2025 (valores aproximados - atualizar conforme INSS)
        const decimal bracket1 = 1412.00m;   // Até 1 salário mínimo
        const decimal bracket2 = 2666.68m;   
        const decimal bracket3 = 4000.03m;   
        const decimal bracket4 = 7786.02m;   // Teto
        
        const decimal rate1 = 0.075m;  // 7.5%
        const decimal rate2 = 0.09m;   // 9%
        const decimal rate3 = 0.12m;   // 12%
        const decimal rate4 = 0.14m;   // 14%

        decimal inss = 0;

        if (salary <= bracket1)
        {
            inss = salary * rate1;
        }
        else if (salary <= bracket2)
        {
            inss = bracket1 * rate1 + (salary - bracket1) * rate2;
        }
        else if (salary <= bracket3)
        {
            inss = bracket1 * rate1 + (bracket2 - bracket1) * rate2 + (salary - bracket2) * rate3;
        }
        else if (salary <= bracket4)
        {
            inss = bracket1 * rate1 + (bracket2 - bracket1) * rate2 + 
                   (bracket3 - bracket2) * rate3 + (salary - bracket3) * rate4;
        }
        else
        {
            // Acima do teto, usa o valor máximo
            inss = bracket1 * rate1 + (bracket2 - bracket1) * rate2 + 
                   (bracket3 - bracket2) * rate3 + (bracket4 - bracket3) * rate4;
        }

        decimal effectiveRate = salary > 0 ? inss / salary : 0;
        return (Math.Round(inss, 2), Math.Round(effectiveRate * 100, 2));
    }

    /// <summary>
    /// Calcula o IRRF (Imposto de Renda Retido na Fonte) de acordo com a tabela progressiva 2025
    /// </summary>
    public static (decimal irrfValue, decimal effectiveRate) CalculateIRRF(decimal salary, int dependents = 0)
    {
        // Calcula INSS para deduzir da base
        var (inssValue, _) = CalculateINSS(salary);
        
        // Dedução por dependente (valor aproximado 2025)
        const decimal dependentDeduction = 189.59m;
        decimal totalDependentDeduction = dependents * dependentDeduction;

        // Base de cálculo
        decimal taxableIncome = salary - inssValue - totalDependentDeduction;

        if (taxableIncome <= 0) return (0, 0);

        // Tabela IRRF 2025 (valores aproximados - atualizar conforme Receita Federal)
        const decimal bracket1 = 2259.20m;
        const decimal bracket2 = 2826.65m;
        const decimal bracket3 = 3751.05m;
        const decimal bracket4 = 4664.68m;

        const decimal rate1 = 0.00m;   // Isento
        const decimal rate2 = 0.075m;  // 7.5%
        const decimal rate3 = 0.15m;   // 15%
        const decimal rate4 = 0.225m;  // 22.5%
        const decimal rate5 = 0.275m;  // 27.5%

        const decimal deduction2 = 169.44m;
        const decimal deduction3 = 381.44m;
        const decimal deduction4 = 662.77m;
        const decimal deduction5 = 896.00m;

        decimal irrf = 0;

        if (taxableIncome <= bracket1)
        {
            irrf = 0; // Isento
        }
        else if (taxableIncome <= bracket2)
        {
            irrf = taxableIncome * rate2 - deduction2;
        }
        else if (taxableIncome <= bracket3)
        {
            irrf = taxableIncome * rate3 - deduction3;
        }
        else if (taxableIncome <= bracket4)
        {
            irrf = taxableIncome * rate4 - deduction4;
        }
        else
        {
            irrf = taxableIncome * rate5 - deduction5;
        }

        irrf = Math.Max(irrf, 0); // Nunca negativo
        decimal effectiveRate = salary > 0 ? irrf / salary : 0;
        
        return (Math.Round(irrf, 2), Math.Round(effectiveRate * 100, 2));
    }

    /// <summary>
    /// Calcula o salário líquido após descontos
    /// </summary>
    public static decimal CalculateNetSalary(decimal grossSalary, int dependents = 0, decimal otherDeductions = 0)
    {
        var (inssValue, _) = CalculateINSS(grossSalary);
        var (irrfValue, _) = CalculateIRRF(grossSalary, dependents);

        decimal netSalary = grossSalary - inssValue - irrfValue - otherDeductions;
        return Math.Round(netSalary, 2);
    }

    /// <summary>
    /// Calcula dias de férias proporcionais (1/12 por mês)
    /// </summary>
    public static int CalculateVacationDays(int monthsWorked)
    {
        if (monthsWorked < 1) return 0;
        
        // 30 dias a cada 12 meses
        int days = 30 * Math.Min(monthsWorked, 12) / 12;
        return Math.Max(days, 0);
    }

    /// <summary>
    /// Verifica se o funcionário está em período de experiência
    /// </summary>
    public static bool IsInProbationPeriod(DateOnly hireDate, DateOnly currentDate)
    {
        int daysDiff = currentDate.DayNumber - hireDate.DayNumber;
        return daysDiff <= 90; // 90 dias de experiência
    }

    /// <summary>
    /// Calcula aviso prévio (30 dias + 3 dias por ano trabalhado, até 90 dias)
    /// </summary>
    public static int CalculateNoticePeriodDays(DateOnly hireDate, DateOnly terminationDate)
    {
        int yearsWorked = terminationDate.Year - hireDate.Year;
        
        // Ajusta se não completou aniversário no ano
        if (terminationDate.Month < hireDate.Month || 
            terminationDate.Month == hireDate.Month && terminationDate.Day < hireDate.Day)
        {
            yearsWorked--;
        }

        // Base: 30 dias + 3 dias por ano (máximo 90 dias)
        int noticeDays = 30 + Math.Min(yearsWorked, 20) * 3;
        return Math.Min(noticeDays, 90);
    }

    /// <summary>
    /// Calcula valor de rescisão (demissão sem justa causa)
    /// </summary>
    public static (decimal totalValue, string breakdown) CalculateSeverancePay(
        decimal salary, 
        DateOnly hireDate, 
        DateOnly terminationDate,
        int vacationDaysNotTaken = 0)
    {
        int monthsWorked = (terminationDate.Year - hireDate.Year) * 12 + 
                          (terminationDate.Month - hireDate.Month);

        decimal thirteenth = Calculate13thSalary(salary, monthsWorked % 12);
        decimal vacationPay = CalculateVacationPay(salary, monthsWorked % 12);
        
        // Saldo de salário (dias trabalhados no mês)
        int daysWorked = terminationDate.Day;
        decimal salaryBalance = salary / 30 * daysWorked;

        // Multa FGTS (40%)
        decimal fgtsBalance = CalculateFGTS(salary) * monthsWorked;
        decimal fgtsPenalty = fgtsBalance * 0.40m;

        // Aviso prévio indenizado
        int noticeDays = CalculateNoticePeriodDays(hireDate, terminationDate);
        decimal noticeValue = salary / 30 * noticeDays;

        decimal total = salaryBalance + thirteenth + vacationPay + fgtsPenalty + noticeValue;

        string breakdown = $@"
Saldo de Salário: R$ {salaryBalance:N2}
13º Proporcional: R$ {thirteenth:N2}
Férias Proporcionais: R$ {vacationPay:N2}
Multa FGTS (40%): R$ {fgtsPenalty:N2}
Aviso Prévio: R$ {noticeValue:N2}
TOTAL: R$ {total:N2}
        ";

        return (Math.Round(total, 2), breakdown);
    }
}
