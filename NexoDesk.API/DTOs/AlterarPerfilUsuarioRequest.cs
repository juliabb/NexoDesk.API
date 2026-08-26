using NexoDesk.Enums;

namespace NexoDesk.DTOs;

public class AlterarPerfilUsuarioRequest
{
    /// <summary>
    /// Novo perfil que será atribuído ao usuário.
    /// </summary>
    public PerfilUsuario Perfil { get; set; }
}