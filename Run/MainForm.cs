using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using NHotkey;
using NHotkey.WindowsForms;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Timer = System.Timers.Timer;

namespace Run {
    public partial class MainForm : Form {

        private bool _isShowing = true;
        private bool _shouldClose = true;

        private Timer _t = new Timer();
        private readonly Keys _key = Keys.Control | Keys.Alt | Keys.Q;

        public MainForm() {
            InitializeComponent();

            InitializeSystemInfo();
            InitializeCommandPanel();
            InitializeHotkeyManager();
        }

        private void InitializeHotkeyManager() {
            HotkeyManager.Current.AddOrReplace("ToggleVisibility", _key, ToggleVisibilityHandler);
        }

        private void ToggleVisibilityHandler(object sender, HotkeyEventArgs e) {
            ToggleVisibility();
            e.Handled = true;
        }

        private void ToggleVisibility() {
            if (_isShowing) {
                HideWindow();
            } else {
                ShowWindow();
            }

            _isShowing = !_isShowing;
        }

        private void ShowWindow() {
            Show();
        }

        private void HideWindow() {
            Hide();
            Command.Text = "";
        }

        #region imported_part

        /// <summary>
        ///     Initialize the command textbox
        /// </summary>
        private void InitializeCommandPanel() {
            Command.Focus();
        }

        /// <summary>
        ///     Initialize the current time and date
        /// </summary>
        private void InitializeSystemInfo() {
            UpdateSystemInfo();
        }

        private void UpdateSystemInfo() {
            var timeString = DateTime.Now.ToString(Properties.Resource.TimeFormat);
            var dateString = DateTime.Now.ToString(Properties.Resource.DateFormat);
            InfoTime.Text = timeString;
            InfoDate.Text = dateString;
        }

        private void Command_TextChanged(object sender, EventArgs e) {
            var content = Command.Text;
            //Command.Opacity = content.Length == 0 ? 0 : 1;
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
            } else {
                // Read command file 
                if (ReadAndAttemptExecuteCustomCommand(s)) {
                    ToggleVisibility();
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
                ToggleVisibility();
            } else {
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
                fs = new FileStream(Properties.Resource.CommandFileName +
                                    Properties.Resource.CommandFileNameSuffix, FileMode.Open);
                sr = new StreamReader(fs);
            } catch (IOException ex) {
                SystemSounds.Exclamation.Play();
                MessageBox.Show("Error while reading the map file");

                _shouldClose = false;
                return false;
            }

            // Read the stream
            var commentHeader = Properties.Resource.CommentHeader;
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
                    } catch (Exception ex) {
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
            } else if (s == "h" || s == "help") {
                OpenHelpWindow();
            } else if (s == "c" || s == "create") {
                CreateNewCommandMapFile();
            }
        }

        /// <summary>
        ///     Create a new command map file.
        ///     This will also create a backup file
        /// </summary>
        private void CreateNewCommandMapFile() {
            CreateBackupCommandMapFile();

            var fs = new FileStream(Properties.Resource.CommandFileName +
                                    Properties.Resource.CommandFileNameSuffix, FileMode.CreateNew);
            var fw = new StreamWriter(fs);

            // Write custom messages here!
            fw.WriteLine("#shortcut,command");
            fw.WriteLine("#Use \"#\" to start a new command line, otherwise use comma to separate them");
            fw.WriteLine("mail,https://mail.google.com/mail/ca");
            fw.WriteLine("cal,https://calendar.google.com/calendar/render");
            fw.WriteLine("app,appwiz.cpl");

            fw.Close();

            _shouldClose = false;
        }

        private void CreateBackupCommandMapFile() {
            try {
                var filename = Properties.Resource.CommandFileName +
                               Properties.Resource.CommandFileNameSuffix;
                var backupFilename = Properties.Resource.CommandFileName +
                                     Properties.Resource.CommandFileNameBackupSuffix;
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
                            "$c: backup and reset command map file");
            _shouldClose = false;
        }

        private void OpenCommandMapFile() {
            var currentDir = Directory.GetCurrentDirectory();
            try {
                ExecuteSystemCommand(Path.Combine(currentDir,
                    Properties.Resource.CommandFileName +
                    Properties.Resource.CommandFileNameSuffix));
            } catch (Win32Exception ex) {
                // The file doesn't exist
                CreateNewCommandMapFile();
                OpenCommandMapFile();
            }
        }

        private static void ExecuteSystemCommand(string s) {
            Process.Start(s);
        }

        #endregion

    }
}