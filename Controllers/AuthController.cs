using System.Security.Claims;
using ApiCookieHttpOnly.DTOS;
using ApiCookieHttpOnly.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ApiCookieHttpOnly.Controllers;

/// <summary>
/// Gerencia autenticação por cookie, contas de usuário e funções.
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private const string CookieScheme = "AppCookie";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    
    public AuthController(UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ILogger<AuthController> logger)
    {
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
    /// Autentica um usuário e cria uma sessão protegida por cookie.
    /// </summary>
    /// <param name="model">Nome de usuário e senha.</param>
    /// <param name="antiforgery">Serviço usado para criar o token CSRF associado à sessão.</param>
    /// <returns>Token antiforgery para as próximas operações.</returns>
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
                new Claim("id", user.UserName!)
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

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
    /// <summary>
    /// Encerra a sessão atual e invalida o refresh token do usuário autenticado.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieScheme);
        return NoContent();
    }


    

}
