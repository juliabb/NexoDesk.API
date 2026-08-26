namespace NexoDesk.DTOs;

public class ListarColaboradoresRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    public string? Nome { get; set; }

    public int? DepartamentoId { get; set; }
    public int? SetorId { get; set; }
    public NexoDesk.Enums.PerfilUsuario? Perfil { get; set; }
}
