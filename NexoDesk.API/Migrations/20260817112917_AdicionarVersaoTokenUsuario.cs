using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexoDesk.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarVersaoTokenUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_areas_CoordenadoriaId",
                table: "areas");

            migrationBuilder.AddColumn<int>(
                name: "VersaoToken",
                table: "usuarios",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_coordenadorias_Nome",
                table: "coordenadorias",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_areas_CoordenadoriaId_Nome",
                table: "areas",
                columns: new[] { "CoordenadoriaId", "Nome" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_coordenadorias_Nome",
                table: "coordenadorias");

            migrationBuilder.DropIndex(
                name: "IX_areas_CoordenadoriaId_Nome",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "VersaoToken",
                table: "usuarios");

            migrationBuilder.CreateIndex(
                name: "IX_areas_CoordenadoriaId",
                table: "areas",
                column: "CoordenadoriaId");
        }
    }
}
