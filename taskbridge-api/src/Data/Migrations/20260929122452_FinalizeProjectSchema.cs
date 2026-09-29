using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskBridge.Api.src.Data.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeProjectSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM [dbo].[Projects]
                    WHERE DATALENGTH([Name]) > 400)
                BEGIN
                    THROW 51001, 'Project names longer than 200 characters must be resolved before this migration.', 1;
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Projects",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.Sql("""
                CREATE TRIGGER [dbo].[TR_AuditEntries_Immutable]
                ON [dbo].[AuditEntries]
                INSTEAD OF UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    THROW 51002, 'Audit entries cannot be updated or deleted.', 1;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS [dbo].[TR_AuditEntries_Immutable];");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);
        }
    }
}
