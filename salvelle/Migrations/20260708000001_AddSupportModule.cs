using Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260708000001_AddSupportModule")]
    public partial class AddSupportModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS support_tickets (
    id UUID PRIMARY KEY,
    origin VARCHAR(20) NOT NULL DEFAULT 'MANUAL',
    category VARCHAR(30) NOT NULL DEFAULT 'GENERAL',
    establishment_id UUID NULL REFERENCES ""Establishments""(""Id"") ON DELETE SET NULL,
    title VARCHAR(200) NOT NULL DEFAULT '',
    description TEXT NOT NULL DEFAULT '',
    priority VARCHAR(10) NOT NULL DEFAULT 'MEDIUM',
    status VARCHAR(20) NOT NULL DEFAULT 'OPEN',
    deduplication_key VARCHAR(100) NULL,
    is_auto_resolvable BOOLEAN NOT NULL DEFAULT FALSE,
    assigned_to VARCHAR(100) NULL,
    resolved_at TIMESTAMPTZ NULL,
    closed_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_support_tickets_status ON support_tickets(status);
CREATE INDEX IF NOT EXISTS ix_support_tickets_establishment ON support_tickets(establishment_id);
CREATE INDEX IF NOT EXISTS ix_support_tickets_dedup ON support_tickets(deduplication_key);
CREATE INDEX IF NOT EXISTS ix_support_tickets_created ON support_tickets(created_at DESC);

CREATE TABLE IF NOT EXISTS support_ticket_messages (
    id UUID PRIMARY KEY,
    ticket_id UUID NOT NULL REFERENCES support_tickets(id) ON DELETE CASCADE,
    author_type VARCHAR(20) NOT NULL DEFAULT 'PHARMACY',
    author_id UUID NULL,
    author_name VARCHAR(100) NOT NULL DEFAULT '',
    body TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS ix_support_ticket_messages_ticket ON support_ticket_messages(ticket_id);

CREATE TABLE IF NOT EXISTS whatsapp_instance_status (
    instance_name VARCHAR(100) PRIMARY KEY,
    status VARCHAR(20) NOT NULL DEFAULT 'UNKNOWN',
    last_checked_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    disconnected_since TIMESTAMPTZ NULL,
    last_connected_at TIMESTAMPTZ NULL,
    active_ticket_id UUID NULL
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TABLE IF EXISTS support_ticket_messages;
DROP TABLE IF EXISTS support_tickets;
DROP TABLE IF EXISTS whatsapp_instance_status;
");
        }
    }
}
