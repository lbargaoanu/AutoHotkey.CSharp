Console.WriteLine("Started...");

SystemEvents.PowerModeChanged += OnPowerModeChanged;

Keyboard.RegisterHotkeys(new Hotkeys{
    { Keys.Control|Keys.Alt|Keys.C, _=> Process.Start(@"C:\Copy paste.txt") },
    { Keys.Control|Keys.N, _=> Process.Start(@"http://") },
    { Keys.Control|Keys.Alt|Keys.I, _=> RunGoogle() },
    { Keys.Control|Keys.Alt|Keys.M, _=> RunGoogle("site:allmusic.com") },
    { Keys.Control|Keys.Alt|Keys.V, _=> RunGoogle("site:allmovie.com") },
    { Keys.Control|Keys.Alt|Keys.D, _=> RunUrlWithSelection("dexonline.ro/definitie/{0}") },
    { Keys.Control|Keys.Alt|Keys.F, _=> RestartFirefox() },
    { Keys.Control|Keys.Alt|Keys.W, _=> Process.Start(@"http://www.meteoromania.ro/anm/?lang=ro_ro") },    
});

Keyboard.RegisterHotkey(Keys.Control|Keys.Alt|Keys.S, _=> 
{
    var result = MessageBox.Show("Sleep?", "Confirmation", MessageBoxButtons.YesNo);
    if(result == DialogResult.Yes)
    {
        Processes.CloseWindows("firefox");
        Application.SetSuspendState(PowerState.Suspend, force: false, disableWakeEvent: true);
    }
});

Run();

void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
{
  if(e.Mode == PowerModes.Resume)
  {
    RestartFirefox();
  }
  else if(e.Mode == PowerModes.Suspend)
  {
    Processes.CloseWindows("firefox");
  }
}

void RestartFirefox()
{
    Processes.CloseWindows("firefox");
    Process.Start(new ProcessStartInfo { FileName = GetBrowser(), WindowStyle = ProcessWindowStyle.Maximized });
}

void RunGoogle(string filter = null)
{
    RunUrlWithSelection("www.google.com/search?q={0} "+filter);
}

void RunUrlWithSelection(string uri)
{
    Keyboard.Send(Keys.Control|Keys.C);
    Thread.Sleep(50);
    var clipboard = Clipboard.GetText();
    var url = Uri.EscapeUriString(string.Format(uri, clipboard.Replace("&", " ")));
    //MessageBox.Show(url);
    Process.Start("http://"+url);
}

string GetBrowser()
{
    // Find the Registry key name for the default browser
    var browserKeyName = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.html\UserChoice", "Progid");
    // Find the executable command associated with the above Registry key
    var browserFullCommand = Registry.GetValue(@"HKEY_CLASSES_ROOT\"+ browserKeyName +@"\shell\open\command");
    // The above RegRead will return the path and executable name of the brower contained within quotes and optional parameters
    // We only want the text contained inside the first set of quotes which is the path and executable
    // Find the ending quote position (we know the beginning quote is in position 0 so start searching at position 1)
    var doubleQuoteIndex = browserFullCommand.IndexOf('"', 1);
    // Extract and return the path and executable of the browser
    return browserFullCommand.Substring(1, doubleQuoteIndex - 1);
}