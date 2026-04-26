using Microsoft.AspNetCore.Identity;

namespace Community_Event_Finder.Services
{
    /// <summary>
    /// Provides access to the current authenticated user's context and information.
    /// </summary>
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<IdentityUser> _userManager;

        public UserContext(IHttpContextAccessor httpContextAccessor, UserManager<IdentityUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        /// <summary>
        /// Gets the current user's ID.
        /// </summary>
        /// <returns>The user ID if authenticated, null if not authenticated.</returns>
        public string? GetUserId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return _userManager.GetUserId(user);
            }

            return null;
        }

        /// <summary>
        /// Gets the current user's name.
        /// </summary>
        /// <returns>The user name if authenticated, null if not authenticated.</returns>
        public string? GetUserName()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return user.Identity.Name;
            }

            return null;
        }

        /// <summary>
        /// Determines if the current user is authenticated.
        /// </summary>
        /// <returns>True if the user is authenticated, false otherwise.</returns>
        public bool IsAuthenticated()
        {
            return _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
        }

        /// <summary>
        /// Checks if the current user has a specific role.
        /// </summary>
        /// <param name="role">The role to check.</param>
        /// <returns>True if the user has the role, false otherwise.</returns>
        public bool HasRole(string role)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.IsInRole(role) ?? false;
        }

        /// <summary>
        /// Gets all roles for the current user.
        /// </summary>
        /// <returns>A list of role names for the current user, empty if not authenticated.</returns>
        public IReadOnlyList<string> GetUserRoles()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                var roles = user.FindAll("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                    .Select(c => c.Value)
                    .ToList();
                return roles.AsReadOnly();
            }

            return new List<string>().AsReadOnly();
        }
    }
}
