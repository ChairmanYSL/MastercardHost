using MastercardHost.Properties;
using System.Globalization;
using System.Resources;

namespace MastercardHost
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.logRowLimitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
            this.ToolStripMenuItem_SelfDefine = new System.Windows.Forms.ToolStripMenuItem();
            this.logSwitchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.logLocationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.appToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.tabControl_Server_Setting = new System.Windows.Forms.TabControl();
            this.tabPage_Server_Setting = new System.Windows.Forms.TabPage();
            this.button_Close_Server = new System.Windows.Forms.Button();
            this.button_Listen_Server = new System.Windows.Forms.Button();
            this.textBox_Port_Server = new System.Windows.Forms.TextBox();
            this.textBox_IP_Addr_Server = new System.Windows.Forms.TextBox();
            this.label_Port_Server = new System.Windows.Forms.Label();
            this.label_IP_Addr_Server = new System.Windows.Forms.Label();
            this.tabPage_Client_Setting = new System.Windows.Forms.TabPage();
            this.button_Close_Client = new System.Windows.Forms.Button();
            this.button_Bind = new System.Windows.Forms.Button();
            this.textBox_Port_Client = new System.Windows.Forms.TextBox();
            this.textBox_IP_Addr_Client = new System.Windows.Forms.TextBox();
            this.label_Port_Client = new System.Windows.Forms.Label();
            this.label_IP_Addr_Client = new System.Windows.Forms.Label();
            this.button_Test = new System.Windows.Forms.Button();
            this.button_ClearScreen = new System.Windows.Forms.Button();
            this.textBox_Script = new System.Windows.Forms.TextBox();
            this.label_Script = new System.Windows.Forms.Label();
            this.label_IssuerAuthData = new System.Windows.Forms.Label();
            this.textBox_IAD = new System.Windows.Forms.TextBox();
            this.textBox_RespCode = new System.Windows.Forms.TextBox();
            this.label_RespCode = new System.Windows.Forms.Label();
            this.comboBox_Revokey = new System.Windows.Forms.ComboBox();
            this.label_Revokey = new System.Windows.Forms.Label();
            this.comboBox_CAPK = new System.Windows.Forms.ComboBox();
            this.label_CAPK_Info = new System.Windows.Forms.Label();
            this.label_Config_Info = new System.Windows.Forms.Label();
            this.comboBox_Config = new System.Windows.Forms.ComboBox();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.tabControl_Server_Setting.SuspendLayout();
            this.tabPage_Server_Setting.SuspendLayout();
            this.tabPage_Client_Setting.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1});
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Name = "menuStrip1";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.logRowLimitToolStripMenuItem,
            this.logSwitchToolStripMenuItem,
            this.logLocationToolStripMenuItem});
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            resources.ApplyResources(this.toolStripMenuItem1, "toolStripMenuItem1");
            this.toolStripMenuItem1.Text = global::MastercardHost.Properties.Resources.MainMenu;
            // 
            // logRowLimitToolStripMenuItem
            // 
            this.logRowLimitToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem3,
            this.toolStripMenuItem4,
            this.toolStripMenuItem5,
            this.toolStripMenuItem6,
            this.ToolStripMenuItem_SelfDefine});
            this.logRowLimitToolStripMenuItem.Name = "logRowLimitToolStripMenuItem";
            resources.ApplyResources(this.logRowLimitToolStripMenuItem, "logRowLimitToolStripMenuItem");
            this.logRowLimitToolStripMenuItem.Text = global::MastercardHost.Properties.Resources.LogRowLimit;
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            resources.ApplyResources(this.toolStripMenuItem3, "toolStripMenuItem3");
            this.toolStripMenuItem3.Click += new System.EventHandler(this.toolStripMenuItem3_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            resources.ApplyResources(this.toolStripMenuItem4, "toolStripMenuItem4");
            this.toolStripMenuItem4.Click += new System.EventHandler(this.toolStripMenuItem4_Click);
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            resources.ApplyResources(this.toolStripMenuItem5, "toolStripMenuItem5");
            this.toolStripMenuItem5.Click += new System.EventHandler(this.toolStripMenuItem5_Click);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            resources.ApplyResources(this.toolStripMenuItem6, "toolStripMenuItem6");
            this.toolStripMenuItem6.Click += new System.EventHandler(this.toolStripMenuItem6_Click);
            // 
            // ToolStripMenuItem_SelfDefine
            // 
            this.ToolStripMenuItem_SelfDefine.Name = "ToolStripMenuItem_SelfDefine";
            resources.ApplyResources(this.ToolStripMenuItem_SelfDefine, "ToolStripMenuItem_SelfDefine");
            this.ToolStripMenuItem_SelfDefine.Text = global::MastercardHost.Properties.Resources.SelfDefine;
            this.ToolStripMenuItem_SelfDefine.Click += new System.EventHandler(this.ToolStripMenuItem_SelfDefine_Click);
            // 
            // logSwitchToolStripMenuItem
            // 
            this.logSwitchToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.closeToolStripMenuItem});
            this.logSwitchToolStripMenuItem.Name = "logSwitchToolStripMenuItem";
            resources.ApplyResources(this.logSwitchToolStripMenuItem, "logSwitchToolStripMenuItem");
            this.logSwitchToolStripMenuItem.Text = global::MastercardHost.Properties.Resources.LogSwitch;
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            resources.ApplyResources(this.openToolStripMenuItem, "openToolStripMenuItem");
            this.openToolStripMenuItem.Text = global::MastercardHost.Properties.Resources.OpenLog;
            // 
            // closeToolStripMenuItem
            // 
            this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
            resources.ApplyResources(this.closeToolStripMenuItem, "closeToolStripMenuItem");
            this.closeToolStripMenuItem.Text = global::MastercardHost.Properties.Resources.CloseLog;
            // 
            // logLocationToolStripMenuItem
            // 
            this.logLocationToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.appToolStripMenuItem});
            this.logLocationToolStripMenuItem.Name = "logLocationToolStripMenuItem";
            resources.ApplyResources(this.logLocationToolStripMenuItem, "logLocationToolStripMenuItem");
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            resources.ApplyResources(this.fileToolStripMenuItem, "fileToolStripMenuItem");
            this.fileToolStripMenuItem.Text = global::MastercardHost.Properties.Resources.LogInFile;
            // 
            // appToolStripMenuItem
            // 
            this.appToolStripMenuItem.Name = "appToolStripMenuItem";
            resources.ApplyResources(this.appToolStripMenuItem, "appToolStripMenuItem");
            this.appToolStripMenuItem.Text = global::MastercardHost.Properties.Resources.LogInText;
            // 
            // splitContainer1
            // 
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.richTextBox1);
            // 
            // splitContainer2
            // 
            resources.ApplyResources(this.splitContainer2, "splitContainer2");
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.tabControl_Server_Setting);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.button_Test);
            this.splitContainer2.Panel2.Controls.Add(this.button_ClearScreen);
            this.splitContainer2.Panel2.Controls.Add(this.textBox_Script);
            this.splitContainer2.Panel2.Controls.Add(this.label_Script);
            this.splitContainer2.Panel2.Controls.Add(this.label_IssuerAuthData);
            this.splitContainer2.Panel2.Controls.Add(this.textBox_IAD);
            this.splitContainer2.Panel2.Controls.Add(this.textBox_RespCode);
            this.splitContainer2.Panel2.Controls.Add(this.label_RespCode);
            this.splitContainer2.Panel2.Controls.Add(this.comboBox_Revokey);
            this.splitContainer2.Panel2.Controls.Add(this.label_Revokey);
            this.splitContainer2.Panel2.Controls.Add(this.comboBox_CAPK);
            this.splitContainer2.Panel2.Controls.Add(this.label_CAPK_Info);
            this.splitContainer2.Panel2.Controls.Add(this.label_Config_Info);
            this.splitContainer2.Panel2.Controls.Add(this.comboBox_Config);
            // 
            // tabControl_Server_Setting
            // 
            this.tabControl_Server_Setting.Controls.Add(this.tabPage_Server_Setting);
            this.tabControl_Server_Setting.Controls.Add(this.tabPage_Client_Setting);
            resources.ApplyResources(this.tabControl_Server_Setting, "tabControl_Server_Setting");
            this.tabControl_Server_Setting.HotTrack = true;
            this.tabControl_Server_Setting.Name = "tabControl_Server_Setting";
            this.tabControl_Server_Setting.SelectedIndex = 0;
            // 
            // tabPage_Server_Setting
            // 
            this.tabPage_Server_Setting.Controls.Add(this.button_Close_Server);
            this.tabPage_Server_Setting.Controls.Add(this.button_Listen_Server);
            this.tabPage_Server_Setting.Controls.Add(this.textBox_Port_Server);
            this.tabPage_Server_Setting.Controls.Add(this.textBox_IP_Addr_Server);
            this.tabPage_Server_Setting.Controls.Add(this.label_Port_Server);
            this.tabPage_Server_Setting.Controls.Add(this.label_IP_Addr_Server);
            resources.ApplyResources(this.tabPage_Server_Setting, "tabPage_Server_Setting");
            this.tabPage_Server_Setting.Name = "tabPage_Server_Setting";
            this.tabPage_Server_Setting.UseVisualStyleBackColor = true;
            // 
            // button_Close_Server
            // 
            resources.ApplyResources(this.button_Close_Server, "button_Close_Server");
            this.button_Close_Server.Name = "button_Close_Server";
            this.button_Close_Server.Text = global::MastercardHost.Properties.Resources.CloseServerListen;
            this.button_Close_Server.UseVisualStyleBackColor = true;
            // 
            // button_Listen_Server
            // 
            resources.ApplyResources(this.button_Listen_Server, "button_Listen_Server");
            this.button_Listen_Server.Name = "button_Listen_Server";
            this.button_Listen_Server.Text = global::MastercardHost.Properties.Resources.OpenServerListen;
            this.button_Listen_Server.UseVisualStyleBackColor = true;
            // 
            // textBox_Port_Server
            // 
            resources.ApplyResources(this.textBox_Port_Server, "textBox_Port_Server");
            this.textBox_Port_Server.Name = "textBox_Port_Server";
            // 
            // textBox_IP_Addr_Server
            // 
            resources.ApplyResources(this.textBox_IP_Addr_Server, "textBox_IP_Addr_Server");
            this.textBox_IP_Addr_Server.Name = "textBox_IP_Addr_Server";
            // 
            // label_Port_Server
            // 
            resources.ApplyResources(this.label_Port_Server, "label_Port_Server");
            this.label_Port_Server.Name = "label_Port_Server";
            // 
            // label_IP_Addr_Server
            // 
            resources.ApplyResources(this.label_IP_Addr_Server, "label_IP_Addr_Server");
            this.label_IP_Addr_Server.Name = "label_IP_Addr_Server";
            // 
            // tabPage_Client_Setting
            // 
            this.tabPage_Client_Setting.Controls.Add(this.button_Close_Client);
            this.tabPage_Client_Setting.Controls.Add(this.button_Bind);
            this.tabPage_Client_Setting.Controls.Add(this.textBox_Port_Client);
            this.tabPage_Client_Setting.Controls.Add(this.textBox_IP_Addr_Client);
            this.tabPage_Client_Setting.Controls.Add(this.label_Port_Client);
            this.tabPage_Client_Setting.Controls.Add(this.label_IP_Addr_Client);
            resources.ApplyResources(this.tabPage_Client_Setting, "tabPage_Client_Setting");
            this.tabPage_Client_Setting.Name = "tabPage_Client_Setting";
            this.tabPage_Client_Setting.UseVisualStyleBackColor = true;
            // 
            // button_Close_Client
            // 
            resources.ApplyResources(this.button_Close_Client, "button_Close_Client");
            this.button_Close_Client.Name = "button_Close_Client";
            this.button_Close_Client.Text = global::MastercardHost.Properties.Resources.CloseClient;
            this.button_Close_Client.UseVisualStyleBackColor = true;
            // 
            // button_Bind
            // 
            resources.ApplyResources(this.button_Bind, "button_Bind");
            this.button_Bind.Name = "button_Bind";
            this.button_Bind.Text = global::MastercardHost.Properties.Resources.BindClient;
            this.button_Bind.UseVisualStyleBackColor = true;
            // 
            // textBox_Port_Client
            // 
            resources.ApplyResources(this.textBox_Port_Client, "textBox_Port_Client");
            this.textBox_Port_Client.Name = "textBox_Port_Client";
            // 
            // textBox_IP_Addr_Client
            // 
            resources.ApplyResources(this.textBox_IP_Addr_Client, "textBox_IP_Addr_Client");
            this.textBox_IP_Addr_Client.Name = "textBox_IP_Addr_Client";
            // 
            // label_Port_Client
            // 
            resources.ApplyResources(this.label_Port_Client, "label_Port_Client");
            this.label_Port_Client.Name = "label_Port_Client";
            // 
            // label_IP_Addr_Client
            // 
            resources.ApplyResources(this.label_IP_Addr_Client, "label_IP_Addr_Client");
            this.label_IP_Addr_Client.Name = "label_IP_Addr_Client";
            // 
            // button_Test
            // 
            resources.ApplyResources(this.button_Test, "button_Test");
            this.button_Test.Name = "button_Test";
            this.button_Test.UseVisualStyleBackColor = true;
            this.button_Test.Click += new System.EventHandler(this.button_Test_Click);
            // 
            // button_ClearScreen
            // 
            resources.ApplyResources(this.button_ClearScreen, "button_ClearScreen");
            this.button_ClearScreen.Name = "button_ClearScreen";
            this.button_ClearScreen.Text = global::MastercardHost.Properties.Resources.ClearScreen;
            this.button_ClearScreen.UseVisualStyleBackColor = true;
            // 
            // textBox_Script
            // 
            resources.ApplyResources(this.textBox_Script, "textBox_Script");
            this.textBox_Script.Name = "textBox_Script";
            // 
            // label_Script
            // 
            resources.ApplyResources(this.label_Script, "label_Script");
            this.label_Script.Name = "label_Script";
            // 
            // label_IssuerAuthData
            // 
            resources.ApplyResources(this.label_IssuerAuthData, "label_IssuerAuthData");
            this.label_IssuerAuthData.Name = "label_IssuerAuthData";
            // 
            // textBox_IAD
            // 
            resources.ApplyResources(this.textBox_IAD, "textBox_IAD");
            this.textBox_IAD.Name = "textBox_IAD";
            // 
            // textBox_RespCode
            // 
            resources.ApplyResources(this.textBox_RespCode, "textBox_RespCode");
            this.textBox_RespCode.Name = "textBox_RespCode";
            // 
            // label_RespCode
            // 
            resources.ApplyResources(this.label_RespCode, "label_RespCode");
            this.label_RespCode.Name = "label_RespCode";
            // 
            // comboBox_Revokey
            // 
            this.comboBox_Revokey.FormattingEnabled = true;
            resources.ApplyResources(this.comboBox_Revokey, "comboBox_Revokey");
            this.comboBox_Revokey.Name = "comboBox_Revokey";
            // 
            // label_Revokey
            // 
            resources.ApplyResources(this.label_Revokey, "label_Revokey");
            this.label_Revokey.Name = "label_Revokey";
            // 
            // comboBox_CAPK
            // 
            this.comboBox_CAPK.FormattingEnabled = true;
            resources.ApplyResources(this.comboBox_CAPK, "comboBox_CAPK");
            this.comboBox_CAPK.Name = "comboBox_CAPK";
            // 
            // label_CAPK_Info
            // 
            resources.ApplyResources(this.label_CAPK_Info, "label_CAPK_Info");
            this.label_CAPK_Info.Name = "label_CAPK_Info";
            // 
            // label_Config_Info
            // 
            resources.ApplyResources(this.label_Config_Info, "label_Config_Info");
            this.label_Config_Info.Name = "label_Config_Info";
            // 
            // comboBox_Config
            // 
            this.comboBox_Config.FormattingEnabled = true;
            resources.ApplyResources(this.comboBox_Config, "comboBox_Config");
            this.comboBox_Config.Name = "comboBox_Config";
            // 
            // richTextBox1
            // 
            resources.ApplyResources(this.richTextBox1, "richTextBox1");
            this.richTextBox1.Name = "richTextBox1";
            // 
            // MainForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.tabControl_Server_Setting.ResumeLayout(false);
            this.tabPage_Server_Setting.ResumeLayout(false);
            this.tabPage_Server_Setting.PerformLayout();
            this.tabPage_Client_Setting.ResumeLayout(false);
            this.tabPage_Client_Setting.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem logRowLimitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
        private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItem_SelfDefine;
        private System.Windows.Forms.ToolStripMenuItem logSwitchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem logLocationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem appToolStripMenuItem;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.TabControl tabControl_Server_Setting;
        private System.Windows.Forms.TabPage tabPage_Server_Setting;
        private System.Windows.Forms.Button button_Close_Server;
        private System.Windows.Forms.Button button_Listen_Server;
        private System.Windows.Forms.TextBox textBox_Port_Server;
        private System.Windows.Forms.TextBox textBox_IP_Addr_Server;
        private System.Windows.Forms.Label label_Port_Server;
        private System.Windows.Forms.Label label_IP_Addr_Server;
        private System.Windows.Forms.TabPage tabPage_Client_Setting;
        private System.Windows.Forms.Button button_Close_Client;
        private System.Windows.Forms.Button button_Bind;
        private System.Windows.Forms.TextBox textBox_Port_Client;
        private System.Windows.Forms.TextBox textBox_IP_Addr_Client;
        private System.Windows.Forms.Label label_Port_Client;
        private System.Windows.Forms.Label label_IP_Addr_Client;
        private System.Windows.Forms.Button button_ClearScreen;
        private System.Windows.Forms.TextBox textBox_Script;
        private System.Windows.Forms.Label label_Script;
        private System.Windows.Forms.Label label_IssuerAuthData;
        private System.Windows.Forms.TextBox textBox_IAD;
        private System.Windows.Forms.TextBox textBox_RespCode;
        private System.Windows.Forms.Label label_RespCode;
        private System.Windows.Forms.ComboBox comboBox_Revokey;
        private System.Windows.Forms.Label label_Revokey;
        private System.Windows.Forms.ComboBox comboBox_CAPK;
        private System.Windows.Forms.Label label_CAPK_Info;
        private System.Windows.Forms.Label label_Config_Info;
        private System.Windows.Forms.ComboBox comboBox_Config;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button button_Test;
    }
}