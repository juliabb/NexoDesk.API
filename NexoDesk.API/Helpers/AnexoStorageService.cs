namespace NexoDesk.Helpers;

public sealed class AnexoStorageException : Exception
{
    public AnexoStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public interface IAnexoStorageService
{
    Task<string> SalvarAsync(Guid chamadoId, IFormFile arquivo, CancellationToken cancellationToken);
    Task<Stream?> AbrirLeituraAsync(Guid chamadoId, string nomeArmazenado, CancellationToken cancellationToken);
    Task ExcluirAsync(Guid chamadoId, string nomeArmazenado);
}

public class AnexoStorageService : IAnexoStorageService
{
    private readonly string _rootPath;

    public AnexoStorageService(IWebHostEnvironment environment)
    {
        _rootPath = Path.Combine(environment.ContentRootPath, "storage", "anexos");
    }

    public async Task<string> SalvarAsync(
        Guid chamadoId,
        IFormFile arquivo,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        var nomeArmazenado = $"{Guid.NewGuid():N}{extension}";
        var directory = ObterDiretorioChamado(chamadoId);
        Directory.CreateDirectory(directory);
        var path = ObterCaminhoSeguro(directory, nomeArmazenado);

        await using var stream = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous);
        await arquivo.CopyToAsync(stream, cancellationToken);

        return nomeArmazenado;
    }

    public Task<Stream?> AbrirLeituraAsync(
        Guid chamadoId,
        string nomeArmazenado,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var directory = ObterDiretorioChamado(chamadoId);
        var path = ObterCaminhoSeguro(directory, nomeArmazenado);
        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous)
            : null;
        return Task.FromResult(stream);
    }

    public Task ExcluirAsync(Guid chamadoId, string nomeArmazenado)
    {
        var directory = ObterDiretorioChamado(chamadoId);
        var path = ObterCaminhoSeguro(directory, nomeArmazenado);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string ObterDiretorioChamado(Guid chamadoId) =>
        Path.Combine(_rootPath, chamadoId.ToString("N"));

    private static string ObterCaminhoSeguro(string directory, string nomeArmazenado)
    {
        var fullDirectory = Path.GetFullPath(directory) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(directory, Path.GetFileName(nomeArmazenado)));
        if (!fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Caminho de anexo inválido.");
        }

        return fullPath;
    }
}
