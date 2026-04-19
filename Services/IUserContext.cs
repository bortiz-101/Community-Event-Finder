namespace Community_Event_Finder.Services
{
    /// <summary>
    /// Provides access to the current authenticated user's context and information.
    /// </summary>
    public interface IUserContext
    {
        /// <summary>
        /// Gets the current user's ID.
        /// </summary>
        /// <returns>The user ID if authenticated, null if not authenticated.</returns>
        string? GetUserId();

        /// <summary>
        /// Gets the current user's name.
        /// </summary>
        /// <returns>The user name if authenticated, null if not authenticated.</returns>
        string? GetUserName();

        /// <summary>
        /// Determines if the current user is authenticated.
        /// </summary>
        /// <returns>True if the user is authenticated, false otherwise.</returns>
        bool IsAuthenticated();

        /// <summary>
        /// Checks if the current user has a specific role.
        /// </summary>
        /// <param name="role">The role to check.</param>
        /// <returns>True if the user has the role, false otherwise.</returns>
        bool HasRole(string role);

        /// <summary>
        /// Gets all roles for the current user.
        /// </summary>
        /// <returns>A list of role names for the current user, empty if not authenticated.</returns>
        IReadOnlyList<string> GetUserRoles();
    }
}
