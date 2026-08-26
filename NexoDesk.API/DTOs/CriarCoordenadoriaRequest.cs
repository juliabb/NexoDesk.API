namespace NexoDesk.DTOs;

public class CriarCoordenadoriaRequest
{
    /// <summary>
    /// Nome da coordenadoria.
    /// </summary>

    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Sigla da coordenadoria.
    /// </summary>
    public string Sigla { get; set; } = string.Empty;
}
