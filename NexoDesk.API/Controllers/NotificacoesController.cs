using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexoDesk.Data;

namespace NexoDesk.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificacoesController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificacoesController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Lista as notificações do usuário autenticado.</summary>
    [HttpGet]
    [Tags("Notificações")]
    public async Task<IActionResult> Listar(
        [FromQuery] bool? lida,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (!TryObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Notificacoes.AsNoTracking()
            .Where(notificacao => notificacao.UsuarioId == usuarioId);

        if (lida.HasValue)
        {
            query = query.Where(notificacao => notificacao.Lida == lida.Value);
        }

        var totalRegistros = await query.CountAsync();
        var naoLidas = await _context.Notificacoes.CountAsync(notificacao =>
            notificacao.UsuarioId == usuarioId && !notificacao.Lida);
        var notificacoes = await query
            .OrderByDescending(notificacao => notificacao.CriadaEm)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(notificacao => new
            {
                notificacao.Id,
                notificacao.Tipo,
                notificacao.Titulo,
                notificacao.Mensagem,
                notificacao.Lida,
                notificacao.CriadaEm,
                notificacao.LidaEm,
                chamado = new
                {
                    notificacao.Chamado.Id,
                    notificacao.Chamado.Protocolo
                }
            })
            .ToListAsync();

        return Ok(new
        {
            pagina = page,
            tamanhoPagina = pageSize,
            totalRegistros,
            naoLidas,
            notificacoes
        });
    }

    /// <summary>Retorna apenas a quantidade de notificações não lidas.</summary>
    [HttpGet("nao-lidas/quantidade")]
    [Tags("Notificações")]
    public async Task<IActionResult> ObterQuantidadeNaoLidas()
    {
        if (!TryObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var quantidade = await _context.Notificacoes.CountAsync(notificacao =>
            notificacao.UsuarioId == usuarioId && !notificacao.Lida);

        return Ok(new { quantidade });
    }

    /// <summary>Marca uma notificação do usuário como lida.</summary>
    [HttpPatch("{id:guid}/lida")]
    [Tags("Notificações")]
    public async Task<IActionResult> MarcarComoLida(Guid id)
    {
        if (!TryObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var notificacao = await _context.Notificacoes.FirstOrDefaultAsync(item =>
            item.Id == id && item.UsuarioId == usuarioId);

        if (notificacao is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Notificação não encontrada.",
                detail: "A notificação não existe ou não pertence ao usuário autenticado.");
        }

        if (!notificacao.Lida)
        {
            notificacao.Lida = true;
            notificacao.LidaEm = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    /// <summary>Marca todas as notificações do usuário como lidas.</summary>
    [HttpPatch("ler-todas")]
    [Tags("Notificações")]
    public async Task<IActionResult> MarcarTodasComoLidas()
    {
        if (!TryObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var agora = DateTime.UtcNow;
        var quantidade = await _context.Notificacoes
            .Where(notificacao => notificacao.UsuarioId == usuarioId && !notificacao.Lida)
            .ExecuteUpdateAsync(atualizacao => atualizacao
                .SetProperty(notificacao => notificacao.Lida, true)
                .SetProperty(notificacao => notificacao.LidaEm, agora));

        return Ok(new { mensagem = "Notificações marcadas como lidas.", quantidade });
    }

    private bool TryObterUsuarioId(out Guid usuarioId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);
}
