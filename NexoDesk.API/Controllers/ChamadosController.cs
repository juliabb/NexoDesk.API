using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NexoDesk.Data;
using NexoDesk.DTOs;
using NexoDesk.Enums;
using NexoDesk.Helpers;
using NexoDesk.Models;

namespace NexoDesk.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChamadosController : ControllerBase
{
    private const long TamanhoMaximoAnexo = 10 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string[]> TiposAnexoPermitidos =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".png"] = ["image/png"],
            [".jpg"] = ["image/jpeg"],
            [".jpeg"] = ["image/jpeg"],
            [".txt"] = ["text/plain"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            [".xlsx"] = ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]
        };

    private readonly AppDbContext _context;
    private readonly INotificacaoService _notificacoes;
    private readonly IAnexoStorageService _anexoStorage;

    public ChamadosController(
        AppDbContext context,
        INotificacaoService notificacoes,
        IAnexoStorageService anexoStorage)
    {
        _context = context;
        _notificacoes = notificacoes;
        _anexoStorage = anexoStorage;
    }

    /// <summary>Abre um chamado para uma categoria ativa.</summary>
    [HttpPost]
    [Tags("Chamados")]
    public async Task<IActionResult> Criar([FromBody] CriarChamadoRequest request)
    {
        if (!TryObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        if (!Enum.IsDefined(request.Prioridade))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Prioridade inválida.", detail: "Informe uma prioridade válida para o chamado.");
        }

        var usuario = await _context.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == usuarioId);

        if (usuario is null)
        {
            return Unauthorized();
        }

        var categoria = await _context.CategoriasChamados.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == request.CategoriaId);

        if (categoria is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Categoria não encontrada.",
                detail: $"Não existe uma categoria com o identificador {request.CategoriaId}.");
        }

        if (!categoria.Ativa)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Categoria indisponível.",
                detail: "Essa categoria está inativa e não aceita novos chamados.");
        }

        var chamado = new Chamado
        {
            Protocolo = GerarProtocolo(),
            Titulo = request.Titulo.Trim(),
            Descricao = request.Descricao.Trim(),
            Prioridade = request.Prioridade,
            SolicitanteId = usuario.Id,
            CategoriaId = categoria.Id,
            SetorSolicitanteId = usuario.SetorId,
            SetorResponsavelId = categoria.SetorResponsavelId
        };

        _context.Chamados.Add(chamado);
        _context.HistoricosChamados.Add(new HistoricoChamado
        {
            ChamadoId = chamado.Id,
            Acao = "Chamado aberto",
            StatusNovo = chamado.Status,
            RealizadoPorUsuarioId = usuario.Id
        });
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorId), new { id = chamado.Id }, new
        {
            chamado.Id,
            chamado.Protocolo,
            chamado.Titulo,
            chamado.Prioridade,
            chamado.Status,
            chamado.CriadoEm
        });
    }

    /// <summary>Lista os chamados do usuário autenticado.</summary>
    [HttpGet("me")]
    [Tags("Chamados")]
    public async Task<IActionResult> ListarMeus(
        [FromQuery] StatusChamado? status,
        [FromQuery] PrioridadeChamado? prioridade,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!TryObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        if (status.HasValue && !Enum.IsDefined(status.Value) ||
            prioridade.HasValue && !Enum.IsDefined(prioridade.Value))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Filtro inválido.", detail: "Informe valores válidos para status e prioridade.");
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.Chamados.AsNoTracking()
            .Where(chamado => chamado.SolicitanteId == usuarioId);

        if (status.HasValue)
        {
            query = query.Where(chamado => chamado.Status == status.Value);
        }

        if (prioridade.HasValue)
        {
            query = query.Where(chamado => chamado.Prioridade == prioridade.Value);
        }

        var totalRegistros = await query.CountAsync();
        var chamados = await query
            .OrderByDescending(chamado => chamado.CriadoEm)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(chamado => new
            {
                chamado.Id,
                chamado.Protocolo,
                chamado.Titulo,
                chamado.Prioridade,
                chamado.Status,
                categoria = chamado.Categoria.Nome,
                setorResponsavel = chamado.SetorResponsavel.Nome,
                chamado.CriadoEm,
                chamado.AtualizadoEm
            })
            .ToListAsync();

        return Ok(new { pagina = page, tamanhoPagina = pageSize, totalRegistros, chamados });
    }

    /// <summary>Obtém os detalhes de um chamado do usuário autenticado.</summary>
    [HttpGet("{id:guid}")]
    [Tags("Chamados")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        if (!TryObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var podeConsultarTodos = User.IsInRole("Administrador");
        int? departamentoDoGestor = null;
        int? setorDoAtendente = null;
        if (User.IsInRole("Gestor"))
        {
            departamentoDoGestor = await _context.Usuarios.AsNoTracking()
                .Where(usuario => usuario.Id == usuarioId)
                .Select(usuario => (int?)usuario.DepartamentoId)
                .FirstOrDefaultAsync();
        }
        else if (User.IsInRole("Atendente"))
        {
            setorDoAtendente = await _context.Usuarios.AsNoTracking()
                .Where(usuario => usuario.Id == usuarioId)
                .Select(usuario => (int?)usuario.SetorId)
                .FirstOrDefaultAsync();
        }

        var chamado = await _context.Chamados.AsNoTracking()
            .Where(item => item.Id == id)
            .Where(item => item.SolicitanteId == usuarioId ||
                podeConsultarTodos ||
                departamentoDoGestor.HasValue &&
                item.SetorResponsavel.DepartamentoId == departamentoDoGestor.Value ||
                setorDoAtendente.HasValue && item.SetorResponsavelId == setorDoAtendente.Value)
            .Select(item => new
            {
                item.Id,
                item.Protocolo,
                item.Titulo,
                item.Descricao,
                item.Prioridade,
                item.Status,
                categoria = new { item.Categoria.Id, item.Categoria.Nome },
                solicitante = new { item.Solicitante.Id, item.Solicitante.NomeCompleto },
                setorSolicitante = new { item.SetorSolicitante.Id, item.SetorSolicitante.Nome },
                setorResponsavel = new { item.SetorResponsavel.Id, item.SetorResponsavel.Nome },
                responsavel = item.Responsavel == null
                    ? null
                    : new { item.Responsavel.Id, item.Responsavel.NomeCompleto },
                item.CriadoEm,
                item.AtualizadoEm,
                item.ResolvidoEm,
                item.FechadoEm
            })
            .FirstOrDefaultAsync();

        if (chamado is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Chamado não encontrado.",
                detail: "O chamado não existe ou você não possui acesso a ele.");
        }

        return Ok(chamado);
    }

    /// <summary>Lista a fila de atendimento da departamento do gestor.</summary>
    [HttpGet("fila")]
    [Authorize(Roles = "Atendente,Gestor,Administrador")]
    [Tags("Triagem de chamados")]
    public async Task<IActionResult> ListarFila(
        [FromQuery] StatusChamado? status,
        [FromQuery] PrioridadeChamado? prioridade,
        [FromQuery] int? setorResponsavelId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (!TryObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        if (status.HasValue && !Enum.IsDefined(status.Value) ||
            prioridade.HasValue && !Enum.IsDefined(prioridade.Value))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Filtro inválido.", detail: "Informe valores válidos para status e prioridade.");
        }

        var operador = await _context.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(usuario => usuario.Id == usuarioId);
        if (operador is null)
        {
            return Unauthorized();
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _context.Chamados.AsNoTracking().AsQueryable();

        if (User.IsInRole("Gestor"))
        {
            query = query.Where(chamado =>
                chamado.SetorResponsavel.DepartamentoId == operador.DepartamentoId);
        }
        else if (User.IsInRole("Atendente"))
        {
            query = query.Where(chamado => chamado.SetorResponsavelId == operador.SetorId);
        }

        if (setorResponsavelId.HasValue)
        {
            query = query.Where(chamado => chamado.SetorResponsavelId == setorResponsavelId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(chamado => chamado.Status == status.Value);
        }

        if (prioridade.HasValue)
        {
            query = query.Where(chamado => chamado.Prioridade == prioridade.Value);
        }

        var totalRegistros = await query.CountAsync();
        var chamados = await query
            .OrderByDescending(chamado => chamado.Prioridade)
            .ThenBy(chamado => chamado.CriadoEm)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(chamado => new
            {
                chamado.Id,
                chamado.Protocolo,
                chamado.Titulo,
                chamado.Prioridade,
                chamado.Status,
                categoria = chamado.Categoria.Nome,
                solicitante = chamado.Solicitante.NomeCompleto,
                setorSolicitante = chamado.SetorSolicitante.Nome,
                setorResponsavel = chamado.SetorResponsavel.Nome,
                responsavel = chamado.Responsavel == null ? null : chamado.Responsavel.NomeCompleto,
                chamado.CriadoEm,
                chamado.AtualizadoEm
            })
            .ToListAsync();

        return Ok(new { pagina = page, tamanhoPagina = pageSize, totalRegistros, chamados });
    }

    /// <summary>Lista os atendentes disponíveis em uma setor responsável.</summary>
    [HttpGet("atendentes")]
    [Authorize(Roles = "Gestor,Administrador")]
    [Tags("Triagem de chamados")]
    public async Task<IActionResult> ListarAtendentes([FromQuery] int setorResponsavelId)
    {
        if (!TryObterUsuarioId(out var operadorId))
        {
            return Unauthorized();
        }

        var setor = await _context.Setores.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == setorResponsavelId);

        if (setor is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Setor não encontrada.",
                detail: $"Não existe uma setor com o identificador {setorResponsavelId}.");
        }

        if (!User.IsInRole("Administrador"))
        {
            var departamentoId = await _context.Usuarios.AsNoTracking()
                .Where(usuario => usuario.Id == operadorId)
                .Select(usuario => (int?)usuario.DepartamentoId)
                .FirstOrDefaultAsync();

            if (departamentoId != setor.DepartamentoId)
            {
                return Problem(statusCode: StatusCodes.Status404NotFound,
                    title: "Setor não encontrada.",
                    detail: "A setor não existe ou não pertence à sua departamento.");
            }
        }

        var atendentes = await _context.Usuarios.AsNoTracking()
            .Where(usuario =>
                usuario.SetorId == setorResponsavelId &&
                usuario.Perfil == PerfilUsuario.Atendente &&
                usuario.Status == StatusUsuario.Aprovado &&
                usuario.Ativo)
            .OrderBy(usuario => usuario.NomeCompleto)
            .Select(usuario => new
            {
                usuario.Id,
                usuario.NomeCompleto,
                usuario.EmailCorporativo
            })
            .ToListAsync();

        return Ok(atendentes);
    }

    /// <summary>Atribui um responsável da setor de atendimento ao chamado.</summary>
    [HttpPatch("{id:guid}/responsavel")]
    [Authorize(Roles = "Gestor,Administrador")]
    [Tags("Triagem de chamados")]
    public async Task<IActionResult> AtribuirResponsavel(Guid id, [FromBody] AtribuirChamadoRequest request)
    {
        if (!TryObterUsuarioId(out var operadorId))
        {
            return Unauthorized();
        }

        var chamado = await _context.Chamados
            .Include(item => item.SetorResponsavel)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (chamado is null || !await PodeGerenciarAsync(chamado, operadorId))
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Chamado não encontrado.",
                detail: "O chamado não existe ou você não possui acesso à sua fila de atendimento.");
        }

        if (chamado.Status is StatusChamado.Fechado or StatusChamado.Cancelado)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Chamado encerrado.", detail: "Não é possível atribuir um chamado fechado ou cancelado.");
        }

        var responsavel = await _context.Usuarios.AsNoTracking().FirstOrDefaultAsync(usuario =>
            usuario.Id == request.ResponsavelId &&
            usuario.SetorId == chamado.SetorResponsavelId &&
            usuario.Ativo &&
            usuario.Status == StatusUsuario.Aprovado &&
            usuario.Perfil == PerfilUsuario.Atendente);

        if (responsavel is null)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Responsável inválido.",
                detail: "O responsável deve ser um atendente ativo e aprovado da setor responsável pelo chamado.");
        }

        if (chamado.ResponsavelId == responsavel.Id)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Responsável já atribuído.", detail: "Esse usuário já é o responsável pelo chamado.");
        }

        var responsavelAnteriorId = chamado.ResponsavelId;
        var statusAnterior = chamado.Status;
        chamado.ResponsavelId = responsavel.Id;
        if (chamado.Status == StatusChamado.Aberto)
        {
            chamado.Status = StatusChamado.EmTriagem;
        }
        chamado.AtualizadoEm = DateTime.UtcNow;

        _context.HistoricosChamados.Add(new HistoricoChamado
        {
            ChamadoId = chamado.Id,
            Acao = "Responsável atribuído",
            StatusAnterior = statusAnterior,
            StatusNovo = chamado.Status,
            ResponsavelAnteriorId = responsavelAnteriorId,
            ResponsavelNovoId = responsavel.Id,
            RealizadoPorUsuarioId = operadorId,
            Observacao = NormalizarObservacao(request.Observacao)
        });
        _notificacoes.Adicionar(
            responsavel.Id,
            chamado,
            TipoNotificacao.ChamadoAtribuido,
            "Novo chamado atribuído",
            $"O chamado {chamado.Protocolo} foi atribuído a você.",
            operadorId);

        await _context.SaveChangesAsync();
        return Ok(new
        {
            mensagem = "Responsável atribuído com sucesso.",
            chamado.Id,
            chamado.Protocolo,
            chamado.Status,
            responsavel = new { responsavel.Id, responsavel.NomeCompleto }
        });
    }

    /// <summary>Altera o status respeitando o fluxo de atendimento.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Atendente,Gestor,Administrador")]
    [Tags("Triagem de chamados")]
    public async Task<IActionResult> AlterarStatus(Guid id, [FromBody] AlterarStatusChamadoRequest request)
    {
        if (!TryObterUsuarioId(out var operadorId))
        {
            return Unauthorized();
        }

        if (!Enum.IsDefined(request.Status))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Status inválido.", detail: "Informe um status válido para o chamado.");
        }

        var chamado = await _context.Chamados
            .Include(item => item.SetorResponsavel)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (chamado is null || !await PodeGerenciarAsync(chamado, operadorId))
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Chamado não encontrado.",
                detail: "O chamado não existe ou você não possui acesso à sua fila de atendimento.");
        }

        if (!TransicaoPermitida(chamado.Status, request.Status))
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Transição de status inválida.",
                detail: $"Não é permitido alterar o chamado de {chamado.Status} para {request.Status}.");
        }

        if (request.Status is StatusChamado.EmAtendimento or StatusChamado.Resolvido &&
            !chamado.ResponsavelId.HasValue)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Chamado sem responsável.",
                detail: "Atribua um responsável antes de iniciar ou resolver o atendimento.");
        }

        var statusAnterior = chamado.Status;
        chamado.Status = request.Status;
        chamado.AtualizadoEm = DateTime.UtcNow;

        if (request.Status == StatusChamado.Resolvido)
        {
            chamado.ResolvidoEm = DateTime.UtcNow;
        }
        else if (statusAnterior == StatusChamado.Resolvido)
        {
            chamado.ResolvidoEm = null;
        }

        if (request.Status == StatusChamado.Fechado)
        {
            chamado.FechadoEm = DateTime.UtcNow;
        }

        _context.HistoricosChamados.Add(new HistoricoChamado
        {
            ChamadoId = chamado.Id,
            Acao = "Status alterado",
            StatusAnterior = statusAnterior,
            StatusNovo = chamado.Status,
            ResponsavelAnteriorId = chamado.ResponsavelId,
            ResponsavelNovoId = chamado.ResponsavelId,
            RealizadoPorUsuarioId = operadorId,
            Observacao = NormalizarObservacao(request.Observacao)
        });
        _notificacoes.Adicionar(
            chamado.SolicitanteId,
            chamado,
            TipoNotificacao.StatusAlterado,
            "Status do chamado atualizado",
            $"O chamado {chamado.Protocolo} agora está com status {chamado.Status}.",
            operadorId);

        await _context.SaveChangesAsync();
        return Ok(new
        {
            mensagem = "Status alterado com sucesso.",
            chamado.Id,
            chamado.Protocolo,
            chamado.Status,
            chamado.AtualizadoEm
        });
    }

    /// <summary>Lista o histórico de atividades de um chamado.</summary>
    [HttpGet("{id:guid}/historico")]
    [Tags("Chamados")]
    public async Task<IActionResult> ListarHistorico(Guid id)
    {
        if (!TryObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var chamado = await _context.Chamados.AsNoTracking()
            .Include(item => item.SetorResponsavel)
            .FirstOrDefaultAsync(item => item.Id == id);

        var podeConsultar = chamado is not null &&
            (chamado.SolicitanteId == usuarioId || await PodeGerenciarAsync(chamado, usuarioId));

        if (!podeConsultar)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Chamado não encontrado.",
                detail: "O chamado não existe ou você não possui acesso ao seu histórico.");
        }

        var historico = await _context.HistoricosChamados.AsNoTracking()
            .Where(item => item.ChamadoId == id)
            .OrderBy(item => item.CriadoEm)
            .Select(item => new
            {
                item.Id,
                item.Acao,
                item.StatusAnterior,
                item.StatusNovo,
                item.ResponsavelAnteriorId,
                item.ResponsavelNovoId,
                realizadoPor = new
                {
                    item.RealizadoPorUsuario.Id,
                    item.RealizadoPorUsuario.NomeCompleto
                },
                item.Observacao,
                item.CriadoEm
            })
            .ToListAsync();

        return Ok(new { chamado!.Id, chamado.Protocolo, historico });
    }

    /// <summary>Adiciona um comentário público ou interno ao chamado.</summary>
    [HttpPost("{id:guid}/comentarios")]
    [Tags("Chamados - Comentários")]
    public async Task<IActionResult> AdicionarComentario(
        Guid id,
        [FromBody] CriarComentarioChamadoRequest request)
    {
        if (!TryObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var chamado = await _context.Chamados
            .Include(item => item.SetorResponsavel)
            .FirstOrDefaultAsync(item => item.Id == id);

        var fazParteDaEquipe = chamado is not null && await PodeGerenciarAsync(chamado, usuarioId);
        var ehSolicitante = chamado?.SolicitanteId == usuarioId;

        if (chamado is null || !ehSolicitante && !fazParteDaEquipe)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Chamado não encontrado.",
                detail: "O chamado não existe ou você não possui acesso aos seus comentários.");
        }

        if (chamado.Status is StatusChamado.Fechado or StatusChamado.Cancelado)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Comentários encerrados.",
                detail: "Não é possível comentar em um chamado fechado ou cancelado.");
        }

        if (request.Interno && !fazParteDaEquipe)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden,
                title: "Comentário interno não permitido.",
                detail: "Somente a equipe de atendimento pode publicar comentários internos.");
        }

        var comentario = new ComentarioChamado
        {
            ChamadoId = chamado.Id,
            AutorId = usuarioId,
            Conteudo = request.Conteudo.Trim(),
            Interno = request.Interno
        };

        _context.ComentariosChamados.Add(comentario);
        if (!request.Interno)
        {
            if (ehSolicitante && chamado.ResponsavelId.HasValue)
            {
                _notificacoes.Adicionar(
                    chamado.ResponsavelId.Value,
                    chamado,
                    TipoNotificacao.NovoComentario,
                    "Novo comentário no chamado",
                    $"O solicitante comentou no chamado {chamado.Protocolo}.",
                    usuarioId);
            }
            else if (fazParteDaEquipe)
            {
                _notificacoes.Adicionar(
                    chamado.SolicitanteId,
                    chamado,
                    TipoNotificacao.NovoComentario,
                    "Nova resposta no chamado",
                    $"A equipe respondeu ao chamado {chamado.Protocolo}.",
                    usuarioId);
            }
        }
        chamado.AtualizadoEm = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Created($"/api/chamados/{chamado.Id}/comentarios", new
        {
            comentario.Id,
            comentario.Conteudo,
            comentario.Interno,
            comentario.CriadoEm
        });
    }

    /// <summary>Lista os comentários visíveis para o usuário autenticado.</summary>
    [HttpGet("{id:guid}/comentarios")]
    [Tags("Chamados - Comentários")]
    public async Task<IActionResult> ListarComentarios(Guid id)
    {
        if (!TryObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var chamado = await _context.Chamados.AsNoTracking()
            .Include(item => item.SetorResponsavel)
            .FirstOrDefaultAsync(item => item.Id == id);

        var fazParteDaEquipe = chamado is not null && await PodeGerenciarAsync(chamado, usuarioId);
        var ehSolicitante = chamado?.SolicitanteId == usuarioId;

        if (chamado is null || !ehSolicitante && !fazParteDaEquipe)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Chamado não encontrado.",
                detail: "O chamado não existe ou você não possui acesso aos seus comentários.");
        }

        var query = _context.ComentariosChamados.AsNoTracking()
            .Where(comentario => comentario.ChamadoId == id);

        if (!fazParteDaEquipe)
        {
            query = query.Where(comentario => !comentario.Interno);
        }

        var comentarios = await query
            .OrderBy(comentario => comentario.CriadoEm)
            .Select(comentario => new
            {
                comentario.Id,
                autor = new { comentario.Autor.Id, comentario.Autor.NomeCompleto },
                comentario.Conteudo,
                comentario.Interno,
                comentario.CriadoEm
            })
            .ToListAsync();

        return Ok(comentarios);
    }

    /// <summary>Envia um anexo público ou interno para o chamado.</summary>
    [HttpPost("{id:guid}/anexos")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(TamanhoMaximoAnexo)]
    [Tags("Chamados - Anexos")]
    public async Task<IActionResult> AdicionarAnexo(
        Guid id,
        [FromForm] AdicionarAnexoChamadoRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var chamado = await _context.Chamados
            .Include(item => item.SetorResponsavel)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        var fazParteDaEquipe = chamado is not null && await PodeGerenciarAsync(chamado, usuarioId);
        var ehSolicitante = chamado?.SolicitanteId == usuarioId;

        if (chamado is null || !ehSolicitante && !fazParteDaEquipe)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Chamado não encontrado.",
                detail: "O chamado não existe ou você não possui acesso aos seus anexos.");
        }

        if (chamado.Status is StatusChamado.Fechado or StatusChamado.Cancelado)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Anexos encerrados.",
                detail: "Não é possível anexar arquivos a um chamado fechado ou cancelado.");
        }

        if (request.Interno && !fazParteDaEquipe)
        {
            return Problem(statusCode: StatusCodes.Status403Forbidden,
                title: "Anexo interno não permitido.",
                detail: "Somente a equipe de atendimento pode enviar anexos internos.");
        }

        var arquivo = request.Arquivo;
        var nomeOriginal = Path.GetFileName(arquivo.FileName);
        var extension = Path.GetExtension(nomeOriginal);
        var tipoValido = TiposAnexoPermitidos.TryGetValue(extension, out var tiposConteudo) &&
            tiposConteudo.Contains(arquivo.ContentType, StringComparer.OrdinalIgnoreCase);

        if (arquivo.Length <= 0 || arquivo.Length > TamanhoMaximoAnexo)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Tamanho de arquivo inválido.",
                detail: "O anexo deve possuir conteúdo e ter no máximo 10 MB.");
        }

        if (string.IsNullOrWhiteSpace(nomeOriginal) || nomeOriginal.Length > 255 || !tipoValido)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                title: "Formato de arquivo inválido.",
                detail: "São aceitos arquivos PDF, PNG, JPG, TXT, DOCX e XLSX.");
        }

        var nomeArmazenado = await _anexoStorage.SalvarAsync(chamado.Id, arquivo, cancellationToken);
        try
        {
            var anexo = new AnexoChamado
            {
                ChamadoId = chamado.Id,
                EnviadoPorUsuarioId = usuarioId,
                NomeOriginal = nomeOriginal,
                NomeArmazenado = nomeArmazenado,
                TipoConteudo = arquivo.ContentType,
                TamanhoBytes = arquivo.Length,
                Interno = request.Interno
            };

            _context.AnexosChamados.Add(anexo);
            if (!request.Interno)
            {
                if (ehSolicitante && chamado.ResponsavelId.HasValue)
                {
                    _notificacoes.Adicionar(chamado.ResponsavelId.Value, chamado,
                        TipoNotificacao.NovoAnexo, "Novo anexo no chamado",
                        $"O solicitante anexou um arquivo ao chamado {chamado.Protocolo}.", usuarioId);
                }
                else if (fazParteDaEquipe)
                {
                    _notificacoes.Adicionar(chamado.SolicitanteId, chamado,
                        TipoNotificacao.NovoAnexo, "Novo anexo no chamado",
                        $"A equipe anexou um arquivo ao chamado {chamado.Protocolo}.", usuarioId);
                }
            }

            chamado.AtualizadoEm = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            return Created($"/api/chamados/{chamado.Id}/anexos/{anexo.Id}/download", new
            {
                anexo.Id,
                anexo.NomeOriginal,
                anexo.TipoConteudo,
                anexo.TamanhoBytes,
                anexo.Interno,
                anexo.CriadoEm
            });
        }
        catch
        {
            await _anexoStorage.ExcluirAsync(chamado.Id, nomeArmazenado);
            throw;
        }
    }

    /// <summary>Lista os anexos visíveis para o usuário autenticado.</summary>
    [HttpGet("{id:guid}/anexos")]
    [Tags("Chamados - Anexos")]
    public async Task<IActionResult> ListarAnexos(Guid id)
    {
        if (!TryObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var chamado = await _context.Chamados.AsNoTracking()
            .Include(item => item.SetorResponsavel)
            .FirstOrDefaultAsync(item => item.Id == id);
        var fazParteDaEquipe = chamado is not null && await PodeGerenciarAsync(chamado, usuarioId);
        var ehSolicitante = chamado?.SolicitanteId == usuarioId;

        if (chamado is null || !ehSolicitante && !fazParteDaEquipe)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Chamado não encontrado.",
                detail: "O chamado não existe ou você não possui acesso aos seus anexos.");
        }

        var query = _context.AnexosChamados.AsNoTracking()
            .Where(anexo => anexo.ChamadoId == id);
        if (!fazParteDaEquipe)
        {
            query = query.Where(anexo => !anexo.Interno);
        }

        var anexos = await query.OrderBy(anexo => anexo.CriadoEm)
            .Select(anexo => new
            {
                anexo.Id,
                anexo.NomeOriginal,
                anexo.TipoConteudo,
                anexo.TamanhoBytes,
                anexo.Interno,
                enviadoPor = new { anexo.EnviadoPorUsuario.Id, anexo.EnviadoPorUsuario.NomeCompleto },
                anexo.CriadoEm,
                downloadUrl = $"/api/chamados/{id}/anexos/{anexo.Id}/download"
            })
            .ToListAsync();

        return Ok(anexos);
    }

    /// <summary>Baixa um anexo visível para o usuário autenticado.</summary>
    [HttpGet("{id:guid}/anexos/{anexoId:guid}/download")]
    [Tags("Chamados - Anexos")]
    public async Task<IActionResult> BaixarAnexo(
        Guid id,
        Guid anexoId,
        CancellationToken cancellationToken)
    {
        if (!TryObterUsuarioId(out var usuarioId))
        {
            return Unauthorized();
        }

        var chamado = await _context.Chamados.AsNoTracking()
            .Include(item => item.SetorResponsavel)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        var fazParteDaEquipe = chamado is not null && await PodeGerenciarAsync(chamado, usuarioId);
        var ehSolicitante = chamado?.SolicitanteId == usuarioId;

        if (chamado is null || !ehSolicitante && !fazParteDaEquipe)
        {
            return NotFound();
        }

        var anexo = await _context.AnexosChamados.AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == anexoId && item.ChamadoId == id && (!item.Interno || fazParteDaEquipe),
                cancellationToken);
        if (anexo is null)
        {
            return NotFound();
        }

        var stream = await _anexoStorage.AbrirLeituraAsync(
            chamado.Id, anexo.NomeArmazenado, cancellationToken);
        if (stream is null)
        {
            return Problem(statusCode: StatusCodes.Status410Gone,
                title: "Arquivo indisponível.",
                detail: "Os metadados existem, mas o arquivo não está disponível no armazenamento.");
        }

        return File(stream, anexo.TipoConteudo, anexo.NomeOriginal, enableRangeProcessing: true);
    }

    private bool TryObterUsuarioId(out Guid usuarioId) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out usuarioId);

    private async Task<bool> PodeGerenciarAsync(Chamado chamado, Guid usuarioId)
    {
        if (User.IsInRole("Administrador"))
        {
            return true;
        }

        if (!User.IsInRole("Gestor"))
        {
            if (!User.IsInRole("Atendente"))
            {
                return false;
            }

            var setorId = await _context.Usuarios.AsNoTracking()
                .Where(usuario => usuario.Id == usuarioId)
                .Select(usuario => (int?)usuario.SetorId)
                .FirstOrDefaultAsync();

            return setorId.HasValue && chamado.SetorResponsavelId == setorId.Value;
        }

        var departamentoId = await _context.Usuarios.AsNoTracking()
            .Where(usuario => usuario.Id == usuarioId)
            .Select(usuario => (int?)usuario.DepartamentoId)
            .FirstOrDefaultAsync();

        return departamentoId.HasValue &&
            chamado.SetorResponsavel.DepartamentoId == departamentoId.Value;
    }

    private static bool TransicaoPermitida(StatusChamado atual, StatusChamado novo) =>
        atual switch
        {
            StatusChamado.Aberto => novo is StatusChamado.EmTriagem or StatusChamado.Cancelado,
            StatusChamado.EmTriagem => novo is StatusChamado.EmAtendimento or StatusChamado.Cancelado,
            StatusChamado.EmAtendimento => novo is StatusChamado.AguardandoSolicitante
                or StatusChamado.Resolvido or StatusChamado.Cancelado,
            StatusChamado.AguardandoSolicitante => novo is StatusChamado.EmAtendimento
                or StatusChamado.Resolvido or StatusChamado.Cancelado,
            StatusChamado.Resolvido => novo is StatusChamado.Fechado or StatusChamado.EmAtendimento,
            _ => false
        };

    private static string? NormalizarObservacao(string? observacao) =>
        string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();

    private static string GerarProtocolo() =>
        $"NXD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant();
}
