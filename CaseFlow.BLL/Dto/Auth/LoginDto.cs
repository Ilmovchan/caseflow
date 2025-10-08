namespace CaseFlow.BLL.Dto.Auth;

public class LoginDto
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string Role { get; set; } = null!;
    public bool RememberMe { get; set; }
}
