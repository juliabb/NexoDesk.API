using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;

namespace NexoDesk.Middleware;

public class DemoAccountGuardMiddleware
{
    private static readonly string[] ProtectedRoutePrefixes =
    [
        "/api/admin",
        "/api/departamentos",
        "/api/setores",
        "/api/categorias-chamados",
        "/api/usuarios"
    ];

    private readonly RequestDelegate _next;
    private readonly string? _demoAccountEmail;

    public DemoAccountGuardMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _demoAccountEmail = configuration["DemoAccount:Email"]?
            .Trim()
            .ToLowerInvariant();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsBlockedMutation(context))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                mensagem = "Esta ação está bloqueada no ambiente de demonstração para preservar os dados compartilhados."
            }));
            return;
        }

        await _next(context);
    }

    private bool IsBlockedMutation(HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(_demoAccountEmail) ||
            HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            return false;
        }

        var authenticatedEmail = context.User.FindFirstValue(ClaimTypes.Email) ??
            context.User.FindFirstValue(JwtRegisteredClaimNames.Email);

        if (!string.Equals(authenticatedEmail, _demoAccountEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return ProtectedRoutePrefixes.Any(prefix =>
            context.Request.Path.StartsWithSegments(prefix,
                StringComparison.OrdinalIgnoreCase));
    }
}
