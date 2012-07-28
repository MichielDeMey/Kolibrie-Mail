using System.Collections.Generic;
using System.IO;
using Crystalbyte.Equinox.Mime;
using System;
using Crystalbyte.Equinox.Text;

namespace Crystalbyte.Equinox.Pop3
{
    public sealed class TopPop3Response : Pop3Response
    {
        private readonly List<HeaderField> _headerFields;

        public TopPop3Response()
        {
            _headerFields = new List<HeaderField>();
        }

        public IEnumerable<HeaderField> Headers { get { return _headerFields; } }
        public string Lines { get; private set; }

        internal override void ReadResponse(Pop3ResponseReader reader)
        {
            TakeSnapshot(reader);

            if (IsNegative) {
                return;
            }

            using (var writer = new StringWriter()) {
                while (true) {
                    reader.ReadNextLine();
                    if (reader.IsCompleted) {
                        break;
                    }

                    writer.WriteLine(reader.CurrentLine.StartsWith("..")
                     ? reader.CurrentLine.Substring(1)
                     : reader.CurrentLine);
                }

                
                var text = writer.ToString();
                var index = text.IndexOf(Strings.CrLf + Strings.CrLf);

                var entity = new Entity();
                entity.Deserialize(text.Substring(0, index));

                _headerFields.AddRange(entity.Headers);
                Lines = text.Substring(index).Trim();
            }
        }
    }
}
