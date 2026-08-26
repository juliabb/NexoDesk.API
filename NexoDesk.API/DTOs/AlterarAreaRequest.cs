namespace NexoDesk.DTOs;

public class AlterarAreaRequest
{
    /// <summary>
    /// Novo nome da área.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Coordenadoria à qual a área será vinculada.
    /// </summary>
    public int CoordenadoriaId { get; set; }
}
