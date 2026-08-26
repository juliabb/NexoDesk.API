namespace NexoDesk.Models;

public class AnexoChamado
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChamadoId { get; set; }
    public Chamado Chamado { get; set; } = null!;
    public Guid EnviadoPorUsuarioId { get; set; }
    public Usuario EnviadoPorUsuario { get; set; } = null!;
    public string NomeOriginal { get; set; } = string.Empty;
    public string NomeArmazenado { get; set; } = string.Empty;
    public string TipoConteudo { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }
    public bool Interno { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
