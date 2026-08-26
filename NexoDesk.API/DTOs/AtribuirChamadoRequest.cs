using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class AtribuirChamadoRequest
{
    [Required]
    public Guid? ResponsavelId { get; set; }

    [StringLength(500)]
    public string? Observacao { get; set; }
}
