# 🚀 Comandos de Setup e Migration

## 📦 1. Instalação de Dependências

Execute estes comandos no diretório do projeto:

```bash
# Entity Framework Core
dotnet add package Microsoft.EntityFrameworkCore --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 9.0.0

# PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.0.0

# Argon2 para hash de senhas (já deve estar instalado)
dotnet add package Isopoh.Cryptography.Argon2 --version 2.0.0

# FluentValidation (para próxima fase)
dotnet add package FluentValidation --version 11.9.0
dotnet add package FluentValidation.AspNetCore --version 11.3.0
```

## 🗄️ 2. Configurar Connection String

No arquivo `appsettings.json`, adicione:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=salvelle;Username=seu_usuario;Password=sua_senha"
  }
}
```

No arquivo `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=salvelle_dev;Username=postgres;Password=postgres"
  }
}
```

## 🏗️ 3. Registrar o DbContext no Program.cs

Adicione no seu `Program.cs` (se ainda não estiver):

```csharp
using Microsoft.EntityFrameworkCore;
using Data;

var builder = WebApplication.CreateBuilder(args);

// Adicionar DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ... resto do código
```

## 📊 4. Criar e Aplicar Migration

```bash
# Criar a migration
dotnet ef migrations add AddEmployeeManagementSystem

# Visualizar o SQL que será executado (opcional)
dotnet ef migrations script

# Aplicar ao banco de dados
dotnet ef database update

# Se precisar reverter
dotnet ef database update PreviousMigrationName

# Para remover a última migration não aplicada
dotnet ef migrations remove
```

## ✅ 5. Verificar Tabelas Criadas

Conecte ao PostgreSQL e execute:

```sql
-- Ver todas as tabelas criadas
\dt

-- Ver estrutura da tabela employees
\d employees

-- Ver dados de seed (permissões)
SELECT * FROM permissions;

-- Ver níveis de acesso
SELECT * FROM access_levels;
```

## 🧪 6. Testar os Helpers

Crie um arquivo de teste `TestHelpers.cs`:

```csharp
using Helpers;

public class TestHelpers
{
    public static void TestDocumentValidator()
    {
        // Teste CPF
        var cpf = "12345678909";
        var isValid = DocumentValidator.IsValidCpf(cpf);
        Console.WriteLine($"CPF {cpf} válido: {isValid}");
        
        var formatted = DocumentValidator.FormatCpf(cpf);
        Console.WriteLine($"CPF formatado: {formatted}");
        
        // Teste CNPJ
        var cnpj = "12345678000195";
        var cnpjValid = DocumentValidator.IsValidCnpj(cnpj);
        Console.WriteLine($"CNPJ {cnpj} válido: {cnpjValid}");
        
        // Teste PIS
        var pis = "12345678901";
        var pisValid = DocumentValidator.IsValidPis(pis);
        Console.WriteLine($"PIS {pis} válido: {pisValid}");
    }
    
    public static void TestPasswordValidator()
    {
        var password = "MinhaSenh@123";
        
        var (isValid, errors) = PasswordValidator.ValidatePassword(password);
        Console.WriteLine($"Senha válida: {isValid}");
        
        if (!isValid)
        {
            Console.WriteLine("Erros:");
            errors.ForEach(e => Console.WriteLine($"  - {e}"));
        }
        
        var (strength, level) = PasswordValidator.CalculatePasswordStrength(password);
        Console.WriteLine($"Força da senha: {strength}% ({level})");
        
        var strongPassword = PasswordValidator.GenerateStrongPassword(16);
        Console.WriteLine($"Senha forte gerada: {strongPassword}");
    }
    
    public static void TestLaborLawCalculator()
    {
        decimal salary = 3000m;
        int monthsWorked = 12;
        int dependents = 2;
        
        // Férias
        var vacationPay = LaborLawCalculator.CalculateVacationPay(salary, monthsWorked);
        Console.WriteLine($"Férias: R$ {vacationPay:N2}");
        
        // 13º
        var thirteenth = LaborLawCalculator.Calculate13thSalary(salary, monthsWorked);
        Console.WriteLine($"13º Salário: R$ {thirteenth:N2}");
        
        // FGTS
        var fgts = LaborLawCalculator.CalculateFGTS(salary);
        Console.WriteLine($"FGTS: R$ {fgts:N2}");
        
        // INSS
        var (inssValue, inssRate) = LaborLawCalculator.CalculateINSS(salary);
        Console.WriteLine($"INSS: R$ {inssValue:N2} ({inssRate:N2}%)");
        
        // IRRF
        var (irrfValue, irrfRate) = LaborLawCalculator.CalculateIRRF(salary, dependents);
        Console.WriteLine($"IRRF: R$ {irrfValue:N2} ({irrfRate:N2}%)");
        
        // Salário Líquido
        var netSalary = LaborLawCalculator.CalculateNetSalary(salary, dependents);
        Console.WriteLine($"Salário Líquido: R$ {netSalary:N2}");
        
        // Rescisão
        var hireDate = new DateOnly(2020, 1, 1);
        var terminationDate = new DateOnly(2024, 12, 31);
        var (severanceValue, breakdown) = LaborLawCalculator.CalculateSeverancePay(
            salary, hireDate, terminationDate
        );
        Console.WriteLine($"Rescisão: R$ {severanceValue:N2}");
        Console.WriteLine(breakdown);
    }
}
```

Execute no Program.cs:

```csharp
// Em Program.cs, antes do app.Run()
if (app.Environment.IsDevelopment())
{
    TestHelpers.TestDocumentValidator();
    TestHelpers.TestPasswordValidator();
    TestHelpers.TestLaborLawCalculator();
}
```

## 🔄 7. Seed Adicional (Opcional)

Se quiser adicionar dados de teste, crie um arquivo `SeedData.cs`:

```csharp
using Data;
using Models;
using Models.Employees;
using Isopoh.Cryptography.Argon2;

public static class SeedData
{
    public static async Task SeedTestData(AppDbContext context)
    {
        // Verificar se já tem dados
        if (context.Employees.Any()) return;
        
        // Buscar um estabelecimento existente
        var establishment = await context.Establishments.FirstOrDefaultAsync();
        if (establishment == null)
        {
            Console.WriteLine("Nenhum estabelecimento encontrado. Crie um estabelecimento primeiro.");
            return;
        }
        
        // Criar cargos padrão
        var ownerPosition = new JobPosition
        {
            EstablishmentId = establishment.Id,
            Code = "owner",
            Name = "Proprietário",
            HierarchyLevel = 10,
            IsSystemDefault = true
        };
        
        var managerPosition = new JobPosition
        {
            EstablishmentId = establishment.Id,
            Code = "manager",
            Name = "Gerente",
            HierarchyLevel = 8,
            ReportsTo = ownerPosition.Id,
            IsSystemDefault = true
        };
        
        var employeePosition = new JobPosition
        {
            EstablishmentId = establishment.Id,
            Code = "employee",
            Name = "Funcionário",
            HierarchyLevel = 4,
            ReportsTo = managerPosition.Id,
            IsSystemDefault = true
        };
        
        context.JobPositions.AddRange(ownerPosition, managerPosition, employeePosition);
        await context.SaveChangesAsync();
        
        // Criar funcionário de teste
        var employee = new Employee
        {
            EstablishmentId = establishment.Id,
            JobPositionId = employeePosition.Id,
            FullName = "João da Silva",
            Cpf = "12345678909",
            Email = "joao@example.com",
            DateOfBirth = new DateOnly(1990, 1, 1),
            HireDate = DateOnly.FromDateTime(DateTime.Now),
            Salary = 3000m,
            Street = "Rua Teste",
            Number = "123",
            Neighborhood = "Centro",
            City = "São Paulo",
            State = "SP",
            PostalCode = "01000000",
            ContractType = "CLT",
            Status = "Ativo",
            PasswordHash = Argon2.Hash("Senha@123"),
            PasswordAlgorithm = "argon2id-v1",
            PasswordCreatedAt = DateTime.UtcNow,
            RequirePasswordChange = true
        };
        
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        
        Console.WriteLine("Dados de teste criados com sucesso!");
    }
}
```

Execute no Program.cs:

```csharp
// Em Program.cs, antes do app.Run()
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.SeedTestData(context);
}
```

## 🐳 8. Docker (Opcional)

Se quiser rodar PostgreSQL no Docker:

```bash
# Criar e iniciar container PostgreSQL
docker run --name salvelle-postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=salvelle \
  -p 5432:5432 \
  -d postgres:16

# Ver logs
docker logs salvelle-postgres

# Conectar ao banco
docker exec -it salvelle-postgres psql -U postgres -d salvelle

# Parar container
docker stop salvelle-postgres

# Iniciar container
docker start salvelle-postgres

# Remover container
docker rm -f salvelle-postgres
```

## ✅ 9. Checklist de Instalação

- [ ] Dependências instaladas
- [ ] Connection string configurada
- [ ] DbContext registrado no Program.cs
- [ ] Migration criada
- [ ] Migration aplicada ao banco
- [ ] Tabelas verificadas no PostgreSQL
- [ ] Helpers testados
- [ ] Seed data aplicado (opcional)

## 🆘 Troubleshooting

### Erro: "No database provider has been configured"
```csharp
// Certifique-se de ter no Program.cs:
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### Erro: "Failed to connect to database"
- Verifique se o PostgreSQL está rodando
- Verifique a connection string
- Teste a conexão: `psql -h localhost -U postgres -d salvelle`

### Erro: "Migration already exists"
```bash
# Remover última migration
dotnet ef migrations remove

# Recriar
dotnet ef migrations add AddEmployeeManagementSystem
```

### Erro: "Table already exists"
```bash
# Dropar o banco e recriar
dotnet ef database drop
dotnet ef database update
```

## 📞 Próximo Chat

Quando estiver pronto para a **Fase 2**, inicie o chat com:

> "Estou pronto para a Fase 2 do sistema de funcionários. A Fase 1 está instalada e funcionando."

---

**Boa sorte! 🚀**
