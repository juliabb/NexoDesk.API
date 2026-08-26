using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexoDesk.Data;
using NexoDesk.DTOs;
using NexoDesk.Models;

namespace NexoDesk.Controllers;

[ApiController]
[Route("api/categorias-chamados")]
[Authorize]
public class CategoriasChamadosController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriasChamadosController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Lista as categorias disponíveis para abertura de chamados.</summary>
    [HttpGet]
    [Tags("Chamados - Categorias")]
    public async Task<IActionResult> Listar([FromQuery] bool incluirInativas = false)
    {
        var query = _context.CategoriasChamados.AsNoTracking();

        if (!incluirInativas || !User.IsInRole("Administrador"))
        {
            query = query.Where(categoria => categoria.Ativa);
        }

        var categorias = await query
            .OrderBy(categoria => categoria.Nome)
            .Select(categoria => new
            {
                categoria.Id,
                categoria.Nome,
                categoria.Descricao,
                categoria.Ativa,
                setorResponsavel = new
                {
                    categoria.SetorResponsavel.Id,
                    categoria.SetorResponsavel.Nome
                }
            })
            .ToListAsync();

        return Ok(categorias);
    }

    /// <summary>Cadastra uma categoria de chamado.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    [Tags("Admin - Categorias de chamados")]
    public async Task<IActionResult> Criar([FromBody] CriarCategoriaChamadoRequest request)
    {
        var nome = request.Nome.Trim();
        var setorExiste = await _context.Setores.AnyAsync(setor => setor.Id == request.SetorResponsavelId);

        if (!setorExiste)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Setor responsável não encontrado.",
                detail: $"Não existe um setor com o identificador {request.SetorResponsavelId}.");
        }

        var nomeEmUso = await _context.CategoriasChamados
            .AnyAsync(categoria => categoria.Nome.ToLower() == nome.ToLower());

        if (nomeEmUso)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Categoria duplicada.", detail: $"Já existe uma categoria chamada \"{nome}\".");
        }

        var categoria = new CategoriaChamado
        {
            Nome = nome,
            Descricao = NormalizarTextoOpcional(request.Descricao),
            SetorResponsavelId = request.SetorResponsavelId
        };

        _context.CategoriasChamados.Add(categoria);
        await _context.SaveChangesAsync();

        return Created($"/api/categorias-chamados/{categoria.Id}", new
        {
            categoria.Id,
            categoria.Nome,
            categoria.Descricao,
            categoria.SetorResponsavelId,
            categoria.Ativa
        });
    }

    /// <summary>Atualiza uma categoria e permite ativá-la ou desativá-la.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    [Tags("Admin - Categorias de chamados")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] AtualizarCategoriaChamadoRequest request)
    {
        var categoria = await _context.CategoriasChamados.FindAsync(id);

        if (categoria is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Categoria não encontrada.",
                detail: $"Não existe uma categoria com o identificador {id}.");
        }

        var setorExiste = await _context.Setores.AnyAsync(setor => setor.Id == request.SetorResponsavelId);
        if (!setorExiste)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Setor responsável não encontrado.",
                detail: $"Não existe um setor com o identificador {request.SetorResponsavelId}.");
        }

        var nome = request.Nome.Trim();
        var nomeEmUso = await _context.CategoriasChamados.AnyAsync(categoriaExistente =>
            categoriaExistente.Id != id && categoriaExistente.Nome.ToLower() == nome.ToLower());

        if (nomeEmUso)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Categoria duplicada.", detail: $"Já existe uma categoria chamada \"{nome}\".");
        }

        categoria.Nome = nome;
        categoria.Descricao = NormalizarTextoOpcional(request.Descricao);
        categoria.SetorResponsavelId = request.SetorResponsavelId;
        categoria.Ativa = request.Ativa;
        categoria.AtualizadaEm = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { mensagem = "Categoria atualizada com sucesso." });
    }

    private static string? NormalizarTextoOpcional(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
