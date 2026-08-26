namespace NexoDesk.DTOs;

public class CriarDepartamentoRequest
{
    /// <summary>
    /// Nome do departamento.
    /// </summary>

    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Sigla do departamento.
    /// </summary>
    public string Sigla { get; set; } = string.Empty;
}
