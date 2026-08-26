using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexoDesk.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarAuditoriaUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auditorias_usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UsuarioAlvoId = table.Column<Guid>(type: "uuid", nullable: false),
                    RealizadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: true),
                    Acao = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Alteracoes = table.Column<string>(type: "jsonb", nullable: false),
                    IpOrigem = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auditorias_usuarios", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auditorias_usuarios_UsuarioAlvoId_CriadoEm",
                table: "auditorias_usuarios",
                columns: new[] { "UsuarioAlvoId", "CriadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditorias_usuarios");
        }
    }
}
