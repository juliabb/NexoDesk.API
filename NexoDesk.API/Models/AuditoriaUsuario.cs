namespace NexoDesk.Models;

public class AuditoriaUsuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioAlvoId { get; set; }
    public Guid? RealizadoPorUsuarioId { get; set; }
    public string Acao { get; set; } = string.Empty;
    public string Alteracoes { get; set; } = string.Empty;
    public string? IpOrigem { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
