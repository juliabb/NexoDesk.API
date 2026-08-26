using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexoDesk.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoverEmailSecundarioELiberarDominio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailSecundario",
                table: "usuarios");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailSecundario",
                table: "usuarios",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);
        }
    }
}
