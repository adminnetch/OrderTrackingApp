using Microsoft.AspNetCore.Identity;

namespace OrderTrackingApp.Models
{
    public static class SeedData
    {
        public static async Task InitializeAsync(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Creare il ruolo "Admin" se non esiste
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Controlla se l'utente admin esiste
            if (await userManager.FindByNameAsync("admin") == null)
            {
                var adminUser = new User
                {
                    UserName = "admin",
                    Email = "admin@example.com",
                    FirstName = "Admin",
                    LastName = "User",
                    PhoneNumber = "1234567890",
                    Role = "Admin",
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, "1234assoKAPPA@#");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    throw new Exception("Errore durante la creazione dell'utente admin");
                }
            }
        }
    }
}
