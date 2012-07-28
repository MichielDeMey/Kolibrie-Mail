/***************************************************************************
 *  Project: WinFormsSample
 *  File:    WebForm.cs
 *  Version: 1.7.0.0
 *
 *  Copyright ©2012 Perikles C. Stephanidis; All rights reserved.
 *  This code is provided "AS IS" without warranty of any kind.
 *__________________________________________________________________________
 *
 *  Notes:
 *
 *  Demonstrates rendering an Awesomium WebView to a Windows Forms UI.
 *  In this sample, we simply render on the Form itself. In a real-life
 *  scenario, this should be a custom user control.
 *   
 ***************************************************************************/

#region Using
using System;
using System.Drawing;
using Awesomium.Core;
using System.Diagnostics;
using System.Windows.Forms;
using Awesomium.Windows.Forms;
#endregion

namespace WinFormsSample
{
    public partial class WebForm : Form
    {
        #region Fields
        private WebView webView;
        private ImageSurface surface;
        private bool needsResize;
        private WebSession session;
        #endregion


        #region Ctors
        public WebForm()
        {
            WebCore.Initialize( WebConfig.Default );
            session = WebCore.CreateWebSession( WebPreferences.Default );

            Debug.Print( WebCore.Version.ToString() );

            // Notice that 'Control.DoubleBuffered' has been set to true
            // in the designer, to prevent flickering.

            InitializeComponent();

            // Initialize the view.
            InitializeView( WebCore.CreateWebView( this.ClientSize.Width, this.ClientSize.Height, session ) );
        }

        // Used to create child (popup) windows.
        internal WebForm( WebView view, int width, int height )
        {
            this.Width = width;
            this.Height = height;

            InitializeComponent();

            // Initialize the view.
            InitializeView( view );

            // We should immediately call a resize,
            // after wrapping child views.
            if ( view != null )
                view.Resize( width, height );
        }
        #endregion


        #region Methods
        private void InitializeView( WebView view )
        {
            if ( view == null )
                return;

            // Create an image surface to render the
            // WebView's pixel buffer.
            surface = new ImageSurface();
            surface.Updated += OnSurfaceUpdated;

            webView = view;
            // Assign our surface.
            webView.Surface = surface;

            // Handle some important events.
            webView.CursorChanged += OnCursorChanged;
            webView.TitleChanged += OnTitleChanged;
            webView.DocumentReady += OnDocumentReady;
            webView.ShowCreatedWebView += OnShowNewView;
            webView.Crashed += OnCrashed;

            // Load a URL, if this is not a child view.
            if ( webView.Parent == null )
                webView.LoadHTML( "<h1>Opening a popup window...</h1>" );

            // Give focus to the view.
            webView.FocusView();
        }

        private void ResizeView()
        {
            if ( ( webView == null ) || !webView.IsLive )
                return;

            if ( needsResize )
            {
                // Request a resize.
                webView.Resize( this.ClientSize.Width, this.ClientSize.Height );
                needsResize = false;
            }
        }

        protected override void OnPaint( PaintEventArgs e )
        {
            if ( surface.Image != null )
                e.Graphics.DrawImageUnscaled( surface.Image, 0, 0 );
            else
                base.OnPaint( e );
        }

        protected override void OnActivated( EventArgs e )
        {
            base.OnActivated( e );
            this.Opacity = 1.0D;

            if ( !webView.IsLive )
                return;

            webView.FocusView();
        }

        protected override void OnDeactivate( EventArgs e )
        {
            base.OnDeactivate( e );

            if ( !webView.IsLive )
                return;

            // Let popup windows be semi-transparent,
            // when they are not active.
            if ( webView.Parent != null )
                this.Opacity = 0.8D;

            webView.UnfocusView();
        }

        protected override void OnFormClosed( FormClosedEventArgs e )
        {
            // Get if this is form hosting a child view.
            bool isChild = webView.Parent != null;

            // Destroy the WebView.
            if ( webView != null )
                webView.Dispose();

            // The surface that is currently assigned to the view,
            // does not need to be disposed. It will be disposed 
            // internally.

            base.OnFormClosed( e );

            // Shut down the WebCore last.
            if ( !isChild )
                WebCore.Shutdown();
        }

        protected override void OnResize( EventArgs e )
        {
            base.OnResize( e );

            if ( ( webView == null ) || !webView.IsLive )
                return;

            if ( this.ClientSize.Width > 0 && this.ClientSize.Height > 0 )
                needsResize = true;

            // Request resize, if needed.
            this.ResizeView();
        }

        protected override void OnKeyPress( KeyPressEventArgs e )
        {
            base.OnKeyPress( e );

            if ( !webView.IsLive )
                return;

            webView.InjectKeyboardEvent( e.GetKeyboardEvent() );
        }

        protected override void OnKeyDown( KeyEventArgs e )
        {
            base.OnKeyDown( e );

            if ( !webView.IsLive )
                return;

            webView.InjectKeyboardEvent( e.GetKeyboardEvent( WebKeyboardEventType.KeyDown ) );
        }

        protected override void OnKeyUp( KeyEventArgs e )
        {
            base.OnKeyUp( e );

            if ( !webView.IsLive )
                return;

            webView.InjectKeyboardEvent( e.GetKeyboardEvent( WebKeyboardEventType.KeyUp ) );
        }

        protected override void OnMouseDown( MouseEventArgs e )
        {
            base.OnMouseDown( e );

            if ( !webView.IsLive )
                return;

            webView.InjectMouseDown( e.Button.GetMouseButton() );
        }

        protected override void OnMouseUp( MouseEventArgs e )
        {
            base.OnMouseUp( e );

            if ( !webView.IsLive )
                return;

            webView.InjectMouseUp( e.Button.GetMouseButton() );
        }

        protected override void OnMouseMove( MouseEventArgs e )
        {
            base.OnMouseMove( e );

            if ( !webView.IsLive )
                return;

            webView.InjectMouseMove( e.X, e.Y );
        }

        protected override void OnMouseWheel( MouseEventArgs e )
        {
            base.OnMouseWheel( e );

            if ( !webView.IsLive )
                return;

            webView.InjectMouseWheel( e.Delta, 0 );
        }
        #endregion

        #region Event Handlers
        private void OnTitleChanged( object sender, TitleChangedEventArgs e )
        {
            // Reflect the page's title to the window text.
            this.Text = e.Title;
        }


        private void OnCursorChanged( object sender, CursorChangedEventArgs e )
        {
            // Update the cursor.
            this.Cursor = Awesomium.Windows.Forms.Utilities.GetCursor( e.CursorType );
        }

        private void OnSurfaceUpdated( object sender, SurfaceUpdatedEventArgs e )
        {
            // When the surface is updated, invalidate the 'dirty' region.
            // This will force the form to repaint that region.
            Invalidate( e.DirtyRegion.ToRectangle(), false );
        }

        private void OnShowNewView( object sender, ShowCreatedWebViewEventArgs e )
        {
            if ( !webView.IsLive )
                return;

            if ( e.IsPopup )
            {
                // Create a WebView wrapping the view created by Awesomium.
                WebView view = new WebView( e.NewViewInstance );
                // ShowCreatedWebViewEventArgs.InitialPos indicates screen coordinates.
                Rectangle screenRect = e.InitialPos.ToRectangle();
                // Create a new WebForm to render the new view and size it.
                WebForm childForm = new WebForm( view, screenRect.Width, screenRect.Height )
                {
                    ShowInTaskbar = false,
                    FormBorderStyle = FormBorderStyle.FixedToolWindow,
                    Size = screenRect.Size
                };

                // Show the form.
                childForm.Show( this );
                // Move it to the specified coordinates.
                childForm.DesktopLocation = screenRect.Location;
            }
            else
            {
                // Let the new view be destroyed. It is important to set Cancel to true 
                // if you are not wrapping the new view, to avoid keeping it alive along
                // with a reference to its parent.
                e.Cancel = true;

                // Load the url to the existing view.
                webView.LoadURL( e.TargetURL );
            }
        }

        private void OnDocumentReady( object sender, UrlEventArgs e )
        {
            // Make sure the view is alive.
            if ( !webView.IsLive )
                return;

            // We only want this called once.
            webView.DocumentReady -= OnDocumentReady;

            // Do not do anything for child windows.
            if ( webView.Parent != null )
                return;

            // Gets the JS window object. No explicit cast is needed here. JSValue supports
            // implicit casting.
            JSObject window = webView.ExecuteJavascriptWithResult( "window" );

            // Make sure we have the window object.
            if ( window == null )
                return;

            using ( window )
            {
                // Get the available properties.
                string[] props = window.GetPropertyNames();

                // Print them to the output.
                Debug.Print( "=================== Window Properties ===================" );
                foreach ( string prop in props )
                    Debug.Print( prop );
                Debug.Print( "===========================================================" );
                Debug.Print( "Summary: " + props.Length );
                Debug.Print( "===========================================================" );

                // Invoke 'window.open' passing some parameters and get the new window.
                // Awesomium will immediately create a new view for this window and fire
                // the 'IWebView.ShowCreatedWebView' event. See 'OnShowNewView' above.
                JSObject newWindow = window.Invoke( "open", "", "", "width=300, height=200, top=50, left=50" );

                // Make sure we have the new window.
                if ( newWindow == null )
                    return;

                using ( newWindow )
                {
                    // It should have a 'document' property. :-P
                    if ( !newWindow.HasProperty( "document" ) )
                        return;

                    try
                    {
                        // The following examples demonstrate casting to dynamic. To use this technique,
                        // you must make sure you explicitly cast the returned JSValue to JSObject.
                        // JSObject inherits DynamicObject and it's the only one supporting the DLR.
                        dynamic document = (JSObject)newWindow[ "document" ];

                        // Make sure we have the object.
                        if ( document == null )
                            return;

                        using ( document )
                            // Invoke 'write' just as you would in JS! (Note that JS is case sensitive; 'document.Write' would fail.)
                            document.write( "<h1>Hello World!</h1><p>Have a nice day!</p>" );

                        // Here we demonstrate invoking JS function objects. We create one, and get it from JS.
                        dynamic myFunction = (JSObject)webView.ExecuteJavascriptWithResult( "var myFunction = function(text) { document.write(text); }; myFunction;" );

                        // Make sure we have the object.
                        if ( myFunction == null )
                            return;

                        using ( myFunction )
                            // Invoke it directly just as you would in JS!
                            myFunction( "<h1>Successfully opened a popup window!</h1><p><button onclick='myGlobalObject.changeText( \"<b>New Content</b><p><a href=http://www.google.com>Go to: Google</a></p>\" )'>Click Me</button></p>" );

                        // Execute a Global Javascript Object sample:
                        GlobalJavascriptObjectSample();


                        // Execute a Global Javascript Object sample, remonstrating dynamic objects:
                        DynamicGlobalJavascriptObjectSample();

                        // Working with the DOM; Dynamically calling child members:
                        //dynamic doc = (JSObject)newWindow[ "document" ];
                        //string html = doc.body.innerHTML;

                        //Debug.Print( html );
                    }
                    catch ( Exception ex )
                    {
                        // Using JSObject as dynamic, can prove error prone. Do not let any exceptions
                        // be propagated to the core. This will crash the WebCore and all views.
                        // Note however that in most scenarios, while handling exceptions here will
                        // save the core, exceptions that may occur in native synchronous calls 
                        // (such the 'ExecuteJavascriptWithResult' above or 'JSObject.Invoke'),
                        // may still crash the view (not the core), even though they are handled.
                        // After a while, you may get an IWebView.Crashed event (see 'OnCrashed' below).
                        Debug.Print( String.Format( "Oops! {0}", ex.Message ) );
                    }
                }
            }
        }

        private void GlobalJavascriptObjectSample()
        {
            if ( !webView.IsLive )
                return;

            // This sample demonstrates creating and acquiring a Global Javascript object.
            // These object persist for the lifetime of the web-view.
            using ( JSObject myGlobalObject = webView.CreateGlobalJavascriptObject( "myGlobalObject" ) )
            {
                // 'Bind' is the method of the regular API, that needs to be used to create
                // a custom method on our global object and bind it to a handler.
                // The handler is of type JavascriptMethodEventHandler. Here we define it
                // using a lambda expression.
                myGlobalObject.Bind( "changeText", false, ( s, e ) =>
                {
                    // We need to call this asynchronously because the code of 'ChangeHTML'
                    // includes synchronous calls. In this case, 'ExecuteJavascriptWithResult'.
                    // Synchronous Javascript interoperation API calls, cannot be made from
                    // inside Javascript method handlers.
                    BeginInvoke( (Action<String>)ChangeHTML, (string)e.Arguments[ 0 ] );
                } );
            }
        }

        private void ChangeHTML( string newText )
        {
            if ( !webView.IsLive )
                return;

            // Get the 'document' object.
            dynamic document = (JSObject)webView.ExecuteJavascriptWithResult( "document" );

            // Make sure we have the object.
            if ( document == null )
                return;

            using ( document )
            {
                // We demonstrate setting properties on child objects directly.
                document.body.innerHTML = newText;
                // Here is an advanced scenario. Lists are supported too (NodeList in this case).
                document.title = document.getElementsByTagName( "b" )[ 0 ].innerText;
            }
        }

        private void DynamicGlobalJavascriptObjectSample()
        {
            if ( !webView.IsLive )
                return;

            // Get the global object we had previously created, and assign it to a dynamic this time.
            dynamic myGlobalObject = (JSObject)webView.ExecuteJavascriptWithResult( "myGlobalObject" );

            // Make sure we have the object.
            if ( myGlobalObject == null )
                return;

            using ( myGlobalObject )
            {
                // Create and bind a custom method dynamically. When using the dynamic model, methods
                // created are always Javascript methods with return value.
                myGlobalObject.myMethod = (JavascriptMethodEventHandler)OnCustomJavascriptMethod;

                // Our method can now be executed from Javascript. It could be Javascript already available in the page.
                // For this sample, we will inject the Javascript ourselves and execute it.
                string jsResponse = webView.ExecuteJavascriptWithResult( "myGlobalObject.myMethod( 'This is a call from Javascript.' );" );

                if ( !String.IsNullOrEmpty( jsResponse ) )
                    // Print the response.
                    Debug.Print( String.Format( "And this is the response: {0}", jsResponse ) );

                // Like with DOM objects, you can also invoke our method directly, just as you would in JS.
                string response = myGlobalObject.myMethod( "I can do this!" );

                if ( !String.IsNullOrEmpty( response ) )
                    // Print the response.
                    Debug.Print( String.Format( "And this is the response: {0}", response ) );

                // Assigning complex arrays directly.
                myGlobalObject.myFirstNumber = new object[] { 1, new int[] { 5, 6 }, 3 };
                // Assigning arrays created through Javascript.
                myGlobalObject.mySecondNumber = webView.ExecuteJavascriptWithResult( "new Array(6,7,8);" );

                // Retrieving elements and performing binary or unary operations. Note that although
                // typed arrays can be passed to remote objects, when retrieved, they will always
                // be of type: object[]. The members of the array however, will be of the initially defined type.
                object result = myGlobalObject.myFirstNumber[ 1 ][ 0 ] + myGlobalObject.mySecondNumber[ 0 ];

                // Print the result.
                Debug.Print( result.ToString() );
            }


        }

        private static void OnCustomJavascriptMethod( object sender, JavascriptMethodEventArgs e )
        {
            // We can have the same handler handling many remote methods.
            // Check here the method that is calling the handler.
            if ( String.Compare( e.MethodName, "myMethod", false ) == 0 )
            {
                // Print the text passed.
                Debug.Print( e.Arguments[ 0 ] );
                // Provide a response.
                e.Result = "Message Received!";
            }
        }

        private void OnCrashed( object sender, CrashedEventArgs e )
        {
            Debug.Print( e.Status.ToString() );
        }
        #endregion
    }
}