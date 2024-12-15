namespace ABT.Test.Exec.Logging {
    partial class SerialNumberDialog {
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
            this.BarCodeText = new System.Windows.Forms.TextBox();
            this.Cancel = new System.Windows.Forms.Button();
            this.OK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // BarCodeText
            // 
            this.BarCodeText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.BarCodeText.Enabled = false;
            this.BarCodeText.Font = new System.Drawing.Font("Microsoft Sans Serif", 20.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BarCodeText.Location = new System.Drawing.Point(47, 13);
            this.BarCodeText.Margin = new System.Windows.Forms.Padding(4);
            this.BarCodeText.MaxLength = 11;
            this.BarCodeText.Name = "BarCodeText";
            this.BarCodeText.Size = new System.Drawing.Size(632, 46);
            this.BarCodeText.TabIndex = 2;
            this.BarCodeText.TabStop = false;
            this.BarCodeText.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.BarCodeText.WordWrap = false;
            // 
            // Cancel
            // 
            this.Cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.Cancel.BackColor = System.Drawing.Color.Red;
            this.Cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.Cancel.Location = new System.Drawing.Point(490, 67);
            this.Cancel.Margin = new System.Windows.Forms.Padding(4);
            this.Cancel.Name = "Cancel";
            this.Cancel.Size = new System.Drawing.Size(133, 60);
            this.Cancel.TabIndex = 1;
            this.Cancel.Text = "&Cancel";
            this.Cancel.UseVisualStyleBackColor = false;
            this.Cancel.Click += new System.EventHandler(this.Cancel_Clicked);
            // 
            // OK
            // 
            this.OK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.OK.BackColor = System.Drawing.Color.Green;
            this.OK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.OK.Location = new System.Drawing.Point(111, 67);
            this.OK.Margin = new System.Windows.Forms.Padding(4);
            this.OK.Name = "OK";
            this.OK.Size = new System.Drawing.Size(133, 62);
            this.OK.TabIndex = 0;
            this.OK.Text = "&OK";
            this.OK.UseVisualStyleBackColor = false;
            this.OK.Click += new System.EventHandler(this.OK_Clicked);
            // 
            // SerialNumberDialog
            // 
            this.AcceptButton = this.OK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.Cancel;
            this.ClientSize = new System.Drawing.Size(727, 169);
            this.ControlBox = false;
            this.Controls.Add(this.BarCodeText);
            this.Controls.Add(this.Cancel);
            this.Controls.Add(this.OK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SerialNumberDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Scan Bar-Coded Serial Number";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox BarCodeText;
        private System.Windows.Forms.Button Cancel;
        private System.Windows.Forms.Button OK;
    }
}