﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using Messages;
using MimeKit;
using Server_base;

namespace NoIPChat_mail
{
    internal class SMTPServer
    {
        private readonly Mail mail;
        private readonly ConcurrentList<Task> tasks = [];
        internal SMTPServer(Mail mail, IList<Interface> interfaces)
        {
            this.mail = mail;
            foreach (var iface in interfaces)
            {
                if (IPAddress.TryParse(iface.InterfaceIP, out IPAddress? IP) && IP != null)
                {
                    _ = StartAsync(IP, iface.Port);
                }
            }
        }
        private async Task StartAsync(IPAddress IP, int Port)
        {
            var listener = new TcpListener(IP, Port);
            listener.Start();
            while (mail.active)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync();
                    // Start handling the client without waiting for it to complete
                    tasks.Add(Task.Run(() => HandleClientAsync(client)));
                }
                catch (Exception ex)
                {
                    mail.WriteLog(ex);
                }
            }
        }
        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII);
                using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
                await writer.WriteLineAsync($"220 {mail.Server?.name}");
                string? sender = null;
                List<string?> recipients = [];
                var data = new StringBuilder();
                string? line;
                while ((line = await reader.ReadLineAsync()) != null && mail.active)
                {
                    if (line.StartsWith("HELO", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("250 Hello");
                    }
                    else if (line.StartsWith("MAIL FROM:", StringComparison.OrdinalIgnoreCase))
                    {
                        sender = line[10..].Trim('<', '>');
                        Console.WriteLine(sender);
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
                        foreach (string? receiver in recipients)
                        {
                            if (receiver != null)
                            {
                                Message msg = new() { Sender = sender, Receiver = receiver, Msg = Encoding.UTF8.GetBytes(message.TextBody) };
                                await mail.SendMessage(receiver, msg);
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
                mail.WriteLog(ex);
            }
            finally
            {
                client.Close();
            }
        }
        internal async Task Close()
        {
            await Task.WhenAll(tasks);
        }
    }
}