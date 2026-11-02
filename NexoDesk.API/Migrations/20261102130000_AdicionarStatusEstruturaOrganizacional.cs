using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexoDesk.API.Migrations
{
    public partial class AdicionarStatusEstruturaOrganizacional : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(name: "Ativo", table: "departamentos", type: "boolean", nullable: false, defaultValue: true);
            migrationBuilder.AddColumn<bool>(name: "Ativo", table: "setores", type: "boolean", nullable: false, defaultValue: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Ativo", table: "departamentos");
            migrationBuilder.DropColumn(name: "Ativo", table: "setores");
        }
    }
}
