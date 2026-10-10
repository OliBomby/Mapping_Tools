using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

internal static class Client
{
    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: Client.exe <osu! process id> probe|reload|status|capture|clear|stop|circle <x> <y> <radius>|move <x> <y>");
            return 1;
        }
        try
        {
            string[] endpoint = File.ReadAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "endpoint-" + int.Parse(args[0]) + ".txt"));
            using (var client = new TcpClient())
            {
                client.ReceiveTimeout = 15000;
                client.SendTimeout = 5000;
                client.Connect(IPAddress.Loopback, int.Parse(endpoint[0]));
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream))
                using (var writer = new StreamWriter(stream) { AutoFlush = true })
                {
                    writer.WriteLine(endpoint[1] + " " + string.Join(" ", args, 1, args.Length - 1));
                    string result = reader.ReadLine();
                    Console.WriteLine(result);
                    return result != null && result.StartsWith("OK ") ? 0 : 2;
                }
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 3;
        }
    }
}
