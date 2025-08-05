using System.Net;

namespace ServerEmulator;
class Program
{
    static int DefaultPort = 8888;
    static string DefaultHost = IPAddress.Loopback.ToString();
    static string DefaultSourceFile = "";
    // Spin up a server that will feed raw data logs from a file to the client
    // Optionally, spin up a client that will connect to the server and request a file as a means of testing the server functionality
    public static CancellationTokenSource tokenSource;
    static void Main(string[] args)
    {
        ExtractHostAndPort(args, out int port, out string host, out string sourcefile);
        tokenSource = new CancellationTokenSource();
        Task.Run(() =>
        {
            var server = new TcpServer(host, port, sourcefile);
            if (server.StartUp())
            {
                server.StartListeningAsync(tokenSource.Token);
            }
        });
        if (args.Any(x => x.StartsWith("Client", StringComparison.OrdinalIgnoreCase)))
        {
            // Spin up a client that will connect to the server and request a file
            Task.Run(() =>
            {
                Thread.Sleep(10000); // Give the server time to startup and process external request
                var client = new ClientEmulator.ClientEmulator("ECHO Hello From Internal Client");
                client.InitEmulator(host, port, sourcefile);
            });
        }
        Console.WriteLine("Listening....Press Enter to Stop.");
        Console.ReadLine();
        tokenSource.Cancel();
        Console.WriteLine("Server Stopped. Press Enter to exit.");
        Console.ReadLine();
    }

    private static void ExtractHostAndPort(string[] args, out int port, out string host, out string sourcefile)
    {
        port = (int)GetArg("Port", DefaultPort, args);
        host = (string)GetArg("Host", DefaultHost, args);
        sourcefile = (string)GetArg("Source", DefaultSourceFile, args);
    }

    private static object GetArg(string arg, object defaultValue, string[] args)
    {

        if (args == null || args.Length == 0)
            return defaultValue;

        string argument = args.ToList().Find(x => x.StartsWith(arg + "=", StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(argument))
        {
            string[] values = argument.Split('=');
            if (values.Length == 2)
            {
                if (defaultValue is int && int.TryParse(values[1], out int result))
                    return result;
                if (defaultValue is string && !string.IsNullOrEmpty(values[1]))
                    return values[1];
            }
        }
        return defaultValue;
    }
}
