namespace NexoDesk.Models;

public class Setor
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public int DepartamentoId { get; set; }

    public Departamento Departamento { get; set; } = null!;

    public ICollection<Usuario> Usuarios { get; set; }
        = new List<Usuario>();

    public ICollection<CategoriaChamado> CategoriasResponsaveis { get; set; }
        = new List<CategoriaChamado>();

    public ICollection<Chamado> ChamadosSolicitados { get; set; }
        = new List<Chamado>();

    public ICollection<Chamado> ChamadosResponsaveis { get; set; }
        = new List<Chamado>();
}
