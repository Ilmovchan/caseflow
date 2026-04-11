using CaseFlow.DAL.Enums;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaseFlow.DAL.Migrations;

/// <inheritdoc />
public partial class JunctionApprovalRemovedAndCreatedByDetective : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "created_by_detective_id",
            table: "evidence",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "created_by_detective_id",
            table: "suspect",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "created_by_detective_id",
            table: "report",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "created_by_detective_id",
            table: "expense",
            type: "integer",
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE evidence e
            SET approval_status = x.approval_status
            FROM (
              SELECT evidence_id, (array_agg(approval_status ORDER BY case_id))[1] AS approval_status
              FROM case_evidence
              GROUP BY evidence_id
            ) x
            WHERE e.id = x.evidence_id;

            UPDATE suspect s
            SET approval_status = x.approval_status
            FROM (
              SELECT suspect_id, (array_agg(approval_status ORDER BY case_id))[1] AS approval_status
              FROM case_suspect
              GROUP BY suspect_id
            ) x
            WHERE s.id = x.suspect_id;

            UPDATE evidence e
            SET created_by_detective_id = x.detective_id
            FROM (
              SELECT DISTINCT ON (ce.evidence_id) ce.evidence_id, c.detective_id
              FROM case_evidence ce
              INNER JOIN "case" c ON c.id = ce.case_id
              WHERE c.detective_id IS NOT NULL
              ORDER BY ce.evidence_id, ce.case_id
            ) x
            WHERE e.id = x.evidence_id;

            UPDATE suspect s
            SET created_by_detective_id = x.detective_id
            FROM (
              SELECT DISTINCT ON (cs.suspect_id) cs.suspect_id, c.detective_id
              FROM case_suspect cs
              INNER JOIN "case" c ON c.id = cs.case_id
              WHERE c.detective_id IS NOT NULL
              ORDER BY cs.suspect_id, cs.case_id
            ) x
            WHERE s.id = x.suspect_id;

            UPDATE report r
            SET created_by_detective_id = c.detective_id
            FROM "case" c
            WHERE r.case_id = c.id AND c.detective_id IS NOT NULL;

            UPDATE expense e
            SET created_by_detective_id = c.detective_id
            FROM "case" c
            WHERE e.case_id = c.id AND c.detective_id IS NOT NULL;
            """);

        migrationBuilder.DropColumn(
            name: "approval_status",
            table: "case_evidence");

        migrationBuilder.DropColumn(
            name: "approval_status",
            table: "case_suspect");

        migrationBuilder.CreateIndex(
            name: "IX_evidence_created_by_detective_id",
            table: "evidence",
            column: "created_by_detective_id");

        migrationBuilder.CreateIndex(
            name: "IX_suspect_created_by_detective_id",
            table: "suspect",
            column: "created_by_detective_id");

        migrationBuilder.CreateIndex(
            name: "IX_report_created_by_detective_id",
            table: "report",
            column: "created_by_detective_id");

        migrationBuilder.CreateIndex(
            name: "IX_expense_created_by_detective_id",
            table: "expense",
            column: "created_by_detective_id");

        migrationBuilder.AddForeignKey(
            name: "FK_evidence_detective_created_by_detective_id",
            table: "evidence",
            column: "created_by_detective_id",
            principalTable: "detective",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_suspect_detective_created_by_detective_id",
            table: "suspect",
            column: "created_by_detective_id",
            principalTable: "detective",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_report_detective_created_by_detective_id",
            table: "report",
            column: "created_by_detective_id",
            principalTable: "detective",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_expense_detective_created_by_detective_id",
            table: "expense",
            column: "created_by_detective_id",
            principalTable: "detective",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_evidence_detective_created_by_detective_id",
            table: "evidence");

        migrationBuilder.DropForeignKey(
            name: "FK_suspect_detective_created_by_detective_id",
            table: "suspect");

        migrationBuilder.DropForeignKey(
            name: "FK_report_detective_created_by_detective_id",
            table: "report");

        migrationBuilder.DropForeignKey(
            name: "FK_expense_detective_created_by_detective_id",
            table: "expense");

        migrationBuilder.DropIndex(
            name: "IX_evidence_created_by_detective_id",
            table: "evidence");

        migrationBuilder.DropIndex(
            name: "IX_suspect_created_by_detective_id",
            table: "suspect");

        migrationBuilder.DropIndex(
            name: "IX_report_created_by_detective_id",
            table: "report");

        migrationBuilder.DropIndex(
            name: "IX_expense_created_by_detective_id",
            table: "expense");

        migrationBuilder.AddColumn<ApprovalStatus>(
            name: "approval_status",
            table: "case_evidence",
            type: "approval_status",
            nullable: false,
            defaultValue: ApprovalStatus.Draft);

        migrationBuilder.AddColumn<ApprovalStatus>(
            name: "approval_status",
            table: "case_suspect",
            type: "approval_status",
            nullable: false,
            defaultValue: ApprovalStatus.Draft);

        migrationBuilder.Sql("""
            UPDATE case_evidence ce
            SET approval_status = e.approval_status
            FROM evidence e
            WHERE ce.evidence_id = e.id;

            UPDATE case_suspect cs
            SET approval_status = s.approval_status
            FROM suspect s
            WHERE cs.suspect_id = s.id;
            """);

        migrationBuilder.DropColumn(
            name: "created_by_detective_id",
            table: "evidence");

        migrationBuilder.DropColumn(
            name: "created_by_detective_id",
            table: "suspect");

        migrationBuilder.DropColumn(
            name: "created_by_detective_id",
            table: "report");

        migrationBuilder.DropColumn(
            name: "created_by_detective_id",
            table: "expense");
    }
}
