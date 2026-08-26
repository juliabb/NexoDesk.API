using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexoDesk.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCoordenadoriaArea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CoordenadoriaId",
                table: "areas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_areas_CoordenadoriaId",
                table: "areas",
                column: "CoordenadoriaId");

            migrationBuilder.AddForeignKey(
                name: "FK_areas_coordenadorias_CoordenadoriaId",
                table: "areas",
                column: "CoordenadoriaId",
                principalTable: "coordenadorias",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_areas_coordenadorias_CoordenadoriaId",
                table: "areas");

            migrationBuilder.DropIndex(
                name: "IX_areas_CoordenadoriaId",
                table: "areas");

            migrationBuilder.DropColumn(
                name: "CoordenadoriaId",
                table: "areas");
        }
    }
}
