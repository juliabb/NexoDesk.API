using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexoDesk.API.Migrations
{
    /// <inheritdoc />
    public partial class ProtegerCpfComHmac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Os dados atuais são exclusivamente fictícios e não podem ser
            // convertidos sem expor a chave HMAC dentro da migração.
            migrationBuilder.Sql("DELETE FROM auditorias_usuarios;");
            migrationBuilder.Sql("DELETE FROM usuarios;");

            migrationBuilder.DropIndex(
                name: "IX_usuarios_Cpf",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "Cpf",
                table: "usuarios");

            migrationBuilder.AddColumn<string>(
                name: "CpfHash",
                table: "usuarios",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_CpfHash",
                table: "usuarios",
                column: "CpfHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM auditorias_usuarios;");
            migrationBuilder.Sql("DELETE FROM usuarios;");

            migrationBuilder.DropIndex(
                name: "IX_usuarios_CpfHash",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "CpfHash",
                table: "usuarios");

            migrationBuilder.AddColumn<string>(
                name: "Cpf",
                table: "usuarios",
                type: "character varying(11)",
                maxLength: 11,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Cpf",
                table: "usuarios",
                column: "Cpf",
                unique: true);
        }
    }
}
