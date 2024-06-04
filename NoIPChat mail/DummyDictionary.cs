using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messages;
using MimeKit;
using Server_base;

namespace NoIPChat_mail
{
    /// <summary>
    /// Serves as dictionary based on data from Server.
    /// </summary>
    internal class DummyDictionary : IDictionary<string, List<MimeMessage>>
    {
        private readonly Server? Server;
        private readonly ConcurrentDictionary<string, DataHandler> server_messages;
        internal DummyDictionary(Server? server) {
            Server = server;
            if (Server != null)
            {
                server_messages = Server.messages;
            }
            else
            {
                server_messages = [];
            }
        }
        private static MimeMessage GetMime(Message message)
        {
            var email = new MimeMessage();
            if (message.Msg != null) {
                email.From.Add(new MailboxAddress("Sender", message.Sender));
                email.To.Add(new MailboxAddress("Recipient", message.Receiver));
                email.Subject = "NoIPChat email gateway";
                email.Body = new TextPart("plain") { Text = Encoding.UTF8.GetString(message.Msg) };
            }
            return email;
        }
        private async Task<List<MimeMessage>> MimeMessageList(string user)
        {
            List<MimeMessage> list = [];
            if (Server != null && server_messages.TryGetValue(user, out DataHandler? handler) && handler != null)
            {
                await foreach (Message message in handler.GetMessages())
                {
                    list.Add(GetMime(message));
                }
            }
            return list;
        }
        public List<MimeMessage> this[string key] { 
            get {
                return MimeMessageList(key).Result;
            }
            set => throw new NotImplementedException(); 
        }
        public ICollection<string> Keys => server_messages.Keys;
        public int Count => server_messages.Count;
        public bool IsReadOnly => true;
        
        public bool ContainsKey(string key)
        {
            return server_messages.ContainsKey(key);
        }
        public ICollection<List<MimeMessage>> Values => throw new NotImplementedException();
        public void CopyTo(KeyValuePair<string, List<MimeMessage>>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }
        public IEnumerator<KeyValuePair<string, List<MimeMessage>>> GetEnumerator()
        {
            throw new NotImplementedException();
        }
        public bool Remove(string key)
        {
            throw new NotImplementedException();
        }
        public bool Remove(KeyValuePair<string, List<MimeMessage>> item)
        {
            throw new NotImplementedException();
        }
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out List<MimeMessage> value)
        {
            throw new NotImplementedException();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        public bool Contains(KeyValuePair<string, List<MimeMessage>> item)
        {
            throw new NotImplementedException();
        }
        public void Add(string key, List<MimeMessage> value)
        {
            throw new NotImplementedException();
        }
        public void Add(KeyValuePair<string, List<MimeMessage>> item)
        {
            throw new NotImplementedException();
        }
        public void Clear()
        {
            throw new NotImplementedException();
        }
    }
}
