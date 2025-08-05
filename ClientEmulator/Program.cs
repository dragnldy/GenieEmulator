using System.Net;

namespace ClientEmulator;
internal class Program
{
    static int DefaultPort = 8888;
    static string DefaultHost = IPAddress.Loopback.ToString();
    static string DefaultSourceFile = "TestSimple.txt";
    // Spin up a server that will feed raw data logs from a file to the client
    // Optionally, spin up a client that will connect to the server and request a file as a means of testing the server functionality
    public static CancellationTokenSource tokenSource;
    static void Main(string[] args)
    {
        ExtractHostAndPort(args, out int port, out string host, out string sourcefile);
        tokenSource = new CancellationTokenSource();
        // Spin up a client that will connect to the server and request a file
        Task.Run(() =>
        {
            Thread.Sleep(500); // Give the server time to startup
            var client = new ClientEmulator("ECHO Hello from external client");
            client.InitEmulator(host, port, sourcefile);
        });
        Console.WriteLine("Connecting to server....Press Enter to Stop.");
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