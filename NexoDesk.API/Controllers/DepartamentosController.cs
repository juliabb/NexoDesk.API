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
public class DepartamentosController : ControllerBase
{
    private readonly AppDbContext _context;

    public DepartamentosController(AppDbContext context)

    {
        _context = context;
    }

    /// <summary>
    /// Cadastra um departamento.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Tags("Admin - Departamentos")]
    public async Task<IActionResult> Criar(
        [FromBody] CriarDepartamentoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Sigla))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Dados inválidos.", detail: "Informe o nome e a sigla do departamento.");
        }

        var nome = request.Nome.Trim();
        var sigla = request.Sigla.Trim().ToUpperInvariant();
        if (sigla.Length > 20)
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Dados inválidos.", detail: "A sigla deve ter no máximo 20 caracteres.");

        var existeNome = await _context.Departamentos
            .AnyAsync(departamento => departamento.Nome.ToLower() == nome.ToLower());
        var existeSigla = await _context.Departamentos
            .AnyAsync(departamento => departamento.Sigla == sigla);

        if (existeNome || existeSigla)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Departamento duplicado.", detail: existeNome
                    ? $"Já existe um departamento chamado \"{nome}\"."
                    : $"Já existe um departamento com a sigla \"{sigla}\".");
        }

        var departamento = new Departamento
        {
            Nome = nome,
            Sigla = sigla
        };

        _context.Departamentos.Add(departamento);

        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(Listar),
            new { id = departamento.Id },
            new
            {
                departamento.Id,
                departamento.Nome,
                departamento.Sigla
            });
    }

    /// <summary>
    /// Altera um departamento.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Tags("Admin - Departamentos")]
    public async Task<IActionResult> Alterar(
        int id,
        [FromBody] AlterarDepartamentoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nome) || string.IsNullOrWhiteSpace(request.Sigla))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Dados inválidos.", detail: "Informe o nome e a sigla do departamento.");
        }

        var departamento = await _context.Departamentos
            .FirstOrDefaultAsync(departamento =>
                departamento.Id == id);

        if (departamento == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Departamento não encontrado.", detail: $"Não existe um departamento com o identificador {id}.");
        }

        var nome = request.Nome.Trim();
        var sigla = request.Sigla.Trim().ToUpperInvariant();
        if (sigla.Length > 20)
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Dados inválidos.", detail: "A sigla deve ter no máximo 20 caracteres.");

        var existeNome = await _context.Departamentos
            .AnyAsync(departamento => departamento.Id != id && departamento.Nome.ToLower() == nome.ToLower());
        var existeSigla = await _context.Departamentos
            .AnyAsync(departamento => departamento.Id != id && departamento.Sigla == sigla);

        if (existeNome || existeSigla)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Departamento duplicado.", detail: existeNome
                    ? $"Já existe um departamento chamado \"{nome}\"."
                    : $"Já existe um departamento com a sigla \"{sigla}\".");
        }

        departamento.Nome = nome;
        departamento.Sigla = sigla;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Departamento alterado com sucesso.",
            departamento.Id,
            departamento.Nome,
            departamento.Sigla
        });
    }

    /// <summary>
    /// Exclui um departamento.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Tags("Admin - Departamentos")]
    public async Task<IActionResult> Excluir(int id)
    {
        var departamento = await _context.Departamentos
            .FirstOrDefaultAsync(departamento =>
                departamento.Id == id);

        if (departamento == null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Departamento não encontrado.", detail: $"Não existe um departamento com o identificador {id}.");
        }

        // Verifica se existem setores vinculados.
        var quantidadeSetores = await _context.Setores
            .CountAsync(setor => setor.DepartamentoId == id);

        if (quantidadeSetores > 0)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível excluir o departamento.",
                detail: $"O departamento \"{departamento.Nome}\" possui {quantidadeSetores} setor(es) vinculado(s). Exclua ou transfira esses setores antes de excluir o departamento.");
        }

        // Verifica se existem usuários vinculados.
        var quantidadeUsuarios = await _context.Usuarios
            .CountAsync(usuario => usuario.DepartamentoId == id);

        if (quantidadeUsuarios > 0)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Não foi possível excluir o departamento.",
                detail: $"O departamento \"{departamento.Nome}\" possui {quantidadeUsuarios} usuário(s) vinculado(s). Transfira esses usuários antes de excluir o departamento.");
        }

        _context.Departamentos.Remove(departamento);

        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Lista todos os departamentos.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Tags("Cadastro - Departamentos")]
    public async Task<IActionResult> Listar()
    {
        var departamentos = await _context.Departamentos
            .AsNoTracking()
            .OrderBy(departamento => departamento.Nome)
            .Select(departamento => new
            {
                departamento.Id,
                departamento.Nome,
                departamento.Sigla
            })
            .ToListAsync();

        return Ok(departamentos);
    }
}
