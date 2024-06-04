using NoIPChat_mail;
namespace Test
{
    public class Program()
    {
        public static void Main()
        {
            SMTPServer server = new();
            server.Initialize();
            Console.ReadLine();
        }
    }

}
