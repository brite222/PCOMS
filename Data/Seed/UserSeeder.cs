using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace PCOMS.Data.Seed
{
    public static class UserSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            var userManager =
                services.GetRequiredService<UserManager<IdentityUser>>();

            var roleManager =
                services.GetRequiredService<RoleManager<IdentityRole>>();

            const string adminEmail = "admin@pcoms.local";
            const string adminPassword = "Admin123!";
         

            // 1️⃣ Ensure Admin role exists
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(
                    new IdentityRole("Admin"));
            }

            // 2️⃣ Check if admin user exists
            var adminUser =
                await userManager.FindByEmailAsync(adminEmail);

            if (adminUser != null)
                return;

            // 3️⃣ Create admin user
            adminUser = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result =
                await userManager.CreateAsync(
                    adminUser, adminPassword);

            if (!result.Succeeded)
            {
                throw new Exception(
                    "Failed to create admin user: " +
                    string.Join(", ",
                        result.Errors.Select(e => e.Description)));
            }

            // 4️⃣ Assign Admin role
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}
