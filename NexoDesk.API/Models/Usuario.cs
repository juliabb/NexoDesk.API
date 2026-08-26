using NexoDesk.Enums;

namespace NexoDesk.Models;

public class Usuario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NomeCompleto { get; set; } = string.Empty;
    public string EmailCorporativo { get; set; } = string.Empty;
    public string CpfHash { get; set; } = string.Empty;
    public string Ramal { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public int DepartamentoId { get; set; }
    public Departamento? Departamento { get; set; }
    public int SetorId { get; set; }
    public Setor? Setor { get; set; }
    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Padrao;
    public StatusUsuario Status { get; set; } = StatusUsuario.AguardandoAprovacao;
    public bool Ativo { get; set; } = true;
    public int VersaoToken { get; set; } = 1;
    public int TentativasLoginFalhas { get; set; }
    public DateTime? BloqueadoAte { get; set; }
    public bool DeveAlterarSenha { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; set; }
    public DateTime? AprovadoEm { get; set; }
    public Guid? AprovadoPorUsuarioId { get; set; }
    public ICollection<Chamado> ChamadosSolicitados { get; set; } = new List<Chamado>();
    public ICollection<Chamado> ChamadosAtribuidos { get; set; } = new List<Chamado>();
    public ICollection<HistoricoChamado> AcoesEmChamados { get; set; } = new List<HistoricoChamado>();
    public ICollection<ComentarioChamado> ComentariosEmChamados { get; set; } = new List<ComentarioChamado>();
    public ICollection<Notificacao> Notificacoes { get; set; } = new List<Notificacao>();
    public ICollection<AnexoChamado> AnexosEnviados { get; set; } = new List<AnexoChamado>();
}
