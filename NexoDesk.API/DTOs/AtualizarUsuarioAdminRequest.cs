using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

/// <summary>
/// Dados cadastrais que um administrador pode alterar em um usuário.
/// </summary>
public class AtualizarUsuarioAdminRequest
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

    [Range(1, int.MaxValue, ErrorMessage = "A coordenadoria deve ser informada.")]
    public int CoordenadoriaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A área deve ser informada.")]
    public int AreaId { get; set; }
}
