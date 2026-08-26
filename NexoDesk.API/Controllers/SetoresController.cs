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
public class SetoresController : ControllerBase
{
    private readonly AppDbContext _context;

    public SetoresController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Cadastra uma nova setor vinculada a uma departamento.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Tags("Admin - Setores")]
    public async Task<IActionResult> Criar(
        [FromBody] CriarSetorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Dados inválidos.",
                detail: "Informe o nome da setor.");
        }

        var departamento = await _context.Departamentos
            .FirstOrDefaultAsync(departamento =>
                departamento.Id == request.DepartamentoId);

        if (departamento == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Departamento não encontrada.",
                detail: $"Não existe uma departamento com o identificador {request.DepartamentoId}.");
        }

        var nome = request.Nome.Trim();

        var existe = await _context.Setores
            .AnyAsync(setor =>
                setor.DepartamentoId == request.DepartamentoId &&
                setor.Nome.ToLower() == nome.ToLower());

        if (existe)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Setor duplicada.",
                detail: $"Já existe uma setor chamada \"{nome}\" nessa departamento.");
        }

        var setor = new Setor
        {
            Nome = nome,
            DepartamentoId = request.DepartamentoId
        };

        _context.Setores.Add(setor);

        await _context.SaveChangesAsync();

        return Created(
            $"/api/setores/{setor.Id}",
            new
            {
                setor.Id,
                setor.Nome,
                departamentoId = setor.DepartamentoId
            });
    }

    /// <summary>
    /// Altera uma setor e sua departamento.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Tags("Admin - Setores")]
    public async Task<IActionResult> Alterar(
        int id,
        [FromBody] AlterarSetorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Dados inválidos.",
                detail: "Informe o nome da setor.");
        }

        var setor = await _context.Setores
            .FirstOrDefaultAsync(setor =>
                setor.Id == id);

        if (setor == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Setor não encontrada.",
                detail: $"Não existe uma setor com o identificador {id}.");
        }

        var departamentoExiste =
            await _context.Departamentos
                .AnyAsync(departamento =>
                    departamento.Id == request.DepartamentoId);

        if (!departamentoExiste)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Departamento não encontrada.",
                detail: $"Não existe uma departamento com o identificador {request.DepartamentoId}.");
        }

        var nome = request.Nome.Trim();

        var existe = await _context.Setores
            .AnyAsync(setor =>
                setor.Id != id &&
                setor.DepartamentoId == request.DepartamentoId &&
                setor.Nome.ToLower() == nome.ToLower());

        if (existe)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Setor duplicada.",
                detail: $"Já existe uma setor chamada \"{nome}\" nessa departamento.");
        }

        setor.Nome = nome;
        setor.DepartamentoId = request.DepartamentoId;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Setor alterada com sucesso.",
            setor.Id,
            setor.Nome,
            setor.DepartamentoId
        });
    }

    /// <summary>
    /// Exclui uma setor.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Tags("Admin - Setores")]
    public async Task<IActionResult> Excluir(int id)
    {
        var setor = await _context.Setores
            .FirstOrDefaultAsync(setor =>
                setor.Id == id);

        if (setor == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Setor não encontrada.",
                detail: $"Não existe uma setor com o identificador {id}.");
        }

        // Verifica se existem usuários vinculados.
        var quantidadeUsuarios = await _context.Usuarios
            .CountAsync(usuario => usuario.SetorId == id);

        if (quantidadeUsuarios > 0)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível excluir a setor.",
                detail: $"A setor \"{setor.Nome}\" possui {quantidadeUsuarios} usuário(s) vinculado(s). Transfira esses usuários para outra setor antes de excluí-la.");
        }

        _context.Setores.Remove(setor);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Lista os setores de um departamento.
    /// </summary>
    [HttpGet("/api/departamentos/{departamentoId}/setores")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Tags("Cadastro - Setores")]
    public async Task<IActionResult> ListarPorDepartamento(
        int departamentoId,
        [FromQuery] string? nome = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        var departamentoExiste =
            await _context.Departamentos
                .AnyAsync(departamento =>
                    departamento.Id == departamentoId);

        if (!departamentoExiste)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Departamento não encontrada.",
                detail: $"Não existe uma departamento com o identificador {departamentoId}.");
        }

        var query = _context.Setores
            .AsNoTracking()
            .Where(setor =>
                setor.DepartamentoId == departamentoId);

        if (!string.IsNullOrWhiteSpace(nome))
        {
            var termo = nome.Trim().ToLower();
            query = query.Where(setor => setor.Nome.ToLower().Contains(termo));
        }

        // Sem paginação, preserva o contrato usado nos formulários de cadastro.
        if (!page.HasValue && !pageSize.HasValue)
        {
            var todasAsSetores = await query
                .OrderBy(setor => setor.Nome)
                .Select(setor => new { setor.Id, setor.Nome })
                .ToListAsync();

            return Ok(todasAsSetores);
        }

        var pagina = Math.Max(1, page ?? 1);
        var tamanhoPagina = Math.Clamp(pageSize ?? 8, 1, 100);
        var totalRegistros = await query.CountAsync();
        var setores = await query
            .OrderBy(setor => setor.Nome)
            .Skip((pagina - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(setor => new
            {
                setor.Id,
                setor.Nome,
            })
            .ToListAsync();

        return Ok(new
        {
            pagina,
            tamanhoPagina,
            totalRegistros,
            setores
        });
    }
}
