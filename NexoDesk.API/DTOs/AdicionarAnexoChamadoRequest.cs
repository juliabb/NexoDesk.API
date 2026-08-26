using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class AdicionarAnexoChamadoRequest
{
    [Required]
    public IFormFile Arquivo { get; set; } = null!;

    public bool Interno { get; set; }
}
