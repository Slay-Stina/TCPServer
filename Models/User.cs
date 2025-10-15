namespace TCPServer.Models;

public class User
{
    public Guid UserId { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public AutherizationLevel AuthLevel { get; set; } = AutherizationLevel.Operator;
}

public enum AutherizationLevel
{
    Admin,
    Foreman,
    Maintenance,
    Supervisor,
    Engineer,
    Operator,
    Designer,
    Manager,
    View
}

