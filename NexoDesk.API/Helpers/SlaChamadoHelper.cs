using NexoDesk.Enums;

namespace NexoDesk.Helpers;

public static class SlaChamadoHelper
{
    public static TimeSpan ObterPrazoResolucao(PrioridadeChamado prioridade) => prioridade switch
    {
        PrioridadeChamado.Critica => TimeSpan.FromHours(8),
        PrioridadeChamado.Alta => TimeSpan.FromHours(24),
        PrioridadeChamado.Media => TimeSpan.FromHours(48),
        PrioridadeChamado.Baixa => TimeSpan.FromHours(72),
        _ => throw new ArgumentOutOfRangeException(nameof(prioridade))
    };
}
