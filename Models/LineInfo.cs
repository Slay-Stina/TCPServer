namespace TCPServer.Models;

public class LineInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Portnumber { get; set; }
    public bool IsDefault { get; set; }
}
