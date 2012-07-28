using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Crystalbyte.Equinox.Imap;
using Kolibrie_Mail.Protocols;

namespace Kolibrie_Mail.Converter
{
    class MessageInfoConverter: IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return String.Empty;
            }

            var mb = Imap.Client.SelectedMailbox;
            // TODO: Make this dynamic
            Imap.Client.Select("INBOX");

            var bs = (MessageInfo) value;
            var viewinfo = bs.Views.Where(x => x.MediaType == "text/plain").ToList().FirstOrDefault();

            var view = Imap.Client.FetchView(viewinfo);

            Imap.Client.Select(mb);

            if(view.Text.Length >= 100)
            {
               return view.Text.Substring(0, 100) + "..."; 
            }

            return view.Text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // TODO: Convert back (optional if not used)
            throw new NotImplementedException();
        }
    }
}
