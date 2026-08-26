using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NexoDesk.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarHistoricoChamados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "historicos_chamados",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChamadoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Acao = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StatusAnterior = table.Column<string>(type: "text", nullable: true),
                    StatusNovo = table.Column<string>(type: "text", nullable: false),
                    ResponsavelAnteriorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResponsavelNovoId = table.Column<Guid>(type: "uuid", nullable: true),
                    RealizadoPorUsuarioId = table.Column<Guid>(type: "uuid", nullable: false),
                    Observacao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_historicos_chamados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_historicos_chamados_chamados_ChamadoId",
                        column: x => x.ChamadoId,
                        principalTable: "chamados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_historicos_chamados_usuarios_RealizadoPorUsuarioId",
                        column: x => x.RealizadoPorUsuarioId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_historicos_chamados_ChamadoId_CriadoEm",
                table: "historicos_chamados",
                columns: new[] { "ChamadoId", "CriadoEm" });

            migrationBuilder.CreateIndex(
                name: "IX_historicos_chamados_RealizadoPorUsuarioId",
                table: "historicos_chamados",
                column: "RealizadoPorUsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "historicos_chamados");
        }
    }
}
