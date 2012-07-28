using System;
using System.Linq;
using Crystalbyte.Equinox;
using Crystalbyte.Equinox.Imap;

namespace Kolibrie_Mail.Model
{
    class MessageContainer
    {
        public int Uid { get; set; }
        public Envelope Envelope { get; set; }
        public Mailbox Mailbox { get; set; }
        public MessageInfo BodyStructure { get; set; }
        public Message Message { get; set; }
        public View HtmlView { get; set; }
        public View PlainView { get; set; }
        public Size Size { get; set; }

        public string Subject
        {
            get { return Envelope == null ? string.Empty : Envelope.Subject; }
        }

        public DateTime? Date
        {
            get { return Envelope != null ? Envelope.Date : null; }
        }

        public string From
        {
            get { return Envelope.From != null && Envelope.From.Count() > 0 ? Envelope.From.First().ToString() : string.Empty; }
        }

        public string FromName
        {
            get { return Envelope.From != null && Envelope.From.Count() > 0 ? Envelope.From.First().Name : string.Empty; }
        }

        public string FromAddress
        {
            get { return Envelope.From != null && Envelope.From.Count() > 0 ? Envelope.From.First().Address.FullAddress : string.Empty; }
        }

        public string To
        {
            get { return Envelope.To != null && Envelope.To.Count() > 0 ? Envelope.To.First().ToString() : string.Empty; }
        }

        public override string ToString()
        {
            return From + " " + Date + "\n" + Subject;
        }
    }
}
