using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TCPServer.Models;

namespace TCPServer;

public class ClientHandler
{
    private readonly TcpClient _client;
    private readonly DummyDatabase _db;
    private static readonly ConnectionLogger _logger = new();

    public ClientHandler(TcpClient client, DummyDatabase db)
    {
        _client = client;
        _db = db;
    }

    public async Task Handle()
    {
        var stream = _client.GetStream();
        var remoteEndPoint = _client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        var ip = _client.Client.RemoteEndPoint is System.Net.IPEndPoint ipEp ? ipEp.Address.ToString() : "unknown";
        var port = _client.Client.RemoteEndPoint is System.Net.IPEndPoint ipEp2 ? ipEp2.Port : 0;

        try
        {
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            using var writer = new StreamWriter(stream, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };

            string? message = await reader.ReadLineAsync();
            if (!string.IsNullOrEmpty(message))
            {
                Console.WriteLine($"Message received from {remoteEndPoint} [{DateTime.Now}]: {message}");

                string reply;
                try
                {
                    reply = HandleCommand(message);
                }
                catch (Exception ex)
                {
                    reply = $"ERROR: {ex.Message}";
                    _logger.LogAsync(new ConnectionLogEntry
                    {
                        IpAddress = ip,
                        Port = port,
                        RemoteEndPoint = remoteEndPoint,
                        Message = message,
                        Timestamp = DateTime.UtcNow,
                        Success = false,
                        Exception = ex.ToString()
                    });
                }
                await writer.WriteLineAsync(reply);
            }
        }
        catch (Exception ex)
        {
            _logger.LogAsync(new ConnectionLogEntry
            {
                IpAddress = ip,
                Port = port,
                RemoteEndPoint = remoteEndPoint,
                Message = string.Empty,
                Timestamp = DateTime.UtcNow,
                Success = false,
                Exception = ex.ToString()
            });
            Console.WriteLine($"Client connection error: {ex.Message}");
        }
        finally
        {
            _db.Save();
            _client.Close();
        }
    }

    private string HandleCommand(string message)
    {
        try
        {
            var data = _db.GetData();
            return message switch
            {
                var m when m.StartsWith("GET_ALL_LINES") => JsonSerializer.Serialize(data),
                var m when m.StartsWith("GET_LINE_BY_NAME") =>
                    data.FirstOrDefault(l => l.Name == m[16..]) is LineInfo lineByName
                        ? JsonSerializer.Serialize(lineByName)
                        : "",
                var m when m.StartsWith("ADD_LINE") =>
                    JsonSerializer.Deserialize<LineInfo>(m[8..]) is LineInfo addLine
                        ? AddLine(data, addLine)
                        : "FAIL",
                var m when m.StartsWith("UPDATE_LINE") =>
                    JsonSerializer.Deserialize<LineInfo>(m[11..]) is LineInfo updatedLine
                        ? UpdateLine(data, updatedLine)
                        : "FAIL",
                var m when m.StartsWith("DELETE_LINE") =>
                    JsonSerializer.Deserialize<LineInfo>(m[11..]) is LineInfo delLine
                        ? DeleteLine(data, delLine)
                        : "FAIL",
                _ => "UNKNOWN_COMMAND"
            };
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    private static string AddLine(List<LineInfo> data, LineInfo line)
    {
        data.Add(line);
        return "OK";
    }

    private static string UpdateLine(List<LineInfo> data, LineInfo updatedLine)
    {
        var idx = data.FindIndex(l => l.Name == updatedLine.Name);
        if (idx >= 0)
        {
            data[idx] = updatedLine;
            return "OK";
        }
        return "FAIL";
    }

    private static string DeleteLine(List<LineInfo> data, LineInfo delLine)
    {
        var idx = data.FindIndex(l => l.Name == delLine.Name);
        if (idx >= 0)
        {
            data.RemoveAt(idx);
            return "OK";
        }
        return "FAIL";
    }
}
