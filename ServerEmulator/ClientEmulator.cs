using System.Net;
using System.Net.Sockets;

namespace ClientEmulator;
public class ClientEmulator
{
    private static string _greeting = "Hello from ClientEmulator";
    public ClientEmulator(string greeting)
    {
        // Default constructor
        _greeting = greeting;
    }
    public void InitEmulator(string ipAddress, int port, string sourcefile)
    {
        TcpClient _client;
        bool _ownsClient;
        StreamReader _reader;
        StreamWriter _writer;

        Thread.Sleep(5000); // Give the server time to startup

        _client = new TcpClient();
        IPAddress ip = IPAddress.Parse(ipAddress);
        _client.Connect(ip,port);
        Console.WriteLine($"Connected to server at {ipAddress}:{port} Requesting file {sourcefile}");
        using (NetworkStream stream = _client.GetStream())
        {
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream) { AutoFlush = true };
            try
            {
                Task.Run(() => DoSomethingWithClientAsync(_reader, _writer, sourcefile)).Wait();
            }
            catch (SocketException ex)
            {
                Console.WriteLine("SocketException: {0}", ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);
            }
            finally
            {
                Console.WriteLine("Connection closed.");
            }
        }
        Console.WriteLine("Press Enter to exit.");
        Console.ReadLine();
        _client.Close();
    }
    public static async Task DoSomethingWithClientAsync(StreamReader _reader, StreamWriter _writer, string sourceFile)
    {
        try
        {
            string messageToSend = string.IsNullOrEmpty(_greeting) ? "ECHO Hello from ClientEmulator" : _greeting;
            await _writer.WriteLineAsync(messageToSend).ConfigureAwait(false);
            Thread.Sleep(100); // Simulate some delay
            string data = await _reader.ReadLineAsync().ConfigureAwait(false);
            Console.WriteLine("Received: {0}", data);
            data = string.Empty;
            messageToSend = $"SEND {sourceFile}";
            await _writer.WriteLineAsync(messageToSend).ConfigureAwait(false);
            while (!data.StartsWith("exit", StringComparison.OrdinalIgnoreCase))
            {
                Task.Delay(100).Wait(); // Simulate some delay
                data = await _reader.ReadLineAsync().ConfigureAwait(false);
                Console.WriteLine("Received: {0}", data);
            }
            return;
        }
        catch (SocketException ex)
        {
            Console.WriteLine("SocketException: {0}", ex.Message);
        }
        catch (IOException ex)
        {
            Console.WriteLine("IOException: {0}", ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: {0}", ex.Message);
        }
    }
}
