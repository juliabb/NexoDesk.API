using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class CriarSetorRequest
{
    /// <summary>
    /// Nome do setor.
    /// </summary>
    [Required, StringLength(150, MinimumLength = 2)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Identificador da Departamento à qual a setor pertence.
    /// </summary>
    public int DepartamentoId { get; set; }
}
