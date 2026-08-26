namespace NexoDesk.Models;

/// <summary>
/// Mantém hashes de senhas anteriores para impedir reutilização.
/// Nunca armazena a senha em texto puro.
/// </summary>
public class HistoricoSenha
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public string SenhaHash { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}
