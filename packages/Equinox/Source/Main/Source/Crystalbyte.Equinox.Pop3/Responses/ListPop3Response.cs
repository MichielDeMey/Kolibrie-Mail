using System.Collections.Generic;

namespace Crystalbyte.Equinox.Pop3
{
    public sealed class ListPop3Response : Pop3Response
    {
        private readonly bool _isSingle;

        internal ListPop3Response(bool isSingle)
        {
            _isSingle = isSingle;
            MessageSizes = new Dictionary<int, Size>();
        }

        public IDictionary<int,Size> MessageSizes { get; private set; }

        internal override void ReadResponse(Pop3ResponseReader reader)
        {
            TakeSnapshot(reader);
            if (_isSingle) {
                if (reader.IsNegative) {
                    return;
                }
                var values = reader.CurrentLine.Split(' ');
                MessageSizes.Add(int.Parse(values[1]), Size.FromBytes(int.Parse(values[2])));
                return;
            }

            while (true) {
                reader.ReadNextLine();
                if (reader.IsCompleted) {
                    break;
                }
                var values = reader.CurrentLine.Split(' ');
                MessageSizes.Add(int.Parse(values[0]), Size.FromBytes(int.Parse(values[1])));
                
            }
        }
    }
}
