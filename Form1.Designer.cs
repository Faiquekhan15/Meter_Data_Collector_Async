namespace MDC_Server
{
    partial class Form1
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.start = new System.Windows.Forms.Button();
            this.Stop = new System.Windows.Forms.Button();
            this.btnClientService = new System.Windows.Forms.Button();
            this.labelServer = new System.Windows.Forms.Label();
            this.labelClient = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tbActivePower = new System.Windows.Forms.TextBox();
            this.tbReactivePower = new System.Windows.Forms.TextBox();
            this.labelContime = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.IPBox = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.listBox_ConnectedClients = new System.Windows.Forms.ListBox();
            this.buttonClear = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.menuToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.close = new System.Windows.Forms.ToolStripMenuItem();
            this.label3 = new System.Windows.Forms.Label();
            this.serverPort = new System.Windows.Forms.ComboBox();
            this.button1 = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // start
            // 
            this.start.Location = new System.Drawing.Point(29, 27);
            this.start.Name = "start";
            this.start.Size = new System.Drawing.Size(75, 23);
            this.start.TabIndex = 0;
            this.start.Text = "Start Server";
            this.start.UseVisualStyleBackColor = true;
            this.start.Click += new System.EventHandler(this.start_Click);
            // 
            // Stop
            // 
            this.Stop.Location = new System.Drawing.Point(110, 27);
            this.Stop.Name = "Stop";
            this.Stop.Size = new System.Drawing.Size(75, 23);
            this.Stop.TabIndex = 1;
            this.Stop.Text = "Stop Server";
            this.Stop.UseVisualStyleBackColor = true;
            this.Stop.Click += new System.EventHandler(this.Stop_Click);
            // 
            // btnClientService
            // 
            this.btnClientService.Location = new System.Drawing.Point(29, 86);
            this.btnClientService.Name = "btnClientService";
            this.btnClientService.Size = new System.Drawing.Size(120, 23);
            this.btnClientService.TabIndex = 2;
            this.btnClientService.Text = "Begin Client Service";
            this.btnClientService.UseVisualStyleBackColor = true;
            this.btnClientService.Click += new System.EventHandler(this.btnClientService_Click);
            // 
            // labelServer
            // 
            this.labelServer.AutoSize = true;
            this.labelServer.Location = new System.Drawing.Point(26, 53);
            this.labelServer.Name = "labelServer";
            this.labelServer.Size = new System.Drawing.Size(75, 13);
            this.labelServer.TabIndex = 3;
            this.labelServer.Text = "Server Started";
            // 
            // labelClient
            // 
            this.labelClient.AutoSize = true;
            this.labelClient.Location = new System.Drawing.Point(38, 127);
            this.labelClient.Name = "labelClient";
            this.labelClient.Size = new System.Drawing.Size(37, 13);
            this.labelClient.TabIndex = 4;
            this.labelClient.Text = "----------";
            this.labelClient.MouseHover += new System.EventHandler(this.labelClient_MouseHover);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(155, 91);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Service Started";
            // 
            // tbActivePower
            // 
            this.tbActivePower.Location = new System.Drawing.Point(29, 143);
            this.tbActivePower.Name = "tbActivePower";
            this.tbActivePower.Size = new System.Drawing.Size(100, 20);
            this.tbActivePower.TabIndex = 6;
            this.tbActivePower.MouseHover += new System.EventHandler(this.tbActivePower_MouseHover);
            // 
            // tbReactivePower
            // 
            this.tbReactivePower.Location = new System.Drawing.Point(135, 143);
            this.tbReactivePower.Name = "tbReactivePower";
            this.tbReactivePower.Size = new System.Drawing.Size(100, 20);
            this.tbReactivePower.TabIndex = 7;
            // 
            // labelContime
            // 
            this.labelContime.AutoSize = true;
            this.labelContime.Location = new System.Drawing.Point(151, 127);
            this.labelContime.Name = "labelContime";
            this.labelContime.Size = new System.Drawing.Size(34, 13);
            this.labelContime.TabIndex = 9;
            this.labelContime.Text = "---------";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(29, 70);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "-------------";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(241, 146);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(96, 13);
            this.label5.TabIndex = 16;
            this.label5.Text = "Realtime Readings";
            // 
            // IPBox
            // 
            this.IPBox.FormattingEnabled = true;
            this.IPBox.Location = new System.Drawing.Point(259, 32);
            this.IPBox.Name = "IPBox";
            this.IPBox.Size = new System.Drawing.Size(108, 21);
            this.IPBox.TabIndex = 19;
            this.IPBox.Text = "209.150.146.236";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(371, 34);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(60, 13);
            this.label6.TabIndex = 21;
            this.label6.Text = "Server Port";
            // 
            // listBox_ConnectedClients
            // 
            this.listBox_ConnectedClients.FormattingEnabled = true;
            this.listBox_ConnectedClients.Location = new System.Drawing.Point(29, 186);
            this.listBox_ConnectedClients.Name = "listBox_ConnectedClients";
            this.listBox_ConnectedClients.ScrollAlwaysVisible = true;
            this.listBox_ConnectedClients.Size = new System.Drawing.Size(745, 160);
            this.listBox_ConnectedClients.TabIndex = 23;
            // 
            // buttonClear
            // 
            this.buttonClear.Location = new System.Drawing.Point(656, 146);
            this.buttonClear.Name = "buttonClear";
            this.buttonClear.Size = new System.Drawing.Size(103, 23);
            this.buttonClear.TabIndex = 11;
            this.buttonClear.Text = "Clear";
            this.buttonClear.UseVisualStyleBackColor = true;
            this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(4, 1, 0, 1);
            this.menuStrip1.Size = new System.Drawing.Size(786, 31);
            this.menuStrip1.TabIndex = 24;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // menuToolStripMenuItem
            // 
            this.menuToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.close});
            this.menuToolStripMenuItem.Name = "menuToolStripMenuItem";
            this.menuToolStripMenuItem.Size = new System.Drawing.Size(69, 29);
            this.menuToolStripMenuItem.Text = "Menu";
            // 
            // close
            // 
            this.close.Name = "close";
            this.close.Size = new System.Drawing.Size(139, 30);
            this.close.Text = "Close";
            this.close.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(201, 32);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(47, 13);
            this.label3.TabIndex = 25;
            this.label3.Text = "Static IP";
            // 
            // serverPort
            // 
            this.serverPort.FormattingEnabled = true;
            this.serverPort.Items.AddRange(new object[] {
            "9001",
            "9002",
            "9003",
            "9004",
            "9005",
            "9006",
            "9007",
            "9008",
            "9009"});
            this.serverPort.Location = new System.Drawing.Point(438, 35);
            this.serverPort.Name = "serverPort";
            this.serverPort.Size = new System.Drawing.Size(121, 21);
            this.serverPort.TabIndex = 26;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(656, 42);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(23, 23);
            this.button1.TabIndex = 27;
            this.button1.Text = "r";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.reload_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(786, 370);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.serverPort);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.listBox_ConnectedClients);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.IPBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.buttonClear);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.labelContime);
            this.Controls.Add(this.tbReactivePower);
            this.Controls.Add(this.tbActivePower);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.labelClient);
            this.Controls.Add(this.labelServer);
            this.Controls.Add(this.btnClientService);
            this.Controls.Add(this.Stop);
            this.Controls.Add(this.start);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.Text = "MDC";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button start;
        private System.Windows.Forms.Button Stop;
        private System.Windows.Forms.Button btnClientService;
        private System.Windows.Forms.Label labelServer;
        private System.Windows.Forms.Label labelClient;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbActivePower;
        private System.Windows.Forms.TextBox tbReactivePower;
        private System.Windows.Forms.Label labelContime;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox IPBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ListBox listBox_ConnectedClients;
        private System.Windows.Forms.Button buttonClear;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem close;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox serverPort;
        private System.Windows.Forms.Button button1;
    }
}

