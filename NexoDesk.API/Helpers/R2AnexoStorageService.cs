using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace NexoDesk.Helpers;

public sealed class R2AnexoStorageService : IAnexoStorageService, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string _bucketName;

    public R2AnexoStorageService(IConfiguration configuration)
    {
        var accessKeyId = ObterObrigatorio(configuration, "R2:AccessKeyId");
        var secretAccessKey = ObterObrigatorio(configuration, "R2:SecretAccessKey");
        var endpoint = ObterObrigatorio(configuration, "R2:Endpoint").TrimEnd('/');
        _bucketName = ObterObrigatorio(configuration, "R2:BucketName");

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var endpointUri) ||
            endpointUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("R2_ENDPOINT deve ser uma URL HTTPS válida.");
        }

        _client = new AmazonS3Client(
            new BasicAWSCredentials(accessKeyId, secretAccessKey),
            new AmazonS3Config
            {
                ServiceURL = endpoint,
                AuthenticationRegion = "auto",
                ForcePathStyle = true
            });
    }

    public async Task<string> SalvarAsync(
        Guid chamadoId,
        IFormFile arquivo,
        CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
        var nomeArmazenado = $"{Guid.NewGuid():N}{extension}";
        var key = ObterChave(chamadoId, nomeArmazenado);

        await using var stream = arquivo.OpenReadStream();
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = arquivo.ContentType,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        }, cancellationToken);

        return nomeArmazenado;
    }

    public async Task<Stream?> AbrirLeituraAsync(
        Guid chamadoId,
        string nomeArmazenado,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _client.GetObjectAsync(
                _bucketName,
                ObterChave(chamadoId, nomeArmazenado),
                cancellationToken);
            var memory = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memory, cancellationToken);
            memory.Position = 0;
            return memory;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task ExcluirAsync(Guid chamadoId, string nomeArmazenado)
    {
        await _client.DeleteObjectAsync(_bucketName, ObterChave(chamadoId, nomeArmazenado));
    }

    public void Dispose() => _client.Dispose();

    private static string ObterChave(Guid chamadoId, string nomeArmazenado) =>
        $"chamados/{chamadoId:N}/{Path.GetFileName(nomeArmazenado)}";

    private static string ObterObrigatorio(IConfiguration configuration, string key) =>
        !string.IsNullOrWhiteSpace(configuration[key])
            ? configuration[key]!
            : throw new InvalidOperationException($"A configuração {key} é obrigatória para utilizar o R2.");
}
