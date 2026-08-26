using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Connection string para migrations em design-time.
        // Lida do ambiente para nao versionar segredos. Ex.:
        //   export ConnectionStrings__DefaultConnection="Host=...;Password=..."
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=pg.atentbot.com;Port=5432;Database=salvelle;Username=postgres;Password=CHANGE_ME;";

        optionsBuilder.UseNpgsql(connectionString);
        // Design-time apenas: o build de migrations compila os fontes com codepage divergente,
        // gerando "drift" espúrio em strings acentuadas (defaults/seed). Não é mudança de schema.
        optionsBuilder.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));

        return new AppDbContext(optionsBuilder.Options);
    }
}