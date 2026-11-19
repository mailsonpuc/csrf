using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ApiCookieHttpOnly.DTOS;
using ApiCookieHttpOnly.Models;
using ApiCookieHttpOnly.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace ApiCookieHttpOnly.Controllers;

/// <summary>
/// Gerencia autenticação, contas de usuário, funções e ciclo de vida dos tokens JWT.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private const string CookieScheme = "AppCookie";
    private readonly ITokenService _tokenService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(ITokenService tokenService,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
        _tokenService = tokenService;
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Emite o token antiforgery que deve ser enviado no cabeçalho X-CSRF-TOKEN
    /// em operações realizadas com autenticação por cookie.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("antiforgery-token")]
    public IActionResult GetAntiforgeryToken([FromServices] IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { requestToken = tokens.RequestToken });
    }

    
    
    
    
    
    
    /// <summary>
    /// Cria uma função (role) se ela ainda não existir.
    /// </summary>
    /// <param name="roleName">Nome da função a criar.</param>
    /// <returns>Resultado da criação ou uma mensagem informando que a função já existe.</returns>
    [HttpPost]
    [Route("CreateRole")]
    public async Task<IActionResult> CreateRole(string roleName)
    {
        var roleExist = await _roleManager.RoleExistsAsync(roleName);

        if (!roleExist)
        {
            var roleResult = await _roleManager.CreateAsync(new IdentityRole(roleName));

            if (roleResult.Succeeded)
            {
                _logger.LogInformation(1, "Roles Added");
                return StatusCode(StatusCodes.Status200OK,
                        new Response { Status = "Success", Message = 
                        $"Role {roleName} added successfully" });
            }
            else
            {
                _logger.LogInformation(2, "Error");
                return StatusCode(StatusCodes.Status400BadRequest,
                   new Response { Status = "Error", Message = 
                       $"Issue adding the new {roleName} role" });
            }
        }
        return StatusCode(StatusCodes.Status400BadRequest,
          new Response { Status = "Error", Message = "Role already exist." });
    }

    /// <summary>
    /// Adiciona um usuário existente a uma função.
    /// </summary>
    /// <param name="email">E-mail do usuário.</param>
    /// <param name="roleName">Nome da função.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPost]
    [Route("AddUserToRole")]
    public async Task<IActionResult> AddUserToRole(string email, string roleName)
    {
        var user = await _userManager.FindByEmailAsync(email);

        if (user != null)
        {
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if(result.Succeeded)
            {
                _logger.LogInformation(1, $"User {user.Email} added to the {roleName} role");
                return StatusCode(StatusCodes.Status200OK,
                       new Response { Status = "Success", Message = 
                       $"User {user.Email} added to the {roleName} role" });
            }
            else
            {
                _logger.LogInformation(1, $"Error: Unable to add user {user.Email} to the {roleName} role");
                return StatusCode(StatusCodes.Status400BadRequest, new Response
                {
                    Status = "Error",
                    Message =$"Error: Unable to add user {user.Email} to the {roleName} role"
                });
            }
        }
        return BadRequest(new { error = "Unable to find user" });
    }

    
    
    /// <summary>
    /// Autentica um usuário e emite um token de acesso JWT e um refresh token.
    /// </summary>
    /// <param name="model">Nome de usuário e senha.</param>
    /// <param name="antiforgery">Serviço usado para criar o token CSRF associado à sessão.</param>
    /// <returns>Tokens e horário de expiração do token de acesso.</returns>
    [HttpPost]
    [Route("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginModel model,
        [FromServices] IAntiforgery antiforgery)
    {
        var user = await _userManager.FindByNameAsync(model.UserName!);

        if (user is not null && await _userManager.CheckPasswordAsync(user, model.Password!))
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim("id", user.UserName!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = _tokenService.GenerateAccessToken(authClaims,
                                                         _configuration);

            var refreshToken = _tokenService.GenerateRefreshToken();

            _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInMinutes"],
                               out int refreshTokenValidityInMinutes);

            user.RefreshTokenExpiryTime =
                            DateTime.UtcNow.AddMinutes(refreshTokenValidityInMinutes);

            user.RefreshToken = refreshToken;

            await _userManager.UpdateAsync(user);

            var cookieIdentity = new ClaimsIdentity(
                authClaims, CookieScheme, ClaimTypes.Name, ClaimTypes.Role);
            var cookieLifetimeMinutes = _configuration.GetValue<double?>(
                "Cookie:ExpirationInMinutes") ?? 60;
            await HttpContext.SignInAsync(CookieScheme,
                new ClaimsPrincipal(cookieIdentity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(cookieLifetimeMinutes)
                });
            HttpContext.User = new ClaimsPrincipal(cookieIdentity);
            var antiforgeryTokens = antiforgery.GetAndStoreTokens(HttpContext);

            return Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken,
                Expiration = token.ValidTo,
                CsrfToken = antiforgeryTokens.RequestToken
            });
        }
        return Unauthorized();
    }

    
    
    
    
    /// <summary>
    /// Cria uma nova conta de usuário.
    /// </summary>
    /// <param name="model">Dados necessários para registrar a conta.</param>
    /// <returns>Resultado do cadastro.</returns>
    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        var userExists = await _userManager.FindByNameAsync(model.Username!);

        if (userExists != null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                   new Response { Status = "Error", Message = "User already exists!" });
        }

        ApplicationUser user = new()
        {
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = model.Username
        };

        var result = await _userManager.CreateAsync(user, model.Password!);

        if (!result.Succeeded)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                   new Response { Status = "Error", Message = "User creation failed." });
        }
        return Ok(new Response { Status = "Success", Message = "User created successfully!" });
    }

    /// <summary>
    /// Valida um access token expirado e um refresh token, e emite um novo par de tokens.
    /// O refresh token anterior é invalidado após a rotação.
    /// </summary>
    /// <param name="tokenModel">Access token anterior e refresh token válido.</param>
    /// <returns>Novo access token e novo refresh token.</returns>
    [HttpPost]
    [Route("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] TokenModel? tokenModel)
    {
        if (string.IsNullOrWhiteSpace(tokenModel?.AccessToken)
            || string.IsNullOrWhiteSpace(tokenModel.RefreshToken))
        {
            return BadRequest("Invalid client request");
        }

        ClaimsPrincipal principal;
        try
        {
            principal = _tokenService.GetPrincipalFromExpiredToken(
                tokenModel.AccessToken, _configuration);
        }
        catch (SecurityTokenException)
        {
            return BadRequest("Invalid access token/refresh token");
        }
        catch (ArgumentException)
        {
            return BadRequest("Invalid access token/refresh token");
        }

        var username = principal.Identity?.Name;
        if (string.IsNullOrWhiteSpace(username))
            return BadRequest("Invalid access token/refresh token");

        var user = await _userManager.FindByNameAsync(username);

        if (user == null || user.RefreshToken != tokenModel.RefreshToken
                         || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return BadRequest("Invalid access token/refresh token");
        }

        var newAccessToken = _tokenService.GenerateAccessToken(
                                           principal.Claims.ToList(), _configuration);

        var newRefreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInMinutes"],
            out var refreshTokenValidityInMinutes);
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(refreshTokenValidityInMinutes);

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            _logger.LogError("Failed to persist rotated refresh token for user {UserId}", user.Id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                "Could not refresh token");
        }

        var cookieIdentity = new ClaimsIdentity(
            principal.Claims, CookieScheme, ClaimTypes.Name, ClaimTypes.Role);
        var cookieLifetimeMinutes = _configuration.GetValue<double?>(
            "Cookie:ExpirationInMinutes") ?? 60;
        await HttpContext.SignInAsync(CookieScheme,
            new ClaimsPrincipal(cookieIdentity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(cookieLifetimeMinutes)
            });

        return new ObjectResult(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
            refreshToken = newRefreshToken
        });
    }

    /// <summary>
    /// Revoga o refresh token do usuário informado, encerrando a possibilidade de renovação.
    /// </summary>
    /// <param name="username">Nome de usuário cujo refresh token será revogado.</param>
    /// <returns>Sem conteúdo quando a revogação é concluída.</returns>
    [Authorize]
    [HttpPost]
    [Route("revoke/{username}")]
    public async Task<IActionResult> Revoke(string username)
    {
        var user = await _userManager.FindByNameAsync(username);

        if (user == null) return BadRequest("Invalid user name");

        user.RefreshToken = null;

        await _userManager.UpdateAsync(user);

        return NoContent();
    }

    /// <summary>
    /// Encerra a sessão atual e invalida o refresh token do usuário autenticado.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var username = User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(username))
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user is not null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }
        }

        await HttpContext.SignOutAsync(CookieScheme);
        return NoContent();
    }


    

}
