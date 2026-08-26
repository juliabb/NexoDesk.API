using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NexoDesk.API.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarCategoriasEChamados : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias_chamados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AreaResponsavelId = table.Column<int>(type: "integer", nullable: false),
                    Ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CriadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadaEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias_chamados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_categorias_chamados_areas_AreaResponsavelId",
                        column: x => x.AreaResponsavelId,
                        principalTable: "areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chamados",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Protocolo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Titulo = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Prioridade = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SolicitanteId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoriaId = table.Column<int>(type: "integer", nullable: false),
                    AreaSolicitanteId = table.Column<int>(type: "integer", nullable: false),
                    AreaResponsavelId = table.Column<int>(type: "integer", nullable: false),
                    ResponsavelId = table.Column<Guid>(type: "uuid", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvidoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechadoEm = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chamados", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chamados_areas_AreaResponsavelId",
                        column: x => x.AreaResponsavelId,
                        principalTable: "areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamados_areas_AreaSolicitanteId",
                        column: x => x.AreaSolicitanteId,
                        principalTable: "areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamados_categorias_chamados_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "categorias_chamados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamados_usuarios_ResponsavelId",
                        column: x => x.ResponsavelId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_chamados_usuarios_SolicitanteId",
                        column: x => x.SolicitanteId,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_categorias_chamados_AreaResponsavelId",
                table: "categorias_chamados",
                column: "AreaResponsavelId");

            migrationBuilder.CreateIndex(
                name: "IX_categorias_chamados_Nome",
                table: "categorias_chamados",
                column: "Nome",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chamados_AreaResponsavelId_Status",
                table: "chamados",
                columns: new[] { "AreaResponsavelId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_chamados_AreaSolicitanteId",
                table: "chamados",
                column: "AreaSolicitanteId");

            migrationBuilder.CreateIndex(
                name: "IX_chamados_CategoriaId",
                table: "chamados",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_chamados_Protocolo",
                table: "chamados",
                column: "Protocolo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chamados_ResponsavelId",
                table: "chamados",
                column: "ResponsavelId");

            migrationBuilder.CreateIndex(
                name: "IX_chamados_SolicitanteId_CriadoEm",
                table: "chamados",
                columns: new[] { "SolicitanteId", "CriadoEm" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "chamados");

            migrationBuilder.DropTable(
                name: "categorias_chamados");
        }
    }
}
