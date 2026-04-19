using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Community_Event_Finder.Services;

namespace Community_Event_Finder.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IUserContext _userContext;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IUserContext userContext,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userContext = userContext;
            _logger = logger;
        }

        /// <summary>
        /// Registers a new user account.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Email and password are required." });
            }

            try
            {
                var user = new IdentityUser { UserName = request.Email, Email = request.Email };
                var result = await _userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        error = "Registration failed.",
                        errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                _logger.LogInformation("User registered successfully: {Email}", request.Email);

                return Ok(new { message = "User registered successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, new { error = "An error occurred during registration." });
            }
        }

        /// <summary>
        /// Logs in a user and establishes a session.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { error = "Email and password are required." });
            }

            try
            {
                var result = await _signInManager.PasswordSignInAsync(
                    request.Email,
                    request.Password,
                    isPersistent: request.RememberMe,
                    lockoutOnFailure: false);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed login attempt for email: {Email}", request.Email);
                    return Unauthorized(new { error = "Invalid email or password." });
                }

                _logger.LogInformation("User logged in successfully: {Email}", request.Email);

                return Ok(new { message = "Login successful." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { error = "An error occurred during login." });
            }
        }

        /// <summary>
        /// Logs out the current user.
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                _logger.LogInformation("User logged out successfully");
                return Ok(new { message = "Logout successful." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { error = "An error occurred during logout." });
            }
        }

        /// <summary>
        /// Gets the current authenticated user's profile information.
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = _userContext.GetUserId();
                var userName = _userContext.GetUserName();
                var roles = _userContext.GetUserRoles();

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated." });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { error = "User not found." });
                }

                return Ok(new
                {
                    userId,
                    userName,
                    email = user.Email,
                    roles = roles.ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                return StatusCode(500, new { error = "An error occurred while retrieving profile." });
            }
        }

        /// <summary>
        /// Changes the password for the current authenticated user.
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(request.CurrentPassword) ||
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { error = "Current and new passwords are required." });
            }

            try
            {
                var userId = _userContext.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { error = "User not authenticated." });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { error = "User not found." });
                }

                var result = await _userManager.ChangePasswordAsync(
                    user,
                    request.CurrentPassword,
                    request.NewPassword);

                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        error = "Password change failed.",
                        errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                _logger.LogInformation("Password changed successfully for user: {UserId}", userId);

                return Ok(new { message = "Password changed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { error = "An error occurred while changing password." });
            }
        }
    }

    // Request/Response DTOs
    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; } = false;
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
