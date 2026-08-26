using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class AlterarSenhaRequest
{
    [Required]
    public string SenhaAtual { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z0-9\\s]).{8,128}$")]
    public string NovaSenha { get; set; } = string.Empty;
}
