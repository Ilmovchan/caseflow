using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseFlow.DAL.Migrations
{
    /// <inheritdoc />
    public partial class FixDetectiveHireDateConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "detective_hire_date_format",
                table: "detective");

            migrationBuilder.AddCheckConstraint(
                name: "detective_hire_date_format",
                table: "detective",
                sql: "hire_date::date <= CURRENT_DATE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "detective_hire_date_format",
                table: "detective");

            migrationBuilder.AddCheckConstraint(
                name: "detective_hire_date_format",
                table: "detective",
                sql: "hire_date <= CURRENT_DATE");
        }
    }
}
