using NexoDesk.Enums;

namespace NexoDesk.Helpers;

public static class SlaChamadoHelper
{
    public static PrioridadeChamado CalcularPrioridade(
        ImpactoChamado impacto,
        UrgenciaChamado urgencia)
    {
        var pesoUrgencia = urgencia switch
        {
            UrgenciaChamado.PodeAguardar => 1,
            UrgenciaChamado.PrecisaHoje => 2,
            UrgenciaChamado.ImpedidoTrabalhar => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(urgencia))
        };

        var pontuacao = (int)impacto + pesoUrgencia;
        return pontuacao switch
        {
            <= 3 => PrioridadeChamado.Baixa,
            <= 5 => PrioridadeChamado.Media,
            <= 7 => PrioridadeChamado.Alta,
            _ => PrioridadeChamado.Critica
        };
    }

    public static TimeSpan ObterPrazoResolucao(PrioridadeChamado prioridade) => prioridade switch
    {
        PrioridadeChamado.Critica => TimeSpan.FromHours(8),
        PrioridadeChamado.Alta => TimeSpan.FromHours(24),
        PrioridadeChamado.Media => TimeSpan.FromHours(48),
        PrioridadeChamado.Baixa => TimeSpan.FromHours(72),
        _ => throw new ArgumentOutOfRangeException(nameof(prioridade))
    };
}
