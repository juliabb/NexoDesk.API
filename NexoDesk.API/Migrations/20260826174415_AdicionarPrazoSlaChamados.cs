using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexoDesk.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarPrazoSlaChamados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PrazoResolucaoEm",
                table: "chamados",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE chamados
                SET "PrazoResolucaoEm" = "CriadoEm" + CASE "Prioridade"
                    WHEN 'Critica' THEN INTERVAL '8 hours'
                    WHEN 'Alta' THEN INTERVAL '24 hours'
                    WHEN 'Media' THEN INTERVAL '48 hours'
                    ELSE INTERVAL '72 hours'
                END;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "PrazoResolucaoEm",
                table: "chamados",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrazoResolucaoEm",
                table: "chamados");
        }
    }
}
