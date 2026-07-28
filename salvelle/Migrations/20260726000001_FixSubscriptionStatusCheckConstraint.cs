using Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations
{
    /// <summary>
    /// Corrige a check constraint chk_status da tabela subscriptions.
    /// A constraint original só permitia ACTIVE/PAST_DUE/CANCELED/TRIALING, mas o
    /// código grava status adicionais legítimos (TRIAL_EXPIRED via TrialExpirationJob e
    /// SubscriptionRequiredMiddleware; SUSPENDED via SubscriptionMaintenanceJob;
    /// INCOMPLETE/PAUSED/UNKNOWN via mapeamento do webhook Stripe). Isso fazia o
    /// SaveChanges falhar com "violates check constraint chk_status".
    /// </summary>
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260726000001_FixSubscriptionStatusCheckConstraint")]
    public partial class FixSubscriptionStatusCheckConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE subscriptions DROP CONSTRAINT IF EXISTS chk_status;
ALTER TABLE subscriptions ADD CONSTRAINT chk_status CHECK (status IN (
    'ACTIVE', 'PAST_DUE', 'CANCELED', 'TRIALING',
    'TRIAL_EXPIRED', 'SUSPENDED', 'INCOMPLETE', 'PAUSED', 'UNKNOWN'
));
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER TABLE subscriptions DROP CONSTRAINT IF EXISTS chk_status;
ALTER TABLE subscriptions ADD CONSTRAINT chk_status CHECK (status IN (
    'ACTIVE', 'PAST_DUE', 'CANCELED', 'TRIALING'
));
");
        }
    }
}
