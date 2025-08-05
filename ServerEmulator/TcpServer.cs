using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace ServerEmulator;

public class TcpServer
{
    private string _host="127,0,1";
    private int _port=8888;
    private string _sourceFile="";
    private TcpListener _listener;
    private TcpClient _client;
    private StreamReader _reader;
    private StreamWriter _writer;

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

    public Boolean StartUp()
    {
        Console.WriteLine("Listening...");
        IPAddress ipAddress = IPAddress.Parse(_host);
        _listener = new TcpListener(ipAddress, _port);
        _listener.Start();
        _client = _listener.AcceptTcpClient();
        Console.WriteLine("Received connection request ...");
        return _client.Connected;
    }

    public async void StartListeningAsync(CancellationToken ct)
    {
        _reader = new StreamReader(_client.GetStream());
        var progress = new Progress<ClientMessage>(HandleProgress);
        //while (!ct.IsCancellationRequested)
        //{
        try
        {
            var receiveTask = ReceiveAsync(progress, ct);
            await receiveTask;
        }
        catch (Exception ex)
        {
            Logger.PrintException(ex);
        }
        //if (!ct.IsCancellationRequested) await Task.Delay(TimeSpan.FromMilliseconds(20));
        //}
    }

    public async Task ReceiveAsync(IProgress<ClientMessage> progress, CancellationToken token)
    {

        if (_reader == null) return;

        while (!token.IsCancellationRequested)
        {
            try
            {
                if (!_client.Connected)
                {
                    Console.WriteLine("Client disconnected.");
                    return; // Exit if the client is no longer connected
                }
                var message = await _reader.ReadLineAsync();
                if (String.IsNullOrWhiteSpace(message))
                    continue;
                var result = new ClientMessage();
                result.TimeReceived = DateTime.Now;
                result.Message = message;
                progress.Report(result);
                string[] command = message.Split(' ');
                switch (command[0])
                {
                    case "ECHO":
                        {
                            try
                            {
                                if (_writer == null)
                                    _writer = new StreamWriter(_client.GetStream()) { AutoFlush = true };
                                await _writer.WriteLineAsync(message);
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
                                Console.WriteLine("Client requested to run script: " + scriptToRun);
                                await StartSendingScriptAsync(true, scriptToRun, token);
                            }
                            break;
                        }
                    case "EXIT":
                        {
                            Console.WriteLine("Client requested to exit.");
                            _client.Close();
                            _listener.Stop();
                            return; // Exit the loop if the client sends "exit"
                        }

                }
            }
            catch (Exception exe)
            {
                Logger.PrintException(exe);
                break;
            }
            continue;
        }
    }

    private Regex LogCleaner = new Regex(@"^Time.*[/][/][/][/]");
    public async Task StartSendingScriptAsync(bool enableEcho, string scriptToRun, CancellationToken ct)
    {
        string filePath = @"..\..\..\TestData\" + scriptToRun;
        if (String.IsNullOrWhiteSpace(scriptToRun) || !File.Exists(filePath))
        {
            Console.WriteLine("No script to run " + scriptToRun);
            return;
        }

        EchoEnabled = enableEcho;
        _writer = new StreamWriter(_client.GetStream()) { AutoFlush = true };
        if (_writer == null) return;

        Console.WriteLine("Sending Script");
        var allLines = File.ReadAllLines(filePath);
        foreach (String line in allLines)
        {
            if (ct.IsCancellationRequested && _client.Connected)
                break;
            string cleaned = LogCleaner.Replace(line, "");
            try
            {
                await _writer.WriteLineAsync(cleaned);
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
            await Task.Delay(100).ConfigureAwait(false);
        }
        await _writer.WriteLineAsync("EXIT");

    }
}
public static class Logger
{
    public static void PrintException(Exception ex, [CallerMemberName] string memberName = "")
    {
        Console.WriteLine("{0}: \n '{1}' \n '{2}'", memberName, ex.Message, ex.StackTrace);
    }
}
