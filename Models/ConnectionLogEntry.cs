namespace TCPServer.Models;

public class ConnectionLogEntry
{
    public string IpAddress { get; set; }
    public int Port { get; set; }
    public string RemoteEndPoint { get; set; }
    public string Message { get; set; }
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public string? Exception { get; set; }
}
