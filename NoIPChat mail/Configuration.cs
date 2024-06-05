using System.Xml.Serialization;

namespace NoIPChat_mail
{
    [Serializable]
    public class Interface
    {
        public required string InterfaceIP { get; set; }
        public required int Port { get; set; }
    }

    [Serializable]
    [XmlRoot("Configuration")]
    public class Configuration
    {
        public required List<Interface> SMTP { get; set; }
        public required List<Interface> POP3 { get; set; }
        public string? Logfile { get; set; }
    }
}
