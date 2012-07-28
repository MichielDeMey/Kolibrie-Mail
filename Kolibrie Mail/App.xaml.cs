using System.Windows;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Raven.Database.Server;
using log4net;

namespace Kolibrie_Mail
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly ILog Log = LogManager.GetLogger(typeof(App));

        public static readonly EmbeddableDocumentStore DocumentStore = new EmbeddableDocumentStore {ConnectionStringName = "Local"};

        public App()
        {
            // Set up Log4Net configuration
            log4net.Config.XmlConfigurator.Configure();

            // Initialize RavenDb
            NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(8080);
            DocumentStore.UseEmbeddedHttpServer = true;

            DocumentStore.Initialize();
            Log.Info("RavenDb studio started on port 8080");

            Log.Info("Kolibrie Mail started");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            /*if(ImapController.IdleThread != null)
            {
                ImapController.IdleThread.Abort();
            }*/
        }
    }
}
