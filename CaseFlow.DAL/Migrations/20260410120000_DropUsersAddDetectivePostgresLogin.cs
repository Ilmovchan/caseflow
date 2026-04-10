using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CaseFlow.DAL.Migrations
{
    /// <inheritdoc />
    public partial class DropUsersAddDetectivePostgresLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "postgres_login",
                table: "detective",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE detective d
                SET postgres_login = u."Username"
                FROM "Users" u
                WHERE u."Email" = d.email AND u."Role" = 'Detective';
                """);

            migrationBuilder.DropTable(name: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_detective_postgres_login",
                table: "detective",
                column: "postgres_login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_detective_postgres_login",
                table: "detective");

            migrationBuilder.DropColumn(
                name: "postgres_login",
                table: "detective");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("created_at_format", "\"CreatedAt\" <= CURRENT_TIMESTAMP");
                    table.CheckConstraint("email_format", "\"Email\" ~ '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$'");
                    table.CheckConstraint("last_login_at_format", "\"LastLoginAt\" IS NULL OR \"LastLoginAt\" <= CURRENT_TIMESTAMP");
                    table.CheckConstraint("role_format", "\"Role\" IN ('Admin', 'Detective')");
                    table.CheckConstraint("username_format", "\"Username\" ~ '^[a-zA-Z0-9_]+$'");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }
    }
}
