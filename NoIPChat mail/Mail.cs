using System.Net;
using Messages;
using Server_base;

namespace NoIPChat_mail
{
    public class Mail : IPlugin
    {
        public Server? Server { get; set; }
        private SMTPServer? SMTP;
        private POP3Server? POP3;
        List<(IPAddress, int)> interfaces = [];
        public void Initialize()
        {
            if (Server != null)
            {
                //Just example that listens on all IPs
                interfaces.Add((IPAddress.Any, 25));
                SMTP = new SMTPServer(this, interfaces);
                //TODO: POP3 initialization
            }
        }
        public void WriteLog(Exception ex)
        {
            //Write to the same log as Server
            Server?.WriteLog(ex);
        }
        public void Close()
        {
            SMTP?.Close().Wait();
            //TODO: POP3 close
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
