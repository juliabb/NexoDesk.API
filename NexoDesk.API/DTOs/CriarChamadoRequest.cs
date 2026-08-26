using NexoDesk.Enums;
using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class CriarChamadoRequest
{
    [Required, StringLength(150, MinimumLength = 5)]
    public string Titulo { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 10)]
    public string Descricao { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int CategoriaId { get; set; }

    public PrioridadeChamado Prioridade { get; set; } = PrioridadeChamado.Media;
}
