using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OrderTrackingApp.Models;

namespace OrderTrackingApp.Services;

public class SuperadminService : ISuperadminService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDbContext _db;
    private readonly ILogger<SuperadminService> _logger;

    public SuperadminService(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager,
        AppDbContext db,
        ILogger<SuperadminService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
        _logger = logger;
    }

    public async Task<(bool Success, List<string> Errors)> CreateSuperadminAsync(
        string username, string email, string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 50)
        {
            errors.Add("Username must be between 3 and 50 characters");
        }

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            errors.Add("Valid email is required");
        }

        var passwordValidation = ValidatePassword(password);
        errors.AddRange(passwordValidation);

        if (errors.Count > 0)
        {
            return (false, errors);
        }

        var existingUser = await _userManager.FindByNameAsync(username);
        if (existingUser != null)
        {
            errors.Add("Username already exists");
            return (false, errors);
        }

        var existingEmail = await _userManager.FindByEmailAsync(email);
        if (existingEmail != null)
        {
            errors.Add("Email already registered");
            return (false, errors);
        }

        var user = new User
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                errors.Add(error.Description);
            }
            return (false, errors);
        }

        // Ensure SuperAdmin role exists
        if (!await _roleManager.RoleExistsAsync("SuperAdmin"))
        {
            await _roleManager.CreateAsync(new IdentityRole("SuperAdmin"));
            _logger.LogInformation("Created SuperAdmin role");
        }

        // Assign SuperAdmin role to user
        var roleResult = await _userManager.AddToRoleAsync(user, "SuperAdmin");
        if (!roleResult.Succeeded)
        {
            _logger.LogWarning("Failed to assign SuperAdmin role to superadmin: {Errors}",
                string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }

        // Set Role property on User model
        user.Role = "SuperAdmin";
        await _userManager.UpdateAsync(user);

        // Assign ALL permissions as redundancy (in case bypass fails)
        var allPermissions = await _db.Permessi.ToListAsync();
        foreach (var perm in allPermissions)
        {
            if (!_db.PermessiUtente.Any(p => p.UserId == user.Id && p.PermissionId == perm.Id))
            {
                _db.PermessiUtente.Add(new UserPermission
                {
                    UserId = user.Id,
                    PermissionId = perm.Id
                });
            }
        }
        await _db.SaveChangesAsync();

        _logger.LogInformation("Superadmin created: {Username}", username);
        return (true, new List<string>());
    }

    private List<string> ValidatePassword(string password)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            errors.Add("Password is required");
            return errors;
        }

        if (password.Length < 12)
            errors.Add("Password must be at least 12 characters");

        if (!password.Any(char.IsUpper))
            errors.Add("Password must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            errors.Add("Password must contain at least one lowercase letter");

        if (!password.Any(char.IsDigit))
            errors.Add("Password must contain at least one number");

        if (!password.Any(c => "!@#$%^&*".Contains(c)))
            errors.Add("Password must contain at least one special character (!@#$%^&*)");

        return errors;
    }
}