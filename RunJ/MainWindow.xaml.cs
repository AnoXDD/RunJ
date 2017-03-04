using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NHotkey;
using NHotkey.Wpf;

namespace RunJ {
    public partial class MainWindow : Window {
        private static readonly PredictionArrow _predictionArrow = new PredictionArrow();
        private readonly Key _hotkey = Key.Q;

        private readonly ModifierKeys _modiferHotkeys = ModifierKeys.Alt |
                                                        ModifierKeys.Control;

        private readonly List<string> _presetCustomCommands =
            new List<string>(new[] {
                "############################################################",
                "# Use \"#\" to start a new comment line, which the program will ignore the content",
                "# Use \"!\" to start a Command to pop up a window with content followed",
                "## E.g. If you have `hello,!helloworld` in this file, put `hello` will pop up a window says helloworld",
                "# Use comma to separate a shortcut and the command them, in the format of: shortcut,command",
                "## E.g. If you have `task,taskmgr` in this file, put `task` and enter will open a task manager",
                "# Use {0}, {1}, ... , {4} to match the command",
                "## E.g. for the command `c {0} {1},cmd {0} {1}",
                "##   it will match `c 2 3` and execute `cmd 2 3",
                "## NOTE: in `shortcut` {0} must be present before {1}, {2}, ..., ",
                "##                     {1} before {2}, ..., etc. ",
                "# For advanced usage, enter `$h` to show the debug window",
                "############################################################",
                "### Broswer",
                "mail,https://mail.google.com/mail/ca",
                "cal,https://calendar.google.com/calendar/render",
                "map,https://www.google.com/maps",
                "keep,https://keep.google.com/u/0/",
                "?{0},https://www.google.com/search?q={0}",
                "### Command",
                "app,appwiz.cpl",
                "sd,shutdown -s -t 0",
                "rb,shutdown -r -t 0"
            });

        private readonly Timer _t = new Timer();
        private bool _doRefreshPrediction = true;
        private bool _isVisible = true;
        private bool _isOffline = false;
        private bool _justTabbed;
        private string[] _predictResult = {};

        private bool _shouldClose = true;

        public MainWindow() {
            InitializeComponent();

            InitializeSystemInfo();
            InitializeCommandPanel();
            InitializeHotkeyManager();

            _predictionArrow.arrow = Arrow;
        }

        /// <summary>
        ///     Initiailize the hotkey manager
        /// </summary>
        private void InitializeHotkeyManager() {
            try {
                HotkeyManager.Current.AddOrReplace("Test", _hotkey,
                    _modiferHotkeys, ToggleVisibilityHandler);
            } catch (Exception ex) {
                // Hotkey already registered
                MessageBox.Show(
                    Properties.Resources.ErrorHotkeyAlreadyRegistered);
                // Kill this program
                QuitApp();
            }
        }

        /// <summary>
        ///     Initialize the command textbox
        /// </summary>
        private void InitializeCommandPanel() {
            Command.Focus();
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo =
                FileVersionInfo.GetVersionInfo(assembly.Location);
#if DEBUG
            VersionLabel.Content = fileVersionInfo.FileVersion;
#else
            VersionLabel.Content = fileVersionInfo.ProductVersion;
#endif
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e) {
            // Add anything you need to handle here
            HideWindow();
            e.Cancel = true;
        }

        /// <summary>
        ///     Initialize the current time and date
        /// </summary>
        private void InitializeSystemInfo() {
            UpdateSystemInfo();
        }

        private void UpdateSystemInfo() {
            var timeString =
                DateTime.Now.ToString(Properties.Resources.TimeFormat);
            var dateString =
                DateTime.Now.ToString(Properties.Resources.DateFormat);
            InfoTime.Content = timeString;
            InfoDate.Content = dateString;
        }

        private void TimerClick(object sender, ElapsedEventArgs e) {
            var timeString =
                DateTime.Now.ToString(Properties.Resources.TimeFormat);
            var dateString =
                DateTime.Now.ToString(Properties.Resources.DateFormat);

            if (!Dispatcher.CheckAccess()) {
                Dispatcher.Invoke(
                    () => InfoTime.Content = timeString,
                    DispatcherPriority.Normal);
                Dispatcher.Invoke(
                    () => InfoDate.Content = dateString,
                    DispatcherPriority.Normal);
            } else {
                InfoTime.Content = timeString;
                InfoDate.Content = dateString;
            }
        }

        /// <summary>
        ///     The handler to toggle the visibility of this app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleVisibilityHandler(object sender, HotkeyEventArgs e) {
            ToggleVisibility();
        }

        /// <summary>
        ///     The function to toggle the visibility of this app
        /// </summary>
        private void ToggleVisibility() {
            if (_isVisible)
                HideWindow();
            else
                ShowWindow();
        }

        private void ShowWindow() {
            UpdateSystemInfo();
            Show();
            Activate();
            Command.Focus();
            _isVisible = true;
        }

        private void HideWindow() {
            Command.Text = "";
            Hide();
            _isVisible = false;
        }

        private void Command_TextChanged(object sender, TextChangedEventArgs e) {
            var content = Command.Text;
            Command.Opacity = Predictions.Opacity = content.Length == 0 ? 0 : 1;
            _predictionArrow.ResetArrow();
        }

        private void MainWindow_OnKeyUp(object sender, KeyEventArgs e) {
            RefreshAutocomplete();

            if ((e.Key == Key.Tab) && (_predictResult.Length != 0)) {
                var index = _predictionArrow.GetIndex();
                Command.Text = _predictResult[index == -1 ? 0 : index];
                Command.CaretIndex = Command.Text.Length;
                RefreshAutocomplete();
                _justTabbed = true;
                return;
            }

            _justTabbed = false;
        }

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e) {
            _doRefreshPrediction = false;

            if (e.Key == Key.Escape) {
                // Test if the command window is to be closed
                if (Command.Text.Length == 0)
                    HideWindow();
                else
                    Command.Text = "";
                return;
            }

            if (e.Key == Key.Enter) {
                // If ctrl+search is pressed
                if (_justTabbed || ((Keyboard.Modifiers & ModifierKeys.Control) ==
                                    ModifierKeys.Control))
                    Execute("?" + Command.Text);
                else
                    Execute(Command.Text);
                return;
            }

            if (e.Key == Key.Down) {
                _predictionArrow.IncrementPredictionIndex();
                return;
            }

            if (e.Key == Key.Up) {
                _predictionArrow.DecrementPredictionIndex();
                return;
            }

            _doRefreshPrediction = true;
        }

        /// <summary>
        ///     Refresh the autocomplete list
        /// </summary>
        private void RefreshAutocomplete() {
            if (_isOffline || !_doRefreshPrediction) {
                _doRefreshPrediction = true;
                return;
            }

            // See this one:
            // http://stackoverflow.com/questions/57615/how-to-add-a-timeout-to-console-readline

            _predictResult = FetchAutoComplete(Command.Text.TrimEnd());
            Predictions.Text = string.Join(Environment.NewLine, _predictResult);
        }

        /// <summary>
        ///     Execute this string
        ///     The first method called from app
        ///     To test this function:
        ///     1) `cmd`
        ///     2) `ipconfig -all` (command with space)
        ///     3) `something interesting` (search something with space)
        ///     4) `C:\Program Files\Git` (open a local directory)
        /// </summary>
        /// <param name="s">command of the string</param>
        private void Execute(string s) {
            _shouldClose = true;

            // Test if it's a command
            if (s.StartsWith("$")) {
                ExecuteAppCommand(s.Substring(1));
            } else {
                // Read command file 
                if (ReadAndAttemptExecuteCustomCommand(s)) {
                    HideWindow();
                    return;
                }

                // Execute system task
                try {
                    ExecuteSystemCommand(s);
                } catch (Exception ex) {
                    // Create a warning sound
                    SystemSounds.Exclamation.Play();
                    _shouldClose = false;
                }
            }

            if (_shouldClose)
                HideWindow();
            else
                Command.Text = "";
        }

        /// <summary>
        ///     Show a pop up window with the content
        /// </summary>
        /// <param name="content">The content to be popped</param>
        private void ExecutePopCommand(string content) {
            MessageBox.Show(content.Replace(@"\n", Environment.NewLine));
        }

        /// <summary>
        ///     Read and execute customized command
        /// </summary>
        /// <param name="s">The command to be indexed</param>
        /// <returns>whether a customized command is found</returns>
        private bool ReadAndAttemptExecuteCustomCommand(string s) {
            StreamReader sr;

            try {
                var fs = new FileStream(Properties.Resources.CommandFileName +
                                        Properties.Resources
                                            .CommandFileNameSuffix,
                    FileMode.Open);
                sr = new StreamReader(fs);
            } catch (IOException ex) {
                SystemSounds.Exclamation.Play();
                MessageBox.Show("Close command file map and try again");

                _shouldClose = false;
                return false;
            }

            // Read the stream
            var commentHeader = Properties.Resources.CommentHeader;
            while (!sr.EndOfStream) {
                var line = sr.ReadLine();
                if (line.StartsWith(commentHeader))
                    continue;

                var groups = line.Split(',');
                if (groups[0] == s) {
                    // Matched!
                    var index = 1;
                    while (index < groups.Length)
                        try {
                            var mappedCommand = groups[index];

                            if (mappedCommand.StartsWith("!"))
                                ExecutePopCommand(mappedCommand.Substring(1));
                            else
                                ExecuteSystemCommand(mappedCommand);

                            // Return true if nothing bad happens
                            sr.Close();
                            return true;
                        } catch (Exception ex) {
                            // Increment the index to execute the next command
                            if (++index == groups.Length)
                                SystemSounds.Exclamation.Play();
                        }
                } else {
                    var processedString = ReplaceRegexGroups(s, groups);
                    if (processedString != s) {
                        // Matched!
                        ExecuteSystemCommand(processedString);

                        sr.Close();
                        return true;
                    }
                }
            }

            sr.Close();
            return false;
        }

        /// <summary>
        ///     Fetch the autocomplete data from Google server
        /// </summary>
        /// <param name="q">the query string</param>
        /// <returns>an array of string given by Google, the first element is the original query</returns>
        private static string[] FetchAutoComplete(string q) {
            string[] result = {};

            if ((q.Length == 0) || (q[0] == '$'))
                return result;

            if (q[0] == '^')
                return calculateXOR(q.Substring(1));

            var request =
                WebRequest.Create(
                    "http://suggestqueries.google.com/complete/search?client=firefox&q=" +
                    q);
            request.Credentials = CredentialCache.DefaultCredentials;
            var response = request.GetResponse();

            if (((HttpWebResponse) response).StatusCode == HttpStatusCode.OK) {
                // Get the stream containing content returned by the server.
                var dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                var reader = new StreamReader(dataStream);
                // Read the content and unescape it
                var responseFromServer = Regex.Unescape(reader.ReadToEnd());

                reader.Close();
                response.Close();

#if DEBUG
                Console.WriteLine(responseFromServer);
#endif

                // Convert the content
                return ConvertFetchResultToArray(responseFromServer, q);
            }

            return result;
        }

        /// <summary>
        /// Calculates the XOR and returns it.
        /// The expression format follows {number}{space}{number}, e.g. 123 54
        /// </summary>
        /// <param name="exp">The expression</param>
        /// <returns></returns>
        private static string[] calculateXOR(string exp) {
            var numbers = exp.Split(' ');
            string[] result;
            if (numbers.Length < 2) {
                result = new string[0];
            } else {
                result = new string[1];
                try {
                    result[0] = (Int32.Parse(numbers[0]) ^ Int32.Parse(numbers[1])).ToString();
                } catch (Exception) {
                    result = new string[0];
                }
            }

            return result;
        }

        /// <summary>
        ///     Parse the JSON data returned by Google
        ///     Requires: q is not empty
        /// </summary>
        /// <param name="response">The response</param>
        /// <param name="q">The request string</param>
        /// <param name="keepOriginalResult">If q will be kept in the list</param>
        /// <returns>Parsed string</returns>
        public static string[] ConvertFetchResultToArray(string response,
            string q) {
            response = response.Substring(5 + q.Length,
                response.Length - q.Length - 7);
            string[] result = {};

            if (response.Length != 0) {
                result = response.Split(',');

                // Remove quote
                for (var i = 0; i != result.Length; ++i) {
                    var str = result[i];
                    result[i] = str.Substring(1, str.Length - 2);
                }

//                // Remove the element that is same as `q`
//                if (result[0] == q) {
//                    var tmp = new List<string>(result);
//                    tmp.RemoveAt(0);
//                    return tmp.ToArray();
//                }
            }

            return result;
        }

        /// <summary>
        ///     Replace {0}, {1} ,...,{5} with their corresponding parts
        /// </summary>
        /// <param name="s">command</param>
        /// <param name="groups">the group to be processed</param>
        public static string ReplaceRegexGroups(string s, string[] groups) {
            // Check the possibility of regex expression
            // First, change all {?} to `.` to match the result
            // E.g. start from groups[0]=?{0}??{1}?
            // Escape all the characters
            var regex = new Regex(@"\\{[01234]}");
            var escapedCommand = Regex.Escape(groups[0]); // \?\{0}\?\?\{1}\?
            var argsNumber = regex.Matches(escapedCommand).Count;

            if (argsNumber != 0) {
                var matchingRegex = regex.Replace(escapedCommand, "(.+)");
                // \?(.+)\?\?(.+)\?
                // Then match it to the input command
                regex = new Regex(matchingRegex);
                var match = regex.Match(s);
                var matchedGroups = match.Groups;

                // Check if the #arguments required is the same as #args provided
                if (argsNumber != matchedGroups.Count - 1)
                    return s;

                // Convert the result to a string
                var args = new string[5];

                for (var i = 1;
                    (i < matchedGroups.Count) && (i <= args.Length);
                    i++) {
                    var g = matchedGroups[i];
                    args[i - 1] = g.Captures[0].ToString();
                }

                // Use the args to get the correct command
                // Refrain from using string.Format to avoid errors while parsing strings with "{5}"
                return groups[1].Replace("{0}", args[0])
                    .Replace("{1}", args[1])
                    .Replace("{2}", args[2])
                    .Replace("{3}", args[3])
                    .Replace("{4}", args[4]);
            }

            // Nothing matched, return the original value
            return s;
        }

        /// <summary>
        ///     Executes the system command
        /// </summary>
        /// <param name="s">The command</param>
        private void ExecuteAppCommand(string s) {
            if ((s == "$") || (s == "o") || (s == "open")) {
                OpenCommandMapFile();
            } else if ((s == "h") || (s == "help")) {
                OpenHelpWindow();
            } else if ((s == "c") || (s == "create")) {
                CreateNewCommandMapFile();
            } else if ((s == "q") || (s == "quit")) {
                QuitApp();
            } else if ((s == "r") || (s == "resize")) {
                CenterToScreen();
                _shouldClose = false;
            } else if ((s == "d") || (s == "dir")) {
                OpenAppDirectory();
            } else if ((s == "t") || (s == "toggle")) {
                _isOffline = !_isOffline;
                _shouldClose = false;
            }
        }

        private void OpenAppDirectory() {
            var currentDir = Directory.GetCurrentDirectory();
            ExecuteSystemCommand(currentDir);
        }

        /// <summary>
        ///     Center the window in the screen
        /// </summary>
        private void CenterToScreen() {
            var desktopWorkingArea = SystemParameters.WorkArea;
            Top = (desktopWorkingArea.Bottom - Height) / 2;
            Left = (desktopWorkingArea.Right - Width) / 2;
        }

        private void QuitApp() {
            Application.Current.Shutdown();
        }

        /// <summary>
        ///     Create a new command map file.
        ///     This will also create a backup file
        /// </summary>
        private void CreateNewCommandMapFile() {
            CreateBackupCommandMapFile();

            var fs = new FileStream(Properties.Resources.CommandFileName +
                                    Properties.Resources.CommandFileNameSuffix,
                FileMode.CreateNew);
            var fw = new StreamWriter(fs);

            // Write custom messages here!
            foreach (var line in _presetCustomCommands)
                fw.WriteLine(line);

            fw.Close();

            _shouldClose = false;
        }

        private void CreateBackupCommandMapFile() {
            try {
                var filename = Properties.Resources.CommandFileName +
                               Properties.Resources.CommandFileNameSuffix;
                var backupFilename = Properties.Resources.CommandFileName +
                                     Properties.Resources
                                         .CommandFileNameBackupSuffix;
                // Remove the old file first
                File.Delete(backupFilename);
                File.Move(filename, backupFilename);
            } catch (FileNotFoundException) {
                // ignored
            }
        }

        private void OpenHelpWindow() {
            MessageBox.Show("$$: open command map file\n" +
                            "$c: backup and reset command map file\n" +
                            "$d: open the directory of this app\n" +
                            "$h: open help window\n" +
                            "$q: quit this app\n" +
                            "$r: resize the app to the center\n" +
                            "$t: toggle auto-prediction");
            _shouldClose = false;
        }

        private void OpenCommandMapFile() {
            var currentDir = Directory.GetCurrentDirectory();
            try {
                ExecuteSystemCommand(Path.Combine(currentDir,
                    Properties.Resources.CommandFileName +
                    Properties.Resources.CommandFileNameSuffix));
            } catch (Win32Exception) {
                // The file doesn't exist
                CreateNewCommandMapFile();
                OpenCommandMapFile();
            }
        }

        /// <summary>
        ///     Execute system command as you would in a cmd line.
        /// </summary>
        /// <param name="s">the command</param>
        private static void ExecuteSystemCommand(string s) {
            try {
                Process.Start(s);
            } catch (Win32Exception ex) {
                var command = SplitExecuteCommand(s);

                if (command.Length != 1)
                    Process.Start(command[0], command[1]);
            }
        }

        /// <summary>
        ///     Returns if this string will execute correctly in cmd, i.e. if it's a valid http or file path
        /// </summary>
        /// <param name="s">The uri</param>
        /// <returns></returns>
        private static bool IsWellFormedUriString(string s) {
            if (Uri.IsWellFormedUriString(s, UriKind.RelativeOrAbsolute))
                return true;

            try {
                var path = Path.GetPathRoot(s);
                return File.Exists(path);
            } catch (Exception) {
                return false;
            }
        }

        /// <summary>
        ///     Split a system command into two parts
        /// </summary>
        /// <param name="s">the command</param>
        /// <returns>An string array with the first element the name of the application and the second element th args</returns>
        private static string[] SplitExecuteCommand(string s) {
            return s.Split(new[] {' '}, 2);
        }

        private void Command_GotKeyboardFocus(object sender,
            KeyboardFocusChangedEventArgs e) {
            Opacity =
                Convert.ToDouble(Properties.Resources.WindowGotFocusOpacity);
            FocusIndicator.Visibility = Visibility.Visible;
        }

        private void Command_LostKeyboardFocus(object sender,
            KeyboardFocusChangedEventArgs e) {
            Opacity =
                Convert.ToDouble(Properties.Resources.WindowLostFocusOpacity);
            FocusIndicator.Visibility = Visibility.Hidden;
        }
    }
}