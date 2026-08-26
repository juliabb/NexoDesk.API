using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace NexoDesk.DTOs;

public class CadastroUsuarioRequest
{
    [Required(ErrorMessage = "O nome completo é obrigatório.")]
    [StringLength(150, MinimumLength = 3)]
    public string NomeCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "O email corporativo é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    public string EmailCorporativo { get; set; } = string.Empty;

    [Required(ErrorMessage = "O CPF é obrigatório.")]
    public string Cpf { get; set; } = string.Empty;

    [Required(ErrorMessage = "O ramal é obrigatório.")]
    public string Ramal { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [StringLength(128, MinimumLength = 8,
        ErrorMessage = "A senha deve ter entre 8 e 128 caracteres.")]
    [RegularExpression(
        "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z0-9\\s]).{8,128}$",
        ErrorMessage = "A senha deve conter letra maiúscula, letra minúscula, número e caractere especial.")]
    public string Senha { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "A coordenadoria deve ser informada.")]
    [DefaultValue(1)]
    public int CoordenadoriaId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "A área deve ser informada.")]
    [DefaultValue(1)]
    public int AreaId { get; set; }
}
