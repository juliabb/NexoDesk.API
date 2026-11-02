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
    /// Cadastra um novo setor vinculado a um departamento.
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
                detail: "Informe o nome do setor.");
        }

        var departamento = await _context.Departamentos
            .FirstOrDefaultAsync(departamento =>
                departamento.Id == request.DepartamentoId && departamento.Ativo);

        if (departamento == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Departamento não encontrado.",
                detail: $"Não existe um departamento com o identificador {request.DepartamentoId}.");
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
                title: "Setor duplicado.",
                detail: $"Já existe um setor chamado \"{nome}\" nesse departamento.");
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
    /// Altera um setor e seu departamento.
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
                detail: "Informe o nome do setor.");
        }

        var setor = await _context.Setores
            .FirstOrDefaultAsync(setor =>
                setor.Id == id);

        if (setor == null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Setor não encontrado.",
                detail: $"Não existe um setor com o identificador {id}.");
        }

        var departamentoExiste =
            await _context.Departamentos
                .AnyAsync(departamento =>
                    departamento.Id == request.DepartamentoId && (departamento.Ativo || setor.DepartamentoId == request.DepartamentoId));

        if (!departamentoExiste)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Departamento não encontrado.",
                detail: $"Não existe um departamento com o identificador {request.DepartamentoId}.");
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
                title: "Setor duplicado.",
                detail: $"Já existe um setor chamado \"{nome}\" nesse departamento.");
        }

        if (setor.DepartamentoId != request.DepartamentoId &&
            (await _context.Usuarios.AnyAsync(u => u.SetorId == id) ||
             await _context.Chamados.AnyAsync(c => c.SetorResponsavelId == id || c.SetorSolicitanteId == id)))
            return Conflict(new { mensagem = "Este setor possui usuários ou chamados vinculados. Crie outro setor para preservar os vínculos existentes." });

        setor.Nome = nome;
        setor.DepartamentoId = request.DepartamentoId;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Setor alterado com sucesso.",
            setor.Id,
            setor.Nome,
            setor.DepartamentoId
        });
    }

    /// <summary>
    /// Desativa um setor sem excluir o histórico.
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
                title: "Setor não encontrado.",
                detail: $"Não existe um setor com o identificador {id}.");
        }

        setor.Ativo = false;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> AlterarStatus(int id, [FromBody] AlterarStatusEstruturaRequest request)
    {
        var item = await _context.Setores.FindAsync(id);
        if (item is null) return NotFound(new { mensagem = "Registro não encontrado." });
        if (request.Ativo && !await _context.Departamentos.AnyAsync(d => d.Id == item.DepartamentoId && d.Ativo))
            return Conflict(new { mensagem = "Reative o departamento antes de ativar o setor." });
        item.Ativo = request.Ativo;
        await _context.SaveChangesAsync();
        return Ok(new { item.Id, item.Ativo });
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
        [FromQuery] bool incluirInativos = false,
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
                title: "Departamento não encontrado.",
                detail: $"Não existe um departamento com o identificador {departamentoId}.");
        }

        var query = _context.Setores
            .AsNoTracking()
            .Where(setor =>
                setor.DepartamentoId == departamentoId)
            .Where(s => (s.Ativo && s.Departamento.Ativo) || (incluirInativos && User.IsInRole("Administrador")));

        if (!string.IsNullOrWhiteSpace(nome))
        {
            var termo = nome.Trim().ToLower();
            query = query.Where(setor => setor.Nome.ToLower().Contains(termo));
        }

        // Sem paginação, preserva o contrato usado nos formulários de cadastro.
        if (!page.HasValue && !pageSize.HasValue)
        {
            var todosOsSetores = await query
                .OrderBy(setor => setor.Nome)
                .Select(setor => new { setor.Id, setor.Nome, setor.Ativo, setor.DepartamentoId })
                .ToListAsync();

            return Ok(todosOsSetores);
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
                setor.Ativo,
                setor.DepartamentoId
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
