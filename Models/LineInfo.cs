namespace TCPServer.Models;

public class LineInfo
{
    public Guid LineId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Portnumber { get; set; }
    public bool IsDefault { get; set; }
    public bool IsSelected { get; set; }
}
