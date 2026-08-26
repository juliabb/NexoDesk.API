namespace NexoDesk.Models;

public class CategoriaChamado
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string? Descricao { get; set; }
    public int SetorResponsavelId { get; set; }
    public Setor SetorResponsavel { get; set; } = null!;
    public bool Ativa { get; set; } = true;
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadaEm { get; set; }
    public ICollection<Chamado> Chamados { get; set; } = new List<Chamado>();
}
