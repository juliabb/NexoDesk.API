namespace NexoDesk.DTOs;

public class AlterarCoordenadoriaRequest
{
    /// <summary>
    /// Novo nome da coordenadoria.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Nova sigla da coordenadoria.
    /// </summary>
    public string Sigla { get; set; } = string.Empty;
}
