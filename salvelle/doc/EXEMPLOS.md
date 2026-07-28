# 📘 Exemplos Práticos de Uso - Fase 1

## 🎯 Cenários Comuns de Uso

### 1️⃣ Criar um Novo Funcionário

```csharp
using Models.Employees;
using DTOs.Employees;
using Helpers;
using Isopoh.Cryptography.Argon2;

// ==================== VALIDAR CPF ====================
var cpf = "12345678909";
if (!DocumentValidator.IsValidCpf(cpf))
{
    return BadRequest(new { error = "CPF inválido" });
}

// Remover formatação
cpf = DocumentValidator.RemoveFormatting(cpf);

// ==================== CRIAR FUNCIONÁRIO ====================
var employee = new Employee
{
    EstablishmentId = Guid.Parse("..."),
    JobPositionId = Guid.Parse("..."), // Cargo inicial
    
    // Dados Pessoais
    FullName = "João da Silva Santos",
    SocialName = null, // Opcional
    Cpf = cpf,
    Rg = "123456789",
    RgIssuer = "SSP/SP",
    RgIssueDate = new DateOnly(2010, 5, 15),
    DateOfBirth = new DateOnly(1990, 3, 20),
    Gender = "Masculino",
    Nationality = "Brasileira",
    PlaceOfBirth = "São Paulo/SP",
    MaritalStatus = "Solteiro",
    
    // Documentos Trabalhistas
    Ctps = "12345678",
    CtpsSeries = "001",
    CtpsUf = "SP",
    CtpsIssueDate = new DateOnly(2008, 1, 10),
    PisPasep = "12345678901",
    
    // Contatos
    Phone = "11987654321",
    WhatsApp = "11987654321",
    Email = "joao.silva@example.com",
    
    // Endereço
    Street = "Rua das Flores",
    Number = "123",
    Complement = "Apto 45",
    Neighborhood = "Centro",
    City = "São Paulo",
    State = "SP",
    PostalCode = "01310100",
    
    // Dados Trabalhistas
    HireDate = DateOnly.FromDateTime(DateTime.Now),
    ContractType = "CLT",
    WorkShift = "Diurno",
    Salary = 3500.00m,
    Department = "Vendas",
    
    // Dados Bancários
    BankCode = "001",
    BankName = "Banco do Brasil",
    BankBranch = "1234",
    BankAccount = "123456",
    BankAccountType = "Corrente",
    BankAccountDigit = "7",
    
    // Contato de Emergência
    EmergencyContactName = "Maria Silva Santos",
    EmergencyContactRelationship = "Mãe",
    EmergencyContactPhone = "11912345678",
    
    // Dependentes
    DependentsCount = 0,
    
    // Status
    Status = "Ativo",
    ProbationEndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(90)), // 90 dias
    
    // Senha temporária
    PasswordHash = Argon2.Hash("SenhaTemporaria@123"),
    PasswordAlgorithm = "argon2id-v1",
    PasswordCreatedAt = DateTime.UtcNow,
    RequirePasswordChange = true, // Forçar troca na primeira vez
    
    // Auditoria
    CreatedAt = DateTime.UtcNow,
    UpdatedAt = DateTime.UtcNow,
    CreatedBy = Guid.Parse("...") // ID do usuário que está criando
};

// Salvar no banco
await _context.Employees.AddAsync(employee);
await _context.SaveChangesAsync();

// Criar histórico inicial
var jobHistory = new EmployeeJobHistory
{
    EmployeeId = employee.Id,
    JobPositionId = employee.JobPositionId,
    StartDate = employee.HireDate,
    IsCurrent = true,
    SalaryAtTime = employee.Salary,
    ChangeReason = "Contratação",
    Notes = "Admissão inicial",
    CreatedBy = employee.CreatedBy
};

await _context.EmployeeJobHistories.AddAsync(jobHistory);
await _context.SaveChangesAsync();

// Enviar email/WhatsApp com credenciais
await NotificationService.SendWelcomeMessage(employee.WhatsApp, "SenhaTemporaria@123");
```

### 2️⃣ Promover Funcionário (Mudança de Cargo)

```csharp
using Models.Employees;
using Helpers;

// ==================== BUSCAR FUNCIONÁRIO ====================
var employeeId = Guid.Parse("...");
var employee = await _context.Employees
    .Include(e => e.JobPosition)
    .FirstOrDefaultAsync(e => e.Id == employeeId);

if (employee == null)
    return NotFound();

// ==================== BUSCAR NOVO CARGO ====================
var newPositionCode = "manager"; // Promovido a gerente
var newPosition = await _context.JobPositions
    .FirstOrDefaultAsync(jp => jp.Code == newPositionCode 
        && jp.EstablishmentId == employee.EstablishmentId);

if (newPosition == null)
    return NotFound(new { error = "Cargo não encontrado" });

// ==================== VERIFICAR HIERARQUIA ====================
if (newPosition.HierarchyLevel <= employee.JobPosition!.HierarchyLevel)
{
    return BadRequest(new { error = "Nova posição deve ter hierarquia superior" });
}

// ==================== CALCULAR NOVO SALÁRIO ====================
var oldSalary = employee.Salary;
var newSalary = oldSalary * 1.20m; // 20% de aumento

// Verificar se está dentro da faixa sugerida
if (newPosition.SuggestedSalaryMin.HasValue && newSalary < newPosition.SuggestedSalaryMin)
{
    newSalary = newPosition.SuggestedSalaryMin.Value;
}

// ==================== FINALIZAR HISTÓRICO ATUAL ====================
var currentHistory = await _context.EmployeeJobHistories
    .FirstOrDefaultAsync(ejh => ejh.EmployeeId == employeeId && ejh.IsCurrent);

if (currentHistory != null)
{
    currentHistory.EndDate = DateOnly.FromDateTime(DateTime.Now);
    currentHistory.IsCurrent = false;
}

// ==================== CRIAR NOVO HISTÓRICO ====================
var newHistory = new EmployeeJobHistory
{
    EmployeeId = employeeId,
    JobPositionId = newPosition.Id,
    StartDate = DateOnly.FromDateTime(DateTime.Now),
    IsCurrent = true,
    SalaryAtTime = newSalary,
    PreviousSalary = oldSalary,
    ChangeReason = "Promoção",
    Notes = $"Promovido de {employee.JobPosition.Name} para {newPosition.Name}",
    ApprovedBy = Guid.Parse("..."), // ID do aprovador
    ApprovedAt = DateTime.UtcNow,
    CreatedBy = Guid.Parse("...") // ID do usuário que está executando
};

await _context.EmployeeJobHistories.AddAsync(newHistory);

// ==================== ATUALIZAR FUNCIONÁRIO ====================
employee.JobPositionId = newPosition.Id;
employee.Salary = newSalary;
employee.UpdatedAt = DateTime.UtcNow;
employee.UpdatedBy = Guid.Parse("...");

await _context.SaveChangesAsync();

// ==================== NOTIFICAR FUNCIONÁRIO ====================
await NotificationService.SendPromotionMessage(
    employee.WhatsApp, 
    employee.FullName,
    newPosition.Name,
    newSalary
);
```

### 3️⃣ Demitir Funcionário

```csharp
using Models.Employees;
using Helpers;

// ==================== BUSCAR FUNCIONÁRIO ====================
var employeeId = Guid.Parse("...");
var employee = await _context.Employees
    .Include(e => e.JobPosition)
    .FirstOrDefaultAsync(e => e.Id == employeeId);

if (employee == null)
    return NotFound();

// ==================== VALIDAÇÕES ====================
if (employee.Status == "Demitido")
    return BadRequest(new { error = "Funcionário já está demitido" });

// ==================== CALCULAR RESCISÃO ====================
var terminationDate = DateOnly.FromDateTime(DateTime.Now);
var (severanceValue, breakdown) = LaborLawCalculator.CalculateSeverancePay(
    salary: employee.Salary,
    hireDate: employee.HireDate,
    terminationDate: terminationDate
);

Console.WriteLine($"Valor da rescisão: R$ {severanceValue:N2}");
Console.WriteLine(breakdown);

// ==================== ATUALIZAR FUNCIONÁRIO ====================
employee.Status = "Demitido";
employee.TerminationDate = terminationDate;
employee.UpdatedAt = DateTime.UtcNow;
employee.UpdatedBy = Guid.Parse("...");

// ==================== FINALIZAR HISTÓRICO DE CARGO ====================
var currentHistory = await _context.EmployeeJobHistories
    .FirstOrDefaultAsync(ejh => ejh.EmployeeId == employeeId && ejh.IsCurrent);

if (currentHistory != null)
{
    currentHistory.EndDate = terminationDate;
    currentHistory.IsCurrent = false;
}

// ==================== REVOGAR TODAS AS SESSÕES ====================
var activeSessions = await _context.EmployeeSessions
    .Where(es => es.EmployeeId == employeeId && es.IsActive)
    .ToListAsync();

foreach (var session in activeSessions)
{
    session.IsActive = false;
    session.RevokedAt = DateTime.UtcNow;
    session.RevocationReason = "Demissão";
}

await _context.SaveChangesAsync();

// ==================== NOTIFICAR RH ====================
await NotificationService.SendTerminationAlert(
    employee.EstablishmentId,
    employee.FullName,
    terminationDate,
    severanceValue
);
```

### 4️⃣ Adicionar Benefício

```csharp
using Models.Employees;

var benefit = new EmployeeBenefit
{
    EmployeeId = Guid.Parse("..."),
    BenefitType = "ValeRefeicao",
    BenefitName = "Vale Refeição",
    Description = "Vale refeição mensal",
    
    MonthlyValue = 500.00m,
    EmployeeContribution = 50.00m, // Funcionário paga 10%
    EmployerContribution = 450.00m, // Empresa paga 90%
    
    StartDate = DateOnly.FromDateTime(DateTime.Now),
    IsActive = true,
    
    ProviderName = "Alelo",
    CardNumber = "1234567890123456",
    
    DeductFromSalary = true,
    DeductionType = "ValorFixo",
    
    Notes = "Benefício padrão da empresa",
    
    CreatedBy = Guid.Parse("...")
};

await _context.EmployeeBenefits.AddAsync(benefit);
await _context.SaveChangesAsync();
```

### 5️⃣ Upload de Documento

```csharp
using Models.Employees;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;

// ==================== RECEBER ARQUIVO ====================
public async Task<IActionResult> UploadDocument(
    Guid employeeId,
    string documentType,
    IFormFile file)
{
    // Validações
    if (file == null || file.Length == 0)
        return BadRequest(new { error = "Arquivo inválido" });
    
    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
    var extension = Path.GetExtension(file.FileName).ToLower();
    
    if (!allowedExtensions.Contains(extension))
        return BadRequest(new { error = "Tipo de arquivo não permitido" });
    
    // ==================== CALCULAR HASH ====================
    string fileHash;
    using (var sha256 = SHA256.Create())
    {
        using var stream = file.OpenReadStream();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        fileHash = Convert.ToHexString(hashBytes).ToLower();
    }
    
    // ==================== SALVAR ARQUIVO ====================
    var fileName = $"{employeeId}_{documentType}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
    var filePath = Path.Combine("uploads", "employees", fileName);
    
    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
    
    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }
    
    // ==================== CRIAR REGISTRO ====================
    var document = new EmployeeDocument
    {
        EmployeeId = employeeId,
        DocumentType = documentType,
        DocumentName = file.FileName,
        Description = $"Upload de {documentType}",
        
        FilePath = filePath,
        FileName = fileName,
        FileExtension = extension,
        MimeType = file.ContentType,
        FileSizeBytes = file.Length,
        FileHash = fileHash,
        
        IssueDate = null, // Pode ser preenchido depois
        ExpiryDate = null, // Pode ser preenchido depois
        HasExpiry = false,
        
        Status = "Pendente",
        Version = 1,
        
        IsConfidential = true,
        IsEncrypted = false,
        
        CreatedBy = Guid.Parse("...") // ID do usuário
    };
    
    await _context.EmployeeDocuments.AddAsync(document);
    await _context.SaveChangesAsync();
    
    return Ok(new { documentId = document.Id, fileName });
}
```

### 6️⃣ Validar Permissões

```csharp
using Models.Security;
using Models.Employees;

// ==================== VERIFICAR SE FUNCIONÁRIO TEM PERMISSÃO ====================
public async Task<bool> HasPermissionAsync(
    Guid employeeId, 
    string resourceAction)
{
    // Buscar funcionário com cargo
    var employee = await _context.Employees
        .Include(e => e.JobPosition)
        .FirstOrDefaultAsync(e => e.Id == employeeId);
    
    if (employee == null || employee.Status != "Ativo")
        return false;
    
    // Buscar permissão
    var permission = await _context.Permissions
        .FirstOrDefaultAsync(p => p.ResourceAction == resourceAction);
    
    if (permission == null)
        return false;
    
    // Verificar se o cargo tem a permissão
    var rolePermission = await _context.RolePermissions
        .FirstOrDefaultAsync(rp => 
            rp.JobPositionId == employee.JobPositionId &&
            rp.PermissionId == permission.Id &&
            rp.IsActive &&
            rp.IsGranted);
    
    if (rolePermission == null)
        return false;
    
    // Verificar se é temporária e ainda está válida
    if (!rolePermission.IsPermanent)
    {
        var now = DateTime.UtcNow;
        
        if (rolePermission.GrantedFrom.HasValue && now < rolePermission.GrantedFrom)
            return false;
        
        if (rolePermission.GrantedUntil.HasValue && now > rolePermission.GrantedUntil)
            return false;
    }
    
    return true;
}

// ==================== EXEMPLO DE USO ====================
if (!await HasPermissionAsync(employeeId, "employees.create"))
{
    return Forbid(); // 403 Forbidden
}

// Ou como atributo (implementar na Fase 2)
[RequirePermission("employees.create")]
public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request)
{
    // ...
}
```

### 7️⃣ Gerar Relatório de Folha de Pagamento

```csharp
using Helpers;

// ==================== CALCULAR FOLHA DE PAGAMENTO ====================
public async Task<object> GeneratePayrollReport(Guid establishmentId, int month, int year)
{
    var employees = await _context.Employees
        .Include(e => e.JobPosition)
        .Include(e => e.Benefits)
        .Where(e => e.EstablishmentId == establishmentId 
            && e.Status == "Ativo")
        .ToListAsync();
    
    var payrollItems = new List<object>();
    
    foreach (var employee in employees)
    {
        // Salário base
        var grossSalary = employee.Salary;
        
        // Calcular descontos
        var (inssValue, inssRate) = LaborLawCalculator.CalculateINSS(grossSalary);
        var (irrfValue, irrfRate) = LaborLawCalculator.CalculateIRRF(
            grossSalary, 
            employee.DependentsCount
        );
        
        // Benefícios
        var benefits = employee.Benefits?
            .Where(b => b.IsActive)
            .Sum(b => b.EmployeeContribution ?? 0) ?? 0;
        
        // Salário líquido
        var netSalary = grossSalary - inssValue - irrfValue - benefits;
        
        payrollItems.Add(new
        {
            employeeId = employee.Id,
            employeeName = employee.FullName,
            jobPosition = employee.JobPosition?.Name,
            grossSalary,
            deductions = new
            {
                inss = inssValue,
                irrf = irrfValue,
                benefits
            },
            netSalary
        });
    }
    
    return new
    {
        establishment = establishmentId,
        period = $"{month:D2}/{year}",
        totalEmployees = employees.Count,
        totalGross = payrollItems.Sum(p => ((dynamic)p).grossSalary),
        totalNet = payrollItems.Sum(p => ((dynamic)p).netSalary),
        items = payrollItems
    };
}
```

## 🎓 Dicas Importantes

1. **Sempre valide CPF** antes de salvar
2. **Use transações** para operações que envolvem múltiplas tabelas
3. **Registre auditoria** (CreatedBy, UpdatedBy)
4. **Envie notificações** para eventos importantes
5. **Calcule corretamente** os valores trabalhistas
6. **Revogue sessões** imediatamente em demissões
7. **Mantenha histórico** de todas as mudanças
8. **Criptografe dados sensíveis** (CPF, Salário)

---

**Esses exemplos cobrem os cenários mais comuns. Na Fase 2, criaremos os Controllers completos!**
