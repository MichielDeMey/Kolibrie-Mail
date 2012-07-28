namespace Crystalbyte.Equinox.Pop3
{
    public sealed class StatPop3Response : Pop3Response
    {
        public int MessageCount { get; private set; }
        public double MailboxSize { get; private set; }

        internal override void ReadResponse(Pop3ResponseReader reader)
        {
            var values = reader.CurrentLine.Split(' ');
            MessageCount = int.Parse(values[1]);
            MailboxSize = double.Parse(values[2]);
            TakeSnapshot(reader);
        }
    }
}
