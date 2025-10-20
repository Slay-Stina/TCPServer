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
                var m when m.StartsWith("GET_LINE_BY_ID") =>
                    data.FirstOrDefault(l => l.Id.ToString() == m[14..]) is LineInfo lineById
                        ? JsonSerializer.Serialize(lineById)
                        : "",
                var m when m.StartsWith("ADD_LINE") =>
                    JsonSerializer.Deserialize<LineInfo>(m[8..]) is LineInfo addLine
                        ? AddLine(data, addLine)
                        : "FAIL",
                var m when m.StartsWith("UPDATE_LINE") =>
                    JsonSerializer.Deserialize<LineInfo>(m[11..]) is LineInfo updatedLine
                        ? UpdateLine(data, updatedLine)
                        : "FAIL",
                var m when m.StartsWith("GET_DEFAULT") =>
                    data.FirstOrDefault(l => l.IsDefault) is LineInfo lineByDefault
                        ? JsonSerializer.Serialize(lineByDefault)
                        : "",
                var m when m.StartsWith("DELETE_LINE") =>
                    Guid.TryParseExact(m[11..], "D", out Guid delLineId)
                        ? DeleteLine(data, delLineId)
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
        // If the added line is marked as default, unset other defaults
        if (line.IsDefault)
        {
            foreach (var l in data)
                l.IsDefault = false;
        }
        // If no line is default after adding, ensure one default exists
        data.Add(line);
        if (!data.Any(l => l.IsDefault) && data.Count > 0)
            data[0].IsDefault = true;
        return "OK";
    }

    private static string UpdateLine(List<LineInfo> data, LineInfo updatedLine)
    {
        var idx = data.FindIndex(l => l.Id == updatedLine.Id);
        if (idx >= 0)
        {
            // If updated line is set as default, unset other defaults
            if (updatedLine.IsDefault)
            {
                for (int i = 0; i < data.Count; i++)
                {
                    if (data[i].Id != updatedLine.Id)
                        data[i].IsDefault = false;
                }
            }
            data[idx] = updatedLine;
            // Ensure at least one default remains
            if (!data.Any(l => l.IsDefault) && data.Count > 0)
                data[0].IsDefault = true;
            return "OK";
        }
        return "FAIL";
    }

    private static string DeleteLine(List<LineInfo> data, Guid delLineId)
    {
        var idx = data.FindIndex(l => l.Id == delLineId);
        if (idx >= 0)
        {
            bool wasDefault = data[idx].IsDefault;
            data.RemoveAt(idx);
            // If we removed the default line, ensure another becomes default
            if (wasDefault && data.Count > 0 && !data.Any(l => l.IsDefault))
                data[0].IsDefault = true;
            return "OK";
        }
        return "FAIL";
    }
}
