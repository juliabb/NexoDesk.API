using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class RedefinirSenhaAdminRequest
{
    [Required]
    [StringLength(128, MinimumLength = 8)]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z0-9\\s]).{8,128}$")]
    public string SenhaTemporaria { get; set; } = string.Empty;
}
