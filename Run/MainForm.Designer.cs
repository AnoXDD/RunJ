using System;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Forms.KeyEventArgs;

namespace Run {
    partial class MainForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.InfoTime = new System.Windows.Forms.Label();
            this.InfoDate = new System.Windows.Forms.Label();
            this.Command = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // InfoTime
            // 
            this.InfoTime.Font = new System.Drawing.Font("Microsoft Sans Serif", 72F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InfoTime.Location = new System.Drawing.Point(12, 9);
            this.InfoTime.Name = "InfoTime";
            this.InfoTime.Size = new System.Drawing.Size(857, 226);
            this.InfoTime.TabIndex = 0;
            this.InfoTime.Text = "TEST";
            this.InfoTime.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // InfoDate
            // 
            this.InfoDate.Font = new System.Drawing.Font("Microsoft Sans Serif", 20F);
            this.InfoDate.Location = new System.Drawing.Point(3, 225);
            this.InfoDate.Name = "InfoDate";
            this.InfoDate.Size = new System.Drawing.Size(866, 69);
            this.InfoDate.TabIndex = 1;
            this.InfoDate.Text = "Time";
            this.InfoDate.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // Command
            // 
            this.Command.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.Command.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Command.Font = new System.Drawing.Font("Microsoft Sans Serif", 25F);
            this.Command.Location = new System.Drawing.Point(0, 0);
            this.Command.Margin = new System.Windows.Forms.Padding(30, 3, 3, 3);
            this.Command.Name = "Command";
            this.Command.Size = new System.Drawing.Size(881, 85);
            this.Command.TabIndex = 2;
            this.Command.TextChanged += new System.EventHandler(this.Command_TextChanged);
            this.Command.KeyDown += Command_KeyDown;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(14F, 29F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(881, 303);
            this.Controls.Add(this.Command);
            this.Controls.Add(this.InfoDate);
            this.Controls.Add(this.InfoTime);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "MainForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private void Command_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode.ToString() == "Escape") {
                // Test if the command window is to be closed
                if (Command.Text.Length == 0) {
                    // Close the window
                    ToggleVisibility();
                } else {
                    Command.Text = "";
                }
            } else if (e.KeyCode.ToString() == "Return") {
                Execute(Command.Text);
            }
        }


        private System.Windows.Forms.Label InfoTime;
        private System.Windows.Forms.Label InfoDate;
        private System.Windows.Forms.TextBox Command;
    }
}

