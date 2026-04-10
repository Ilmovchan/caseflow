using CaseFlow.BLL.Dto.Auth;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CaseFlow.BLL.Services;

public class AuthService(IConfiguration configuration)
{
    public async Task<AuthResultDto> LoginAsync(LoginDto loginDto)
    {
        if (string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
        {
            return new AuthResultDto
            {
                Success = false,
                Message = "Логін і пароль є обов'язковими"
            };
        }

        // Template: Host/Port/Database (and pool defaults). Username/Password are replaced with
        // the login form values so PostgreSQL authenticates that user (e.g. role "admin" only here).
        var baseConnectionString = configuration.GetConnectionString("DetectiveAgencyDb");
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            return new AuthResultDto
            {
                Success = false,
                Message = "Помилка конфігурації підключення до БД"
            };
        }

        var roleFromPostgres = await ResolvePostgresRoleAsync(baseConnectionString, loginDto.Username.Trim(), loginDto.Password);
        if (roleFromPostgres == null)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = "Невірний логін/пароль або не призначена роль PostgreSQL (admin/detective)"
            };
        }

        return new AuthResultDto
        {
            Success = true,
            Message = "Успішний вхід",
            User = new UserDto
            {
                Id = 0,
                Username = loginDto.Username.Trim(),
                // For detective flow, we use username as identity/email unless mapped elsewhere.
                Email = loginDto.Username.Trim(),
                Role = roleFromPostgres,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
                IsActive = true
            }
        };
    }

    public async Task CreateDefaultUsersAsync()
    {
        // Auth now relies on PostgreSQL users/roles, not app-level default users table.
        await Task.CompletedTask;
    }

    private static async Task<string?> ResolvePostgresRoleAsync(string baseConnectionString, string username, string password)
    {
        try
        {
            var csb = new NpgsqlConnectionStringBuilder(baseConnectionString)
            {
                Username = username,
                Password = password
            };

            await using var connection = new NpgsqlConnection(csb.ConnectionString);
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(@"
SELECT
    pg_has_role(current_user, 'admin', 'member') AS is_admin,
    pg_has_role(current_user, 'detective', 'member') AS is_detective;", connection);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return null;

            var isAdmin = reader.GetBoolean(0);
            var isDetective = reader.GetBoolean(1);

            if (isAdmin) return "Admin";
            if (isDetective) return "Detective";
            return null;
        }
        catch (PostgresException ex) when (ex.SqlState is "28P01" or "28000")
        {
            return null;
        }
    }
}
