using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class AtualizarCategoriaChamadoRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Nome { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Descricao { get; set; }

    [Range(1, int.MaxValue)]
    public int AreaResponsavelId { get; set; }

    public bool Ativa { get; set; }
}
