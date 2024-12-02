namespace ShareFile.Api.Powershell.Browser
{
    partial class OAuthAuthenticationForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.webViewBrowser = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)(this.webViewBrowser)).BeginInit();
            this.SuspendLayout();
            // 
            // webViewBrowser
            // 
            this.webViewBrowser.AllowExternalDrop = true;
            this.webViewBrowser.CreationProperties = null;
            this.webViewBrowser.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webViewBrowser.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webViewBrowser.Location = new System.Drawing.Point(0, 0);
            this.webViewBrowser.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.webViewBrowser.Name = "webViewBrowser";
            this.webViewBrowser.Size = new System.Drawing.Size(879, 825);
            this.webViewBrowser.TabIndex = 0;
            this.webViewBrowser.ZoomFactor = 1D;
            // 
            // OAuthAuthenticationForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(879, 825);
            this.Controls.Add(this.webViewBrowser);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "OAuthAuthenticationForm";
            this.Text = "Authentication";
            ((System.ComponentModel.ISupportInitialize)(this.webViewBrowser)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webViewBrowser;
    }
}