using System.Net;
using System.Net.Sockets;
using System.Text;
using Messages;
using MimeKit;
using Server_base;

namespace NoIPChat_mail
{
    internal class SMTPServer
    {
        private Mail mail;
        private bool active = true;
        private ConcurrentList<Task> tasks = [];
        public SMTPServer(Mail mail, List<(IPAddress, int)> interfaces)
        {
            this.mail = mail;
            foreach (var iface in interfaces)
            {
                _ = StartAsync(iface.Item1, iface.Item2);
            }
        }
        public async Task StartAsync(IPAddress IP, int Port)
        {
            var listener = new TcpListener(IP, Port);
            listener.Start();
            while (active)
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
                await writer.WriteLineAsync($"220 {mail.Name}");
                string? sender = null;
                List<string?> recipients = [];
                var data = new StringBuilder();
                string? line;
                while ((line = await reader.ReadLineAsync()) != null && active)
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
                                Message msg = new Message() { Sender = sender, Receiver = receiver, Msg = Encoding.UTF8.GetBytes(message.TextBody) };
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
        public async Task Close()
        {
            await Task.WhenAll(tasks);
            active = false;
        }
    }
}