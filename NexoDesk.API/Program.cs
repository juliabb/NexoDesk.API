using NexoDesk.API.Filters;
using NexoDesk.Configuration;
using NexoDesk.Data;
using NexoDesk.Helpers;
using NexoDesk.Models;
using NexoDesk.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

DevelopmentEnvLoader.Load(builder);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();


// ============================================================
// CORS
// ============================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


// ============================================================
// POSTGRESQL
// ============================================================

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "A conexão DefaultConnection não foi configurada."
    );
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});


// ============================================================
// PASSWORD HASHER
// ============================================================

builder.Services.AddScoped<
    IPasswordHasher<Usuario>,
    PasswordHasher<Usuario>
>();
builder.Services.AddScoped<IAuditoriaUsuarioService, AuditoriaUsuarioService>();
builder.Services.AddScoped<INotificacaoService, NotificacaoService>();
var r2Configured = new[]
{
    builder.Configuration["R2:AccessKeyId"],
    builder.Configuration["R2:SecretAccessKey"],
    builder.Configuration["R2:Endpoint"],
    builder.Configuration["R2:BucketName"]
}.All(value => !string.IsNullOrWhiteSpace(value));

if (r2Configured)
{
    builder.Services.AddSingleton<IAnexoStorageService, R2AnexoStorageService>();
}
else
{
    builder.Services.AddSingleton<IAnexoStorageService, AnexoStorageService>();
}

var cpfHmacKey = builder.Configuration["Security:CpfHmacKey"];
if (string.IsNullOrWhiteSpace(cpfHmacKey) || Encoding.UTF8.GetByteCount(cpfHmacKey) < 32)
{
    throw new InvalidOperationException(
        "A chave Security:CpfHmacKey não foi configurada ou possui menos de 32 bytes.");
}
builder.Services.AddSingleton<ICpfProtectionService>(new CpfProtectionService(cpfHmacKey));


// ============================================================
// JWT
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException(
        "A chave JWT não foi configurada."
    );
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Valida a assinatura do token.
            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),

            // Valida quem emitiu o token.
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            // Valida para quem o token foi emitido.
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            // Verifica se o token ainda está dentro da validade.
            ValidateLifetime = true,

            // Não adiciona tolerância ao tempo de expiração.
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var usuarioIdClaim = context.Principal?
                    .FindFirstValue(ClaimTypes.NameIdentifier);

                var versaoTokenClaim = context.Principal?
                    .FindFirstValue("token_version");

                if (!Guid.TryParse(usuarioIdClaim, out var usuarioId) ||
                    !int.TryParse(versaoTokenClaim, out var versaoToken))
                {
                    context.Fail("Token inválido.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices
                    .GetRequiredService<AppDbContext>();

                var sessaoValida = await dbContext.Usuarios
                    .AsNoTracking()
                    .AnyAsync(usuario =>
                        usuario.Id == usuarioId &&
                        usuario.Ativo &&
                        usuario.Status == NexoDesk.Enums.StatusUsuario.Aprovado &&
                        usuario.VersaoToken == versaoToken,
                        context.HttpContext.RequestAborted);

                if (!sessaoValida)
                {
                    context.Fail("A sessão não é mais válida.");
                    return;
                }

                var deveAlterarSenha = bool.TryParse(
                    context.Principal?.FindFirstValue("password_change_required"),
                    out var trocaObrigatoria) && trocaObrigatoria;

                var rotaPermitida = context.HttpContext.Request.Path.StartsWithSegments("/api/usuarios/me/senha") ||
                    context.HttpContext.Request.Path.StartsWithSegments("/api/auth/logout");

                if (deveAlterarSenha && !rotaPermitida)
                {
                    context.Fail("A alteração de senha é obrigatória.");
                }
            }
        };
    });


// ============================================================
// AUTHORIZATION
// ============================================================

builder.Services.AddAuthorization();


// ============================================================
// SWAGGER
// ============================================================

builder.Services.AddOpenApi();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    // Define o esquema de autenticação JWT.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description =
            "Digite o token JWT. O Swagger enviará automaticamente como Bearer."
    });

    // Faz o Swagger enviar o JWT nas requisições.
    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] =
                new List<string>()
        });

    // Adiciona comentários XML para documentação.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

    options.IncludeXmlComments(xmlPath);

    // Mostra os valores dos enums no Swagger.
    options.SchemaFilter<EnumDescriptionSchemaFilter>();
});

var app = builder.Build();

// Em desenvolvimento, mantém o banco local sincronizado com as migrations do projeto.
// Em produção, a aplicação das migrations deve ocorrer de forma controlada no processo de deploy.
if (app.Environment.IsDevelopment() ||
    builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}


// ============================================================
// SWAGGER
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}


// ============================================================
// MIDDLEWARE
// ============================================================

app.UseCors("AllowAll");

// Mantém os cabeçalhos CORS também nas respostas produzidas pelo tratador de erros.
app.UseExceptionHandler();

app.UseHttpsRedirection();

// Autentica o usuário através do JWT.
app.UseAuthentication();

// Evita que a conta pública de demonstração altere dados administrativos.
// O bloqueio ocorre no servidor e não pode ser contornado pela interface.
app.UseMiddleware<DemoAccountGuardMiddleware>();

// Verifica se o usuário possui autorização.
app.UseAuthorization();

app.MapGet("/healthz", async (AppDbContext dbContext, CancellationToken cancellationToken) =>
    await dbContext.Database.CanConnectAsync(cancellationToken)
        ? Results.Ok(new { status = "healthy" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous()
    .ExcludeFromDescription();

app.MapControllers();

app.Run();
