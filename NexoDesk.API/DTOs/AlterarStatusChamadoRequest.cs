using NexoDesk.Enums;
using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class AlterarStatusChamadoRequest
{
    public StatusChamado Status { get; set; }

    [StringLength(500)]
    public string? Observacao { get; set; }
}
