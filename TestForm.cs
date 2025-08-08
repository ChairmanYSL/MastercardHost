using Newtonsoft.Json.Linq;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;
using TcpSharp;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MastercardHost
{
    public partial class TestForm : Form
    {
        private TcpSharpSocketServer _tcpServer;
        private TcpSharpSocketClient _tcpClient;
        private string _connectID;
        private int _port;

        public TestForm(MainForm mainForm)
        {
            InitializeComponent();
            Size = new Size(1000, 700);
            SetupLayout();

            _tcpClient = new TcpSharpSocketClient("localhost", 6908);
            _tcpClient.OnConnected += (sender, e) =>
            {
                MyLogManager.Log("Client connected to server");
            };
            _tcpClient.OnError += (sender, e) =>
            {
                MyLogManager.Log($"Client error: {e.ToString()}");
            };
            _tcpClient.Connect();
        }

        private void SetupLayout()
        {
            mainSplitContainer.SplitterDistance = (int)(ClientSize.Width * 0.3);

            // 配置左侧TableLayoutPanel为4等分行
            LeftTableLayoutPanel.ColumnCount = 1;
            LeftTableLayoutPanel.ColumnStyles.Clear();
            LeftTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            LeftTableLayoutPanel.RowCount = 4;
            LeftTableLayoutPanel.RowStyles.Clear();
            for (int i = 0; i < 4; i++)
            {
                LeftTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            }

            panel_ACT.Dock = DockStyle.Fill;
            panel_ACT.BackColor = Color.LightGray;
            panel_LoopACT.Dock = DockStyle.Fill;
            panel_LoopACT.BackColor = Color.LightGray;
            panel_Stop.Dock = DockStyle.Fill;
            panel_Stop.BackColor = Color.LightGray;
            panel_DET.Dock = DockStyle.Fill;
            panel_DET.BackColor = Color.LightGray;

            button_ACT.Width = (int)(LeftTableLayoutPanel.Width * 0.5);
            button_ACT.Height = (int)(LeftTableLayoutPanel.Height / 4 * 0.5);
            button_ACT.Anchor = AnchorStyles.None;
            button_ACT.AutoSize = false;
            button_ACT.FlatStyle = FlatStyle.Flat;
            ButtonPosition(panel_ACT, button_ACT);

            button_LoopACT.Width = (int)(LeftTableLayoutPanel.Width * 0.5);
            button_LoopACT.Height = (int)(LeftTableLayoutPanel.Height / 4 * 0.5);
            button_LoopACT.Anchor = AnchorStyles.None;
            button_LoopACT.AutoSize = false;
            button_LoopACT.FlatStyle = FlatStyle.Flat;
            ButtonPosition(panel_LoopACT, button_LoopACT);

            button_Stop.Width = (int)(LeftTableLayoutPanel.Width * 0.5);
            button_Stop.Height = (int)(LeftTableLayoutPanel.Height / 4 * 0.5);
            button_Stop.Anchor = AnchorStyles.None;
            button_Stop.AutoSize = false;
            button_Stop.FlatStyle = FlatStyle.Flat;
            ButtonPosition(panel_Stop, button_Stop);

            button_DET.Width = (int)(LeftTableLayoutPanel.Width * 0.5);
            button_DET.Height = (int)(LeftTableLayoutPanel.Height / 4 * 0.5);
            button_DET.Anchor = AnchorStyles.None;
            button_DET.AutoSize = false;
            button_DET.FlatStyle = FlatStyle.Flat;
            ButtonPosition(panel_DET, button_DET);

            panel_ACT.Controls.Add(button_ACT);
            panel_LoopACT.Controls.Add(button_LoopACT);
            panel_Stop.Controls.Add(button_Stop);
            panel_DET.Controls.Add(button_DET);

            // 配置右侧TableLayoutPanel为12等分行
            RightTableLayoutPanel.ColumnCount = 3;
            RightTableLayoutPanel.ColumnStyles.Clear();
            RightTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));  // CheckBox列
            RightTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));  // Label列
            RightTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F)); // TextBox列

            RightTableLayoutPanel.RowCount = 12;
            RightTableLayoutPanel.RowStyles.Clear();
            for (int i = 0; i < 12; i++)
            {
                RightTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 12));
            }

            checkBox_Amt.Dock = DockStyle.Fill;
            checkBox_Amt.Margin = new Padding(5,5,0,5);
            checkBox_Amt.Anchor = AnchorStyles.Left;
            checkBox_Amt.AutoSize = false;
            SetupCheckBox(checkBox_Amt); // 调整字体大小以适应CheckBox
            RightTableLayoutPanel.Controls.Add(checkBox_Amt, 0, 0);

            label_Amt.Dock = DockStyle.Fill;
            label_Amt.TextAlign = ContentAlignment.MiddleLeft;
            label_Amt.Margin = new Padding(5,0,5,0);
            label_Amt.AutoSize = false;
            RightTableLayoutPanel.Controls.Add(label_Amt, 1, 0);

            textBox_Amt.Text = "000000000001";
            SetupTextBox(textBox_Amt, 0);
            RightTableLayoutPanel.Controls.Add(textBox_Amt, 2, 0);

            checkBox_AmtOth.Dock = DockStyle.Fill;
            checkBox_AmtOth.Margin = new Padding(5, 5, 0, 5);
            checkBox_AmtOth.Anchor = AnchorStyles.Left;
            checkBox_AmtOth.AutoSize = false;
            SetupCheckBox(checkBox_AmtOth); // 调整字体大小以适应CheckBox
            RightTableLayoutPanel.Controls.Add(checkBox_AmtOth, 0, 1);

            label_AmtOth.Dock = DockStyle.Fill;
            label_AmtOth.TextAlign = ContentAlignment.MiddleLeft;
            label_AmtOth.Margin = new Padding(5, 0, 5, 0);
            label_AmtOth.AutoSize = false;
            RightTableLayoutPanel.Controls.Add(label_AmtOth, 1, 1);

            textBox_AmtOth.Text = "000000000000";
            SetupTextBox(textBox_AmtOth, 1); // 设置初始大小
            RightTableLayoutPanel.Controls.Add(textBox_AmtOth, 2, 1);

            checkBox_TransType.Dock = DockStyle.Fill;
            checkBox_TransType.Margin = new Padding(5, 5, 0, 5);
            checkBox_TransType.Anchor = AnchorStyles.Left;
            checkBox_TransType.AutoSize = false;
            SetupCheckBox(checkBox_TransType); // 调整字体大小以适应CheckBox
            RightTableLayoutPanel.Controls.Add(checkBox_TransType, 0, 2);

            label_TransType.Dock = DockStyle.Fill;
            label_TransType.TextAlign = ContentAlignment.MiddleLeft;
            label_TransType.Margin = new Padding(5, 0, 5, 0);
            label_TransType.AutoSize = true;
            RightTableLayoutPanel.Controls.Add(label_TransType, 1, 2);

            textBox_TransType.Text = "00";
            SetupTextBox(textBox_TransType, 2); 
            RightTableLayoutPanel.Controls.Add(textBox_TransType, 2, 2);

            checkBox_TransDate.Dock = DockStyle.Fill;
            checkBox_TransDate.Margin = new Padding(5, 5, 0, 5);
            checkBox_TransDate.Anchor = AnchorStyles.Left;
            checkBox_TransDate.AutoSize = false;
            SetupCheckBox(checkBox_TransDate); // 调整字体大小以适应CheckBox
            RightTableLayoutPanel.Controls.Add(checkBox_TransDate, 0, 3);

            label_TransDate.Dock = DockStyle.Fill;
            label_TransDate.TextAlign = ContentAlignment.MiddleLeft;
            label_TransDate.Margin = new Padding(5, 0, 5, 0);
            label_TransDate.AutoSize = false;
            RightTableLayoutPanel.Controls.Add(label_TransDate, 1, 3);

            textBox_TransDate.Text = DateTime.Now.ToString("yyMMdd");
            SetupTextBox(textBox_TransDate, 3); // 设置初始大小
            RightTableLayoutPanel.Controls.Add(textBox_TransDate, 2, 3);

            checkBox_TransTime.Dock = DockStyle.Fill;
            checkBox_TransTime.Margin = new Padding(5, 5, 0, 5);
            checkBox_TransTime.Anchor = AnchorStyles.Left;
            checkBox_TransTime.AutoSize = false;
            SetupCheckBox(checkBox_TransTime); // 调整字体大小以适应CheckBox
            RightTableLayoutPanel.Controls.Add(checkBox_TransTime, 0, 4);

            label_TransTime.Dock = DockStyle.Fill;
            label_TransTime.TextAlign = ContentAlignment.MiddleLeft;
            label_TransTime.Margin = new Padding(5, 0, 5, 0);
            label_TransTime.AutoSize = false;
            RightTableLayoutPanel.Controls.Add(label_TransTime, 1, 4);

            textBox_TransTime.Text = DateTime.Now.ToString("HHmmss");
            SetupTextBox(textBox_TransTime, 4); // 设置初始大小
            RightTableLayoutPanel.Controls.Add(textBox_TransTime, 2, 4);

            checkBox_AccountType.Dock = DockStyle.Fill;
            checkBox_AccountType.Margin = new Padding(5, 5, 0, 5);
            checkBox_AccountType.Anchor = AnchorStyles.Left;
            checkBox_AccountType.AutoSize = false;
            SetupCheckBox(checkBox_AccountType); // 调整字体大小以适应CheckBox
            RightTableLayoutPanel.Controls.Add(checkBox_AccountType, 0, 5);

            label_AccountType.Dock = DockStyle.Fill;
            label_AccountType.TextAlign = ContentAlignment.MiddleLeft;
            label_AccountType.Margin = new Padding(5, 0, 5, 0);
            label_AccountType.AutoSize = false;
            RightTableLayoutPanel.Controls.Add(label_AccountType, 1, 5);

            textBox_AccountType.Text = "00";
            SetupTextBox(textBox_AccountType, 5);
            RightTableLayoutPanel.Controls.Add(textBox_AccountType, 2, 5);

            checkBox_MerchCustomData.Dock = DockStyle.Fill;
            checkBox_MerchCustomData.Margin = new Padding(5, 5, 0, 5);
            checkBox_MerchCustomData.Anchor = AnchorStyles.Left;
            checkBox_MerchCustomData.AutoSize = false;
            SetupCheckBox(checkBox_MerchCustomData); // 调整字体大小以适应CheckBox
            RightTableLayoutPanel.Controls.Add(checkBox_MerchCustomData, 0, 6);

            label_MerchCustomData.Dock = DockStyle.Fill;
            label_MerchCustomData.TextAlign = ContentAlignment.MiddleLeft;
            label_MerchCustomData.Margin = new Padding(5, 0, 5, 0);
            label_MerchCustomData.AutoSize = false;
            RightTableLayoutPanel.Controls.Add(label_MerchCustomData, 1, 6);

            textBox_MerchCustomData.Text = "1122334455667788990011223344556677889900";
            SetupTextBox(textBox_MerchCustomData, 6); // 设置初始大小
            RightTableLayoutPanel.Controls.Add(textBox_MerchCustomData, 2, 6);

            checkBox_TransCateCode.Dock = DockStyle.Fill;
            checkBox_TransCateCode.Margin = new Padding(5, 5, 0, 5);
            checkBox_TransCateCode.Anchor = AnchorStyles.Left;
            checkBox_TransCateCode.AutoSize = false;
            SetupCheckBox(checkBox_TransCateCode); // 调整字体大小以适应CheckBox
            RightTableLayoutPanel.Controls.Add(checkBox_TransCateCode, 0, 7);

            label_TransCateCode.Dock = DockStyle.Fill;
            label_TransCateCode.TextAlign = ContentAlignment.MiddleLeft;
            label_TransCateCode.Margin = new Padding(5, 0, 5, 0);
            label_TransCateCode.AutoSize = false;
            RightTableLayoutPanel.Controls.Add(label_TransCateCode, 1, 7);

            textBox_TransCateCode.Text = "46";
            SetupTextBox(textBox_TransCateCode, 7); // 设置初始大小
            RightTableLayoutPanel.Controls.Add(textBox_TransCateCode, 2, 7);

            checkBox_TransCurrCode.Dock = DockStyle.Fill;
            checkBox_TransCurrCode.Margin = new Padding(5, 5, 0, 5);
            checkBox_TransCurrCode.Anchor = AnchorStyles.Left;
            checkBox_TransCurrCode.AutoSize = false;
            SetupCheckBox(checkBox_TransCurrCode); // 调整字体大小以适应CheckBox
            RightTableLayoutPanel.Controls.Add(checkBox_TransCurrCode, 0, 8);

            label_TransCurrCode.Dock = DockStyle.Fill;
            label_TransCurrCode.TextAlign = ContentAlignment.MiddleLeft;
            label_TransCurrCode.Margin = new Padding(5, 0, 5, 0);
            label_TransCurrCode.AutoSize = false;
            RightTableLayoutPanel.Controls.Add(label_TransCurrCode, 1, 8);

            textBox_TransCurrCode.Text = "0978";
            SetupTextBox(textBox_TransCurrCode, 8); // 设置初始大小
            RightTableLayoutPanel.Controls.Add(textBox_TransCurrCode, 2, 8);

            checkBox_TransCurrExp.Dock = DockStyle.Fill;
            checkBox_TransCurrExp.Margin = new Padding(5, 5, 0, 5);
            checkBox_TransCurrExp.Anchor = AnchorStyles.Left;
            checkBox_TransCurrExp.AutoSize = false;
            SetupCheckBox(checkBox_TransCurrExp); // 调整字体大小以适应CheckBox
            RightTableLayoutPanel.Controls.Add(checkBox_TransCurrExp, 0, 9);

            label_TransCurrExp.Dock = DockStyle.Fill;
            label_TransCurrExp.TextAlign = ContentAlignment.MiddleLeft;
            label_TransCurrExp.Margin = new Padding(5, 0, 5, 0);
            label_TransCurrExp.AutoSize = false;
            RightTableLayoutPanel.Controls.Add(label_TransCurrExp, 1, 9);

            textBox_TransCurrExp.Text = "02";
            SetupTextBox(textBox_TransCurrExp, 9); 
            RightTableLayoutPanel.Controls.Add(textBox_TransCurrExp, 2, 9);

            checkBox_TACOnline.Dock = DockStyle.Fill;
            checkBox_TACOnline.Margin = new Padding(5, 5, 0, 5);
            checkBox_TACOnline.Anchor = AnchorStyles.Left;
            checkBox_TACOnline.AutoSize = false;
            SetupCheckBox(checkBox_TACOnline); // 调整字体大小以适应CheckBox
            RightTableLayoutPanel.Controls.Add(checkBox_TACOnline, 0, 10);

            label_TACOnline.Dock = DockStyle.Fill;
            label_TACOnline.TextAlign = ContentAlignment.MiddleLeft;
            label_TACOnline.Margin = new Padding(5, 0, 5, 0);
            label_TACOnline.AutoSize = false;
            RightTableLayoutPanel.Controls.Add(label_TACOnline, 1, 10);

            textBox_TACOnline.Text = "0000000000";
            SetupTextBox(textBox_TACOnline, 10); // 设置初始大小
            RightTableLayoutPanel.Controls.Add(textBox_TACOnline, 2, 10);

            checkBox_ProperTag.Dock = DockStyle.Fill;
            checkBox_ProperTag.Margin = new Padding(5, 5, 0, 5);
            checkBox_ProperTag.Anchor = AnchorStyles.Left;
            checkBox_ProperTag.AutoSize = false;
            SetupCheckBox(checkBox_ProperTag); // 调整字体大小以适应CheckBox
            RightTableLayoutPanel.Controls.Add(checkBox_ProperTag, 0, 11);

            label_ProperTag.Dock = DockStyle.Fill;
            label_ProperTag.TextAlign = ContentAlignment.MiddleLeft;
            label_ProperTag.Margin = new Padding(5, 0, 5, 0);
            label_ProperTag.AutoSize = false;
            RightTableLayoutPanel.Controls.Add(label_ProperTag, 1, 11);

            SetupTextBox(textBox_ProperTag, 11); // 设置初始大小
            RightTableLayoutPanel.Controls.Add(textBox_ProperTag, 2, 11);

            // Update all calls to the renamed method.  
            UpdateTextBoxSizeForRow(textBox_ProperTag, 11); // 设置初始大小  
            AdjustLabelFontSize();
        }

        private void SetCenteredTextBoxMargin(System.Windows.Forms.TextBox textBox, int rowIndex, int minMargin = 5)
        {
            // 获取当前行和列的尺寸
            int[] rowHeights = RightTableLayoutPanel.GetRowHeights();
            int[] colWidths = RightTableLayoutPanel.GetColumnWidths();

            // 计算可用空间（减去TextBox自身大小）
            int availableHeight = rowHeights[rowIndex] - textBox.Height;
            int availableWidth = colWidths[2] - textBox.Width; // 第3列是TextBox列

            // 计算居中边距（至少保留minMargin）
            int topMargin = Math.Max(minMargin, availableHeight / 2);
            int leftMargin = Math.Max(minMargin, availableWidth / 2);

            // 确保对称且不超过最大合理值
            topMargin = Math.Min(topMargin, rowHeights[rowIndex] / 3);
            leftMargin = Math.Min(leftMargin, colWidths[2] / 3);

            // 设置Margin（保持左右/上下对称）
            textBox.Margin = new Padding(
                leftMargin,
                topMargin,
                leftMargin,
                topMargin);

            // 设置锚点确保正确布局
        }

        private void UpdateTextBoxSize(System.Windows.Forms.TextBox textBox, int rowIndex)
        {
            int rowHeight = RightTableLayoutPanel.GetRowHeights()[rowIndex];
            int colWidth = RightTableLayoutPanel.GetColumnWidths()[2];

            // 设置TextBox大小（占满整个列宽，高度适应行高）
            textBox.Width = colWidth - textBox.Margin.Horizontal;
            textBox.Height = Math.Max(20, rowHeight - textBox.Margin.Vertical);
        }

        private void ButtonPosition(System.Windows.Forms.Panel panel, System.Windows.Forms.Button button)
        {
            // 计算按钮大小（Panel可用区域的60%）
            int btnWidth = (int)(panel.ClientSize.Width * 0.6);
            int btnHeight = (int)(panel.ClientSize.Height * 0.6);

            // 确保最小尺寸
            btnWidth = Math.Max(btnWidth, 50);
            btnHeight = Math.Max(btnHeight, 30);

            button.Size = new Size(btnWidth, btnHeight);

            // 计算居中位置
            button.Left = (panel.ClientSize.Width - button.Width) / 2;
            button.Top = (panel.ClientSize.Height - button.Height) / 2;
            AdjustFontSizeToFit(button);
        }

        private void AdjustFontSizeToFit(System.Windows.Forms.Control control, float minSize = 8, float maxSize = 20)
        {
            if (string.IsNullOrEmpty(control.Text)) 
            {
                return;
            }

            SizeF textSize;
            float fontSize = maxSize;
            Font font;

            using (var graphics = control.CreateGraphics())
            {
                do
                {
                    font = new Font(control.Font.FontFamily, fontSize, control.Font.Style);
                    textSize = graphics.MeasureString(control.Text, font);
                    fontSize -= 0.5f;
                } while ((textSize.Width > control.ClientSize.Width ||
                         textSize.Height > control.ClientSize.Height) &&
                         fontSize > minSize);
            }

            // 应用新字体大小
            control.Font = new Font(control.Font.FontFamily, fontSize, control.Font.Style);
        }

        private void AdjustLabelFontSize()
        {
            // 找出所有Label中最小的合适字号
            float minFontSize = 12f; // 初始值

            using (var graphics = RightTableLayoutPanel.CreateGraphics())
            {
                foreach (System.Windows.Forms.Control control in RightTableLayoutPanel.Controls)
                {
                    if (control is System.Windows.Forms.Label label && !string.IsNullOrEmpty(label.Text))
                    {
                        float fontSize = 12f;
                        SizeF textSize;

                        do
                        {
                            Font font = new Font(label.Font.FontFamily, fontSize, label.Font.Style);
                            textSize = graphics.MeasureString(label.Text, font);
                            fontSize -= 0.5f;
                        } while (textSize.Width > label.Width && fontSize > 8f);

                        minFontSize = Math.Min(minFontSize, fontSize);
                    }
                }
            }

            // 应用统一字号
            foreach (System.Windows.Forms.Control control in RightTableLayoutPanel.Controls)
            {
                if (control is System.Windows.Forms.Label label)
                {
                    label.Font = new Font(label.Font.FontFamily, minFontSize, label.Font.Style);
                }
            }
        }

        private void SetupCheckBox(System.Windows.Forms.CheckBox checkBox)
        {
            checkBox.AutoSize = true;  // 关键设置
            checkBox.Margin = new Padding(5);
            checkBox.Anchor = AnchorStyles.Left | AnchorStyles.Right; // 使CheckBox在单元格内水平居中对齐
            checkBox.TextAlign = ContentAlignment.MiddleLeft;
            checkBox.CheckAlign = ContentAlignment.MiddleLeft;

            // 确保有足够的空间显示文本
            checkBox.MinimumSize = new Size(100, 20);
        }

        private void SetupTextBox(System.Windows.Forms.TextBox textBox, int rowIndex)
        {
            textBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            SetCenteredTextBoxMargin(textBox, rowIndex);
            UpdateTextBoxSize(textBox, rowIndex);
            AdjustFontSizeToFit(textBox);
        }

        private void TestForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (_tcpServer != null && _tcpServer.Listening)
                {
                    _tcpServer.StopListening();
                }
                if (_tcpClient != null && _tcpClient.Connected)
                { 
                    _tcpClient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"Close TestForm Exception: {ex.Message}"); ;
            }
        }

        private void button_ACT_Click(object sender, EventArgs e)
        {
            try
            {
                string data = @"{
                                   ""signalType"": ""ACT"",  
                                   ""signalData"": [ 
                                       { ""id"": ""5F57"", ""value"": null },                                       
                                       { ""id"": ""DF8104"", ""value"": null },  
                                       { ""id"": ""DF8105"", ""value"": null },  
                                       { ""id"": ""9F7C"", ""value"": ""1122334455667788990011223344556677889900"" },  
                                       { ""id"": ""9F53"", ""value"": ""46"" },  
                                       { ""id"": ""5F2A"", ""value"": ""0978"" },  
                                       { ""id"": ""5F36"", ""value"": ""02"" },                                     
                                       { ""id"": ""9F21"", ""value"": ""195212"" }  
                                   ]  
                               }";
                JObject jsonObject = new JObject
                {
                    ["signalType"] = "ACT"
                };

                JArray signalData = new JArray();

                string amount = textBox_Amt.Text.Trim();
                MyLogManager.Log($"amount:{amount}");

                if (string.IsNullOrEmpty(amount))
                {
                    amount = "000000000001";
                }
                else
                {
                    amount = new string(amount.Where(char.IsDigit).ToArray());

                    // 如果输入超过12位，截取最后12位
                    if (amount.Length > 12)
                    {
                        amount = amount.Substring(amount.Length - 12);
                    }
                    // 填充到12位
                    amount = amount.PadLeft(12, '0');
                }

                if(checkBox_Amt.Checked)
                {
                    signalData.Add(new JObject(new JProperty("id", "9F02"), new JProperty("value", amount)));
                }

                string amountOther = this.textBox_AmtOth.Text.Trim();
                if(amountOther.Length > 12)
                {
                    amountOther = amountOther.Substring(amountOther.Length - 12);
                }
                amountOther = amountOther.PadLeft(12, '0');
                if (checkBox_AmtOth.Checked)
                {
                    signalData.Add(new JObject(new JProperty("id", "9F03"), new JProperty("value", string.IsNullOrEmpty(amountOther) ? null : amountOther)));
                }

                string transType = this.textBox_TransType.Text.Trim();
                if(string.IsNullOrEmpty(transType))
                {
                    transType = "00"; // 默认值
                }
                else if (transType.Length > 2)
                {
                    transType = transType.Substring(transType.Length - 2);
                }
                signalData.Add(new JObject(new JProperty("id", "9C"), new JProperty("value", transType)));

                string transDate = this.textBox_TransDate.Text.Trim();
                if (string.IsNullOrEmpty(transDate))
                {
                    transDate = DateTime.Now.ToString("yyMMdd"); // 默认值为当前日期
                }
                else if (transDate.Length > 6)
                {
                    transDate = transDate.Substring(transDate.Length - 6);
                }
                if(checkBox_TransDate.Checked)
                {
                    signalData.Add(new JObject(new JProperty("id", "9A"), new JProperty("value", transDate)));
                }

                string transTime = this.textBox_TransTime.Text.Trim();
                if (string.IsNullOrEmpty(transTime))
                {
                    transTime = DateTime.Now.ToString("HHmmss"); // 默认值为当前时间
                }
                else if (transTime.Length > 6)
                {
                    transTime = transTime.Substring(transTime.Length - 6);
                }
                if (checkBox_TransTime.Checked)
                {
                    signalData.Add(new JObject(new JProperty("id", "9F21"), new JProperty("value", transTime)));
                }

                string accountType = this.textBox_AccountType.Text.Trim();
                if (string.IsNullOrEmpty(accountType))
                {
                    accountType = "00"; // 默认值
                }
                else if (accountType.Length > 2)
                {
                    accountType = accountType.Substring(accountType.Length - 2);
                }
                if (checkBox_AccountType.Checked)
                {
                    signalData.Add(new JObject(new JProperty("id", "5F57"), new JProperty("value", accountType)));
                }

                string merchCustomData = this.textBox_MerchCustomData.Text.Trim();
                if (string.IsNullOrEmpty(merchCustomData))
                {
                    merchCustomData = "1122334455667788990011223344556677889900"; // 默认值
                }
                else if (merchCustomData.Length > 40)
                {
                    merchCustomData = merchCustomData.Substring(merchCustomData.Length - 40);
                }
                if (checkBox_MerchCustomData.Checked)
                {
                    signalData.Add(new JObject(new JProperty("id", "9F7C"), new JProperty("value", merchCustomData)));
                }

                string transCateCode = this.textBox_TransCateCode.Text.Trim();
                if(string.IsNullOrEmpty(transCateCode))
                {
                    transCateCode = "46";
                }
                else if(transCateCode.Length > 2)
                {
                    transCateCode = transCateCode.Substring(transCateCode.Length - 2);
                }
                if (checkBox_TransCateCode.Checked)
                {
                    signalData.Add(new JObject(new JProperty("id", "9F53"), new JProperty("value", transCateCode)));
                }

                string transCurrCode = this.textBox_TransCurrCode.Text.Trim();
                if(string.IsNullOrEmpty(transCurrCode))
                {
                    transCurrCode = "0978"; // 默认值
                }
                else if(transCurrCode.Length > 4)
                {
                    transCurrCode = transCurrCode.Substring(transCurrCode.Length - 4);
                }
                if (checkBox_TransCurrCode.Checked)
                    signalData.Add(new JObject(new JProperty("id", "5F2A"), new JProperty("value", transCurrCode)));

                string transCurrExp = this.textBox_TransCurrExp.Text.Trim();
                if(string.IsNullOrEmpty(transCurrExp))
                {
                    transCurrExp = "02"; // 默认值
                }
                else if(transCurrExp.Length > 2)
                {
                    transCurrExp = transCurrExp.Substring(transCurrExp.Length - 2);
                }
                if (checkBox_TransCurrExp.Checked)
                    signalData.Add(new JObject(new JProperty("id", "5F36"), new JProperty("value", transCurrExp)));

                string tacOnline = this.textBox_TACOnline.Text.Trim();
                if (string.IsNullOrEmpty(tacOnline))
                {
                    tacOnline = "0000000000"; // 默认值
                }
                else if (tacOnline.Length > 6)
                {
                    tacOnline = tacOnline.Substring(tacOnline.Length - 6);
                }
                if (checkBox_TACOnline.Checked)
                    signalData.Add(new JObject(new JProperty("id", "DF8122"), new JProperty("value", tacOnline)));

                jsonObject["signalData"] = signalData;

                // 发送数据  
                string finalData = jsonObject.ToString();
                _tcpClient.SendString(finalData);
                MyLogManager.Log($"client send data:{finalData}");
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        private void button_CLEAN_Click(object sender, EventArgs e)
        {
            try
            {
                string data = @"{
                                    ""signalType"":""CLEAN"", 
                                    ""signalData"":[
                                         {""id"":""9A"", ""value"":""170303""},
                                         {""id"":""9F21"", ""value"":""133333""}
                                        ]
                                }";

                _tcpServer.SendString( _connectID, data);
            }
            catch (Exception ex) 
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        private void button_CONFIG_Click(object sender, EventArgs e)
        {
            try
            {
                string data = @"{
                                    ""signalType"":""CONFIG"", 
                                    ""signalData"":[
                                        {""id"":""CONF_NAME"", ""value"":""PPS_MChip1""}
                                    ]
                                }";

                _tcpServer.SendString(_connectID, data);
            }
            catch (Exception ex)
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        private void button_DET_Click(object sender, EventArgs e)
        {
            try
            {
                string data = @"{
                                    ""signalType"":""DET"", 
                                    ""signalData"":[
                                        {""id"":""DET"",""value"":""DF81120182""}
                                    ]
                                }";

                _tcpServer.SendString(_connectID, data);
            }
            catch (Exception ex) 
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        private void button_RUNTEST_RESULT_Click(object sender, EventArgs e)
        {
            try
            {
                string data = @"{
                                    ""signalType"":""RUNTEST_ RESULT"",
                                    ""signalData"":[
                                        {
                                            ""id"":""TestResult"", 
                                            ""value"":""PASS""
                                        }
                                    ]
                                }";

                _tcpServer.SendString (_connectID, data);

            }
            catch(Exception ex)
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        private void button_STOP_Click(object sender, EventArgs e)
        {

        }

        private void button_TEST_INFO_Click(object sender, EventArgs e)
        {
            try
            {
                string data = @"{""signalType"":""TEST_INFO"", ""signalData"":[
                                     {""id"":""InterfaceVersion"", ""value"":""20170303""},
                                     {""id"":""SessionId"", ""value"":""Session_20150829_093731""},
                                     {""id"":""TestId"", ""value"":""3GX2-1600-03""},
                                     {""id"":""DekDetId"", ""value"":""3MX2-2500""}
                                    ]}";

                _tcpServer.SendString ( _connectID, data);
            }
            catch (Exception ex) 
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if(_tcpServer.Listening)
                {
                    _tcpServer.StopListening();
                }

                //if (int.TryParse(this.textBox_TranType.Text, out _port))
                if(false)
                {

                }
                else
                {
                    MyLogManager.Log($"Parse Port to INT error");

                    // Replace the problematic line with the following:
                    MessageBox.Show("Parse Port to INT error", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                _tcpServer.Port = _port;
                _tcpServer.OnStarted += (s, ev) =>
                {
                    MyLogManager.Log($"IN TestForm Server Started Listen on {_tcpServer.Port}");
                };
                _tcpServer.OnError += (s, ev) =>
                {
                    MyLogManager.Log($"IN TestForm Server Error: {ev.Exception.Message}");
                };
                _tcpServer.OnStopped += (s, ev) =>
                {
                    MyLogManager.Log($"IN TestForm Server Stop Listen: {ev.IsStopped}");
                };
                _tcpServer.OnConnected += (s, ev) =>
                {
                    _connectID = ev.ConnectionId;
                    MyLogManager.Log($"IN TestForm Server Connect on {ev.IPAddress}:{ev.Port}, Connect ID is: {ev.ConnectionId}");
                };
                _tcpServer.OnDataReceived += (s, ev) =>
                {
                    MyLogManager.Log($"Server Receive Data: {ev.Data.ToString()}");
                };


                _tcpServer.StartListening();
            }
            catch(Exception ex)
            {
                MyLogManager.Log($"Exception: {ex.Message}");
            }
        }

        public int Port 
        { 
            get => _port;
            private set => _port = value;
        }

        private void TestForm_Resize(object sender, EventArgs e)
        {
            mainSplitContainer.SplitterDistance = (int)(this.ClientSize.Width * 0.3);
            // 更新左侧按钮位置和大小
            ButtonPosition(panel_ACT, button_ACT);
            ButtonPosition(panel_LoopACT, button_LoopACT);
            ButtonPosition(panel_Stop, button_Stop);
            ButtonPosition(panel_DET, button_DET);

            // 更新右侧控件字体大小
            AdjustLabelFontSize();
            foreach (System.Windows.Forms.Control control in RightTableLayoutPanel.Controls)
            {
                if (control is System.Windows.Forms.Button || control is System.Windows.Forms.TextBox || control is System.Windows.Forms.CheckBox checkbox)
                {
                    AdjustFontSizeToFit(control);
                }
            }

            // 更新右侧TextBox大小
            for (int i = 0; i < RightTableLayoutPanel.RowCount; i++)
            {
                if (RightTableLayoutPanel.GetControlFromPosition(2, i) is System.Windows.Forms.TextBox textBox)
                {
                    UpdateTextBoxSize(textBox, i);
                }
            }
        }

        private void button_LoopACT_Click(object sender, EventArgs e)
        {

        }

        private void panel_ACT_SizeChanged(object sender, EventArgs e)
        {
            ButtonPosition(panel_ACT, button_ACT);
        }

        private void panel_LoopACT_SizeChanged(object sender, EventArgs e)
        {
            ButtonPosition(panel_LoopACT, button_LoopACT);
        }

        private void panel_Stop_SizeChanged(object sender, EventArgs e)
        {
            ButtonPosition(panel_Stop, button_Stop);
        }

        private void panel_DET_SizeChanged(object sender, EventArgs e)
        {
            ButtonPosition(panel_DET, button_DET);
        }

        // The issue arises because there are two methods with the same name `UpdateTextBoxSize` and identical parameter types,  
        // causing ambiguity. To resolve this, one of the methods should be renamed to avoid the conflict.  

        // Rename the second `UpdateTextBoxSize` method to `UpdateTextBoxSizeForRow`.  
        private void UpdateTextBoxSizeForRow(System.Windows.Forms.TextBox textBox, int rowIndex)
        {
            int rowHeight = RightTableLayoutPanel.GetRowHeights()[rowIndex];
            int colWidth = RightTableLayoutPanel.GetColumnWidths()[2];

            // 设置TextBox大小（占满整个列宽，高度适应行高）  
            textBox.Width = colWidth - textBox.Margin.Horizontal;
            textBox.Height = Math.Max(20, rowHeight - textBox.Margin.Vertical);
        }
    }
}
