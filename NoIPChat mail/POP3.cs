﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using Server_base;

namespace NoIPChat_mail
{
    internal class POP3Server
    {
        private readonly Mail mail;
        private readonly ConcurrentList<Task> tasks = [];
        private readonly DummyDictionary mailstore;
        private static readonly string[] separator = ["\r\n", "\r", "\n"];
        internal POP3Server(Mail mail, IList<Interface> interfaces)
        {
            this.mail = mail;
            mailstore = new(mail.Server);
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
        private bool Authenticate(string? user, string? passwordline)
        {
            //Placeholder
            if (mail != null && user != null && passwordline != null)
            {
                return true;
            }
            return false;
        }
        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII);
                using var writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
                await writer.WriteLineAsync("+OK SimplePOP3Server");
                string? user = null;
                bool authenticated = false;
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    Console.WriteLine($"C: {line}");
                    if (line.StartsWith("CAPA"))
                    {
                        await writer.WriteLineAsync("+OK Capability list follows");
                        await writer.WriteLineAsync("USER");
                        await writer.WriteLineAsync("TOP");
                        await writer.WriteLineAsync("UIDL");
                        await writer.WriteLineAsync("STAT");
                        await writer.WriteLineAsync(".");
                    }
                    else if (line.StartsWith("USER"))
                    {
                        user = line[5..].Trim();
                        await writer.WriteLineAsync("+OK");
                    }
                    else if (line.StartsWith("PASS"))
                    {
                        // For simplicity, assume any password is valid
                        authenticated = Authenticate(user, line);
                        if (authenticated)
                        {
                            await writer.WriteLineAsync("+OK");
                        }
                        else
                        {
                            await writer.WriteLineAsync("-ERR");
                        }
                    }
                    else if (authenticated && line.StartsWith("STAT"))
                    {
                        if (user != null && mailstore.ContainsKey(user))
                        {
                            var emails = mailstore[user];
                            int totalSize = 0;
                            foreach (var email in emails)
                            {
                                totalSize += email.ToString().Length;
                            }
                            await writer.WriteLineAsync($"+OK {emails.Count} {totalSize}");
                        }
                        else
                        {
                            await writer.WriteLineAsync("+OK 0 0");
                        }
                    }
                    else if (authenticated && line.StartsWith("LIST"))
                    {
                        if (user != null && mailstore.ContainsKey(user))
                        {
                            var emails = mailstore[user];
                            var parts = line.Split(' ');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int msgNum) && msgNum <= emails.Count)
                            {
                                var email = emails[msgNum - 1];
                                await writer.WriteLineAsync($"+OK {msgNum} {email.ToString().Length}");
                            }
                            else
                            {
                                await writer.WriteLineAsync($"+OK {emails.Count} messages");
                                for (int i = 0; i < emails.Count; i++)
                                {
                                    await writer.WriteLineAsync($"{i + 1} {emails[i].ToString().Length}");
                                }
                                await writer.WriteLineAsync(".");
                            }
                        }
                        else
                        {
                            await writer.WriteLineAsync("+OK 0 messages");
                            await writer.WriteLineAsync(".");
                        }
                    }
                    else if (authenticated && line.StartsWith("RETR"))
                    {
                        if (user != null && int.TryParse(line.AsSpan(5), out int msgNum) && mailstore.ContainsKey(user) && msgNum <= mailstore[user].Count)
                        {
                            var email = mailstore[user][msgNum - 1];
                            var emailStr = email.ToString();
                            await writer.WriteLineAsync($"+OK {emailStr.Length} octets");
                            await writer.WriteLineAsync(emailStr);
                            await writer.WriteLineAsync(".");
                        }
                        else
                        {
                            await writer.WriteLineAsync("-ERR no such message");
                        }
                    }
                    else if (authenticated && line.StartsWith("TOP"))
                    {
                        var parts = line.Split(' ');
                        if (parts.Length == 3 && int.TryParse(parts[1], out int msgNum) && int.TryParse(parts[2], out int nLines) && user != null && mailstore.ContainsKey(user) && msgNum <= mailstore[user].Count)
                        {
                            var email = mailstore[user][msgNum - 1];
                            var headerStr = new StringBuilder();
                            foreach (var header in email.Headers)
                            {
                                headerStr.AppendLine($"{header.Field}: {header.Value}");
                            }

                            var bodyLines = email.TextBody.Split(separator, StringSplitOptions.None);
                            var bodyStr = string.Join("\r\n", bodyLines.AsSpan(0, Math.Min(nLines, bodyLines.Length)).ToArray());
                            await writer.WriteLineAsync("+OK");
                            await writer.WriteLineAsync(headerStr.ToString());
                            await writer.WriteLineAsync(bodyStr);
                            await writer.WriteLineAsync(".");
                        }
                        else
                        {
                            await writer.WriteLineAsync("-ERR no such message");
                        }
                    }
                    else if (line.StartsWith("QUIT"))
                    {
                        await writer.WriteLineAsync("+OK Bye");
                        break;
                    }
                    else if (line.StartsWith("NOOP"))
                    {
                        await writer.WriteLineAsync("+OK");
                    }
                    else
                    {
                        await writer.WriteLineAsync("-ERR Command not recognized");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
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
