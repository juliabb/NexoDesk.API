namespace NexoDesk.DTOs;

public class AlterarDepartamentoRequest
{
    /// <summary>
    /// Novo nome do departamento.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Nova sigla do departamento.
    /// </summary>
    public string Sigla { get; set; } = string.Empty;
}
