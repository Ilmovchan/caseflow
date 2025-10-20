using System;
using CaseFlow.DAL.Enums;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CaseFlow.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalStatusToSuspectAndEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<ApprovalStatus>(
                name: "approval_status",
                table: "suspect",
                type: "approval_status",
                nullable: false,
                defaultValue: ApprovalStatus.Draft);

            migrationBuilder.AlterColumn<ApprovalStatus>(
                name: "approval_status",
                table: "report",
                type: "approval_status",
                nullable: false,
                defaultValue: ApprovalStatus.Draft,
                oldClrType: typeof(string),
                oldType: "approval_status",
                oldDefaultValue: "Чернетка");

            migrationBuilder.AlterColumn<ApprovalStatus>(
                name: "approval_status",
                table: "expense",
                type: "approval_status",
                nullable: false,
                defaultValue: ApprovalStatus.Draft,
                oldClrType: typeof(string),
                oldType: "approval_status",
                oldDefaultValue: "Чернетка");

            migrationBuilder.AddColumn<ApprovalStatus>(
                name: "approval_status",
                table: "evidence",
                type: "approval_status",
                nullable: false,
                defaultValue: ApprovalStatus.Draft);

            migrationBuilder.AlterColumn<DetectiveStatus>(
                name: "status",
                table: "detective",
                type: "detective_status",
                nullable: false,
                defaultValue: DetectiveStatus.Active,
                oldClrType: typeof(string),
                oldType: "detective_status",
                oldDefaultValue: "Активний(а)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "hire_date",
                table: "detective",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<ApprovalStatus>(
                name: "approval_status",
                table: "case_suspect",
                type: "approval_status",
                nullable: false,
                defaultValue: ApprovalStatus.Draft,
                oldClrType: typeof(string),
                oldType: "approval_status",
                oldDefaultValue: "Чернетка");

            migrationBuilder.AlterColumn<ApprovalStatus>(
                name: "approval_status",
                table: "case_evidence",
                type: "approval_status",
                nullable: false,
                defaultValue: ApprovalStatus.Draft,
                oldClrType: typeof(string),
                oldType: "approval_status",
                oldDefaultValue: "Чернетка");

            migrationBuilder.AlterColumn<CaseStatus>(
                name: "status",
                table: "case",
                type: "case_status",
                nullable: false,
                defaultValue: CaseStatus.Opened,
                oldClrType: typeof(string),
                oldType: "case_status",
                oldDefaultValue: "Відкрито");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "approval_status",
                table: "suspect");

            migrationBuilder.DropColumn(
                name: "approval_status",
                table: "evidence");

            migrationBuilder.AlterColumn<string>(
                name: "approval_status",
                table: "report",
                type: "approval_status",
                nullable: false,
                defaultValue: "Чернетка",
                oldClrType: typeof(ApprovalStatus),
                oldType: "approval_status",
                oldDefaultValue: ApprovalStatus.Draft);

            migrationBuilder.AlterColumn<string>(
                name: "approval_status",
                table: "expense",
                type: "approval_status",
                nullable: false,
                defaultValue: "Чернетка",
                oldClrType: typeof(ApprovalStatus),
                oldType: "approval_status",
                oldDefaultValue: ApprovalStatus.Draft);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "detective",
                type: "detective_status",
                nullable: false,
                defaultValue: "Активний(а)",
                oldClrType: typeof(DetectiveStatus),
                oldType: "detective_status",
                oldDefaultValue: DetectiveStatus.Active);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "hire_date",
                table: "detective",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<string>(
                name: "approval_status",
                table: "case_suspect",
                type: "approval_status",
                nullable: false,
                defaultValue: "Чернетка",
                oldClrType: typeof(ApprovalStatus),
                oldType: "approval_status",
                oldDefaultValue: ApprovalStatus.Draft);

            migrationBuilder.AlterColumn<string>(
                name: "approval_status",
                table: "case_evidence",
                type: "approval_status",
                nullable: false,
                defaultValue: "Чернетка",
                oldClrType: typeof(ApprovalStatus),
                oldType: "approval_status",
                oldDefaultValue: ApprovalStatus.Draft);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "case",
                type: "case_status",
                nullable: false,
                defaultValue: "Відкрито",
                oldClrType: typeof(CaseStatus),
                oldType: "case_status",
                oldDefaultValue: CaseStatus.Opened);
        }
    }
}
