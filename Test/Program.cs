using NoIPChat_mail;
namespace Test
{
    public class Program()
    {
        public static async Task Main()
        {
            SMTPServer server = new("test", 25);
            await server.StartAsync();
        }
    }

}
