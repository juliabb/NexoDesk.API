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
public class AreasController : ControllerBase
{
    private readonly AppDbContext _context;

    public AreasController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Cadastra uma nova área vinculada a uma coordenadoria.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Tags("Admin - Áreas")]
    public async Task<IActionResult> Criar(
        [FromBody] CriarAreaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Dados inválidos.",
                detail: "Informe o nome da área.");
        }

        var coordenadoria = await _context.Coordenadorias
            .FirstOrDefaultAsync(coordenadoria =>
                coordenadoria.Id == request.CoordenadoriaId);

        if (coordenadoria == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Coordenadoria não encontrada.",
                detail: $"Não existe uma coordenadoria com o identificador {request.CoordenadoriaId}.");
        }

        var nome = request.Nome.Trim();

        var existe = await _context.Areas
            .AnyAsync(area =>
                area.CoordenadoriaId == request.CoordenadoriaId &&
                area.Nome.ToLower() == nome.ToLower());

        if (existe)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Área duplicada.",
                detail: $"Já existe uma área chamada \"{nome}\" nessa coordenadoria.");
        }

        var area = new Area
        {
            Nome = nome,
            CoordenadoriaId = request.CoordenadoriaId
        };

        _context.Areas.Add(area);

        await _context.SaveChangesAsync();

        return Created(
            $"/api/areas/{area.Id}",
            new
            {
                area.Id,
                area.Nome,
                coordenadoriaId = area.CoordenadoriaId
            });
    }

    /// <summary>
    /// Altera uma área e sua coordenadoria.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Tags("Admin - Áreas")]
    public async Task<IActionResult> Alterar(
        int id,
        [FromBody] AlterarAreaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Dados inválidos.",
                detail: "Informe o nome da área.");
        }

        var area = await _context.Areas
            .FirstOrDefaultAsync(area =>
                area.Id == id);

        if (area == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Área não encontrada.",
                detail: $"Não existe uma área com o identificador {id}.");
        }

        var coordenadoriaExiste =
            await _context.Coordenadorias
                .AnyAsync(coordenadoria =>
                    coordenadoria.Id == request.CoordenadoriaId);

        if (!coordenadoriaExiste)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Coordenadoria não encontrada.",
                detail: $"Não existe uma coordenadoria com o identificador {request.CoordenadoriaId}.");
        }

        var nome = request.Nome.Trim();

        var existe = await _context.Areas
            .AnyAsync(area =>
                area.Id != id &&
                area.CoordenadoriaId == request.CoordenadoriaId &&
                area.Nome.ToLower() == nome.ToLower());

        if (existe)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Área duplicada.",
                detail: $"Já existe uma área chamada \"{nome}\" nessa coordenadoria.");
        }

        area.Nome = nome;
        area.CoordenadoriaId = request.CoordenadoriaId;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Área alterada com sucesso.",
            area.Id,
            area.Nome,
            area.CoordenadoriaId
        });
    }

    /// <summary>
    /// Exclui uma área.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Tags("Admin - Áreas")]
    public async Task<IActionResult> Excluir(int id)
    {
        var area = await _context.Areas
            .FirstOrDefaultAsync(area =>
                area.Id == id);

        if (area == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Área não encontrada.",
                detail: $"Não existe uma área com o identificador {id}.");
        }

        // Verifica se existem usuários vinculados.
        var quantidadeUsuarios = await _context.Usuarios
            .CountAsync(usuario => usuario.AreaId == id);

        if (quantidadeUsuarios > 0)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível excluir a área.",
                detail: $"A área \"{area.Nome}\" possui {quantidadeUsuarios} usuário(s) vinculado(s). Transfira esses usuários para outra área antes de excluí-la.");
        }

        _context.Areas.Remove(area);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Lista as áreas de uma coordenadoria.
    /// </summary>
    [HttpGet("/api/coordenadorias/{coordenadoriaId}/areas")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Tags("Cadastro - Áreas")]
    public async Task<IActionResult> ListarPorCoordenadoria(
        int coordenadoriaId,
        [FromQuery] string? nome = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        var coordenadoriaExiste =
            await _context.Coordenadorias
                .AnyAsync(coordenadoria =>
                    coordenadoria.Id == coordenadoriaId);

        if (!coordenadoriaExiste)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Coordenadoria não encontrada.",
                detail: $"Não existe uma coordenadoria com o identificador {coordenadoriaId}.");
        }

        var query = _context.Areas
            .AsNoTracking()
            .Where(area =>
                area.CoordenadoriaId == coordenadoriaId);

        if (!string.IsNullOrWhiteSpace(nome))
        {
            var termo = nome.Trim().ToLower();
            query = query.Where(area => area.Nome.ToLower().Contains(termo));
        }

        // Sem paginação, preserva o contrato usado nos formulários de cadastro.
        if (!page.HasValue && !pageSize.HasValue)
        {
            var todasAsAreas = await query
                .OrderBy(area => area.Nome)
                .Select(area => new { area.Id, area.Nome })
                .ToListAsync();

            return Ok(todasAsAreas);
        }

        var pagina = Math.Max(1, page ?? 1);
        var tamanhoPagina = Math.Clamp(pageSize ?? 8, 1, 100);
        var totalRegistros = await query.CountAsync();
        var areas = await query
            .OrderBy(area => area.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(area => new
            {
                area.Id,
                area.Nome,
            })
            .ToListAsync();

        return Ok(new
        {
            pagina,
            tamanhoPagina,
            totalRegistros,
            areas
        });
    }
}
