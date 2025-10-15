namespace TCPServer;

class Program
{
    static async Task Main(string[] args)
    {
        var db = new DummyDatabase();
        var server = new Server(3077, db);
        await server.StartAsync();
    }
}
