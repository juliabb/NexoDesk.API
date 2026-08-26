namespace NexoDesk.Models;

public class Area
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public int CoordenadoriaId { get; set; }

    public Coordenadoria Coordenadoria { get; set; } = null!;

    public ICollection<Usuario> Usuarios { get; set; }
        = new List<Usuario>();

    public ICollection<CategoriaChamado> CategoriasResponsaveis { get; set; }
        = new List<CategoriaChamado>();

    public ICollection<Chamado> ChamadosSolicitados { get; set; }
        = new List<Chamado>();

    public ICollection<Chamado> ChamadosResponsaveis { get; set; }
        = new List<Chamado>();
}
