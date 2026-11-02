using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class CriarDepartamentoRequest
{
    /// <summary>
    /// Nome do departamento.
    /// </summary>

    [Required, StringLength(150, MinimumLength = 2)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Sigla do departamento.
    /// </summary>
    [Required, StringLength(20, MinimumLength = 2)]
    public string Sigla { get; set; } = string.Empty;
}
