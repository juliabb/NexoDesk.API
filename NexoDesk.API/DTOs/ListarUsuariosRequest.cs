using NexoDesk.Enums;

namespace NexoDesk.DTOs;

public class ListarUsuariosRequest
{
    // Número da página.
    public int Page { get; set; } = 1;

    // Quantidade de usuários por página.
    public int PageSize { get; set; } = 10;

    // Pesquisa pelo nome completo.
    public string? Nome { get; set; }

    // Filtro pela departamento.
    public int? DepartamentoId { get; set; }

    // Filtro pela setor.
    public int? SetorId { get; set; }

    // Filtro pelo perfil do usuário.
    public PerfilUsuario? Perfil { get; set; }

    // Filtro pelo status do usuário.
    public StatusUsuario? Status { get; set; }
}
