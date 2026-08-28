using NexoDesk.Enums;

namespace NexoDesk.Models;

public class Chamado
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Protocolo { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string Descricao { get; set; } = string.Empty;
    public PrioridadeChamado Prioridade { get; set; } = PrioridadeChamado.Media;
    public ImpactoChamado Impacto { get; set; } = ImpactoChamado.Individual;
    public UrgenciaChamado Urgencia { get; set; } = UrgenciaChamado.PodeAguardar;
    public string? Localizacao { get; set; }
    public string? PatrimonioEquipamento { get; set; }
    public string? ContatoPreferencial { get; set; }
    public StatusChamado Status { get; set; } = StatusChamado.Aberto;
    public Guid SolicitanteId { get; set; }
    public Usuario Solicitante { get; set; } = null!;
    public int CategoriaId { get; set; }
    public CategoriaChamado Categoria { get; set; } = null!;
    public int SetorSolicitanteId { get; set; }
    public Setor SetorSolicitante { get; set; } = null!;
    public int SetorResponsavelId { get; set; }
    public Setor SetorResponsavel { get; set; } = null!;
    public Guid? ResponsavelId { get; set; }
    public Usuario? Responsavel { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime PrazoResolucaoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
    public DateTime? ResolvidoEm { get; set; }
    public DateTime? FechadoEm { get; set; }
    public ICollection<HistoricoChamado> Historico { get; set; } = new List<HistoricoChamado>();
    public ICollection<ComentarioChamado> Comentarios { get; set; } = new List<ComentarioChamado>();
    public ICollection<Notificacao> Notificacoes { get; set; } = new List<Notificacao>();
    public ICollection<AnexoChamado> Anexos { get; set; } = new List<AnexoChamado>();
}
