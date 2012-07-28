namespace Kolibrie_Mail.Controller
{
    static class MailController
    {

        public static int LastUid
        {
            get { return Properties.Settings.Default.LastUid; }
            set {
                Properties.Settings.Default.LastUid = value;
                Properties.Settings.Default.Save();
            }
        }
    }
}
