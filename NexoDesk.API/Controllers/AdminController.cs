using NexoDesk.Data;
using NexoDesk.DTOs;
using NexoDesk.Enums;
using NexoDesk.Helpers;
using NexoDesk.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace NexoDesk.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditoriaUsuarioService _auditoria;
    private readonly IPasswordHasher<Usuario> _passwordHasher;

    public AdminController(AppDbContext context, IAuditoriaUsuarioService auditoria, IPasswordHasher<Usuario> passwordHasher)
    {
        _context = context;
        _auditoria = auditoria;
        _passwordHasher = passwordHasher;
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("usuarios/{email}/senha")]
    public async Task<IActionResult> RedefinirSenha(string email, [FromBody] RedefinirSenhaAdminRequest request)
    {
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.EmailCorporativo == email.Trim().ToLowerInvariant());
        if (usuario is null) return NotFound(new { mensagem = "Usuário não encontrado." });

        var historico = await _context.HistoricosSenhas
            .Where(h => h.UsuarioId == usuario.Id)
            .OrderByDescending(h => h.CriadoEm)
            .ToListAsync();
        if (SenhaJaFoiUtilizada(usuario, historico, request.SenhaTemporaria))
            return BadRequest(new { mensagem = "A senha não atende aos requisitos da política de senha. Verifique os requisitos mínimos de comprimento, complexidade e histórico de senhas." });

        _context.HistoricosSenhas.Add(new HistoricoSenha { UsuarioId = usuario.Id, SenhaHash = usuario.SenhaHash });
        _context.HistoricosSenhas.RemoveRange(historico.Skip(4));
        usuario.SenhaHash = _passwordHasher.HashPassword(usuario, request.SenhaTemporaria);
        usuario.DeveAlterarSenha = true;
        usuario.VersaoToken++;
        usuario.AtualizadoEm = DateTime.UtcNow;

        // Pendente: enviar a senha temporária por SMTP quando a configuração estiver disponível.
        await _auditoria.RegistrarAsync(usuario.Id, "Senha temporária redefinida", new { }, HttpContext);
        await _context.SaveChangesAsync();
        return Ok(new { mensagem = "Senha temporária definida. O usuário deverá alterá-la no próximo acesso." });
    }

    private bool SenhaJaFoiUtilizada(Usuario usuario, IEnumerable<HistoricoSenha> historico, string senha)
    {
        return _passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha) != PasswordVerificationResult.Failed
            || historico.Any(h => _passwordHasher.VerifyHashedPassword(usuario, h.SenhaHash, senha) != PasswordVerificationResult.Failed);
    }

    [Authorize(Roles = "Administrador")]
    [HttpGet("auditorias/usuarios")]
    public async Task<IActionResult> ListarAuditorias(
        [FromQuery] string? nome,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query =
            from auditoria in _context.AuditoriasUsuarios.AsNoTracking()
            join usuarioAlvo in _context.Usuarios.AsNoTracking()
                on auditoria.UsuarioAlvoId equals usuarioAlvo.Id
            join realizadoPor in _context.Usuarios.AsNoTracking()
                on auditoria.RealizadoPorUsuarioId equals (Guid?)realizadoPor.Id into responsaveis
            from responsavel in responsaveis.DefaultIfEmpty()
            select new { auditoria, usuarioAlvo, responsavel };

        if (!string.IsNullOrWhiteSpace(nome))
        {
            var termo = nome.Trim();
            query = query.Where(item =>
                EF.Functions.ILike(item.usuarioAlvo.NomeCompleto, $"%{termo}%") ||
                (item.responsavel != null &&
                 EF.Functions.ILike(item.responsavel.NomeCompleto, $"%{termo}%")));
        }

        var total = await query.CountAsync();
        var auditorias = await query
            .OrderByDescending(item => item.auditoria.CriadoEm)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new
            {
                item.auditoria.Id,
                item.auditoria.UsuarioAlvoId,
                usuarioAlvoNome = item.usuarioAlvo.NomeCompleto,
                item.auditoria.RealizadoPorUsuarioId,
                realizadoPorNome = item.responsavel != null
                    ? item.responsavel.NomeCompleto
                    : null,
                item.auditoria.Acao,
                item.auditoria.Alteracoes,
                item.auditoria.IpOrigem,
                item.auditoria.CriadoEm
            })
            .ToListAsync();
        return Ok(new { pagina = page, tamanhoPagina = pageSize, totalRegistros = total, auditorias });
    }

    /// <summary>
    /// Atualiza os dados cadastrais de um usuário.
    /// </summary>
    /// <remarks>
    /// O usuário é localizado pelo e-mail corporativo informado na rota.
    /// Perfil e status possuem endpoints próprios, pois aplicam regras de negócio específicas.
    /// </remarks>
    [Authorize(Roles = "Administrador")]
    [HttpPut("usuarios/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AtualizarUsuario(
        string email,
        [FromBody] AtualizarUsuarioAdminRequest request)
    {
        var emailAtual = email.Trim().ToLowerInvariant();
        var emailCorporativo = request.EmailCorporativo.Trim().ToLowerInvariant();

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.EmailCorporativo == emailAtual);

        if (usuario is null)
        {
            return NotFound(new { mensagem = "Usuário não encontrado." });
        }

        var emailEmUso = await _context.Usuarios
            .AnyAsync(u => u.Id != usuario.Id && u.EmailCorporativo == emailCorporativo);

        if (emailEmUso)
        {
            return Conflict(new { mensagem = "Já existe um usuário com esse e-mail." });
        }

        var areaValida = await _context.Areas
            .AnyAsync(a => a.Id == request.AreaId &&
                           a.CoordenadoriaId == request.CoordenadoriaId);

        if (!areaValida)
        {
            return BadRequest(new
            {
                mensagem = "A área informada não pertence à coordenadoria selecionada."
            });
        }

        if (usuario.Perfil == PerfilUsuario.Gestor && usuario.Ativo)
        {
            var gestoresNaCoordenadoria = await _context.Usuarios
                .CountAsync(u => u.Id != usuario.Id &&
                                 u.CoordenadoriaId == request.CoordenadoriaId &&
                                 u.Perfil == PerfilUsuario.Gestor &&
                                 u.Ativo);

            if (gestoresNaCoordenadoria >= 2)
            {
                return BadRequest(new
                {
                    mensagem = "A coordenadoria já possui o número máximo de 2 gestores."
                });
            }
        }

        var dadosAnteriores = new
        {
            usuario.NomeCompleto, usuario.EmailCorporativo, usuario.Ramal,
            usuario.CoordenadoriaId, usuario.AreaId
        };
        usuario.NomeCompleto = request.NomeCompleto.Trim();
        usuario.EmailCorporativo = emailCorporativo;
        usuario.Ramal = request.Ramal.Trim();
        usuario.CoordenadoriaId = request.CoordenadoriaId;
        usuario.AreaId = request.AreaId;
        usuario.AtualizadoEm = DateTime.UtcNow;

        await _auditoria.RegistrarAsync(usuario.Id, "Dados pessoais alterados", new
        {
            anterior = dadosAnteriores,
            atual = new { usuario.NomeCompleto, usuario.EmailCorporativo,
                usuario.Ramal, usuario.CoordenadoriaId, usuario.AreaId }
        }, HttpContext);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Dados do usuário atualizados com sucesso.",
            usuario.NomeCompleto,
            usuario.EmailCorporativo,
            usuario.Ramal,
            usuario.CoordenadoriaId,
            usuario.AreaId,
            usuario.AtualizadoEm
        });
    }

    // ============================================================
    // ALTERAR STATUS DO USUÁRIO
    // Somente usuários com perfil Administrador
    // podem alterar o status de outros usuários.
    // ============================================================

    /// <summary>
    /// Altera o status de um usuário.
    /// </summary>
    /// <remarks>
    /// Permite ao administrador alterar o status do usuário.
    /// 
    /// Status permitidos:
    /// AguardandoAprovacao = 1 (status inicial),
    /// Aprovado = 2,
    /// Reprovado = 3,
    /// Desativado = 4
    /// 
    /// O usuário é identificado pelo e-mail corporativo.
    /// </remarks>
    [Authorize(Roles = "Administrador")]
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
        // Normaliza o e-mail recebido pela rota.
        var emailCorporativo = email
            .Trim()
            .ToLowerInvariant();

        // Busca o usuário pelo e-mail corporativo.
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(usuario =>
                usuario.EmailCorporativo == emailCorporativo);

        // Usuário não encontrado.
        if (usuario == null)
        {
            return NotFound(new
            {
                mensagem = "Usuário não encontrado."
            });
        }

        var statusAnterior = usuario.Status;
        var ativoAnterior = usuario.Ativo;

        // Obtém o ID do administrador através do JWT.
        var usuarioAdministradorId =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // ========================================================
        // APROVAÇÃO / REATIVAÇÃO
        // ========================================================

        if (request.Status == StatusUsuario.Aprovado)
        {
            // ----------------------------------------------------
            // NOVO USUÁRIO AGUARDANDO APROVAÇÃO
            // ----------------------------------------------------

            if (usuario.Status == StatusUsuario.AguardandoAprovacao)
            {
                usuario.Status = StatusUsuario.Aprovado;
                usuario.Ativo = true;
                usuario.AprovadoEm = DateTime.UtcNow;

                // Registra qual administrador realizou a aprovação.
                if (Guid.TryParse(
                    usuarioAdministradorId,
                    out var administradorId))
                {
                    usuario.AprovadoPorUsuarioId =
                        administradorId;
                }
            }

            // ----------------------------------------------------
            // USUÁRIO DESATIVADO
            // Reativa o usuário.
            // ----------------------------------------------------

            else if (usuario.Status == StatusUsuario.Desativado)
            {
                usuario.Status = StatusUsuario.Aprovado;
                usuario.Ativo = true;
            }

            // ----------------------------------------------------
            // OUTROS STATUS
            // ----------------------------------------------------

            else
            {
                return BadRequest(new
                {
                    mensagem =
                        "O usuário não pode ser aprovado neste status."
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

        else
        {
            return BadRequest(new
            {
                mensagem = "Status informado não pode ser aplicado."
            });
        }

        // Registra quando o usuário foi atualizado.
        usuario.AtualizadoEm = DateTime.UtcNow;
        usuario.VersaoToken++;

        await _auditoria.RegistrarAsync(usuario.Id, "Status alterado", new
        {
            anterior = new { status = statusAnterior.ToString(), ativo = ativoAnterior },
            atual = new { status = usuario.Status.ToString(), ativo = usuario.Ativo }
        }, HttpContext);
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
    // ALTERAR PERFIL DO USUÁRIO
    // Somente usuários com perfil Administrador
    // podem alterar o perfil de outros usuários.
    // ============================================================

    /// <summary>
    /// Altera o perfil de um usuário.
    /// </summary>
    /// <remarks>
    /// Permite ao administrador alterar o perfil do usuário.
    ///
    /// Perfis disponíveis:
    /// Padrao = 1,
    /// Gestor = 2,
    /// Administrador = 3
    ///
    /// O usuário é identificado pelo e-mail corporativo.
    /// </remarks>
    [Authorize(Roles = "Administrador")]
    [HttpPut("usuarios/{email}/perfil")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AlterarPerfilUsuario(
        string email,
        [FromBody] AlterarPerfilUsuarioRequest request)
    {
        // Normaliza o e-mail recebido pela rota.
        var emailCorporativo = email
            .Trim()
            .ToLowerInvariant();

        // Busca o usuário pelo e-mail corporativo.
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(usuario =>
                usuario.EmailCorporativo == emailCorporativo);

        // Usuário não encontrado.
        if (usuario == null)
        {
            return NotFound(new
            {
                mensagem = "Usuário não encontrado."
            });
        }

        var perfilAnterior = usuario.Perfil;

        // Verifica se o perfil informado é válido.
        if (!Enum.IsDefined(typeof(PerfilUsuario), request.Perfil))
        {
            return BadRequest(new
            {
                mensagem = "Perfil informado é inválido."
            });
        }

        // Verifica se está tentando promover o usuário para Gestor.
        if (request.Perfil == PerfilUsuario.Gestor &&
            usuario.Perfil != PerfilUsuario.Gestor)
        {
            var quantidadeGestores = await _context.Usuarios
                .CountAsync(usuarioExistente =>
                    usuarioExistente.CoordenadoriaId == usuario.CoordenadoriaId &&
                    usuarioExistente.Perfil == PerfilUsuario.Gestor &&
                    usuarioExistente.Ativo);

            if (quantidadeGestores >= 2)
            {
                return BadRequest(new
                {
                    mensagem =
                        "A coordenadoria já possui o número máximo de 2 gestores."
                });
            }
        }

        // Altera o perfil.
        usuario.Perfil = request.Perfil;

        // Registra quando o usuário foi atualizado.
        usuario.AtualizadoEm = DateTime.UtcNow;
        usuario.VersaoToken++;

        await _auditoria.RegistrarAsync(usuario.Id, "Perfil alterado", new
        {
            anterior = perfilAnterior.ToString(),
            atual = usuario.Perfil.ToString()
        }, HttpContext);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Perfil do usuário alterado com sucesso.",
            usuario.NomeCompleto,
            perfil = usuario.Perfil.ToString()
        });
    }

    // ============================================================
    // DETALHES DO USUÁRIO
    // Retorna as informações completas de um usuário.
    // Somente administradores podem consultar.
    // ============================================================

    /// <summary>
    /// Retorna os detalhes de um usuário.
    /// </summary>
    /// <remarks>
    /// O usuário é identificado pelo e-mail corporativo.
    /// O CPF não é retornado, pois somente sua representação HMAC é armazenada.
    /// </remarks>
    [Authorize(Roles = "Administrador")]
    [HttpGet("usuarios/{email}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterUsuario(string email)
    {
        var emailCorporativo = email
            .Trim()
            .ToLowerInvariant();

        var usuario = await _context.Usuarios
            .AsNoTracking()
            .Where(usuario =>
                usuario.EmailCorporativo == emailCorporativo)
            .Select(usuario => new
            {
                usuario.NomeCompleto,

                usuario.EmailCorporativo,

                usuario.CoordenadoriaId,

                coordenadoria =
                    usuario.Coordenadoria!.Nome,

                usuario.AreaId,

                area =
                    usuario.Area!.Nome,

                ramal =
                    usuario.Ramal,

                perfil =
                    usuario.Perfil.ToString(),

                status =
                    usuario.Status.ToString(),

                usuario.Ativo,

                usuario.CriadoEm,

                usuario.AtualizadoEm,

                usuario.AprovadoEm,

                usuario.AprovadoPorUsuarioId
            })
            .FirstOrDefaultAsync();

        if (usuario == null)
        {
            return NotFound(new
            {
                mensagem = "Usuário não encontrado."
            });
        }

        // Busca o nome do administrador que aprovou o usuário.
        string? aprovadoPor = null;

        if (usuario.AprovadoPorUsuarioId.HasValue)
        {
            aprovadoPor = await _context.Usuarios
                .AsNoTracking()
                .Where(u =>
                    u.Id == usuario.AprovadoPorUsuarioId.Value)
                .Select(u => u.NomeCompleto)
                .FirstOrDefaultAsync();
        }

        return Ok(new
        {
            nomeCompleto =
                usuario.NomeCompleto,

            emailCorporativo =
                usuario.EmailCorporativo,

            coordenadoriaId = usuario.CoordenadoriaId,

            coordenadoria =
                usuario.coordenadoria,

            areaId = usuario.AreaId,

            area =
                usuario.area,

            ramal =
                usuario.ramal,

            perfil =
                usuario.perfil,

            status =
                usuario.status,

            ativo =
                usuario.Ativo,

            criadoEm =
                usuario.CriadoEm
                    .ToLocalTime()
                    .ToString("dd/MM/yyyy HH:mm"),

            atualizadoEm =
                usuario.AtualizadoEm.HasValue
                    ? usuario.AtualizadoEm.Value
                        .ToLocalTime()
                        .ToString("dd/MM/yyyy HH:mm")
                    : null,

            aprovadoEm =
    usuario.AprovadoEm.HasValue
        ? usuario.AprovadoEm.Value
            .ToLocalTime()
            .ToString("dd/MM/yyyy HH:mm")
        : null,

            aprovadoPor = aprovadoPor
        });
    }

    // ============================================================
    // LISTAR USUÁRIOS PENDENTES
    // Retorna os usuários que estão aguardando aprovação.
    // Somente administradores podem consultar essa lista.
    // ============================================================

    /// <summary>
    /// Lista de usuários pendentes
    /// </summary>
    [Authorize(Roles = "Administrador")]
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

        var query = _context.Usuarios
            .AsNoTracking()
            .Where(usuario =>
                usuario.Status == StatusUsuario.AguardandoAprovacao)
            .AsQueryable();

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
    // Permite ao administrador consultar os usuários cadastrados
    // utilizando paginação, pesquisa por nome, coordenadoria e área.
    // ============================================================

    /// <summary>
    /// Lista os usuários cadastrados no sistema.
    /// </summary>
    /// <remarks>
    /// Permite filtrar os usuários por nome, coordenadoria e área.
    /// A consulta utiliza paginação para evitar o carregamento
    /// de todos os usuários de uma única vez.
    /// </remarks>

    [Authorize(Roles = "Administrador")]
    [HttpGet("usuarios")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListarUsuarios(
        [FromQuery] ListarUsuariosRequest request)
    {
        // Garante que a página seja no mínimo 1.
        if (request.Page < 1)
        {
            request.Page = 1;
        }

        // Limita a quantidade máxima de registros
        // retornados por página.
        if (request.PageSize < 1)
        {
            request.PageSize = 10;
        }

        if (request.PageSize > 100)
        {
            request.PageSize = 100;
        }

        // Inicia a consulta sem executar o SQL ainda.
        var query = _context.Usuarios
            .AsNoTracking()
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
        // FILTRO POR COORDENADORIA
        // ========================================================

        if (request.CoordenadoriaId.HasValue)
        {
            query = query.Where(usuario =>
                usuario.CoordenadoriaId ==
                request.CoordenadoriaId.Value);
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
                usuario.Perfil == request.Perfil.Value);
        }

        // ========================================================
        // FILTRO POR STATUS
        // ========================================================

        if (request.Status.HasValue)
        {
            query = query.Where(usuario =>
                usuario.Status == request.Status.Value);
        }

        // Conta quantos usuários existem depois
        // da aplicação dos filtros.
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

            // Pega somente a quantidade da página atual.
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
