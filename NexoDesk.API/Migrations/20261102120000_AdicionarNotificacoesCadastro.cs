using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexoDesk.API.Migrations
{
    public partial class AdicionarNotificacoesCadastro : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "ChamadoId", table: "notificacoes", type: "uuid", nullable: true,
                oldClrType: typeof(Guid), oldType: "uuid");

            // Inclui os cadastros que já aguardavam aprovação antes deste recurso.
            migrationBuilder.Sql("""
                INSERT INTO notificacoes ("Id", "UsuarioId", "ChamadoId", "Tipo", "Titulo", "Mensagem", "Lida", "CriadaEm")
                SELECT gen_random_uuid(), admin."Id", NULL, 'CadastroPendente',
                    'Novo cadastro aguardando aprovação',
                    pendente."NomeCompleto" || ' (' || pendente."EmailCorporativo" || ') solicitou acesso ao NexoDesk.',
                    false, CURRENT_TIMESTAMP
                FROM usuarios admin CROSS JOIN usuarios pendente
                WHERE admin."Perfil" = 'Administrador' AND admin."Status" = 'Aprovado' AND admin."Ativo"
                  AND pendente."Status" = 'AguardandoAprovacao';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM notificacoes WHERE \"ChamadoId\" IS NULL;");
            migrationBuilder.AlterColumn<Guid>(
                name: "ChamadoId", table: "notificacoes", type: "uuid", nullable: false,
                oldClrType: typeof(Guid), oldType: "uuid", oldNullable: true);
        }
    }
}
