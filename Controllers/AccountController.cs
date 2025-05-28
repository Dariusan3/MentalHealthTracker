using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MentalHealthTracker.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace MentalHealthTracker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet("status")]
        public IActionResult GetAuthStatus()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            var userName = User.Identity?.Name ?? "";
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            
            // Adăugăm informații de diagnosticare
            _logger.LogInformation("Verificare stare autentificare: Autentificat={IsAuthenticated}, Utilizator={UserName}, ID={UserId}", 
                isAuthenticated, userName, userId);
            
            // Verificăm și cookie-urile
            var authCookies = Request.Cookies.Where(c => c.Key.Contains(".Auth") || c.Key.Contains("Identity")).ToList();
            if (authCookies.Any())
            {
                _logger.LogInformation("Cookie-uri de autentificare găsite: {Cookies}", 
                    string.Join(", ", authCookies.Select(c => c.Key)));
            }
            else
            {
                _logger.LogWarning("Nu s-au găsit cookie-uri de autentificare");
            }
            
            return Ok(new { 
                isAuthenticated = isAuthenticated,
                userName = userName,
                userId = userId,
                hasCookies = authCookies.Any(),
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Date invalide" });
            }

            // Forțăm delogarea pentru a evita conflicte
            await _signInManager.SignOutAsync();
            
            // Logăm încercarea de autentificare
            _logger.LogInformation("Încercare de autentificare pentru {Email}", model.Email);
            
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                // Obținem utilizatorul pentru a avea acces la ID
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    // Setăm manual cookie-ul de autentificare
                    await _signInManager.SignInAsync(user, model.RememberMe);
                    
                    // Logăm succesul autentificării
                    _logger.LogInformation("Autentificare reușită pentru {Email} (ID: {UserId})", user.Email, user.Id);
                    
                    // Adăugăm un claim temporar pentru a verifica autentificarea
                    var claims = new List<Claim>
                    {
                        new Claim("LoginTime", DateTime.Now.ToString())
                    };
                    await _userManager.AddClaimsAsync(user, claims);
                    
                    return Ok(new { 
                        success = true, 
                        redirectUrl = "/",
                        userId = user.Id,
                        userName = user.UserName
                    });
                }
                return Ok(new { success = true, redirectUrl = "/" });
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("Cont blocat pentru {Email}", model.Email);
                return BadRequest(new { success = false, message = "Contul tău a fost blocat. Te rugăm să încerci mai târziu." });
            }
            else if (result.RequiresTwoFactor)
            {
                _logger.LogInformation("Necesită 2FA pentru {Email}", model.Email);
                return BadRequest(new { success = false, message = "Autentificarea necesită verificare în doi pași." });
            }
            else
            {
                _logger.LogWarning("Autentificare eșuată pentru {Email}", model.Email);
                return BadRequest(new { success = false, message = "Autentificare eșuată. Verifică email-ul și parola." });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Date invalide" });
            }

            if (model.Password != model.ConfirmPassword)
            {
                return BadRequest(new { success = false, message = "Parolele nu se potrivesc." });
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                DateOfBirth = model.DateOfBirth,
                RegisteredDate = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return Ok(new { success = true, redirectUrl = "/" });
            }
            else
            {
                return BadRequest(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Logăm deconectarea
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = User.Identity?.Name;
                _logger.LogInformation("Deconectare utilizator: {UserName} (ID: {UserId})", userName, userId);
                
                // Deconectăm utilizatorul
                await _signInManager.SignOutAsync();
                
                // Nu încercăm să modificăm cookie-urile direct, SignOutAsync se ocupă de asta
                
                return Ok(new { success = true, redirectUrl = "/" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Eroare la deconectare: {Error}", ex.Message);
                return StatusCode(500, new { success = false, message = "Eroare la deconectare", error = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshAuth([FromBody] RefreshRequest? request = null)
        {
            // Logăm încercarea de reîmprospătare
            _logger.LogInformation("Încercare de reîmprospătare autentificare");
            
            // Verificăm dacă utilizatorul este deja autentificat
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    // Reîmprospătăm starea de autentificare
                    await _signInManager.RefreshSignInAsync(user);
                    
                    _logger.LogInformation("Sesiune reîmprospătată pentru utilizatorul autentificat {Email}", user.Email);
                    
                    return Ok(new { 
                        success = true, 
                        isAuthenticated = true,
                        userName = user.UserName,
                        userId = user.Id
                    });
                }
            }
            
            // Dacă utilizatorul nu este autentificat, dar avem informații din localStorage
            if (request != null && !string.IsNullOrEmpty(request.UserId))
            {
                _logger.LogInformation("Încercare de reautentificare cu ID {UserId}", request.UserId);
                
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user != null)
                {
                    // Forțăm delogarea pentru a evita conflicte
                    await _signInManager.SignOutAsync();
                    
                    // Autentificăm utilizatorul fără parolă (bazat pe ID-ul din localStorage)
                    await _signInManager.SignInAsync(user, true);
                    
                    _logger.LogInformation("Sesiune recreată pentru utilizatorul {Email} din localStorage", user.Email);
                    
                    // Adăugăm un claim temporar pentru a verifica autentificarea
                    var claims = new List<Claim>
                    {
                        new Claim("RefreshTime", DateTime.Now.ToString())
                    };
                    await _userManager.AddClaimsAsync(user, claims);
                    
                    return Ok(new { 
                        success = true, 
                        isAuthenticated = true,
                        userName = user.UserName,
                        userId = user.Id
                    });
                }
                else
                {
                    _logger.LogWarning("Utilizatorul cu ID {UserId} nu a fost găsit", request.UserId);
                }
            }
            
            return Ok(new { 
                success = false, 
                isAuthenticated = false,
                message = "Nu sunteți autentificat"
            });
        }

        [HttpGet("logout-direct")]
        public IActionResult LogoutDirect()
        {
            try
            {
                // Logăm deconectarea
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var userName = User.Identity?.Name;
                _logger.LogInformation("Deconectare directă utilizator: {UserName} (ID: {UserId})", userName, userId);
                
                // Deconectăm utilizatorul
                _signInManager.SignOutAsync().Wait();
                
                // Ștergem cookie-urile de autentificare explicit
                foreach (var cookie in Request.Cookies.Keys)
                {
                    Response.Cookies.Delete(cookie);
                }
                
                // Redirecționăm către pagina principală
                return Redirect("/?deconectat=true&t=" + DateTime.Now.Ticks);
            }
            catch (Exception ex)
            {
                _logger.LogError("Eroare la deconectare directă: {Error}", ex.Message);
                return Redirect("/?eroare=true&t=" + DateTime.Now.Ticks);
            }
        }

        [HttpPost("delete")]
        [Authorize] // Doar utilizatorii autentificați pot șterge contul
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // Ar trebui să fie autentificat datorită [Authorize], dar verificăm pentru siguranță
                return Unauthorized(new { success = false, message = "Utilizatorul nu este autentificat." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                // Contul nu există în baza de date (deși utilizatorul pare autentificat)
                _logger.LogWarning("Încercare de ștergere cont pentru un utilizator inexistent (ID: {UserId})", userId);
                // Poate forțăm o deconectare aici?
                await _signInManager.SignOutAsync();
                return Unauthorized(new { success = false, message = "Contul nu există." });
            }

            // Verificăm parola curentă pentru confirmare
            var isPasswordCorrect = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
            if (!isPasswordCorrect)
            {
                return BadRequest(new { success = false, message = "Parola curentă este incorectă." });
            }

            // Ștergem utilizatorul
            var deleteResult = await _userManager.DeleteAsync(user);

            if (deleteResult.Succeeded)
            {
                 _logger.LogInformation("Cont șters cu succes pentru utilizatorul {Email} (ID: {UserId})", user.Email, user.Id);

                // Deconectăm utilizatorul după ștergere
                await _signInManager.SignOutAsync();

                // Returnăm un răspuns HTTP de redirecționare către pagina de login
                return Redirect("/account/login");
            }
            else
            {
                var errors = string.Join(", ", deleteResult.Errors.Select(e => e.Description));
                _logger.LogError("Eroare la ștergerea contului pentru utilizatorul {Email} (ID: {UserId}): {Errors}", user.Email, user.Id, errors);
                return StatusCode(500, new { success = false, message = "Eroare la ștergerea contului", errors = errors });
            }
        }

        // Adăugăm o clasă model pentru request-ul de ștergere cont
        public class DeleteAccountRequest
        {
            public string CurrentPassword { get; set; } = "";
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public bool RememberMe { get; set; }
    }

    public class RegisterRequest
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
        public DateTime? DateOfBirth { get; set; }
    }

    public class RefreshRequest
    {
        public string UserId { get; set; } = "";
        public string UserName { get; set; } = "";
    }
} 