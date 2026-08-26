namespace NexoDesk.Models;

public class Coordenadoria
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    public string Sigla { get; set; } = string.Empty;

    public ICollection<Area> Areas { get; set; }
        = new List<Area>();

    public ICollection<Usuario> Usuarios { get; set; }
        = new List<Usuario>();
}
