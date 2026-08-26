using NexoDesk.Data;
using NexoDesk.DTOs;
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
[Authorize]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly IAuditoriaUsuarioService _auditoria;

    public UsuariosController(
        AppDbContext context,
        IPasswordHasher<Usuario> passwordHasher,
        IAuditoriaUsuarioService auditoria)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _auditoria = auditoria;
    }

    [HttpPut("me/senha")]
    public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaRequest request)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(id, out var usuarioId)) return Unauthorized();
        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId);
        if (usuario is null) return Unauthorized();
        if (_passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash, request.SenhaAtual)
            == PasswordVerificationResult.Failed)
            return BadRequest(new { mensagem = "A senha atual está incorreta." });

        var historico = await _context.HistoricosSenhas
            .Where(h => h.UsuarioId == usuario.Id)
            .OrderByDescending(h => h.CriadoEm)
            .ToListAsync();
        if (SenhaJaFoiUtilizada(usuario, historico, request.NovaSenha))
            return BadRequest(new { mensagem = "A senha não atende aos requisitos da política de senha. Verifique os requisitos mínimos de comprimento, complexidade e histórico de senhas." });

        _context.HistoricosSenhas.Add(new HistoricoSenha { UsuarioId = usuario.Id, SenhaHash = usuario.SenhaHash });
        _context.HistoricosSenhas.RemoveRange(historico.Skip(4));
        usuario.SenhaHash = _passwordHasher.HashPassword(usuario, request.NovaSenha);
        usuario.DeveAlterarSenha = false;
        usuario.VersaoToken++;
        usuario.AtualizadoEm = DateTime.UtcNow;
        await _auditoria.RegistrarAsync(usuario.Id, "Senha alterada pelo usuário", new { }, HttpContext);
        await _context.SaveChangesAsync();
        return Ok(new { mensagem = "Senha alterada com sucesso. Faça login novamente." });
    }

    private bool SenhaJaFoiUtilizada(Usuario usuario, IEnumerable<HistoricoSenha> historico, string senha)
    {
        return _passwordHasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha) != PasswordVerificationResult.Failed
            || historico.Any(h => _passwordHasher.VerifyHashedPassword(usuario, h.SenhaHash, senha) != PasswordVerificationResult.Failed);
    }

    /// <summary>
    /// Atualiza os dados permitidos do usuário autenticado.
    /// </summary>
    [HttpPut("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AtualizarMeuPerfil(
        [FromBody] AtualizarMeuPerfilRequest request)
    {
        var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioIdClaim, out var usuarioId))
        {
            return Unauthorized(new { mensagem = "Token inválido." });
        }

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId && u.Ativo);

        if (usuario is null)
        {
            return Unauthorized(new
            {
                mensagem = "Usuário não encontrado ou inativo."
            });
        }

        var emailCorporativo = request.EmailCorporativo.Trim().ToLowerInvariant();
        var emailEmUso = await _context.Usuarios
            .AnyAsync(u => u.Id != usuarioId &&
                           u.EmailCorporativo == emailCorporativo);

        if (emailEmUso)
        {
            return Conflict(new
            {
                mensagem = "Já existe um usuário com esse e-mail."
            });
        }

        var dadosAnteriores = new
        {
            usuario.NomeCompleto,
            usuario.EmailCorporativo,
            usuario.Ramal
        };

        usuario.NomeCompleto = request.NomeCompleto.Trim();
        usuario.EmailCorporativo = emailCorporativo;
        usuario.Ramal = request.Ramal.Trim();
        usuario.AtualizadoEm = DateTime.UtcNow;

        await _auditoria.RegistrarAsync(usuario.Id, "Dados pessoais alterados pelo usuário", new
        {
            anterior = dadosAnteriores,
            atual = new { usuario.NomeCompleto, usuario.EmailCorporativo, usuario.Ramal }
        }, HttpContext);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Dados atualizados com sucesso.",
            usuario.NomeCompleto,
            usuario.EmailCorporativo,
            usuario.Ramal,
            usuario.AtualizadoEm
        });
    }

    /// <summary>
    /// Retorna as informações do usuário autenticado.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ObterUsuarioLogado()
    {
        var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioIdClaim, out var usuarioId))
        {
            return Unauthorized(new
            {
                mensagem = "Token inválido."
            });
        }

        var usuario = await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == usuarioId && u.Ativo)
            .Select(u => new
            {
                id = u.Id,
                nomeCompleto = u.NomeCompleto,
                emailCorporativo = u.EmailCorporativo,
                ramal = u.Ramal,
                perfil = u.Perfil,
                coordenadoria = u.Coordenadoria!.Nome,
                area = u.Area!.Nome,
                criadoEm = u.CriadoEm
            })
            .FirstOrDefaultAsync();

        if (usuario is null)
        {
            return Unauthorized(new
            {
                mensagem = "Usuário não encontrado ou inativo."
            });
        }

        return Ok(new
        {
            usuario.id,
            usuario.nomeCompleto,
            usuario.emailCorporativo,
            usuario.ramal,
            usuario.perfil,
            usuario.coordenadoria,
            usuario.area,
            usuario.criadoEm
        });
    }

    /// <summary>
    /// Lista simplificada de colaboradores.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> ListarUsuarios(
        [FromQuery] ListarColaboradoresRequest request)
    {
        if (request.Page < 1)
            request.Page = 1;

        if (request.PageSize < 1)
            request.PageSize = 10;

        if (request.PageSize > 100)
            request.PageSize = 100;

        var query = _context.Usuarios
            .AsNoTracking()
            .Where(u => u.Ativo);

        // Pesquisa por nome
        if (!string.IsNullOrWhiteSpace(request.Nome))
        {
            var nome = request.Nome.Trim();

            query = query.Where(u =>
                EF.Functions.ILike(u.NomeCompleto, $"%{nome}%"));
        }

        // Filtro por coordenadoria
        if (request.CoordenadoriaId.HasValue)
        {
            query = query.Where(u =>
                u.CoordenadoriaId == request.CoordenadoriaId);
        }

        // Filtro por área
        if (request.AreaId.HasValue)
        {
            query = query.Where(u =>
                u.AreaId == request.AreaId);
        }

        if (request.Perfil.HasValue)
        {
            query = query.Where(u => u.Perfil == request.Perfil.Value);
        }

        var totalRegistros = await query.CountAsync();

        var totalPaginas = (int)Math.Ceiling(
            totalRegistros / (double)request.PageSize);

        var usuarios = await query
            .OrderBy(u => u.NomeCompleto)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new
            {
                nomeCompleto = u.NomeCompleto,
                emailCorporativo = u.EmailCorporativo,
                coordenadoria = u.Coordenadoria!.Nome,
                siglaCoordenadoria = u.Coordenadoria!.Sigla,
                area = u.Area!.Nome,
                ramal = u.Ramal,
                perfil = u.Perfil.ToString()
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
