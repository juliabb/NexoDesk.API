using NexoDesk.Data;
using NexoDesk.DTOs;
using NexoDesk.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace NexoDesk.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class CoordenadoriasController : ControllerBase
{
    private readonly AppDbContext _context;

    public CoordenadoriasController(AppDbContext context)

    {
        _context = context;
    }

    /// <summary>
    /// Cadastra uma coordenadoria.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Tags("Admin - Coordenadorias")]
    public async Task<IActionResult> Criar(
        [FromBody] CriarCoordenadoriaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Sigla))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Dados inválidos.", detail: "Informe o nome e a sigla da coordenadoria.");
        }

        var nome = request.Nome.Trim();
        var sigla = request.Sigla.Trim().ToUpperInvariant();
        if (sigla.Length > 20)
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Dados inválidos.", detail: "A sigla deve ter no máximo 20 caracteres.");

        var existeNome = await _context.Coordenadorias
            .AnyAsync(coordenadoria => coordenadoria.Nome.ToLower() == nome.ToLower());
        var existeSigla = await _context.Coordenadorias
            .AnyAsync(coordenadoria => coordenadoria.Sigla == sigla);

        if (existeNome || existeSigla)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Coordenadoria duplicada.", detail: existeNome
                    ? $"Já existe uma coordenadoria chamada \"{nome}\"."
                    : $"Já existe uma coordenadoria com a sigla \"{sigla}\".");
        }

        var coordenadoria = new Coordenadoria
        {
            Nome = nome,
            Sigla = sigla
        };

        _context.Coordenadorias.Add(coordenadoria);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(Listar),
            new { id = coordenadoria.Id },
            new
            {
                coordenadoria.Id,
                coordenadoria.Nome,
                coordenadoria.Sigla
            });
    }

    /// <summary>
    /// Altera o nome de uma coordenadoria.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Tags("Admin - Coordenadorias")]
    public async Task<IActionResult> Alterar(
        int id,
        [FromBody] AlterarCoordenadoriaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Sigla))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Dados inválidos.", detail: "Informe o nome e a sigla da coordenadoria.");
        }

        var coordenadoria = await _context.Coordenadorias
            .FirstOrDefaultAsync(coordenadoria =>
                coordenadoria.Id == id);

        if (coordenadoria == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Coordenadoria não encontrada.", detail: $"Não existe uma coordenadoria com o identificador {id}.");
        }

        var nome = request.Nome.Trim();
        var sigla = request.Sigla.Trim().ToUpperInvariant();
        if (sigla.Length > 20)
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Dados inválidos.", detail: "A sigla deve ter no máximo 20 caracteres.");

        var existeNome = await _context.Coordenadorias
            .AnyAsync(coordenadoria => coordenadoria.Id != id && coordenadoria.Nome.ToLower() == nome.ToLower());
        var existeSigla = await _context.Coordenadorias
            .AnyAsync(coordenadoria => coordenadoria.Id != id && coordenadoria.Sigla == sigla);

        if (existeNome || existeSigla)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Coordenadoria duplicada.", detail: existeNome
                    ? $"Já existe uma coordenadoria chamada \"{nome}\"."
                    : $"Já existe uma coordenadoria com a sigla \"{sigla}\".");
        }

        coordenadoria.Nome = nome;
        coordenadoria.Sigla = sigla;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Coordenadoria alterada com sucesso.",
            coordenadoria.Id,
            coordenadoria.Nome,
            coordenadoria.Sigla
        });
    }

    /// <summary>
    /// Exclui uma coordenadoria.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Tags("Admin - Coordenadorias")]
    public async Task<IActionResult> Excluir(int id)
    {
        var coordenadoria = await _context.Coordenadorias
            .FirstOrDefaultAsync(coordenadoria =>
                coordenadoria.Id == id);

        if (coordenadoria == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Coordenadoria não encontrada.", detail: $"Não existe uma coordenadoria com o identificador {id}.");
        }

        // Verifica se existem áreas vinculadas.
        var quantidadeAreas = await _context.Areas
            .CountAsync(area => area.CoordenadoriaId == id);

        if (quantidadeAreas > 0)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível excluir a coordenadoria.",
                detail: $"A coordenadoria \"{coordenadoria.Nome}\" possui {quantidadeAreas} área(s) vinculada(s). Exclua ou transfira essas áreas antes de excluir a coordenadoria.");
        }

        // Verifica se existem usuários vinculados.
        var quantidadeUsuarios = await _context.Usuarios
            .CountAsync(usuario => usuario.CoordenadoriaId == id);

        if (quantidadeUsuarios > 0)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível excluir a coordenadoria.",
                detail: $"A coordenadoria \"{coordenadoria.Nome}\" possui {quantidadeUsuarios} usuário(s) vinculado(s). Transfira esses usuários antes de excluir a coordenadoria.");
        }

        _context.Coordenadorias.Remove(coordenadoria);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Lista todas as coordenadorias.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Tags("Cadastro - Coordenadorias")]
    public async Task<IActionResult> Listar()
    {
        var coordenadorias = await _context.Coordenadorias
            .AsNoTracking()
            .OrderBy(coordenadoria => coordenadoria.Nome)
            .Select(coordenadoria => new
            {
                coordenadoria.Id,
                coordenadoria.Nome,
                coordenadoria.Sigla
            })
            .ToListAsync();

        return Ok(coordenadorias);
    }
}
