using NexoDesk.Data;
using NexoDesk.DTOs;
using NexoDesk.Enums;
using NexoDesk.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace NexoDesk.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Gestor")]
public class GestorController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditoriaUsuarioService _auditoria;

    public GestorController(
        AppDbContext context,
        IAuditoriaUsuarioService auditoria)
    {
        _context = context;
        _auditoria = auditoria;
    }

    // ============================================================
    // ALTERAR STATUS DO USUÁRIO
    // Gestores podem aprovar, reprovar e desativar usuários
    // do Perfil Padrão somente da própria Coordenadoria.
    // ============================================================

    /// <summary>
    /// Altera o status de um usuário da própria Coordenadoria.
    /// </summary>
    /// <remarks>
    /// O Gestor pode:
    /// - Aprovar usuários;
    /// - Reprovar usuários;
    /// - Desativar usuários.
    /// 
    /// Status permitidos: AguardandoAprovacao = 1 (status inicial), Aprovado = 2, Reprovado = 3, Desativado = 4
    ///
    /// O Gestor somente pode alterar usuários com Perfil Padrão.
    /// Gestores e Administradores não podem ter o status
    /// alterado por este endpoint.
    ///
    /// O usuário é identificado pelo e-mail corporativo.
    /// </remarks>
    [HttpPut("usuarios/{email}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarStatusUsuario(
        string email,
        [FromBody] AlterarStatusUsuarioRequest request)
    {
        // ========================================================
        // IDENTIFICA O GESTOR LOGADO
        // ========================================================

        var usuarioGestorId =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
            usuarioGestorId,
            out var gestorId))
        {
            return Unauthorized(new
            {
                mensagem = "Não foi possível identificar o usuário autenticado."
            });
        }

        // Busca o Gestor no banco para obter sua Coordenadoria.
        var gestor = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(usuario =>
                usuario.Id == gestorId);

        if (gestor == null)
        {
            return Unauthorized(new
            {
                mensagem = "Gestor não encontrado."
            });
        }

        // Segurança adicional:
        // garante que o usuário realmente continua sendo Gestor.
        if (gestor.Perfil != PerfilUsuario.Gestor)
        {
            return Forbid();
        }

        // ========================================================
        // BUSCA O USUÁRIO QUE SERÁ ALTERADO
        // ========================================================

        var emailCorporativo = email
            .Trim()
            .ToLowerInvariant();

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(usuario =>
                usuario.EmailCorporativo == emailCorporativo);

        if (usuario == null)
        {
            return NotFound(new
            {
                mensagem = "Usuário não encontrado."
            });
        }

        // ========================================================
        // VALIDA A COORDENADORIA
        // ========================================================

        if (usuario.CoordenadoriaId != gestor.CoordenadoriaId)
        {
            return Forbid();
        }

        // ========================================================
        // O GESTOR SÓ PODE ADMINISTRAR USUÁRIOS DO PERFIL PADRÃO
        // ========================================================

        if (usuario.Perfil != PerfilUsuario.Padrao)
        {
            return Forbid();
        }

        var statusAnterior = usuario.Status;
        var ativoAnterior = usuario.Ativo;

        // ========================================================
        // APROVAÇÃO
        // ========================================================

        if (request.Status == StatusUsuario.Aprovado)
        {
            if (usuario.Status == StatusUsuario.Desativado)
            {
                usuario.Status = StatusUsuario.Aprovado;
                usuario.Ativo = true;
            }
            else if (usuario.Status == StatusUsuario.AguardandoAprovacao ||
                     usuario.Status == StatusUsuario.Reprovado)
            {
                usuario.Status = StatusUsuario.Aprovado;
                usuario.Ativo = true;
                usuario.AprovadoEm = DateTime.UtcNow;
                usuario.AprovadoPorUsuarioId = gestorId;
            }
            else
            {
                return BadRequest(new
                {
                    mensagem = "O usuário não pode ser aprovado neste status."
                });
            }
        }

        // ========================================================
        // REPROVAÇÃO
        // ========================================================

        else if (request.Status == StatusUsuario.Reprovado)
        {
            if (usuario.Status != StatusUsuario.AguardandoAprovacao)
            {
                return BadRequest(new
                {
                    mensagem =
                        "O usuário não está aguardando aprovação."
                });
            }

            usuario.Status = StatusUsuario.Reprovado;
            usuario.Ativo = false;
        }

        // ========================================================
        // DESATIVAÇÃO
        // ========================================================

        else if (request.Status == StatusUsuario.Desativado)
        {
            usuario.Status = StatusUsuario.Desativado;
            usuario.Ativo = false;
        }

        // ========================================================
        // STATUS NÃO PERMITIDO
        // ========================================================

        else
        {
            return BadRequest(new
            {
                mensagem =
                    "Status informado não pode ser aplicado."
            });
        }

        // Registra quando o usuário foi atualizado.
        usuario.AtualizadoEm = DateTime.UtcNow;
        usuario.VersaoToken++;

        await _auditoria.RegistrarAsync(
            usuario.Id,
            "Status alterado pelo gestor",
            new
            {
                anterior = new
                {
                    status = statusAnterior.ToString(),
                    ativo = ativoAnterior
                },
                atual = new
                {
                    status = usuario.Status.ToString(),
                    ativo = usuario.Ativo
                }
            },
            HttpContext);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Status do usuário alterado com sucesso.",
            usuario.NomeCompleto,
            usuario.Status,
            usuario.Ativo,
            usuario.AprovadoEm
        });
    }

    // ============================================================
    // LISTAR USUÁRIOS PENDENTES
    // Retorna somente os usuários pendentes da Coordenadoria
    // do Gestor autenticado.
    // ============================================================

    /// <summary>
    /// Lista os usuários pendentes da própria Coordenadoria.
    /// </summary>
    [HttpGet("usuarios/pendentes")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarUsuariosPendentes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // ========================================================
        // IDENTIFICA O GESTOR LOGADO
        // ========================================================

        var usuarioGestorId =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
            usuarioGestorId,
            out var gestorId))
        {
            return Unauthorized(new
            {
                mensagem =
                    "Não foi possível identificar o usuário autenticado."
            });
        }

        // Busca o Gestor no banco para obter sua Coordenadoria.
        var gestor = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(usuario =>
                usuario.Id == gestorId);

        if (gestor == null)
        {
            return Unauthorized(new
            {
                mensagem = "Gestor não encontrado."
            });
        }

        // Segurança adicional:
        // garante que o usuário continua sendo Gestor.
        if (gestor.Perfil != PerfilUsuario.Gestor)
        {
            return Forbid();
        }

        // ========================================================
        // BUSCA OS USUÁRIOS PENDENTES
        // SOMENTE DA COORDENADORIA DO GESTOR
        // ========================================================

        var query = _context.Usuarios
            .AsNoTracking()
            .Where(usuario =>
                usuario.Status == StatusUsuario.AguardandoAprovacao &&
                usuario.CoordenadoriaId == gestor.CoordenadoriaId);

        var totalRegistros = await query.CountAsync();
        var totalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);

        var usuarios = await query
            .Select(usuario => new
            {
                usuario.NomeCompleto,
                usuario.EmailCorporativo,
                coordenadoria = usuario.Coordenadoria!.Nome,
                area = usuario.Area!.Nome,
                usuario.CriadoEm
            })
            .OrderBy(usuario => usuario.CriadoEm)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new
        {
            pagina = page,
            tamanhoPagina = pageSize,
            totalRegistros,
            totalPaginas,
            usuarios
        });
    }

    // ============================================================
    // LISTAR USUÁRIOS
    // Permite ao Gestor consultar os usuários cadastrados
    // somente da própria Coordenadoria.
    // Utiliza paginação, pesquisa por nome,
    // área, perfil e status.
    // ============================================================

    /// <summary>
    /// Lista os usuários da própria Coordenadoria.
    /// </summary>
    /// <remarks>
    /// Permite filtrar os usuários por nome, área, perfil e status.
    /// A Coordenadoria é determinada automaticamente pelo Gestor
    /// autenticado e não pode ser alterada através da requisição.
    /// </remarks>
    [HttpGet("usuarios")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarUsuarios(
        [FromQuery] ListarUsuariosRequest request)
    {
        // ========================================================
        // IDENTIFICA O GESTOR LOGADO
        // ========================================================

        var usuarioGestorId =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(
            usuarioGestorId,
            out var gestorId))
        {
            return Unauthorized(new
            {
                mensagem =
                    "Não foi possível identificar o usuário autenticado."
            });
        }

        // Busca o Gestor no banco para obter sua Coordenadoria.
        var gestor = await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(usuario =>
                usuario.Id == gestorId);

        if (gestor == null)
        {
            return Unauthorized(new
            {
                mensagem = "Gestor não encontrado."
            });
        }

        // Segurança adicional:
        // garante que o usuário continua sendo Gestor.
        if (gestor.Perfil != PerfilUsuario.Gestor)
        {
            return Forbid();
        }

        // ========================================================
        // CONFIGURAÇÃO DA PAGINAÇÃO
        // ========================================================

        // Garante que a página seja no mínimo 1.
        if (request.Page < 1)
        {
            request.Page = 1;
        }

        // Define o tamanho padrão da página.
        if (request.PageSize < 1)
        {
            request.PageSize = 10;
        }

        // Limita o tamanho máximo da página.
        if (request.PageSize > 100)
        {
            request.PageSize = 100;
        }

        // ========================================================
        // INICIA A CONSULTA
        // ========================================================

        var query = _context.Usuarios
            .AsNoTracking()
            .Where(usuario =>
                usuario.CoordenadoriaId == gestor.CoordenadoriaId)
            .AsQueryable();

        // ========================================================
        // FILTRO POR NOME
        // ========================================================

        if (!string.IsNullOrWhiteSpace(request.Nome))
        {
            var nome = request.Nome.Trim();

            query = query.Where(usuario =>
                EF.Functions.ILike(
                    usuario.NomeCompleto,
                    $"%{nome}%"
                ));
        }

        // ========================================================
        // FILTRO POR ÁREA
        // ========================================================

        if (request.AreaId.HasValue)
        {
            query = query.Where(usuario =>
                usuario.AreaId ==
                request.AreaId.Value);
        }

        // ========================================================
        // FILTRO POR PERFIL
        // ========================================================

        if (request.Perfil.HasValue)
        {
            query = query.Where(usuario =>
                usuario.Perfil ==
                request.Perfil.Value);
        }

        // ========================================================
        // FILTRO POR STATUS
        // ========================================================

        if (request.Status.HasValue)
        {
            query = query.Where(usuario =>
                usuario.Status ==
                request.Status.Value);
        }

        // ========================================================
        // TOTAL DE REGISTROS
        // ========================================================

        var totalRegistros = await query.CountAsync();

        // Calcula o número total de páginas.
        var totalPaginas = (int)Math.Ceiling(
            totalRegistros / (double)request.PageSize
        );

        // ========================================================
        // PAGINAÇÃO E RETORNO
        // ========================================================

        var usuarios = await query
            .OrderBy(usuario => usuario.NomeCompleto)

            // Pula os registros das páginas anteriores.
            .Skip(
                (request.Page - 1) *
                request.PageSize
            )

            // Limita a quantidade de registros.
            .Take(request.PageSize)

            // Retorna somente os dados necessários.
            .Select(usuario => new
            {
                usuario.NomeCompleto,

                usuario.EmailCorporativo,

                coordenadoria =
                    usuario.Coordenadoria!.Nome,

                area =
                    usuario.Area!.Nome,

                ramal =
                usuario.Ramal,

                perfil =
                    usuario.Perfil.ToString(),

                status =
                    usuario.Status.ToString(),

                usuario.Ativo,

                criadoEm =
                    usuario.CriadoEm
                        .ToLocalTime()
                        .ToString("dd/MM/yyyy")
            })

            .ToListAsync();

        return Ok(new
        {
            pagina = request.Page,
            tamanhoPagina = request.PageSize,
            totalRegistros,
            totalPaginas,
            usuarios
        });
    }
}
