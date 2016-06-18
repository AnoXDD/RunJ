//houssem.dellai@ieee.org 
//+216 95 325 964 

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Timers;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Timer = System.Timers.Timer;

namespace RunJ {
    public partial class MainWindow : Window {
        private readonly Timer _t = new Timer();

        public MainWindow() {
            InitializeComponent();

            InitializeSystemInfo();
            InitializeCommandPanel();

        }

        /// <summary>
        /// Initialize the command textbox
        /// </summary>
        private void InitializeCommandPanel() {
            Command.Focus();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e) {
            // Add anything you need to handle here
        }

        /// <summary>
        ///  Initialize the current time and date
        /// </summary>
        private void InitializeSystemInfo() {
            var timeString = DateTime.Now.ToString(Properties.Resources.TimeFormat);
            var dateString = DateTime.Now.ToString(Properties.Resources.DateFormat);
            InfoTime.Content = timeString;
            InfoDate.Content = dateString;

            return;

            _t.Interval = 1000;
            _t.Elapsed += new ElapsedEventHandler(this.TimerClick);
            _t.Start();
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

        private void Command_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            var content = Command.Text;
            Command.Opacity = content.Length == 0 ? 0 : 1;
        }

        private void MainWindow_OnKeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Escape) {
                // Test if the command window is to be closed
                if (Command.Text.Length == 0) {
                    // Close the window
                    this.Close();
                } else {
                    Command.Text = "";
                }
            } else if (e.Key == Key.Enter) {
                this.Execute(Command.Text);
                this.Close();
            }
        }

        /// <summary>
        /// Execute this string
        /// </summary>
        /// <param name="s">command of the string</param>
        private void Execute(string s) {
            ExecuteSystemCommand(s);
        }

        private static void ExecuteSystemCommand(string s) {
            Process.Start(s);
        }
    }
}
