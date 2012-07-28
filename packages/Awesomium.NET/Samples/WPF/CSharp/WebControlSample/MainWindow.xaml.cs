using System;
using System.Windows;
using Awesomium.Core;

namespace WebControlSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            WebCore.Initialize( new WebConfig() { LogLevel = LogLevel.Verbose } );

            InitializeComponent();
        }

        private void OnShowNewView( object sender, ShowCreatedWebViewEventArgs e )
        {
            if ( !webControl.IsLive )
                return;

            // Let the new view be destroyed. It is important to set Cancel to true 
            // if you are not wrapping the new view, to avoid keeping it alive along
            // with a reference to its parent.
            e.Cancel = true;

            // Load the url to the existing view.
            webControl.LoadURL( e.TargetURL );
        }
    }
}
