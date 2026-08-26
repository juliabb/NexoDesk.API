namespace NexoDesk.DTOs;

public class CriarAreaRequest
{
    /// <summary>
    /// Nome da área.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Identificador da Coordenadoria à qual a área pertence.
    /// </summary>
    public int CoordenadoriaId { get; set; }
}