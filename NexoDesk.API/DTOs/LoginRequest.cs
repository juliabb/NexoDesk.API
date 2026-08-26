using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "O email corporativo é obrigatório.")]
    [EmailAddress(ErrorMessage = "Informe um email válido.")]
    public string EmailCorporativo { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    public string Senha { get; set; } = string.Empty;
}