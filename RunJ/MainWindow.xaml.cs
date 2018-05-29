using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using NHotkey;
using NHotkey.Wpf;
using Expression = NCalc.Expression;

namespace RunJ {
    public partial class MainWindow : Window {
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
        private bool _isVisible = true;
        private bool _shouldClose = true;

        private readonly Dictionary<string, string> _customCommand = new Dictionary<string, string>();

        public MainWindow() {
            InitializeComponent();

            InitializeSystemInfo();
            InitializeCommandPanel();
            InitializeHotkeyManager();
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
            UpdateCustomCommand();
        }

        private void UpdateSystemInfo() {
            var timeString =
                DateTime.Now.ToString(Properties.Resources.TimeFormat);
            var dateString =
                DateTime.Now.ToString(Properties.Resources.DateFormat);
            InfoTime.Content = timeString;
            InfoDate.Content = dateString;
        }

        private void UpdateCustomCommand() {
            StreamReader sr;

            try {
                var fs = new FileStream(Properties.Resources.CommandFileName +
                                        Properties.Resources
                                            .CommandFileNameSuffix,
                    FileMode.Open);
                sr = new StreamReader(fs);
            } catch (IOException ex) {
                SystemSounds.Exclamation.Play();
                MessageBox.Show("Unable to load custom command file. Type `$c` to generate a new one.\n\n" +
                                ex.ToString());
                return;
            }

            _customCommand.Clear();
            // Read the stream
            var commentHeader = Properties.Resources.CommentHeader;
            while (!sr.EndOfStream) {
                var line = sr.ReadLine();
                if (line == null || line.StartsWith(commentHeader))
                    continue;

                var groups = line.Split(",".ToCharArray(), 2);
                _customCommand.Add(groups[0], groups[1]);
            }

            sr.Close();
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
            UpdateCustomCommand();
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
            Command.Opacity = content.Length == 0 ? 0 : 1;
        }

        private void MainWindow_OnKeyUp(object sender, KeyEventArgs e) {
            RefreshSuggestionResult();
        }

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Escape:
                    // Test if the command window is to be closed
                    if (Command.Text.Length == 0)
                        HideWindow();
                    else
                        Command.Text = "";
                    return;
                case Key.Enter:
                    Execute(Command.Text);
                    return;
            }
        }

        private void RefreshSuggestionResult() {
            Suggestion.Content = "";

            var commands = ReadCustomCommand(Command.Text);
            if (commands != null) {
                Suggestion.Content = commands;
            }

            var result = Calculate(Command.Text);
            if (result == null) {
                if (Command.Text == "$") {
                    Suggestion.Content = "$? for help";
                }
            } else {
                Suggestion.Content = "=" + result;
            }
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
                } catch (Exception ) {
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

        private string ReadCustomCommand(string s) {
            if (_customCommand.TryGetValue(s, out string commands)) {
                return commands;
            }

            foreach (var entry in _customCommand) {
                var parsedCommand = ReplaceRegexGroups(s, entry.Key, entry.Value);
                if (parsedCommand == s) {
                    continue;
                }

                return parsedCommand;
            }

            return null;
        }

        /// <summary>
        ///     Read and execute customized command
        /// </summary>
        /// <param name="s">The command to be indexed</param>
        /// <returns>whether a customized command is found</returns>
        private bool ReadAndAttemptExecuteCustomCommand(string s) {
            var commands = ReadCustomCommand(s);
            if (commands == null) {
                return false; 
            }

            foreach (var command in commands.Split(',')) {
                try {
                    if (command.StartsWith("!"))
                        ExecutePopCommand(command.Substring(1));
                    else
                        ExecuteSystemCommand(command);

                    // Return true if nothing bad happens
                    return true;
                } catch (Exception) {
                    // Just consume it, and go to next command
                }
            }

            return false;
        }

        /// <summary>
        ///     Trys to calculate the result.
        /// </summary>
        /// <param name="exp">The expression</param>
        /// <returns></returns>
        private static string Calculate(string exp) {
            try {
                var e = new Expression(exp);
                return e.Evaluate().ToString();
            } catch (Exception) {
                return null;
            }
        }

        /// <summary>
        ///     Replace {0}, {1} ,...,{5} with their corresponding parts
        /// </summary>
        /// <param name="s">command</param>
        /// <param name="rawCommand">The command to be matched</param>
        /// <param name="rawResult">The matched result to be executed</param>
        public static string ReplaceRegexGroups(string s, string rawCommand, string rawResult) {
            // Check the possibility of regex expression
            // First, change all {?} to `.` to match the result
            // E.g. start from groups[0]=?{0}??{1}?
            // Escape all the characters
            var regex = new Regex(@"\\{[01234]}");
            var escapedCommand = Regex.Escape(rawCommand); // \?\{0}\?\?\{1}\?
            var argsNumber = regex.Matches(escapedCommand).Count;

            if (argsNumber == 0) {
                // Nothing matched, return the original value
                return s;
            }

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
                i < matchedGroups.Count && i <= args.Length;
                i++) {
                var g = matchedGroups[i];
                args[i - 1] = g.Captures[0].ToString();
            }

            // Use the args to get the correct command
            // Refrain from using string.Format to avoid errors while parsing strings with "{5}"
            return rawResult.Replace("{0}", args[0])
                .Replace("{1}", args[1])
                .Replace("{2}", args[2])
                .Replace("{3}", args[3])
                .Replace("{4}", args[4]);
        }

        /// <summary>
        ///     Executes the system command
        /// </summary>
        /// <param name="s">The command</param>
        private void ExecuteAppCommand(string s) {
            if (s == "$" || s == "o" || s == "open") {
                OpenCommandMapFile();
            } else if (s == "?" || s == "h" || s == "help") {
                OpenHelpWindow();
            } else if (s == "c" || s == "create") {
                CreateNewCommandMapFile();
            } else if (s == "q" || s == "quit") {
                QuitApp();
            } else if (s == "r" || s == "resize") {
                CenterToScreen();
                _shouldClose = false;
            } else if (s == "d" || s == "dir") {
                OpenAppDirectory();
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
                            "$r: resize the app to the center\n"
            );
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
        }

        private void Command_LostKeyboardFocus(object sender,
            KeyboardFocusChangedEventArgs e) {
            Opacity =
                Convert.ToDouble(Properties.Resources.WindowLostFocusOpacity);
        }
    }
}