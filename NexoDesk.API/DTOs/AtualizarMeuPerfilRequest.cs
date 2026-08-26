using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

/// <summary>
/// Dados que o próprio usuário pode atualizar.
/// </summary>
public class AtualizarMeuPerfilRequest
{
    [Required(ErrorMessage = "O nome completo é obrigatório.")]
    [StringLength(150, MinimumLength = 3)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "O e-mail corporativo é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail corporativo inválido.")]
    public string EmailCorporativo { get; set; } = string.Empty;

    [Required(ErrorMessage = "O ramal é obrigatório.")]
    [StringLength(9)]
    public string Ramal { get; set; } = string.Empty;
}
