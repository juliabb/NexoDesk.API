namespace NexoDesk.DTOs;

public class AlterarDepartamentoRequest
{
    /// <summary>
    /// Novo nome da departamento.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Nova sigla da departamento.
    /// </summary>
    public string Sigla { get; set; } = string.Empty;
}
