using NexoDesk.Enums;

namespace NexoDesk.Models;

public class HistoricoChamado
{
    public long Id { get; set; }
    public Guid ChamadoId { get; set; }
    public Chamado Chamado { get; set; } = null!;
    public string Acao { get; set; } = string.Empty;
    public StatusChamado? StatusAnterior { get; set; }
    public StatusChamado StatusNovo { get; set; }
    public Guid? ResponsavelAnteriorId { get; set; }
    public Guid? ResponsavelNovoId { get; set; }
    public Guid RealizadoPorUsuarioId { get; set; }
    public Usuario RealizadoPorUsuario { get; set; } = null!;
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
