//houssem.dellai@ieee.org 
//+216 95 325 964 

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using NHotkey;
using NHotkey.Wpf;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Timers.Timer;

namespace RunJ {
    public partial class MainWindow : Window {
        private readonly Timer _t = new Timer();
        private bool _shouldClose = true;
        private bool _isVisible = true;

        private readonly Key _hotkey = Key.Q;
        private readonly ModifierKeys _modiferHotkeys = ModifierKeys.Alt | ModifierKeys.Control;

        public MainWindow() {
            InitializeComponent();

            InitializeSystemInfo();
            InitializeCommandPanel();
            InitializeHotkeyManager();
        }

        /// <summary>
        /// Initiailize the hotkey manager
        /// </summary>
        private void InitializeHotkeyManager() {
            HotkeyManager.Current.AddOrReplace("Test", _hotkey, _modiferHotkeys, ToggleVisibilityHandler);
        }

        /// <summary>
        ///     Initialize the command textbox
        /// </summary>
        private void InitializeCommandPanel() {
            Command.Focus();
            VersionLabel.Content = Properties.Resources._version;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e) {
            // Add anything you need to handle here
            this.HideWindow();
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
            }
            else {
                InfoTime.Content = timeString;
                InfoDate.Content = dateString;
            }
        }

        /// <summary>
        ///  The handler to toggle the visibility of this app
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleVisibilityHandler(object sender, HotkeyEventArgs e) {
            ToggleVisibility();
        }

        /// <summary>
        /// The function to toggle the visibility of this app
        /// </summary>
        private void ToggleVisibility() {
            if (_isVisible)
                HideWindow();
            else {
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
                    Close();
                }
                else {
                    Command.Text = "";
                }
            }
            else if (e.Key == Key.Enter) {
                Execute(Command.Text);
            }
        }

        /// <summary>
        ///     Execute this string
        ///     The first method called from app
        /// </summary>
        /// <param name="s">command of the string</param>
        private void Execute(string s) {
            // Test if it's a command
            if (s.StartsWith("$")) {
                ExecuteAppCommand(s.Substring(1));
            }
            else {
                // Read command file 
                if (ReadAndAttemptExecuteCustomCommand(s)) {
                    Close();
                    return;
                }

                // Execute system task
                try {
                    ExecuteSystemCommand(s);
                }
                catch (Exception ex) {
                    // Create a warning sound
                    SystemSounds.Exclamation.Play();
                    _shouldClose = false;
                }
            }

            if (_shouldClose) {
                Close();
            }
            else {
                Command.Text = "";
            }
        }

        /// <summary>
        ///     Read and execute customized command
        /// </summary>
        /// <param name="s">The command to be indexed</param>
        /// <returns>whether a customized command is found</returns>
        private bool ReadAndAttemptExecuteCustomCommand(string s) {
            FileStream fs;

            StreamReader sr;

            try {
                fs = new FileStream(Properties.Resources.CommandFileName +
                                    Properties.Resources.CommandFileNameSuffix, FileMode.Open);
                sr = new StreamReader(fs);
            }
            catch (IOException ex) {
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
                        ExecuteSystemCommand(groups[1]);

                        // Return true if nothing bad happens
                        sr.Close();
                        return true;
                    }
                    catch (Exception ex) {
                        // ignored
                    }
                }
            }

            sr.Close();
            return false;
        }

        /// <summary>
        ///     Executes the system command
        /// </summary>
        /// <param name="s">The command</param>
        private void ExecuteAppCommand(string s) {
            if (s == "$" || s == "o" || s == "open") {
                OpenCommandMapFile();
            }
            else if (s == "h" || s == "help") {
                OpenHelpWindow();
            }
            else if (s == "c" || s == "create") {
                CreateNewCommandMapFile();
            } else if (s == "q" || s == "quit") {
                QuitApp();
            }
        }

        private void QuitApp() {
            Close();
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
            fw.WriteLine("#shortcut,command");
            fw.WriteLine("#Use \"#\" to start a new command line, otherwise use comma to separate them");
            fw.WriteLine("mail,https://mail.google.com/mail/ca");
            fw.WriteLine("cal,https://calendar.google.com/calendar/render");
            fw.WriteLine("map,https://www.google.com/maps");
            fw.WriteLine("keep,https://keep.google.com/u/0/");
            fw.WriteLine("app,appwiz.cpl");

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
            }
            catch (FileNotFoundException ex) {
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
                    Properties.Resources.CommandFileNameSuffix));
            }
            catch (Win32Exception ex) {
                // The file doesn't exist
                CreateNewCommandMapFile();
                OpenCommandMapFile();
            }
        }

        private static void ExecuteSystemCommand(string s) {
            Process.Start(s);
        }
    }
}