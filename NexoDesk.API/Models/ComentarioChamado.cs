namespace NexoDesk.Models;

public class ComentarioChamado
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChamadoId { get; set; }
    public Chamado Chamado { get; set; } = null!;
    public Guid AutorId { get; set; }
    public Usuario Autor { get; set; } = null!;
    public string Conteudo { get; set; } = string.Empty;
    public bool Interno { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
