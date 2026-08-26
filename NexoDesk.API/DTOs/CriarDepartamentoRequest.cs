namespace NexoDesk.DTOs;

public class CriarDepartamentoRequest
{
    /// <summary>
    /// Nome da departamento.
    /// </summary>

    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Sigla da departamento.
    /// </summary>
    public string Sigla { get; set; } = string.Empty;
}
