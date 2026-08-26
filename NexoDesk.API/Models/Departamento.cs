namespace NexoDesk.Models;

public class Departamento
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Sigla { get; set; } = string.Empty;

    public ICollection<Setor> Setores { get; set; }
        = new List<Setor>();

    public ICollection<Usuario> Usuarios { get; set; }
        = new List<Usuario>();
}
