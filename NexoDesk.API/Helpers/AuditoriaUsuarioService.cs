using NexoDesk.Data;
using NexoDesk.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace NexoDesk.Helpers;

public interface IAuditoriaUsuarioService
{
    Task RegistrarAsync(Guid usuarioAlvoId, string acao, object alteracoes, HttpContext httpContext);
}

public class AuditoriaUsuarioService : IAuditoriaUsuarioService
{
    private readonly AppDbContext _context;
    public AuditoriaUsuarioService(AppDbContext context) => _context = context;

    public Task RegistrarAsync(Guid usuarioAlvoId, string acao, object alteracoes, HttpContext httpContext)
    {
        var ator = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _context.AuditoriasUsuarios.Add(new AuditoriaUsuario
        {
            UsuarioAlvoId = usuarioAlvoId,
            RealizadoPorUsuarioId = Guid.TryParse(ator, out var id) ? id : null,
            Acao = acao,
            Alteracoes = JsonSerializer.Serialize(alteracoes),
            // Em desenvolvimento, ::1/127.0.0.1 representa a própria máquina.
            // Em produção atrás de proxy, configure Forwarded Headers para obter
            // com segurança o IP real enviado pelo proxy confiável.
            IpOrigem = httpContext.Connection.RemoteIpAddress?.ToString(),
            CriadoEm = DateTime.UtcNow
        });
        // A gravação é feita pelo endpoint junto da alteração principal,
        // para que ambas pertençam à mesma transação do SaveChangesAsync.
        return Task.CompletedTask;
    }
}
