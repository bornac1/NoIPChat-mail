﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using Messages;
using MimeKit;
using Server_base;

namespace NoIPChat_mail
{
    public class SMTPServer : IPlugin
    {
        public Server? Server { get; set; }
        private int Port;
        private string Name;
        public void Initialize()
        {
            if (Server != null)
            {
                Name = Server.name;
                Port = 25;
                _ = StartAsync();
            }
        }
        public SMTPServer(string name, int port = 25)
        {
            Name = name;
            Port = port;
        }
        public async Task StartAsync()
        {
            var listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();

            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);
            }
        }
        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII);
                using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };

                await writer.WriteLineAsync($"220 {Name}");

                string? sender = null;
                List<string?> recipients = [null];
                var data = new StringBuilder();
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("250 Hello");
                    }
                    else if (line.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase))
                    {
                        sender = line[10..].Trim('<', '>');
                        await writer.WriteLineAsync("250 OK");
                    }
                    else if (line.StartsWith("RCPT TO:", StringComparison.OrdinalIgnoreCase))
                    {
                        recipients.Add(line[8..].Trim('<', '>'));
                        await writer.WriteLineAsync("250 OK");
                    }
                    else if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("354 Start mail input; end with <CRLF>.<CRLF>");
                        while ((line = await reader.ReadLineAsync()) != null && line != ".")
                        {
                            data.AppendLine(line);
                        }
                        await writer.WriteLineAsync("250 OK");

                        var message = MimeMessage.Load(new MemoryStream(Encoding.ASCII.GetBytes(data.ToString())));
                        if (Server != null)
                        {
                            foreach (string? receiver in recipients)
                            {
                                if (receiver != null)
                                {
                                    if (MemoryExtensions.Equals(StringProcessing.GetServer(receiver), Name, StringComparison.OrdinalIgnoreCase))
                                    {
                                        await Server.SendMessageThisServer(receiver, new Message() { Sender = sender, Receiver = receiver, Msg = Encoding.UTF8.GetBytes(message.TextBody) });
                                    }
                                    else
                                    {
                                        await Server.SendMessageOtherServer(receiver, new Message() { Sender = sender, Receiver = receiver, Msg = Encoding.UTF8.GetBytes(message.TextBody) });
                                    }
                                }
                            }
                        }

                    }
                    else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("221 Bye");
                        break;
                    }
                    else
                    {
                        await writer.WriteLineAsync("500 Command not recognized");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog(ex);
            }
            finally
            {
                client.Close();
            }
        }
        public void WriteLog(Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}
