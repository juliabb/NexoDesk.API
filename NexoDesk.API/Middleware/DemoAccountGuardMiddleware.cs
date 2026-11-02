using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;

namespace NexoDesk.Middleware;

public class DemoAccountGuardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string? _demoAccountEmail;
    private readonly string? _systemOwnerEmail;

    public DemoAccountGuardMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _systemOwnerEmail = configuration["SystemOwner:Email"]?.Trim().ToLowerInvariant();
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
                mensagem = "Esta conta de administrador é protegida e não pode ter seus dados ou senha alterados."
            }));
            return;
        }

        await _next(context);
    }

    private bool IsBlockedMutation(HttpContext context)
    {
        if (HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) ||
            HttpMethods.IsOptions(context.Request.Method))
        {
            return false;
        }

        var authenticatedEmail = context.User.FindFirstValue(ClaimTypes.Email) ??
            context.User.FindFirstValue(JwtRegisteredClaimNames.Email);

        if (string.IsNullOrWhiteSpace(authenticatedEmail) ||
            !(string.Equals(authenticatedEmail, _demoAccountEmail, StringComparison.OrdinalIgnoreCase) ||
              string.Equals(authenticatedEmail, _systemOwnerEmail, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Operações administrativas são liberadas; os controllers protegem as contas alvo.
        // Impede também alterações da própria conta protegida pelos endpoints de perfil e senha.
        return context.Request.Path.StartsWithSegments("/api/usuarios",
            StringComparison.OrdinalIgnoreCase);
    }
}
