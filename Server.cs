using System.Net;
using System.Net.Sockets;

namespace TCPServer;

public class Server
{
    private readonly int _port;
    private readonly DummyDatabase _db;

    public Server(int port, DummyDatabase db)
    {
        _port = port;
        _db = db;
    }

    public async Task StartAsync()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, _port);
        TcpListener server = new TcpListener(ep);
        server.Start();
        Console.WriteLine($"TCP server started on port: {_port}. Waiting for connections...");

        while (true)
        {
            var client = await server.AcceptTcpClientAsync();
            _ = Task.Run(async () => await new ClientHandler(client, _db).Handle());
        }
    }
}
