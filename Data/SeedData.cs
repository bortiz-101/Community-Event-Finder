using Microsoft.AspNetCore.Identity;

namespace Community_Event_Finder.Data
{
    /// <summary>
    /// Provides utilities for seeding initial database data including roles.
    /// </summary>
    public static class SeedData
    {
        private static readonly string[] RoleNames = { "Admin", "User" };

        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                
                await CreateRolesAsync(roleManager);
                await CreateSuperUserAsync(userManager);
            }
        }

        private static async Task CreateRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var roleName in RoleNames)
            {
                var roleExists = await roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        private static async Task CreateSuperUserAsync(UserManager<IdentityUser> userManager)
        {
            var superUserName = "admin";
            var superUser = await userManager.FindByNameAsync(superUserName);
            if (superUser == null)
            {
                superUser = new IdentityUser
                {
                    UserName = superUserName,
                    Email = "admin@example.com",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(superUser, "admin");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superUser, "Admin");
                }
            }
        }
    }
}
