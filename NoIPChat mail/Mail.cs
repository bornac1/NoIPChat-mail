using System.Xml.Serialization;
using Messages;
using Server_base;

namespace NoIPChat_mail
{
    public class Mail : IPlugin
    {
        public Server? Server { get; set; }
        private SMTPServer? SMTP;
        private POP3Server? POP3;
        internal bool active = true;
        private string logfile = "Mail.log";
        public void Initialize()
        {
            if (Server != null)
            {
                using TextReader reader = new StreamReader("Config.xml");
                XmlSerializer serializer = new(typeof(Configuration));
                Configuration? config = (Configuration?)serializer.Deserialize(reader);
                if (config != null)
                {
                    SMTP = new SMTPServer(this, config.SMTP);
                    POP3 = new POP3Server(this, config.POP3);
                    if (!string.IsNullOrEmpty(config.Logfile))
                    {
                        logfile = config.Logfile;
                    }
                }
            }
        }
        public void WriteLog(Exception ex)
        {
            string log = DateTime.Now.ToString("d.M.yyyy. H:m:s") + " " + ex.ToString() + Environment.NewLine;
            try
            {
                System.IO.File.AppendAllText(logfile, log);
            }
            catch (Exception ex1)
            {
                Console.WriteLine($"Plugin NoIPChat mail can't save log to file {logfile}.");
                Console.WriteLine(log);
                Console.WriteLine(ex1.ToString());
            }
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
