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

    public ImpactoChamado Impacto { get; set; } = ImpactoChamado.Individual;

    public UrgenciaChamado Urgencia { get; set; } = UrgenciaChamado.PodeAguardar;

    [StringLength(150)]
    public string? Localizacao { get; set; }

    [StringLength(100)]
    public string? PatrimonioEquipamento { get; set; }

    [StringLength(200)]
    public string? ContatoPreferencial { get; set; }
}
