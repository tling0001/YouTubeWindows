using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YouTubeWindows
{
    public struct WebView2RuntimeInfo
    {
        public string Version;
        public string Path;
    }

    public partial class MainForm : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        private string userAgent = string.Empty;
        private string lang = System.Globalization.CultureInfo.InstalledUICulture.Name;
        private readonly LaunchConfiguration launchConfiguration;
        internal bool ShowSplashScreen => launchConfiguration.ShowSplashScreen;
        public bool allowAutoHDR = false;
        public string webview2StartupArgs = "";
        WebView2RuntimeInfo? webview2RuntimeInfo = null;
        private CoreWebView2Environment coreWebView2Environment;
        public WebView2 splashScreenWebView;
        public WebView2 screenWebView;
        public Panel splashScreenWebViewPanel = new Panel();
        public Panel screenWebViewPanel = new Panel();
        private int titleHeight
        {
            get
            {
                Rectangle screenRectangle = this.RectangleToScreen(this.ClientRectangle);
                return screenRectangle.Top - this.Top;
            }
        }

        private bool _fullscreen = false;

        internal void ToggleFullscreen()
        {
            _fullscreen = !_fullscreen;
            if (_fullscreen)
            {
                FormBorderStyle = FormBorderStyle.None;
                WindowState = FormWindowState.Maximized;
            }
            else
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                WindowState = FormWindowState.Normal;
            }
        }

        private bool _cursorShown = true;

        private void SetCursorShown(bool value)
        {
            if (value == _cursorShown)
            {
                return;
            }

            if (value)
            {
                TryInvoke(() =>
                {
                    System.Windows.Forms.Cursor.Show();
                });
            }
            else
            {
                TryInvoke(() =>
                {
                    System.Windows.Forms.Cursor.Hide();
                });
            }

            _cursorShown = value;
        }

        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private static bool UseImmersiveDarkMode(IntPtr handle, bool enabled)
        {
            if (IsWindows10OrGreater(17763))
            {
                var attribute = DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;
                if (IsWindows10OrGreater(18985))
                {
                    attribute = DWMWA_USE_IMMERSIVE_DARK_MODE;
                }

                int useImmersiveDarkMode = enabled ? 1 : 0;
                return DwmSetWindowAttribute(handle, (int)attribute, ref useImmersiveDarkMode, sizeof(int)) == 0;
            }

            return false;
        }

        private static bool IsWindows10OrGreater(int build = -1)
        {
            return Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= build;
        }

        private WebView2RuntimeInfo? ReadRuntime(string path)
        {
            try
            {
                var availableBrowserVersionString = CoreWebView2Environment.GetAvailableBrowserVersionString(path);
                if (availableBrowserVersionString != null)
                {
                    WebView2RuntimeInfo info = new WebView2RuntimeInfo()
                    {
                        Version = availableBrowserVersionString,
                        Path = path
                    };
                    return info;
                }
            }
            catch { }
            return null;
        }

        public void TryInvoke(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }

        public MainForm(string[] args, LaunchConfiguration launchConfiguration = null)
        {
            this.launchConfiguration = launchConfiguration ?? LaunchConfiguration.Default;
            string systemWebViewPath = Path.Combine(Environment.SystemDirectory, "Microsoft-Edge-WebView").ToString();
            string[] runtimePaths = {
                // Fixed Version
                //AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "runtime",
                // Evergreen
                null,
                // System
                systemWebViewPath,
            };

            foreach (string runtimePath in runtimePaths)
            {
                webview2RuntimeInfo = ReadRuntime(runtimePath);
                if (webview2RuntimeInfo != null)
                {
                    break;
                }
            }

            if (webview2RuntimeInfo != null)
            {
#if DEBUG
                var availableBrowserVersionString = CoreWebView2Environment.GetAvailableBrowserVersionString();
                var runtimeInfoHeader = "";
                if (webview2RuntimeInfo.Value.Path == null)
                {
                    runtimeInfoHeader = "Evergreen Runtime";
                }
                else if (webview2RuntimeInfo.Value.Path == systemWebViewPath)
                {
                    runtimeInfoHeader = "System Runtime";
                }
                else
                {
                    runtimeInfoHeader = "Fixed Version Runtime";
                }

                MessageBox.Show("Current WebView2 Runtime:\n" + runtimeInfoHeader + "\nVersion: " + availableBrowserVersionString, this.launchConfiguration.AppTitle);
#endif
            }
            else
            {
                if (lang.StartsWith("zh-"))
                {
                    MessageBox.Show("WebView2 Runtime is missing and the app cannot run.\nYou can fix this in any of the following ways:\n\n1. Install any non-stable Microsoft Edge (Chromium) channel.\n2. Install the WebView2 Runtime Evergreen version.\n3. Place the WebView2 Runtime Fixed Version in the runtime folder for YouTube For Windows.", this.launchConfiguration.AppTitle);
                }
                else
                {
                    MessageBox.Show("The application cannot run because the WebView2 Runtime is missing.\nYou can resolve this by choosing one of the following methods:\n\n1. Install any non-stable channel version of Microsoft Edge (Chromium).\n2. Install the WebView2 Runtime Evergreen version.\n3. Place the WebView2 Runtime Fixed Version in the runtime folder of YouTube For Windows.", this.launchConfiguration.AppTitle);
                }

                Close();
                Application.Exit();
                return;
            }

            StringBuilder webview2StartupArgsBuilder = new StringBuilder();

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "--allow-auto-hdr":
                        {
                            allowAutoHDR = true;
                        }
                        break;
                    default:
                        {
                            webview2StartupArgsBuilder.Append(arg + " ");
                        }
                        break;
                }
            }

            webview2StartupArgs = webview2StartupArgsBuilder.ToString();

            InitializeComponent();

            Text = this.launchConfiguration.AppTitle;

            UseImmersiveDarkMode(this.Handle, true);

            this.Icon = Resource.icon;

            screenWebViewPanel.Dock = DockStyle.Fill;
            screenWebViewPanel.BackColor = Color.Transparent;
            splashScreenWebViewPanel.Dock = DockStyle.Fill;
            splashScreenWebViewPanel.BackColor = Color.Transparent;

            Controls.Add(splashScreenWebViewPanel); // Splash host layer (top)
            Controls.Add(screenWebViewPanel); // App host layer (bottom)

            ToggleFullscreen();
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case 0x0210 /* WM_PARENTNOTIFY */:
                    if (m.WParam == (IntPtr)0x0204 /* WM_RBUTTONDOWN */)
                    {
                        SendKeys.Send("{ESC}");
                    }
                    break;
            }

            base.WndProc(ref m);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var userDataDir = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "User Data";
            userAgent = "TV (PLATFORM_DETAILS_OTT), Cobalt/" + webview2RuntimeInfo.Value.Version + "-CloudMoe (unlike Gecko) Starboard/14, SystemIntegratorName_OTT_CloudMoeSubsystem_2026/FirmwareVersion (Windows NT " + Environment.OSVersion.Version.ToString() + ") com.google.android.youtube.tv/7.02.302";
            webview2StartupArgs = webview2StartupArgs + "--single-process --allow-failed-policy-fetch-for-test --allow-running-insecure-content --disable-web-security --disable-features=UserAgentClientHint";

            if (!allowAutoHDR)
            {
                webview2StartupArgs += " --disable_vp_auto_hdr";
            }

            var options = new CoreWebView2EnvironmentOptions(webview2StartupArgs);
            coreWebView2Environment = CoreWebView2Environment.CreateAsync(webview2RuntimeInfo.Value.Path, userDataDir, options).Result;

            splashScreenWebView = new WebView2();
            splashScreenWebView.DefaultBackgroundColor = Color.Transparent;
            screenWebView = new WebView2();
            screenWebView.DefaultBackgroundColor = Color.Transparent;

            screenWebView.Enabled = false;

            screenWebViewPanel.Visible = false;
            splashScreenWebViewPanel.Visible = false;

            screenWebViewPanel.Controls.Add(screenWebView);
            splashScreenWebViewPanel.Controls.Add(splashScreenWebView);

            BeginInvoke(new Action(() => InitializeSplashScreenAsync()));

            Task.Run(async () =>
            {
                const int stepMs = 100;
                int hideMs = 2000;
                int currentMs = 0;
                Point lastMousePos = System.Windows.Forms.Cursor.Position;
                while (true)
                {
                    await Task.Delay(stepMs);
                    int x = lastMousePos.X;
                    int y = lastMousePos.Y;
                    Point pos = System.Windows.Forms.Cursor.Position;
                    if (pos.X == x && pos.Y == y)
                    {
                        if (currentMs >= hideMs)
                        {
                            SetCursorShown(false);
                        }
                        else
                        {
                            currentMs += stepMs;
                            //Console.WriteLine("Mouse Stop: " + currentMs);
                        }
                    }
                    else
                    {
                        currentMs = 0;
                        SetCursorShown(true);
                        lastMousePos = pos;
                        //Console.WriteLine("Mouse Moved");
                    }
                }
            });
        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private async Task NativeBridgeRegister(WebView2 webView2)
        {
            webView2.CoreWebView2.AddHostObjectToScript("NativeBridge", new Bridge(this));
            // Initialize NativeBridge and hooks
            await webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(Resource.InitJs);
        }

        private async Task EnsureCoreWebView2WithRetryAsync(WebView2 webView2)
        {
            const int maxAttempts = 5;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await webView2.EnsureCoreWebView2Async(coreWebView2Environment);
                    return;
                }
                catch (COMException) when (attempt < maxAttempts)
                {
                    await Task.Delay(500 * attempt);
                }
                catch (InvalidOperationException) when (attempt < maxAttempts)
                {
                    await Task.Delay(500 * attempt);
                }
            }

            await webView2.EnsureCoreWebView2Async(coreWebView2Environment);
        }

        private async void InitializeSplashScreenAsync()
        {
            try
            {
                if (!launchConfiguration.ShowSplashScreen)
                {
                    await InitializeMainAppAsync();
                    return;
                }

                splashScreenWebView.Dock = DockStyle.Fill;
                await EnsureCoreWebView2WithRetryAsync(splashScreenWebView);
                await splashScreenWebView.ExecuteScriptAsync("document.body.style.backgroundColor = '#181818'");
                await NativeBridgeRegister(splashScreenWebView);
                _ = splashScreenWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setEmitTouchEventsForMouse", "{\"enabled\": true}");
                splashScreenWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                splashScreenWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                splashScreenWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                splashScreenWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                await InitializeMainAppAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("WebView2 initialization failed:\n" + ex.Message, "YouTube");
                Close();
            }
        }

        private async Task InitializeMainAppAsync()
        {
            try
            {
                await EnsureCoreWebView2WithRetryAsync(screenWebView);
                screenWebView.CoreWebView2.Settings.UserAgent = userAgent;
                await NativeBridgeRegister(screenWebView);
                _ = screenWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Emulation.setEmitTouchEventsForMouse", "{\"enabled\": true}");
                screenWebView.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
                screenWebView.CoreWebView2.AddWebResourceRequestedFilter("https://www.gstatic.com/ytlr/txt/licenses_*", CoreWebView2WebResourceContext.All);
                screenWebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
                screenWebView.CoreWebView2.WindowCloseRequested += CoreWebView2_WindowCloseRequested;
                screenWebView.CoreWebView2.PermissionRequested += CoreWebView2_PermissionRequested;
                screenWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                screenWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                screenWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                screenWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
#if DEBUG
                screenWebView.CoreWebView2.OpenDevToolsWindow();
#endif
                ReloadApp();
            }
            catch (Exception ex)
            {
                MessageBox.Show("WebView2 initialization failed:\n" + ex.Message, "YouTube");
                Close();
            }
        }

        private void CoreWebView2_WindowCloseRequested(object sender, object e)
        {
            Close();
        }

        private void CoreWebView2_PermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            e.State = CoreWebView2PermissionState.Allow;
            e.Handled = true;
        }

        private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
        {
            Console.WriteLine(e.Request.Uri);
            if (e.Request.Uri.StartsWith("https://www.gstatic.com/ytlr/txt/licenses_"))
            {
                var stream = GenerateStreamFromString(
                    Resource.Staff
                    .Replace("\n", "\n\u200B")
                    .Replace("<--%WEBVIEW_VERSION%-->", webview2RuntimeInfo.Value.Version)
                    .Replace("<--%PROGRAM_VERSION%-->", Version.Parse(Application.ProductVersion).ToString(3)));
                e.Response = coreWebView2Environment.CreateWebResourceResponse(stream, 200, "OK", "Access-Control-Allow-Origin: *\r\nContent-Type: text/html");
                new Thread(() =>
                {
                    Thread.Sleep(3000); // Release the stream resource after 3000 ms
                    var action = new Action(() =>
                    {
                        stream.Close();
                    });

                    TryInvoke(action);
                }).Start();
            }
        }

        public void ReloadApp()
        {
            var splashScreenHtml = launchConfiguration.LoadSplashScreenHtml();
            if (!launchConfiguration.ShowSplashScreen)
            {
                screenWebViewPanel.Visible = true;
                splashScreenWebViewPanel.Visible = false;
                screenWebView.Enabled = true;
                screenWebView.Dock = DockStyle.Fill;
            }
            else if (!string.IsNullOrWhiteSpace(splashScreenHtml) && splashScreenWebView != null && splashScreenWebView.CoreWebView2 != null)
            {
                splashScreenWebView.CoreWebView2.NavigateToString(splashScreenHtml);
                splashScreenWebViewPanel.Visible = true;
                screenWebViewPanel.Visible = false;
            }
            else
            {
                splashScreenWebViewPanel.Visible = false;
                screenWebViewPanel.Visible = true;
            }

            screenWebView.CoreWebView2.Navigate(launchConfiguration.StartUrl);
        }

        private void CoreWebView2_DOMContentLoaded(object sender, CoreWebView2DOMContentLoadedEventArgs e)
        {
            if (screenWebView.Source.ToString().StartsWith("https://www.youtube.com"))
            {
                // Background playback
                screenWebView.ExecuteScriptAsync("for (event_name of ['visibilitychange', 'webkitvisibilitychange', 'blur']) { window.addEventListener(event_name, function(event) { event.stopImmediatePropagation(); }, true); }");
                if (launchConfiguration.ShowSplashScreen)
                {
                    // Inject animation
                    screenWebView.ExecuteScriptAsync("document.body.style.opacity = 0; document.body.style.transition = 'opacity 333ms';");
                }
                else
                {
                    screenWebView.ExecuteScriptAsync("document.body.style.opacity = 1; document.body.style.transition = 'none';");
                }
                // Spoof device model
                screenWebView.ExecuteScriptAsync("window.environment.brand = \"Google\";");
                screenWebView.ExecuteScriptAsync("window.environment.model = \"GoogleTV\";");
                // Override feature flags
                screenWebView.ExecuteScriptAsync("window.environment.has_touch_support = true;");
                screenWebView.ExecuteScriptAsync("window.environment.feature_switches.disable_client_side_app_quality_logic = false;");
                string deviceName = "YouTube on Windows";
                if (!String.IsNullOrEmpty(System.Environment.MachineName))
                {
                    deviceName += $" ({System.Environment.MachineName})";
                }
                screenWebView.ExecuteScriptAsync("window.environment.feature_switches.mdx_device_label = \"" + deviceName + "\";");
            }
            else
            {
                screenWebView.Dock = DockStyle.Fill;
            }
        }

        private void MainForm_Activated(object sender, EventArgs e)
        {
            if (screenWebView != null && screenWebView.IsHandleCreated)
            {
                screenWebView.Focus();
            }
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
        }

        private void MainForm_ResizeBegin(object sender, EventArgs e)
        {
            this.SuspendLayout();
        }

        private void MainForm_ResizeEnd(object sender, EventArgs e)
        {
            this.ResumeLayout();
        }
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class BridgeAnotherClass
    {
        // Sample property.
        public string Prop { get; set; } = "Example";
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class Bridge
    {
        private MainForm ctxMainForm;

        public Bridge(MainForm mainForm)
        {
            ctxMainForm = mainForm;
        }

        public string Func(string param)
        {
            Console.WriteLine(param);
            return "Example: " + param;
        }

        public void Close()
        {
            ctxMainForm.Close();
        }

        public void ReloadApp()
        {
            ctxMainForm.ReloadApp();
        }

        public void ToggleFullscreen()
        {
            ctxMainForm.ToggleFullscreen();
        }

        public void ConsoleWriteLine(string content)
        {
            Console.WriteLine(content);
        }

        public void HideSplashScreen()
        {
            if (!ctxMainForm.ShowSplashScreen || ctxMainForm.splashScreenWebView == null)
            {
                ctxMainForm.TryInvoke(() =>
                {
                    ctxMainForm.screenWebViewPanel.Visible = true;
                    ctxMainForm.screenWebView.Enabled = true;
                    ctxMainForm.screenWebView.ExecuteScriptAsync("document.body.style.opacity = 1; document.body.style.transition = 'none';");
                    if (ctxMainForm.Focused)
                    {
                        ctxMainForm.screenWebView.Focus();
                    }
                });

                return;
            }

            new Thread(() =>
            {
                var action1 = new Action(() =>
                {
                    ctxMainForm.screenWebView.Dock = DockStyle.Fill;
                });

                ctxMainForm.TryInvoke(action1);

                Thread.Sleep(3000);

                var action2 = new Action(() =>
                {
                    ctxMainForm.splashScreenWebView.ExecuteScriptAsync("document.getElementById('background').style.opacity = 0;");
                    ctxMainForm.screenWebViewPanel.Visible = true;
                });

                ctxMainForm.TryInvoke(action2);

                Thread.Sleep(500);

                var action3 = new Action(() =>
                {
                    ctxMainForm.splashScreenWebViewPanel.Visible = false;
                    ctxMainForm.splashScreenWebView.CoreWebView2.Navigate("about:blank");
                    ctxMainForm.splashScreenWebView.ExecuteScriptAsync("document.body.style.backgroundColor = '#181818'");
                    ctxMainForm.screenWebView.Enabled = true;
                    ctxMainForm.screenWebView.ExecuteScriptAsync("document.body.style.opacity = 1;");
                    if (ctxMainForm.Focused)
                    {
                        ctxMainForm.screenWebView.Focus();
                    }
                });

                ctxMainForm.TryInvoke(action3);
            }).Start();
        }

        public BridgeAnotherClass AnotherObject { get; set; } = new BridgeAnotherClass();

        // Sample indexed property.
        [System.Runtime.CompilerServices.IndexerName("Items")]
        public string this[int index]
        {
            get { return m_dictionary[index]; }
            set { m_dictionary[index] = value; }
        }
        private Dictionary<int, string> m_dictionary = new Dictionary<int, string>();
    }
}
