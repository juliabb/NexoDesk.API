using NexoDesk.Enums;
using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class AlterarPrioridadeChamadoRequest
{
    public PrioridadeChamado Prioridade { get; set; }

    [StringLength(350)]
    public string? Justificativa { get; set; }
}
