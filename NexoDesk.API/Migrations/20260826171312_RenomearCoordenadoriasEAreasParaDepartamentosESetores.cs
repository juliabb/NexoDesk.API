using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexoDesk.API.Migrations;

/// <inheritdoc />
public partial class RenomearCoordenadoriasEAreasParaDepartamentosESetores : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        RemoverRelacionamentosAntigos(migrationBuilder);

        migrationBuilder.RenameTable(name: "coordenadorias", newName: "departamentos");
        migrationBuilder.RenameTable(name: "areas", newName: "setores");

        migrationBuilder.RenameColumn(name: "CoordenadoriaId", table: "setores", newName: "DepartamentoId");
        migrationBuilder.RenameColumn(name: "CoordenadoriaId", table: "usuarios", newName: "DepartamentoId");
        migrationBuilder.RenameColumn(name: "AreaId", table: "usuarios", newName: "SetorId");
        migrationBuilder.RenameColumn(name: "AreaResponsavelId", table: "categorias_chamados", newName: "SetorResponsavelId");
        migrationBuilder.RenameColumn(name: "AreaSolicitanteId", table: "chamados", newName: "SetorSolicitanteId");
        migrationBuilder.RenameColumn(name: "AreaResponsavelId", table: "chamados", newName: "SetorResponsavelId");

        RenomearIndicesParaNovaNomenclatura(migrationBuilder);
        migrationBuilder.Sql("ALTER TABLE departamentos RENAME CONSTRAINT \"PK_coordenadorias\" TO \"PK_departamentos\";");
        migrationBuilder.Sql("ALTER TABLE setores RENAME CONSTRAINT \"PK_areas\" TO \"PK_setores\";");

        AdicionarNovosRelacionamentos(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemoverNovosRelacionamentos(migrationBuilder);

        migrationBuilder.Sql("ALTER TABLE departamentos RENAME CONSTRAINT \"PK_departamentos\" TO \"PK_coordenadorias\";");
        migrationBuilder.Sql("ALTER TABLE setores RENAME CONSTRAINT \"PK_setores\" TO \"PK_areas\";");
        RenomearIndicesParaNomenclaturaAntiga(migrationBuilder);

        migrationBuilder.RenameColumn(name: "DepartamentoId", table: "setores", newName: "CoordenadoriaId");
        migrationBuilder.RenameColumn(name: "DepartamentoId", table: "usuarios", newName: "CoordenadoriaId");
        migrationBuilder.RenameColumn(name: "SetorId", table: "usuarios", newName: "AreaId");
        migrationBuilder.RenameColumn(name: "SetorResponsavelId", table: "categorias_chamados", newName: "AreaResponsavelId");
        migrationBuilder.RenameColumn(name: "SetorSolicitanteId", table: "chamados", newName: "AreaSolicitanteId");
        migrationBuilder.RenameColumn(name: "SetorResponsavelId", table: "chamados", newName: "AreaResponsavelId");

        migrationBuilder.RenameTable(name: "departamentos", newName: "coordenadorias");
        migrationBuilder.RenameTable(name: "setores", newName: "areas");
        AdicionarRelacionamentosAntigos(migrationBuilder);
    }

    private static void RemoverRelacionamentosAntigos(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_areas_coordenadorias_CoordenadoriaId", table: "areas");
        migrationBuilder.DropForeignKey(name: "FK_categorias_chamados_areas_AreaResponsavelId", table: "categorias_chamados");
        migrationBuilder.DropForeignKey(name: "FK_chamados_areas_AreaResponsavelId", table: "chamados");
        migrationBuilder.DropForeignKey(name: "FK_chamados_areas_AreaSolicitanteId", table: "chamados");
        migrationBuilder.DropForeignKey(name: "FK_usuarios_areas_AreaId", table: "usuarios");
        migrationBuilder.DropForeignKey(name: "FK_usuarios_coordenadorias_CoordenadoriaId", table: "usuarios");
    }

    private static void AdicionarNovosRelacionamentos(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddForeignKey(name: "FK_setores_departamentos_DepartamentoId", table: "setores", column: "DepartamentoId", principalTable: "departamentos", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_categorias_chamados_setores_SetorResponsavelId", table: "categorias_chamados", column: "SetorResponsavelId", principalTable: "setores", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_chamados_setores_SetorResponsavelId", table: "chamados", column: "SetorResponsavelId", principalTable: "setores", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_chamados_setores_SetorSolicitanteId", table: "chamados", column: "SetorSolicitanteId", principalTable: "setores", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_usuarios_departamentos_DepartamentoId", table: "usuarios", column: "DepartamentoId", principalTable: "departamentos", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_usuarios_setores_SetorId", table: "usuarios", column: "SetorId", principalTable: "setores", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    private static void RemoverNovosRelacionamentos(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_setores_departamentos_DepartamentoId", table: "setores");
        migrationBuilder.DropForeignKey(name: "FK_categorias_chamados_setores_SetorResponsavelId", table: "categorias_chamados");
        migrationBuilder.DropForeignKey(name: "FK_chamados_setores_SetorResponsavelId", table: "chamados");
        migrationBuilder.DropForeignKey(name: "FK_chamados_setores_SetorSolicitanteId", table: "chamados");
        migrationBuilder.DropForeignKey(name: "FK_usuarios_departamentos_DepartamentoId", table: "usuarios");
        migrationBuilder.DropForeignKey(name: "FK_usuarios_setores_SetorId", table: "usuarios");
    }

    private static void AdicionarRelacionamentosAntigos(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddForeignKey(name: "FK_areas_coordenadorias_CoordenadoriaId", table: "areas", column: "CoordenadoriaId", principalTable: "coordenadorias", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_categorias_chamados_areas_AreaResponsavelId", table: "categorias_chamados", column: "AreaResponsavelId", principalTable: "areas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_chamados_areas_AreaResponsavelId", table: "chamados", column: "AreaResponsavelId", principalTable: "areas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_chamados_areas_AreaSolicitanteId", table: "chamados", column: "AreaSolicitanteId", principalTable: "areas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_usuarios_areas_AreaId", table: "usuarios", column: "AreaId", principalTable: "areas", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
        migrationBuilder.AddForeignKey(name: "FK_usuarios_coordenadorias_CoordenadoriaId", table: "usuarios", column: "CoordenadoriaId", principalTable: "coordenadorias", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
    }

    private static void RenomearIndicesParaNovaNomenclatura(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameIndex(name: "IX_coordenadorias_Nome", table: "departamentos", newName: "IX_departamentos_Nome");
        migrationBuilder.RenameIndex(name: "IX_coordenadorias_Sigla", table: "departamentos", newName: "IX_departamentos_Sigla");
        migrationBuilder.RenameIndex(name: "IX_areas_CoordenadoriaId_Nome", table: "setores", newName: "IX_setores_DepartamentoId_Nome");
        migrationBuilder.RenameIndex(name: "IX_usuarios_CoordenadoriaId", table: "usuarios", newName: "IX_usuarios_DepartamentoId");
        migrationBuilder.RenameIndex(name: "IX_usuarios_AreaId", table: "usuarios", newName: "IX_usuarios_SetorId");
        migrationBuilder.RenameIndex(name: "IX_categorias_chamados_AreaResponsavelId", table: "categorias_chamados", newName: "IX_categorias_chamados_SetorResponsavelId");
        migrationBuilder.RenameIndex(name: "IX_chamados_AreaSolicitanteId", table: "chamados", newName: "IX_chamados_SetorSolicitanteId");
        migrationBuilder.RenameIndex(name: "IX_chamados_AreaResponsavelId_Status", table: "chamados", newName: "IX_chamados_SetorResponsavelId_Status");
    }

    private static void RenomearIndicesParaNomenclaturaAntiga(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.RenameIndex(name: "IX_departamentos_Nome", table: "departamentos", newName: "IX_coordenadorias_Nome");
        migrationBuilder.RenameIndex(name: "IX_departamentos_Sigla", table: "departamentos", newName: "IX_coordenadorias_Sigla");
        migrationBuilder.RenameIndex(name: "IX_setores_DepartamentoId_Nome", table: "setores", newName: "IX_areas_CoordenadoriaId_Nome");
        migrationBuilder.RenameIndex(name: "IX_usuarios_DepartamentoId", table: "usuarios", newName: "IX_usuarios_CoordenadoriaId");
        migrationBuilder.RenameIndex(name: "IX_usuarios_SetorId", table: "usuarios", newName: "IX_usuarios_AreaId");
        migrationBuilder.RenameIndex(name: "IX_categorias_chamados_SetorResponsavelId", table: "categorias_chamados", newName: "IX_categorias_chamados_AreaResponsavelId");
        migrationBuilder.RenameIndex(name: "IX_chamados_SetorSolicitanteId", table: "chamados", newName: "IX_chamados_AreaSolicitanteId");
        migrationBuilder.RenameIndex(name: "IX_chamados_SetorResponsavelId_Status", table: "chamados", newName: "IX_chamados_AreaResponsavelId_Status");
    }
}
