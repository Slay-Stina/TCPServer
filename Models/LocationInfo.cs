namespace TCPServer.Models;

public class LocationInfo
{
    public string Name { get; set; }
    public List<LineInfo> Lines { get; set; } = new();
}