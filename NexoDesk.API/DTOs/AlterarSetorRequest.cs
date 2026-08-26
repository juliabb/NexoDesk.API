namespace NexoDesk.DTOs;

public class AlterarSetorRequest
{
    /// <summary>
    /// Novo nome do setor.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Departamento à qual a setor será vinculada.
    /// </summary>
    public int DepartamentoId { get; set; }
}
