using System.Text.Json;
using TCPServer.Models;

namespace TCPServer;

public class DummyDatabase
{
    private readonly string _usersPath = "users.json";
    private readonly string _linesPath = "lines.json";
    private List<User> _users;
    private List<LineInfo> _lines;
    private static readonly Random _random = new();

    public DummyDatabase()
    {
        Load();
    }

    public List<LineInfo> GetData() => _lines;
    public List<User> GetUsers() => _users;
    public User? GetUserById(Guid id) => _users.FirstOrDefault(u => u.Id == id);

    public string AddUser(User user)
    {
        user.Id = Guid.NewGuid();
        if (_users.Any(u => u.Id == user.Id))
            user.Id = Guid.NewGuid();
        _users.Add(user);
        Save();
        return "OK";
    }

    public string UpdateUser(User updatedUser)
    {
        var idx = _users.FindIndex(u => u.Id == updatedUser.Id);
        if (idx >= 0)
        {
            _users[idx] = updatedUser;
            Save();
            return "OK";
        }
        return "FAIL";
    }

    public string DeleteUser(Guid userId)
    {
        var idx = _users.FindIndex(u => u.Id == userId);
        if (idx >= 0)
        {
            _users.RemoveAt(idx);
            Save();
            return "OK";
        }
        return "FAIL";
    }

    public string AddLine(LineInfo line)
    {
        line.Id = Guid.NewGuid();
        while (_lines.Any(l => l.Id == line.Id))
            line.Id = Guid.NewGuid();
        if (line.IsDefault)
        {
            foreach (var l in _lines)
                l.IsDefault = false;
        }
        _lines.Add(line);
        if (!_lines.Any(l => l.IsDefault) && _lines.Count > 0)
            _lines[0].IsDefault = true;
        Save();
        return "OK";
    }
    public string UpdateLine(LineInfo updatedLine)
    {
        var idx = _lines.FindIndex(l => l.Id == updatedLine.Id);
        if (idx >= 0)
        {
            if (updatedLine.IsDefault)
            {
                for (int i = 0; i < _lines.Count; i++)
                {
                    if (_lines[i].Id != updatedLine.Id)
                        _lines[i].IsDefault = false;
                }
            }
            _lines[idx] = updatedLine;
            if (!_lines.Any(l => l.IsDefault) && _lines.Count > 0)
                _lines[0].IsDefault = true;
            Save();
            return "OK";
        }
        return "FAIL";
    }
    public string DeleteLine(Guid delLineId)
    {
        var idx = _lines.FindIndex(l => l.Id == delLineId);
        if (idx >= 0)
        {
            bool wasDefault = _lines[idx].IsDefault;
            _lines.RemoveAt(idx);
            if (wasDefault && _lines.Count > 0 && !_lines.Any(l => l.IsDefault))
                _lines[0].IsDefault = true;
            Save();
            return "OK";
        }
        return "FAIL";
    }

    private void Load()
    {
        if (File.Exists(_linesPath))
        {
            var lines = File.ReadAllText(_linesPath);
            _lines = JsonSerializer.Deserialize<List<LineInfo>>(lines) ?? GenerateRandomLines();
            NormalizeDefault(_lines);
        }
        else
        {
            _lines = GenerateRandomLines();
        }
        if (File.Exists(_usersPath))
        {
            var users = File.ReadAllText(_usersPath);
            _users = JsonSerializer.Deserialize<List<User>>(users) ?? GenerateRandomUsers();
        }
        else
        {
            _users = GenerateRandomUsers();
        }
        Save();
    }

    private List<User> GenerateRandomUsers()
    {
        var list = new List<User>();
        int userCount = _random.Next(3, 8); // 3-7 users
        var names = new[] { "Alice", "Bob", "Charlie", "Diana", "Eve", "Frank", "Grace", "Heidi" };
        var authLevels = Enum.GetValues<AutherizationLevel>();

        for (int i = 0; i < userCount; i++)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = names[_random.Next(names.Length)] + _random.Next(100, 999),
                Password = Guid.NewGuid().ToString("N")[..8],
                AuthLevel = authLevels[_random.Next(authLevels.Length)]
            };
            list.Add(user);
        }
        return list;
    }

    private List<LineInfo> GenerateRandomLines()
    {
        var list = new List<LineInfo>();
        int lineCount = _random.Next(4, 11); // 4-10 lines
        int defaultIdx = lineCount > 0 ? _random.Next(0, lineCount) : -1;
        for (int j = 0; j < lineCount; j++)
        {
            var line = new LineInfo
            {
                Name = $"Line_{j + 1}_{Guid.NewGuid().ToString()[..4]}",
                IpAddress = $"192.168.1.{j + 10}",
                Portnumber = 1000 + _random.Next(0, 9000),
                IsDefault = j == defaultIdx
            };
            list.Add(line);
        }
        return list;
    }

    private static void NormalizeDefault(List<LineInfo> lines)
    {
        if (lines == null || lines.Count == 0)
            return;

        // Keep the first IsDefault if any; otherwise set the first line as default
        int firstDefault = lines.FindIndex(l => l.IsDefault);
        if (firstDefault == -1)
        {
            lines[0].IsDefault = true;
            for (int i = 1; i < lines.Count; i++)
                lines[i].IsDefault = false;
        }
        else
        {
            for (int i = 0; i < lines.Count; i++)
                lines[i].IsDefault = (i == firstDefault);
        }
    }

    public void Save()
    {
        var users = JsonSerializer.Serialize(_users, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_usersPath, users);

        var lines = JsonSerializer.Serialize(_lines, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_linesPath, lines);
    }
}
