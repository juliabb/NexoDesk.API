using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexoDesk.API.Migrations
{
    /// <inheritdoc />
    public partial class AprimorarAberturaChamados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContatoPreferencial",
                table: "chamados",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Impacto",
                table: "chamados",
                type: "text",
                nullable: false,
                defaultValue: "Individual");

            migrationBuilder.AddColumn<string>(
                name: "Localizacao",
                table: "chamados",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PatrimonioEquipamento",
                table: "chamados",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Urgencia",
                table: "chamados",
                type: "text",
                nullable: false,
                defaultValue: "PodeAguardar");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContatoPreferencial",
                table: "chamados");

            migrationBuilder.DropColumn(
                name: "Impacto",
                table: "chamados");

            migrationBuilder.DropColumn(
                name: "Localizacao",
                table: "chamados");

            migrationBuilder.DropColumn(
                name: "PatrimonioEquipamento",
                table: "chamados");

            migrationBuilder.DropColumn(
                name: "Urgencia",
                table: "chamados");
        }
    }
}
