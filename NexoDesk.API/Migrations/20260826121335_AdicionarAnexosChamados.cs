using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexoDesk.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarAnexosChamados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "anexos_chamados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChamadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnviadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    NomeOriginal = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NomeArmazenado = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TipoConteudo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TamanhoBytes = table.Column<long>(type: "bigint", nullable: false),
                    Interno = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_anexos_chamados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_anexos_chamados_chamados_ChamadoId",
                        column: x => x.ChamadoId,
                        principalTable: "chamados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_anexos_chamados_usuarios_EnviadoPorUsuarioId",
                        column: x => x.EnviadoPorUsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_anexos_chamados_ChamadoId_CriadoEm",
                table: "anexos_chamados",
                columns: new[] { "ChamadoId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_anexos_chamados_EnviadoPorUsuarioId",
                table: "anexos_chamados",
                column: "EnviadoPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_anexos_chamados_NomeArmazenado",
                table: "anexos_chamados",
                column: "NomeArmazenado",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anexos_chamados");
        }
    }
}
