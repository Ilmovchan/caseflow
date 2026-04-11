using System;
using CaseFlow.DAL.Enums;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CaseFlow.DAL.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:approval_status", "Відхилено,Надіслано,Схвалено,Чернетка")
                .Annotation("Npgsql:Enum:case_status", "Відкрито,Закрито,Призупинено")
                .Annotation("Npgsql:Enum:detective_status", "Активний(а),Звільнений(а),У відпустці,У відставці")
                .Annotation("Npgsql:Enum:evidence_type", "Інше,Аудіодоказ,Біологічний доказ,Біометричний доказ,Відеодоказ,Документальний доказ,Електронний доказ,Матеріальний доказ,Фотодоказ,Фізичний доказ,Цифровий доказ")
                .Annotation("Npgsql:Enum:public.approval_status", "Чернетка,Надіслано,Схвалено,Відхилено")
                .Annotation("Npgsql:Enum:public.case_status", "Відкрито,Закрито,Призупинено")
                .Annotation("Npgsql:Enum:public.detective_status", "Активний(а),У відпустці,У відставці,Звільнений(а)")
                .Annotation("Npgsql:Enum:public.evidence_type", "Біометричний доказ,Біологічний доказ,Відеодоказ,Фотодоказ,Матеріальний доказ,Цифровий доказ,Документальний доказ,Аудіодоказ,Фізичний доказ,Електронний доказ,Інше");

            migrationBuilder.CreateTable(
                name: "case_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_case_type", x => x.id);
                    table.CheckConstraint("name_format", "name ~ '^[А-ЯІЇЄа-яіїє0-9\\s\\-.,:;/]+$'");
                    table.CheckConstraint("price_format", "price > 0");
                });

            migrationBuilder.CreateTable(
                name: "client",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    apartment_number = table.Column<int>(type: "integer", nullable: true),
                    building_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    city = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    father_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    region = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    registration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    street = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_client", x => x.id);
                    table.CheckConstraint("apartment_number_format", "apartment_number IS NULL OR apartment_number > 0");
                    table.CheckConstraint("building_number_format", "building_number ~ '^[0-9/]+$'");
                    table.CheckConstraint("city_format", "city ~ '^[А-ЯІЇЄа-яіїє\\-]+$'");
                    table.CheckConstraint("date_of_birth_format", "date_of_birth <= CURRENT_DATE");
                    table.CheckConstraint("email_format", "email ~ '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$'");
                    table.CheckConstraint("name_format", "first_name ~ '^[А-ЯІЇЄа-яіїє]+$' AND last_name ~ '^[А-ЯІЇЄа-яіїє]+$' AND (father_name IS NULL OR father_name ~ '^[А-ЯІЇЄа-яіїє]+$')");
                    table.CheckConstraint("phone_number_format", "phone_number ~ '^\\+380\\d{9}$'");
                    table.CheckConstraint("region_format", "region ~ '^[А-ЯІЇЄа-яіїє]+$'");
                    table.CheckConstraint("registration_date_format", "registration_date <= CURRENT_TIMESTAMP");
                    table.CheckConstraint("street_format", "street ~ '^[А-ЯІЇЄа-яіїє\\s\\-]+$'");
                });

            migrationBuilder.CreateTable(
                name: "detective",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    father_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    region = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    city = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    street = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    building_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    apartment_number = table.Column<int>(type: "integer", nullable: true),
                    hire_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    salary = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    personal_notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<DetectiveStatus>(type: "detective_status", nullable: false, defaultValue: DetectiveStatus.Active),
                    postgres_login = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detective", x => x.id);
                    table.CheckConstraint("apartment_number_format", "apartment_number IS NULL OR apartment_number > 0");
                    table.CheckConstraint("building_number_format", "building_number ~ '^[0-9/]+$'");
                    table.CheckConstraint("city_format", "city ~ '^[А-ЯІЇЄа-яіїє\\-]+$'");
                    table.CheckConstraint("date_of_birth_format", "date_of_birth <= CURRENT_DATE");
                    table.CheckConstraint("detective_hire_date_format", "hire_date::date <= CURRENT_DATE");
                    table.CheckConstraint("detective_salary_format", "salary >= 0");
                    table.CheckConstraint("email_format", "email ~ '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$'");
                    table.CheckConstraint("name_format", "first_name ~ '^[А-ЯІЇЄа-яіїє]+$' AND last_name ~ '^[А-ЯІЇЄа-яіїє]+$' AND (father_name IS NULL OR father_name ~ '^[А-ЯІЇЄа-яіїє]+$')");
                    table.CheckConstraint("phone_number_format", "phone_number ~ '^\\+380\\d{9}$'");
                    table.CheckConstraint("region_format", "region ~ '^[А-ЯІЇЄа-яіїє]+$'");
                    table.CheckConstraint("street_format", "street ~ '^[А-ЯІЇЄа-яіїє\\s\\-]+$'");
                });

            migrationBuilder.CreateTable(
                name: "case",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    case_type_id = table.Column<int>(type: "integer", nullable: false),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    detective_id = table.Column<int>(type: "integer", nullable: true),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE"),
                    deadline_date = table.Column<DateOnly>(type: "date", nullable: false),
                    close_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<CaseStatus>(type: "case_status", nullable: false, defaultValue: CaseStatus.Opened)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_case", x => x.id);
                    table.CheckConstraint("close_date_format", "close_date IS NULL OR close_date >= start_date");
                    table.CheckConstraint("deadline_format", "deadline_date >= start_date");
                    table.ForeignKey(
                        name: "FK_case_case_type_id",
                        column: x => x.case_type_id,
                        principalTable: "case_type",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_case_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_case_detective_id",
                        column: x => x.detective_id,
                        principalTable: "detective",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "evidence",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    type = table.Column<EvidenceType>(type: "evidence_type", nullable: false),
                    collection_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    region = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "Не вказано"),
                    annotation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    purpose = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    approval_status = table.Column<ApprovalStatus>(type: "approval_status", nullable: false, defaultValue: ApprovalStatus.Draft),
                    created_by_detective_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evidence", x => x.id);
                    table.CheckConstraint("collection_date_format", "collection_date <= CURRENT_TIMESTAMP");
                    table.ForeignKey(
                        name: "FK_evidence_detective_created_by_detective_id",
                        column: x => x.created_by_detective_id,
                        principalTable: "detective",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "suspect",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    father_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    nickname = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    phone_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                    region = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    city = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    street = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    building_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    apartment_number = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    weight = table.Column<int>(type: "integer", nullable: true),
                    physical_description = table.Column<string>(type: "text", nullable: true),
                    prior_convictions = table.Column<string>(type: "text", nullable: true),
                    approval_status = table.Column<ApprovalStatus>(type: "approval_status", nullable: false, defaultValue: ApprovalStatus.Draft),
                    created_by_detective_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suspect", x => x.id);
                    table.CheckConstraint("apartment_number_format", "apartment_number IS NULL OR apartment_number > 0");
                    table.CheckConstraint("building_number_format", "building_number ~ '^[0-9/]+$'");
                    table.CheckConstraint("city_format", "city ~ '^[А-ЯІЇЄа-яіїє\\-]+$'");
                    table.CheckConstraint("date_of_birth_format", "date_of_birth <= CURRENT_DATE");
                    table.CheckConstraint("name_format", "first_name ~ '^[А-ЯІЇЄа-яіїє]+$' AND last_name ~ '^[А-ЯІЇЄа-яіїє]+$' AND (father_name IS NULL OR father_name ~ '^[А-ЯІЇЄа-яіїє]+$')");
                    table.CheckConstraint("phone_number_format", "phone_number ~ '^\\+380\\d{9}$'");
                    table.CheckConstraint("region_format", "region ~ '^[А-ЯІЇЄа-яіїє]+$'");
                    table.CheckConstraint("street_format", "street ~ '^[А-ЯІЇЄа-яіїє\\s\\-]+$'");
                    table.CheckConstraint("weight_height_format", "weight IS NOT NULL AND weight > 0 AND height IS NOT NULL AND height > 0 OR weight IS NULL AND height IS NULL");
                    table.ForeignKey(
                        name: "FK_suspect_detective_created_by_detective_id",
                        column: x => x.created_by_detective_id,
                        principalTable: "detective",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "expense",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    case_id = table.Column<int>(type: "integer", nullable: false),
                    date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    purpose = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    annotation = table.Column<string>(type: "text", nullable: true),
                    approval_status = table.Column<ApprovalStatus>(type: "approval_status", nullable: false, defaultValue: ApprovalStatus.Draft),
                    created_by_detective_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense", x => x.id);
                    table.CheckConstraint("amount_format", "amount > 0");
                    table.CheckConstraint("date_time_format", "date_time <= CURRENT_TIMESTAMP");
                    table.CheckConstraint("purpose_format", "purpose ~ '^[А-ЯІЇЄа-яіїєA-Za-z0-9\\s\\-\\.,:;’]+$'");
                    table.ForeignKey(
                        name: "FK_expense_detective_created_by_detective_id",
                        column: x => x.created_by_detective_id,
                        principalTable: "detective",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "expense_case_id_fk",
                        column: x => x.case_id,
                        principalTable: "case",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "report",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    case_id = table.Column<int>(type: "integer", nullable: false),
                    report_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    summary = table.Column<string>(type: "text", nullable: false),
                    comments = table.Column<string>(type: "text", nullable: true),
                    approval_status = table.Column<ApprovalStatus>(type: "approval_status", nullable: false, defaultValue: ApprovalStatus.Draft),
                    created_by_detective_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report", x => x.id);
                    table.CheckConstraint("date_format", "report_date <= CURRENT_TIMESTAMP");
                    table.ForeignKey(
                        name: "FK_report_case_id",
                        column: x => x.case_id,
                        principalTable: "case",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_report_detective_created_by_detective_id",
                        column: x => x.created_by_detective_id,
                        principalTable: "detective",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "case_evidence",
                columns: table => new
                {
                    evidence_id = table.Column<int>(type: "integer", nullable: false),
                    case_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_case_evidence", x => new { x.evidence_id, x.case_id });
                    table.ForeignKey(
                        name: "FK_case_evidence_case_id",
                        column: x => x.case_id,
                        principalTable: "case",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_case_evidence_suspect_id",
                        column: x => x.evidence_id,
                        principalTable: "evidence",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "case_suspect",
                columns: table => new
                {
                    suspect_id = table.Column<int>(type: "integer", nullable: false),
                    case_id = table.Column<int>(type: "integer", nullable: false),
                    is_interrogated = table.Column<bool>(type: "boolean", nullable: false),
                    alibi = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_case_suspect", x => new { x.suspect_id, x.case_id });
                    table.ForeignKey(
                        name: "FK_case_suspect_case_id",
                        column: x => x.case_id,
                        principalTable: "case",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_case_suspect_suspect_id",
                        column: x => x.suspect_id,
                        principalTable: "suspect",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_case_case_type_id",
                table: "case",
                column: "case_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_case_client_id",
                table: "case",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_case_detective_id",
                table: "case",
                column: "detective_id");

            migrationBuilder.CreateIndex(
                name: "IX_case_evidence_case_id",
                table: "case_evidence",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "IX_case_suspect_case_id",
                table: "case_suspect",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "IX_detective_postgres_login",
                table: "detective",
                column: "postgres_login",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_evidence_created_by_detective_id",
                table: "evidence",
                column: "created_by_detective_id");

            migrationBuilder.CreateIndex(
                name: "IX_expense_case_id",
                table: "expense",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "IX_expense_created_by_detective_id",
                table: "expense",
                column: "created_by_detective_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_case_id",
                table: "report",
                column: "case_id");

            migrationBuilder.CreateIndex(
                name: "IX_report_created_by_detective_id",
                table: "report",
                column: "created_by_detective_id");

            migrationBuilder.CreateIndex(
                name: "IX_suspect_created_by_detective_id",
                table: "suspect",
                column: "created_by_detective_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "case_evidence");

            migrationBuilder.DropTable(
                name: "case_suspect");

            migrationBuilder.DropTable(
                name: "expense");

            migrationBuilder.DropTable(
                name: "report");

            migrationBuilder.DropTable(
                name: "evidence");

            migrationBuilder.DropTable(
                name: "suspect");

            migrationBuilder.DropTable(
                name: "case");

            migrationBuilder.DropTable(
                name: "case_type");

            migrationBuilder.DropTable(
                name: "client");

            migrationBuilder.DropTable(
                name: "detective");
        }
    }
}
