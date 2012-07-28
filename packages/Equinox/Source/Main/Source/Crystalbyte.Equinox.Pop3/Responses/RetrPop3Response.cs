using System.IO;
using Crystalbyte.Equinox.Mime;

namespace Crystalbyte.Equinox.Pop3
{
    public sealed class RetrPop3Response : Pop3Response
    {
        public Message Message { get; private set; }

        internal override void ReadResponse(Pop3ResponseReader reader)
        {
            TakeSnapshot(reader);

            if (reader.IsNegative) {
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

                var entity = new Entity();
                entity.Deserialize(writer.ToString());
                Message = entity.ToMessage();
            }
        }
    }
}
