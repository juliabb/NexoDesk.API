using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexoDesk.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarSiglaCoordenadoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Sigla",
                table: "coordenadorias",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Mantém a migration aplicável em bases que já tenham coordenadorias.
            // Esses valores provisórios devem ser revisados pelo administrador.
            migrationBuilder.Sql("UPDATE coordenadorias SET \"Sigla\" = 'COORD-' || \"Id\"::text WHERE \"Sigla\" IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "Sigla",
                table: "coordenadorias",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_coordenadorias_Sigla",
                table: "coordenadorias",
                column: "Sigla",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_coordenadorias_Sigla",
                table: "coordenadorias");

            migrationBuilder.DropColumn(
                name: "Sigla",
                table: "coordenadorias");
        }
    }
}
