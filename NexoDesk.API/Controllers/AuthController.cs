using NexoDesk.Data;
using NexoDesk.DTOs;
using NexoDesk.Enums;
using NexoDesk.Helpers;
using NexoDesk.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NexoDesk.Controllers;

/// Controller responsável pelas operações de autenticação
/// e cadastro de usuários do sistema.
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<Usuario> _passwordHasher;
    private readonly IConfiguration _configuration;
    private readonly ICpfProtectionService _cpfProtection;

    public AuthController(
        AppDbContext context,
        IPasswordHasher<Usuario> passwordHasher,
        IConfiguration configuration,
        ICpfProtectionService cpfProtection)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
        _cpfProtection = cpfProtection;
    }


    // ============================================================
    // CADASTRO
    // ============================================================

    /// <summary>
    /// Realiza o cadastro de um novo usuário.
    /// </summary>
    [HttpPost("cadastro")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cadastro(
        [FromBody] CadastroUsuarioRequest request)
    {
        // Normaliza o e-mail corporativo:
        // remove espaços e converte para letras minúsculas.
        var emailCorporativo = request.EmailCorporativo
            .Trim()
            .ToLowerInvariant();

        // Remove pontos, traços e outros caracteres do CPF.
        var cpfNormalizado = CpfHelper.SomenteNumeros(
            request.Cpf
        );

        // Valida o CPF antes de continuar o cadastro.
        if (!CpfHelper.EhValido(cpfNormalizado))
        {
            return BadRequest(new
            {
                mensagem = "O CPF informado é inválido."
            });
        }

        var cpfHash = _cpfProtection.GerarHash(cpfNormalizado);

        // Verifica se o e-mail corporativo já está cadastrado.
        var emailExiste = await _context.Usuarios
            .AsNoTracking()
            .AnyAsync(usuario =>
                usuario.EmailCorporativo == emailCorporativo
            );

        if (emailExiste)
        {
            return Conflict(new
            {
                mensagem = "Já existe um usuário com esse e-mail."
            });
        }

        // Verifica se o CPF já está cadastrado.
        var cpfExiste = await _context.Usuarios
            .AsNoTracking()
            .AnyAsync(usuario =>
                usuario.CpfHash == cpfHash
            );

        if (cpfExiste)
        {
            return Conflict(new
            {
                mensagem = "Já existe um usuário com esse CPF."
            });
        }
        // Verifica se a coordenadoria existe.
        var coordenadoriaExiste = await _context.Coordenadorias
            .AsNoTracking()
            .AnyAsync(coordenadoria =>
                coordenadoria.Id == request.CoordenadoriaId
            );

        if (!coordenadoriaExiste)
        {
            return BadRequest(new
            {
                mensagem = "A coordenadoria informada não existe."
            });
        }

        // Verifica se a área existe.
        var areaExiste = await _context.Areas
            .AsNoTracking()
            .AnyAsync(area =>
                area.Id == request.AreaId
            );

        if (!areaExiste)
        {
            return BadRequest(new
            {
                mensagem = "A área informada não existe."
            });
        }

        // Verifica se a área pertence à coordenadoria informada.
        var areaValida = await _context.Areas
            .AsNoTracking()
            .AnyAsync(area =>
                area.Id == request.AreaId &&
                area.CoordenadoriaId == request.CoordenadoriaId
            );

        if (!areaValida)
        {
            return BadRequest(new
            {
                mensagem =
                    "A área informada não pertence à coordenadoria selecionada."
            });
        }

        // Cria o usuário com:
        // - Perfil padrão
        // - Status aguardando aprovação
        // - Usuário inicialmente inativo
        var usuario = new Usuario
        {
            NomeCompleto = request.NomeCompleto.Trim(),

            EmailCorporativo = emailCorporativo,

            CpfHash = cpfHash,

            Ramal = request.Ramal.Trim(),

            CoordenadoriaId = request.CoordenadoriaId,

            AreaId = request.AreaId,

            Perfil = PerfilUsuario.Padrao,

            Status = StatusUsuario.AguardandoAprovacao,

            Ativo = false,

            CriadoEm = DateTime.UtcNow
        };

        // Gera o hash seguro da senha.
        // A senha original nunca é armazenada no banco.
        usuario.SenhaHash = _passwordHasher.HashPassword(
            usuario,
            request.Senha
        );

        // Adiciona o usuário ao contexto do Entity Framework.
        _context.Usuarios.Add(usuario);

        // Persiste o novo usuário no PostgreSQL.
        await _context.SaveChangesAsync();

        // Retorna 201 - Created.
        return Created(string.Empty, new
        {
            mensagem =
                "Usuário cadastrado com sucesso. Aguarde a aprovação do cadastro.",

            usuario.Status
        });
    }


    // ============================================================
    // LOGIN
    // ============================================================

    /// <summary>
    /// Realiza a autenticação do usuário e gera um token JWT.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request)
    {
        // Normaliza o e-mail informado pelo usuário.
        var emailCorporativo = request.EmailCorporativo
            .Trim()
            .ToLowerInvariant();

        // Procura o usuário pelo e-mail corporativo.
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(usuario =>
                usuario.EmailCorporativo == emailCorporativo
            );

        // Por segurança, utilizamos a mesma mensagem
        // para usuário inexistente ou senha incorreta.
        if (usuario == null)
        {
            return Unauthorized(new
            {
                mensagem = "Email ou senha inválidos."
            });
        }

        var agora = DateTime.UtcNow;

        if (usuario.BloqueadoAte.HasValue && usuario.BloqueadoAte > agora)
        {
            var segundosRestantes = (int)Math.Ceiling(
                (usuario.BloqueadoAte.Value - agora).TotalSeconds);

            Response.Headers.RetryAfter = segundosRestantes.ToString();

            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                mensagem = "Muitas tentativas de login. Aguarde antes de tentar novamente."
            });
        }

        // Verifica se a senha informada corresponde
        // ao hash armazenado no banco.
        var resultadoSenha =
            _passwordHasher.VerifyHashedPassword(
                usuario,
                usuario.SenhaHash,
                request.Senha
            );

        if (resultadoSenha == PasswordVerificationResult.Failed)
        {
            usuario.TentativasLoginFalhas++;

            var duracaoBloqueio = ObterDuracaoBloqueio(
                usuario.TentativasLoginFalhas);

            if (duracaoBloqueio.HasValue)
            {
                usuario.BloqueadoAte = agora.Add(duracaoBloqueio.Value);
                Response.Headers.RetryAfter = ((int)Math.Ceiling(
                    duracaoBloqueio.Value.TotalSeconds)).ToString();
            }

            usuario.AtualizadoEm = agora;
            await _context.SaveChangesAsync();

            if (duracaoBloqueio.HasValue)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new
                {
                    mensagem = "Muitas tentativas de login. Aguarde antes de tentar novamente."
                });
            }

            return Unauthorized(new
            {
                mensagem = "Email ou senha inválidos."
            });
        }

        // Usuários desativados não podem realizar login.
        if (usuario.Status == StatusUsuario.Desativado)
        {
            return Unauthorized(new
            {
                mensagem = "Usuário está desativado."
            });
        }

        // Usuários precisam ser aprovados antes de acessar o sistema.
        if (usuario.Status != StatusUsuario.Aprovado)
        {
            return Unauthorized(new
            {
                mensagem = "Usuário ainda não foi aprovado."
            });
        }

        // Usuários aprovados precisam estar ativos.
        if (!usuario.Ativo)
        {
            return Unauthorized(new
            {
                mensagem = "Usuário está inativo."
            });
        }

        // Atualiza hashes criados com uma configuração antiga do PasswordHasher.
        if (resultadoSenha == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.SenhaHash = _passwordHasher.HashPassword(
                usuario,
                request.Senha);
        }

        if (usuario.TentativasLoginFalhas > 0 || usuario.BloqueadoAte.HasValue ||
            resultadoSenha == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.TentativasLoginFalhas = 0;
            usuario.BloqueadoAte = null;
            usuario.AtualizadoEm = agora;
            await _context.SaveChangesAsync();
        }

        // Usuário válido:
        // gera o token JWT para autenticar as próximas requisições.
        var token = GerarToken(usuario);

        return Ok(new
        {
            name = usuario.NomeCompleto,
            email = usuario.EmailCorporativo,
            perfil = usuario.Perfil.ToString(),
            deveAlterarSenha = usuario.DeveAlterarSenha,
            token
        });
    }

    /// <summary>
    /// Encerra todas as sessões ativas do usuário autenticado.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var usuarioIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(usuarioIdClaim, out var usuarioId))
        {
            return Unauthorized(new { mensagem = "Token inválido." });
        }

        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == usuarioId);

        if (usuario is null)
        {
            return Unauthorized(new { mensagem = "Usuário não encontrado." });
        }

        usuario.VersaoToken++;
        usuario.AtualizadoEm = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { mensagem = "Logout realizado com sucesso." });
    }


    // ============================================================
    // GERAÇÃO DO TOKEN JWT
    // ============================================================

    /// Gera o token JWT 
    private string GerarToken(Usuario usuario)
    {
        // Claims que serão armazenadas dentro do JWT.
        var claims = new List<Claim>
    {
        // Nome completo do usuário.
        new Claim(
            JwtRegisteredClaimNames.UniqueName,
            usuario.NomeCompleto
        ),

        // E-mail corporativo do usuário.
        new Claim(
            JwtRegisteredClaimNames.Email,
            usuario.EmailCorporativo
        ),

        // ID único do usuário.
        new Claim(
            ClaimTypes.NameIdentifier,
            usuario.Id.ToString()
),

        // Perfil do usuário.
        // Será utilizado posteriormente para autorização.
        new Claim(
            ClaimTypes.Role,
            usuario.Perfil.ToString()
        ),

        // Permite invalidar tokens emitidos antes de um logout,
        // alteração de perfil ou desativação do usuário.
        new Claim(
            "token_version",
            usuario.VersaoToken.ToString()
        ),

        new Claim(
            "password_change_required",
            usuario.DeveAlterarSenha.ToString()
        )
    };

        // Obtém a chave secreta configurada no appsettings.json.
        var jwtKey = _configuration["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            throw new InvalidOperationException(
                "A chave JWT não foi configurada."
            );
        }

        // Cria a chave utilizada para assinar o token.
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        );

        // Define o algoritmo utilizado para assinar o token.
        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        var agora = DateTime.UtcNow;

        // Cria o JWT.
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,

            // Define quando o token começa a ser válido.
            notBefore: agora,

            // Define a expiração configurada (720 minutos = 12 horas).
            expires: agora.AddMinutes(ObterTempoExpiracaoEmMinutos()),

            // Assinatura do token.
            signingCredentials: credentials
        );

        // Converte o JWT para uma string.
        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private int ObterTempoExpiracaoEmMinutos()
    {
        var minutos = _configuration.GetValue<int?>("Jwt:ExpirationInMinutes");

        if (!minutos.HasValue || minutos.Value <= 0)
        {
            throw new InvalidOperationException(
                "O tempo de expiração do JWT não foi configurado corretamente."
            );
        }

        return minutos.Value;
    }

    private static TimeSpan? ObterDuracaoBloqueio(int tentativasFalhas)
    {
        return tentativasFalhas switch
        {
            < 3 => null,
            3 => TimeSpan.FromSeconds(30),
            4 => TimeSpan.FromMinutes(1),
            5 => TimeSpan.FromMinutes(5),
            6 => TimeSpan.FromMinutes(15),
            _ => TimeSpan.FromHours(1)
        };
    }
}
