using System.Net;
using Messages;
using Server_base;
using System.Collections.Immutable;

namespace NoIPChat_mail
{
    public class Mail : IPlugin
    {
        public Server? Server { get; set; }
        private SMTPServer? SMTP;
        private POP3Server? POP3;
        internal bool active = true;
        private readonly ImmutableList<(IPAddress, int)> SMTPinterfaces = [];
        private readonly ImmutableList<(IPAddress, int)> POP3interfaces = [];
        public void Initialize()
        {
            if (Server != null)
            {
                //Just example that listens on all IPs
                SMTPinterfaces.Add((IPAddress.Any, 25));
                POP3interfaces.Add((IPAddress.Any, 110));
                SMTP = new SMTPServer(this, SMTPinterfaces);
                POP3 = new POP3Server(this, POP3interfaces);
            }
        }
        public void WriteLog(Exception ex)
        {
            //Write to the same log as Server
            Server?.WriteLog(ex);
        }
        public void Close()
        {
            Task? smtp = SMTP?.Close();
            Task? pop3 = POP3?.Close();
            if (smtp != null && pop3 != null)
            {
                Task.WhenAll(smtp, pop3).Wait();
            }
        }
        internal async Task SendMessage(string user, Message message)
        {
            if (Server != null && message.Receiver != null)
            {
                if (MemoryExtensions.Equals(StringProcessing.GetServer(user), Server.name, StringComparison.OrdinalIgnoreCase))
                {
                    await Server.SendMessageThisServer(user, message);
                }
                else
                {
                    await Server.SendMessageOtherServer(user, message);
                }
            }
        }
    }
}
