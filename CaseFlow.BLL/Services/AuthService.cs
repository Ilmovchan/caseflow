using CaseFlow.BLL.Dto.Auth;
using CaseFlow.DAL.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CaseFlow.BLL.Services;

public class AuthService(IConfiguration configuration, ILogger<AuthService> logger)
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

        var baseConnectionString = NpgsqlConnectionStringHelper.ApplyEnvironmentOverrides(
            configuration.GetConnectionString("DetectiveAgencyDb"));
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

        var csbInfo = new NpgsqlConnectionStringBuilder(baseConnectionString);
        logger.LogInformation(
            "Успішний вхід у PostgreSQL: pg_user={PgUser}, роль_застосунку={AppRole}, хост={Host}, база={Database}. Перевірено pg_has_role для admin/detective.",
            loginDto.Username.Trim(),
            roleFromPostgres,
            csbInfo.Host ?? "(default)",
            csbInfo.Database ?? "(default)");

        return new AuthResultDto
        {
            Success = true,
            Message = "Успішний вхід",
            User = new UserDto
            {
                Id = 0,
                Username = loginDto.Username.Trim(),
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
