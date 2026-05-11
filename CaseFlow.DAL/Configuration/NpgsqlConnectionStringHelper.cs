using Npgsql;

namespace CaseFlow.DAL.Configuration;

/// <summary>Підстановка логіна/пароля з змінних середовища (див. CASEFLOW_POSTGRES_*).</summary>
public static class NpgsqlConnectionStringHelper
{
    public const string EnvPassword = "CASEFLOW_POSTGRES_PASSWORD";
    public const string EnvUser = "CASEFLOW_POSTGRES_USER";

    public static string ApplyEnvironmentOverrides(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return connectionString ?? string.Empty;

        var envPwd = Environment.GetEnvironmentVariable(EnvPassword);
        var envUser = Environment.GetEnvironmentVariable(EnvUser);
        if (string.IsNullOrEmpty(envPwd) && string.IsNullOrEmpty(envUser))
            return connectionString;

        var csb = new NpgsqlConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(envUser))
            csb.Username = envUser;
        if (!string.IsNullOrEmpty(envPwd))
            csb.Password = envPwd;

        return csb.ConnectionString;
    }
}
