using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

namespace RunJ {
    public partial class MainWindow : Window {
        private readonly Key _hotkey = Key.Q;
        private readonly ModifierKeys _modiferHotkeys = ModifierKeys.Alt | ModifierKeys.Control;

        private readonly List<string> _presetCustomCommands = new List<string>(new[] {
            "############################################################",
            "# Use \"#\" to start a new command line",
            "# Use \"!\" to start a Command to pop up a window with content followed",
            "# Use {0}, {1}, ... , {4} to match the command",
            "## E.g. for the command `c {0} {1},cmd {0} {1}",
            "##   it will match `c 2 3` and execute `cmd 2 3",
            "## NOTE: in `shortcut` {0} must be before {1}, {2}, ..., ",
            "##                     {1} before {2}, ..., etc. ",
            "# otherwise use comma to separate them, in the format of: shortcut,command",
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
            "rb,shutdown -r -t 0",
            "### Apps",
            "q,D:\\runandhide.exe"
        });

        private readonly Timer _t = new Timer();
        private bool _isVisible = true;

        private bool _shouldClose = true;

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
                HotkeyManager.Current.AddOrReplace("Test", _hotkey, _modiferHotkeys, ToggleVisibilityHandler);
            } catch (Exception ex) {
                // Hotkey already registered
                MessageBox.Show(Properties.Resources.ErrorHotkeyAlreadyRegistered);
            }
        }

        /// <summary>
        ///     Initialize the command textbox
        /// </summary>
        private void InitializeCommandPanel() {
            Command.Focus();
            var assembly = Assembly.GetExecutingAssembly();
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
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

            return;

            _t.Interval = 1000;
            _t.Elapsed += TimerClick;
            _t.Start();
        }

        private void UpdateSystemInfo() {
            var timeString = DateTime.Now.ToString(Properties.Resources.TimeFormat);
            var dateString = DateTime.Now.ToString(Properties.Resources.DateFormat);
            InfoTime.Content = timeString;
            InfoDate.Content = dateString;
        }

        private void TimerClick(object sender, ElapsedEventArgs e) {
            var timeString = DateTime.Now.ToString(Properties.Resources.TimeFormat);
            var dateString = DateTime.Now.ToString(Properties.Resources.DateFormat);

            if (!Dispatcher.CheckAccess()) {
                Dispatcher.Invoke(
                    () => InfoTime.Content = timeString, DispatcherPriority.Normal);
                Dispatcher.Invoke(
                    () => InfoDate.Content = dateString, DispatcherPriority.Normal);
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
            if (_isVisible) {
                HideWindow();
            } else {
                ShowWindow();
            }
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
            Command.Opacity = content.Length == 0 ? 0 : 1;
        }

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                // Test if the command window is to be closed
                if (Command.Text.Length == 0) {
                    // Close the window
                    HideWindow();
                } else {
                    Command.Text = "";
                }
            } else if (e.Key == Key.Enter) {
                Execute(Command.Text);
            }
        }

        /// <summary>
        ///     Execute this string
        ///     The first method called from app
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

            if (_shouldClose) {
                HideWindow();
            } else {
                Command.Text = "";
            }
        }

        /// <summary>
        /// Show a pop up window with the content
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
                                               Properties.Resources.CommandFileNameSuffix, FileMode.Open);
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
                if (line.StartsWith(commentHeader)) {
                    continue;
                }

                var groups = line.Split(',');
                if (groups[0] == s) {
                    // Matched!
                    try {
                        var mappedCommand = groups[1];

                        if (mappedCommand.StartsWith("!"))
                            ExecutePopCommand(mappedCommand.Substring(1));
                        else
                            ExecuteSystemCommand(mappedCommand);

                        // Return true if nothing bad happens
                        sr.Close();
                        return true;
                    } catch (Exception ex) {
                        // ignored
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
                var matchingRegex = regex.Replace(escapedCommand, "(.+)"); // \?(.+)\?\?(.+)\?
                // Then match it to the input command
                regex = new Regex(matchingRegex);
                var match = regex.Match(s);
                var matchedGroups = match.Groups;

                // Check if the #arguments required is the same as #args provided
                if (argsNumber != matchedGroups.Count - 1) {
                    return s;
                }

                // Convert the result to a string
                var args = new string[5];

                for (var i = 1; i < matchedGroups.Count && i <= args.Length; i++) {
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
            if (s == "$" || s == "o" || s == "open") {
                OpenCommandMapFile();
            } else if (s == "h" || s == "help") {
                OpenHelpWindow();
            } else if (s == "c" || s == "create") {
                CreateNewCommandMapFile();
            } else if (s == "q" || s == "quit") {
                QuitApp();
            }
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
                                    Properties.Resources.CommandFileNameSuffix, FileMode.CreateNew);
            var fw = new StreamWriter(fs);

            // Write custom messages here!
            foreach (var line in _presetCustomCommands) {
                fw.WriteLine(line);
            }

            fw.Close();

            _shouldClose = false;
        }

        private void CreateBackupCommandMapFile() {
            try {
                var filename = Properties.Resources.CommandFileName +
                               Properties.Resources.CommandFileNameSuffix;
                var backupFilename = Properties.Resources.CommandFileName +
                                     Properties.Resources.CommandFileNameBackupSuffix;
                // Remove the old file first
                File.Delete(backupFilename);
                File.Move(filename, backupFilename);
            } catch (FileNotFoundException ex) {
                // ignored
            }
        }

        private void OpenHelpWindow() {
            MessageBox.Show("$$: open command map file\n" +
                            "$h: open help window\n" +
                            "$c: backup and reset command map file" +
                            "$q: quit this app");
            _shouldClose = false;
        }

        private void OpenCommandMapFile() {
            var currentDir = Directory.GetCurrentDirectory();
            try {
                ExecuteSystemCommand(Path.Combine(currentDir,
                    Properties.Resources.CommandFileName +
                    Properties.Resources.CommandFileNameSuffix), true);
            } catch (Win32Exception ex) {
                // The file doesn't exist
                CreateNewCommandMapFile();
                OpenCommandMapFile();
            }
        }

        /// <summary>
        ///     Execute system command as you would in a cmd line
        /// </summary>
        /// <param name="s">the command</param>
        /// <param name="forcingSystemCommand">whether to force the system run the command. If set to true, this function will not process space(s)</param>
        private static void ExecuteSystemCommand(string s, bool forcingSystemCommand = false) {
            if (forcingSystemCommand) {
                Process.Start(s);
                return;
            }

            var command = SplitExecuteCommand(s);

            if (command.Length == 1)
                Process.Start(command[0]);
            else
                Process.Start(command[0], command[1]);
        }

        /// <summary>
        ///     Split a system command into two parts
        /// </summary>
        /// <param name="s">the command</param>
        /// <returns>An string array with the first element the name of the application and the second element th args</returns>
        private static string[] SplitExecuteCommand(string s) {
            return s.Split(new[] { ' ' }, 2);
        }

        private void Command_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
            Opacity = Convert.ToDouble(Properties.Resources.WindowGotFocusOpacity);
            FocusIndicator.Visibility = Visibility.Visible;
        }

        private void Command_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
            Opacity = Convert.ToDouble(Properties.Resources.WindowLostFocusOpacity);
            FocusIndicator.Visibility = Visibility.Hidden;
        }
    }
}