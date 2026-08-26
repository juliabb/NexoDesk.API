using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class CriarComentarioChamadoRequest
{
    [Required, StringLength(2000, MinimumLength = 1)]
    public string Conteudo { get; set; } = string.Empty;

    public bool Interno { get; set; }
}
