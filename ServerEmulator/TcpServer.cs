using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ServerEmulator;

public class TcpServer
{
    private string _host="127.0.0.1";
    private int _port=8888;
    private string _sourceFile="";
    private TcpListener _listener;

    public Boolean EchoEnabled { get; set; }

    public TcpServer(string host, int port, string sourceFile)
    {
        _host = host;
        _port = port;
        _sourceFile = sourceFile;
        EchoEnabled = false;
    }
    public class ClientMessage
    {
        public DateTime TimeReceived { get; set; }
        public String Message { get; set; }
    }

    private void HandleProgress(ClientMessage message)
    {
        Console.WriteLine("{0} > {1}", message.TimeReceived, message.Message);
    }

    public Boolean StartUpListener()
    {
        Console.WriteLine("Listening...");
        IPAddress ipAddress = IPAddress.Parse(_host);
        _listener = new TcpListener(ipAddress, _port);
        _listener.Start();
        Task.Run(async () =>
        {
            while (_listener.Server.IsBound)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Console.WriteLine("Received connection request ...");
                    StartListeningAsync(client, new CancellationTokenSource().Token);
                    Thread.Sleep(5000); // Give the client time to connect
                }
                catch (Exception ex)
                {
                    Logger.PrintException(ex);
                    break;
                }
            }
        });
        return true;
    }

    public async Task StartListeningAsync(TcpClient client, CancellationToken ct)
    {
        StreamReader reader = new StreamReader(client.GetStream());
        StreamWriter writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
        var progress = new Progress<ClientMessage>(HandleProgress);
        while (!ct.IsCancellationRequested && client.Connected)
        {
            if (client is null || !client.Connected)
                return;
            try
            {
                string? message = await reader.ReadLineAsync();
                Console.WriteLine($"FS: Rcvd {message}");
                if (message == null)
                {
                    Console.WriteLine("Client disconnected.");
                    break;
                }
                string[] command = message.Split(' ');
                switch (command[0])
                {
                    case "ECHO":
                        {
                            try
                            {
                                await writer.WriteLineAsync("FS:" + message);
                            }
                            catch (System.IO.IOException exc)
                            {
                            }
                            break;
                        }
                    case "SEND":
                        {
                            if (command.Length > 1)
                            {
                                string scriptToRun = command[1];
                                Console.WriteLine("Client requested to send file: " + scriptToRun);
                                await StartSendingScriptAsync(client, writer, true, scriptToRun, ct);
                            }
                            break;
                        }
                    case "EXIT":
                        {
                            Console.WriteLine("Client requested to exit.");
                            reader.Close();
                            writer.Close();
                            client.Close();
                            return; // Exit the loop if the client sends "exit"
                        }
                    default:
                        {
                            Console.WriteLine("Unknown command received: " + message);
                            break;
                        }

                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("forcibly closed"))
                    Console.WriteLine("Connection forcibly closed by client");
                else
                    Logger.PrintException(ex);
            }
            Thread.Sleep(500);
        }
    }

    private Regex LogCleaner = new Regex(@"^Time.*[/][/][/][/]");
    public async Task StartSendingScriptAsync(TcpClient client, StreamWriter writer, bool enableEcho, string scriptToRun, CancellationToken ct)
    {
        string filePath = @"..\..\..\TestData\" + scriptToRun;
        if (String.IsNullOrWhiteSpace(scriptToRun) || !File.Exists(filePath))
        {
            Console.WriteLine("No script to run " + scriptToRun);
            return;
        }

        EchoEnabled = enableEcho;
        if (writer == null) return;

        Console.WriteLine("Sending Script");
        var allLines = File.ReadAllLines(filePath);
        foreach (String line in allLines)
        {
            if (ct.IsCancellationRequested || !client.Connected)
                break;
            string cleaned = LogCleaner.Replace(line, "");
            try
            {
                await writer.WriteLineAsync(cleaned);
            }
            catch (System.IO.IOException exe)
            {
                break;
            }
            catch (Exception exc)
            {
                Logger.PrintException(exc);
                break;
            }
            await Task.Delay(50).ConfigureAwait(false);
        }
        await writer.WriteLineAsync("EXIT");

    }
}
public static class Logger
{
    public static void PrintException(Exception ex, [CallerMemberName] string memberName = "")
    {
        Console.WriteLine("{0}: \n '{1}' \n '{2}'", memberName, ex.Message, ex.StackTrace);
    }
}
