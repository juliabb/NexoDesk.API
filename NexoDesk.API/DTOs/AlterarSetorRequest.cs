using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class AlterarSetorRequest
{
    /// <summary>
    /// Novo nome do setor.
    /// </summary>
    [Required, StringLength(150, MinimumLength = 2)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Departamento à qual a setor será vinculada.
    /// </summary>
    public int DepartamentoId { get; set; }
}
