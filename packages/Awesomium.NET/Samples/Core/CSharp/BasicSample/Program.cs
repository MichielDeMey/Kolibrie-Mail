using System;
using Awesomium.Core;
using System.Threading;
using System.Diagnostics;

namespace BasicSample
{
    class Program
    {
        static void Main( string[] args )
        {
            // Initialize the WebCore with default confiuration settings.
            WebCore.Initialize( new WebConfig() { LogPath = Environment.CurrentDirectory, LogLevel = LogLevel.Verbose } );

            // We demonstrate an easy way to hide the scrollbars by providing
            // custom CSS. Read more about how to style the scrollbars here:
            // http://www.webkit.org/blog/363/styling-scrollbars/.
            // Just consider that this setting is WebSession-wide. If you want to apply
            // a similar effect for single pages, you can use ExecuteJavascript
            // and pass: document.documentElement.style.overflow = 'hidden';
            // (Unfortunately WebKit's scrollbar does not have a DOM equivalent yet)
            using ( WebSession session = WebCore.CreateWebSession( new WebPreferences() { CustomCSS = "::-webkit-scrollbar { visibility: hidden; }" } ) )
            {
                // WebView implements IDisposable. Here we demonstrate
                // wrapping it in a using statement.
                using ( WebView view = WebCore.CreateWebView( 1280, 960, session ) )
                {
                    bool finishedLoading = false;

                    Console.WriteLine( "Loading: http://www.awesomium.com ..." );

                    view.LoadURL( new Uri( "http://www.awesomium.com" ) );
                    view.LoadingFrameComplete += ( s, e ) =>
                    {
                        Console.WriteLine( String.Format( "Frame Loaded: {0}", e.FrameID ) );

                        // The main frame always finishes loading last for a given page load.
                        if ( e.IsMainFrame )
                            finishedLoading = true;
                    };

                    while ( !finishedLoading )
                    {
                        Thread.Sleep( 100 );
                        // A Console application does not have a synchronization
                        // context, thus auto-update won't be enabled on WebCore.
                        // We need to manually call Update here.
                        WebCore.Update();
                    }

                    // Print some more information.
                    Console.WriteLine( String.Format( "Page Title: {0}", view.Title ) );
                    Console.WriteLine( String.Format( "Loaded URL: {0}", view.Source ) );

                    // A BitmapSurface is assigned by default to all WebViews.
                    BitmapSurface surface = (BitmapSurface)view.Surface;
                    // Save the buffer to a PNG image.
                    surface.SaveToPNG( "result.png", true );

                } // Destroy and dispose the view.
            } // Release and dispose the session.

            // Announce.
            Console.Write( "Hit any key to see the result..." );
            Console.ReadKey( true );

            // Start the application associated with .png files
            // and display the file.
            Process.Start( "result.png" );

            // Shut down Awesomium before exiting.
            WebCore.Shutdown();
        }
    }
}
