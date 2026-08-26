using NexoDesk.Data;
using NexoDesk.Enums;
using NexoDesk.Models;

namespace NexoDesk.Helpers;

public interface INotificacaoService
{
    void Adicionar(
        Guid usuarioId,
        Chamado chamado,
        TipoNotificacao tipo,
        string titulo,
        string mensagem,
        Guid realizadoPorUsuarioId);
}

public class NotificacaoService : INotificacaoService
{
    private readonly AppDbContext _context;

    public NotificacaoService(AppDbContext context)
    {
        _context = context;
    }

    public void Adicionar(
        Guid usuarioId,
        Chamado chamado,
        TipoNotificacao tipo,
        string titulo,
        string mensagem,
        Guid realizadoPorUsuarioId)
    {
        if (usuarioId == realizadoPorUsuarioId)
        {
            return;
        }

        _context.Notificacoes.Add(new Notificacao
        {
            UsuarioId = usuarioId,
            ChamadoId = chamado.Id,
            Tipo = tipo,
            Titulo = titulo,
            Mensagem = mensagem
        });
    }
}
