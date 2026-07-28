using Isopoh.Cryptography.Argon2;

namespace Utilities;

/// <summary>
/// Utilitário para gerar hash de senhas usando Argon2
/// Use este código para gerar o hash da senha do Super Admin
/// </summary>
public class PasswordHashGenerator
{
    public static void Main(string[] args)
    {
        Console.WriteLine("=== Gerador de Hash de Senha Argon2 ===");
        Console.WriteLine();
        
        if (args.Length == 0)
        {
            Console.WriteLine("Uso: dotnet run <senha>");
            Console.WriteLine("Exemplo: dotnet run Admin@123");
            Console.WriteLine();
            
            // Gerar hash padrão para Admin@123
            GenerateHash("Admin@123");
        }
        else
        {
            GenerateHash(args[0]);
        }
    }

    public static void GenerateHash(string password)
    {
        Console.WriteLine($"Senha: {password}");
        Console.WriteLine();
        
        var hash = Argon2.Hash(password);
        
        Console.WriteLine("Hash gerado:");
        Console.WriteLine(hash);
        Console.WriteLine();
        Console.WriteLine("Use este hash no script SQL create_super_admin.sql");
        Console.WriteLine("Substitua o valor do campo 'password_hash'");
    }

    /// <summary>
    /// Verifica se uma senha corresponde ao hash
    /// </summary>
    public static bool Verify(string password, string hash)
    {
        return Argon2.Verify(hash, password);
    }
}

/*
COMO USAR:

1. Criar um console app temporário ou adicionar este arquivo ao projeto

2. Executar:
   dotnet run Admin@123
   
3. Copiar o hash gerado

4. Colar no script SQL create_super_admin.sql no campo password_hash

5. Executar o script SQL

EXEMPLO DE SAÍDA:
Senha: Admin@123

Hash gerado:
$argon2id$v=19$m=65536,t=3,p=1$abc123def456$xyz789uvw012...

Use este hash no script SQL create_super_admin.sql
Substitua o valor do campo 'password_hash'
*/
