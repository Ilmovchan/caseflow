using CaseFlow.BLL.Dto.Auth;
using CaseFlow.DAL.Data;
using CaseFlow.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CaseFlow.BLL.Services;

public class AuthService(DetectiveAgencyDbContext context)
{
    public async Task<AuthResultDto> LoginAsync(LoginDto loginDto)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == loginDto.Username && u.Role == loginDto.Role);

        if (user == null || !user.IsActive)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = "Невірне ім'я користувача або роль"
            };
        }

        var hashedPassword = HashPassword(loginDto.Password);
        if (user.PasswordHash != hashedPassword)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = "Невірний пароль"
            };
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        return new AuthResultDto
        {
            Success = true,
            Message = "Успішний вхід",
            User = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                IsActive = user.IsActive
            }
        };
    }

    public async Task CreateDefaultUsersAsync()
    {
        // Check if users already exist
        if (await context.Users.AnyAsync())
        {
            return;
        }

        var defaultUsers = new List<User>
        {
            new User
            {
                Username = "admin",
                Email = "admin@caseflow.com",
                PasswordHash = HashPassword("admin123"),
                Role = "Admin",
                IsActive = true
            },
            new User
            {
                Username = "detective",
                Email = "detective@caseflow.com",
                PasswordHash = HashPassword("detective123"),
                Role = "Detective",
                IsActive = true
            }
        };

        context.Users.AddRange(defaultUsers);
        await context.SaveChangesAsync();
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}
