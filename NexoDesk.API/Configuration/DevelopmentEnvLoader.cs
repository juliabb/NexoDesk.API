using System.Globalization;

namespace NexoDesk.Configuration;

public static class DevelopmentEnvLoader
{
    public static void Load(WebApplicationBuilder builder)
    {
        Dictionary<string, string> values;

        if (builder.Environment.IsDevelopment())
        {
            var envPath = EncontrarArquivoEnv(builder.Environment.ContentRootPath);
            if (envPath is null)
            {
                return;
            }

            values = File.ReadLines(envPath)
                .Select(ParseLine)
                .Where(item => item.HasValue)
                .ToDictionary(item => item!.Value.Key, item => item!.Value.Value,
                    StringComparer.OrdinalIgnoreCase);
        }
        else
        {
            var supportedKeys = new[]
            {
                "DATABASE_URL",
                "JWT_SECRET",
                "JWT_ISSUER",
                "JWT_AUDIENCE",
                "JWT_EXPIRATION_MINUTES",
                "CPF_HMAC_KEY",
                "SYSTEM_OWNER_EMAIL",
                "R2_ACCESS_KEY_ID",
                "R2_SECRET_ACCESS_KEY",
                "R2_ENDPOINT",
                "R2_BUCKET_NAME"
            };

            values = supportedKeys
                .Select(key => new KeyValuePair<string, string?>(
                    key, Environment.GetEnvironmentVariable(key)))
                .Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .ToDictionary(item => item.Key, item => item.Value!,
                    StringComparer.OrdinalIgnoreCase);

            // Mantém compatibilidade com a configuração nativa usando chaves com "__".
            if (!values.ContainsKey("DATABASE_URL"))
            {
                return;
            }
        }

        var databaseUrlKey = builder.Environment.IsDevelopment() &&
            values.ContainsKey("LOCAL_DATABASE_URL")
                ? "LOCAL_DATABASE_URL"
                : "DATABASE_URL";

        var configuration = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = ConverterDatabaseUrl(
                ObterObrigatorio(values, databaseUrlKey)),
            ["Jwt:Key"] = ObterObrigatorio(values, "JWT_SECRET"),
            ["Jwt:Issuer"] = ObterObrigatorio(values, "JWT_ISSUER"),
            ["Jwt:Audience"] = ObterObrigatorio(values, "JWT_AUDIENCE"),
            ["Security:CpfHmacKey"] = ObterObrigatorio(values, "CPF_HMAC_KEY")
        };

        if (values.TryGetValue("JWT_EXPIRATION_MINUTES", out var expiration) &&
            int.TryParse(expiration, CultureInfo.InvariantCulture, out var expirationMinutes))
        {
            configuration["Jwt:ExpirationInMinutes"] = expirationMinutes.ToString(CultureInfo.InvariantCulture);
        }

        if (values.TryGetValue("SYSTEM_OWNER_EMAIL", out var ownerEmail) &&
            !string.IsNullOrWhiteSpace(ownerEmail))
        {
            configuration["SystemOwner:Email"] = ownerEmail.Trim().ToLowerInvariant();
        }

        MapOptional(values, configuration, "R2_ACCESS_KEY_ID", "R2:AccessKeyId");
        MapOptional(values, configuration, "R2_SECRET_ACCESS_KEY", "R2:SecretAccessKey");
        MapOptional(values, configuration, "R2_ENDPOINT", "R2:Endpoint");
        MapOptional(values, configuration, "R2_BUCKET_NAME", "R2:BucketName");

        builder.Configuration.AddInMemoryCollection(configuration);
    }

    private static string? EncontrarArquivoEnv(string contentRootPath)
    {
        var candidates = new[]
        {
            Path.Combine(contentRootPath, ".env"),
            Path.GetFullPath(Path.Combine(contentRootPath, "..", ".env"))
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static KeyValuePair<string, string>? ParseLine(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            return null;
        }

        var separatorIndex = trimmed.IndexOf('=');
        if (separatorIndex <= 0)
        {
            return null;
        }

        var key = trimmed[..separatorIndex].Trim();
        var value = trimmed[(separatorIndex + 1)..].Trim();
        if (value.Length >= 2 &&
            (value[0] == '"' && value[^1] == '"' || value[0] == '\'' && value[^1] == '\''))
        {
            value = value[1..^1];
        }

        return new KeyValuePair<string, string>(key, value);
    }

    private static string ObterObrigatorio(
        IReadOnlyDictionary<string, string> values,
        string key)
    {
        if (!values.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"A variável {key} não foi configurada no arquivo .env.");
        }

        return value;
    }

    private static void MapOptional(
        IReadOnlyDictionary<string, string> values,
        IDictionary<string, string?> configuration,
        string environmentKey,
        string configurationKey)
    {
        if (values.TryGetValue(environmentKey, out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            configuration[configurationKey] = value.Trim();
        }
    }

    private static string ConverterDatabaseUrl(string databaseUrl)
    {
        var normalizedUrl = databaseUrl.Replace("postgresql+psycopg://", "postgresql://",
            StringComparison.OrdinalIgnoreCase);

        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("postgresql" or "postgres"))
        {
            throw new InvalidOperationException("DATABASE_URL possui um formato inválido.");
        }

        var userInfo = uri.UserInfo.Split(':', 2);
        if (userInfo.Length != 2)
        {
            throw new InvalidOperationException("DATABASE_URL deve informar usuário e senha.");
        }

        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = Uri.UnescapeDataString(userInfo[1]);
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        var port = uri.IsDefaultPort ? 5432 : uri.Port;

        var connectionString =
            $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password}";

        // URLs do Neon incluem sslmode=require. Bancos locais normalmente não
        // habilitam SSL, então só o tornamos obrigatório quando a URL solicitar.
        var sslObrigatorio = uri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(parameter => parameter.Split('=', 2))
            .Any(parts => parts.Length == 2 &&
                parts[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase) &&
                parts[1].Equals("require", StringComparison.OrdinalIgnoreCase));

        return sslObrigatorio
            ? $"{connectionString};SSL Mode=Require"
            : connectionString;
    }
}
