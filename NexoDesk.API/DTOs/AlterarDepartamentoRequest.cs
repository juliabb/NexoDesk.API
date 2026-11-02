using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class AlterarDepartamentoRequest
{
    /// <summary>
    /// Novo nome do departamento.
    /// </summary>
    [Required, StringLength(150, MinimumLength = 2)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Nova sigla do departamento.
    /// </summary>
    [Required, StringLength(20, MinimumLength = 2)]
    public string Sigla { get; set; } = string.Empty;
}
