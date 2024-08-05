namespace MastercardHost
{
    partial class TestForm
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
            this.button_ACT = new System.Windows.Forms.Button();
            this.button_CLEAN = new System.Windows.Forms.Button();
            this.button_CONFIG = new System.Windows.Forms.Button();
            this.button_DET = new System.Windows.Forms.Button();
            this.button_RUNTEST_RESULT = new System.Windows.Forms.Button();
            this.button_STOP = new System.Windows.Forms.Button();
            this.button_TEST_INFO = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button_ACT
            // 
            this.button_ACT.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_ACT.Location = new System.Drawing.Point(62, 45);
            this.button_ACT.Name = "button_ACT";
            this.button_ACT.Size = new System.Drawing.Size(105, 53);
            this.button_ACT.TabIndex = 0;
            this.button_ACT.Text = "ACT";
            this.button_ACT.UseVisualStyleBackColor = true;
            this.button_ACT.Click += new System.EventHandler(this.button_ACT_Click);
            // 
            // button_CLEAN
            // 
            this.button_CLEAN.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_CLEAN.Location = new System.Drawing.Point(243, 45);
            this.button_CLEAN.Name = "button_CLEAN";
            this.button_CLEAN.Size = new System.Drawing.Size(105, 53);
            this.button_CLEAN.TabIndex = 1;
            this.button_CLEAN.Text = "CLEAN";
            this.button_CLEAN.UseVisualStyleBackColor = true;
            this.button_CLEAN.Click += new System.EventHandler(this.button_CLEAN_Click);
            // 
            // button_CONFIG
            // 
            this.button_CONFIG.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_CONFIG.Location = new System.Drawing.Point(443, 45);
            this.button_CONFIG.Name = "button_CONFIG";
            this.button_CONFIG.Size = new System.Drawing.Size(105, 53);
            this.button_CONFIG.TabIndex = 2;
            this.button_CONFIG.Text = "CONFIG";
            this.button_CONFIG.UseVisualStyleBackColor = true;
            this.button_CONFIG.Click += new System.EventHandler(this.button_CONFIG_Click);
            // 
            // button_DET
            // 
            this.button_DET.Font = new System.Drawing.Font("宋体", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.button_DET.Location = new System.Drawing.Point(62, 135);
            this.button_DET.Name = "button_DET";
            this.button_DET.Size = new System.Drawing.Size(105, 53);
            this.button_DET.TabIndex = 3;
            this.button_DET.Text = "DET";
            this.button_DET.UseVisualStyleBackColor = true;
            this.button_DET.Click += new System.EventHandler(this.button_DET_Click);
            // 
            // button_RUNTEST_RESULT
            // 
            this.button_RUNTEST_RESULT.Font = new System.Drawing.Font("宋体", 10F);
            this.button_RUNTEST_RESULT.Location = new System.Drawing.Point(243, 135);
            this.button_RUNTEST_RESULT.Name = "button_RUNTEST_RESULT";
            this.button_RUNTEST_RESULT.Size = new System.Drawing.Size(105, 53);
            this.button_RUNTEST_RESULT.TabIndex = 4;
            this.button_RUNTEST_RESULT.Text = "RUNTEST_RESULT";
            this.button_RUNTEST_RESULT.UseVisualStyleBackColor = true;
            this.button_RUNTEST_RESULT.Click += new System.EventHandler(this.button_RUNTEST_RESULT_Click);
            // 
            // button_STOP
            // 
            this.button_STOP.Font = new System.Drawing.Font("宋体", 14.25F);
            this.button_STOP.Location = new System.Drawing.Point(443, 135);
            this.button_STOP.Name = "button_STOP";
            this.button_STOP.Size = new System.Drawing.Size(105, 53);
            this.button_STOP.TabIndex = 5;
            this.button_STOP.Text = "STOP";
            this.button_STOP.UseVisualStyleBackColor = true;
            this.button_STOP.Click += new System.EventHandler(this.button_STOP_Click);
            // 
            // button_TEST_INFO
            // 
            this.button_TEST_INFO.Font = new System.Drawing.Font("宋体", 13F);
            this.button_TEST_INFO.Location = new System.Drawing.Point(62, 237);
            this.button_TEST_INFO.Name = "button_TEST_INFO";
            this.button_TEST_INFO.Size = new System.Drawing.Size(105, 53);
            this.button_TEST_INFO.TabIndex = 6;
            this.button_TEST_INFO.Text = "TEST_INFO";
            this.button_TEST_INFO.UseVisualStyleBackColor = true;
            this.button_TEST_INFO.Click += new System.EventHandler(this.button_TEST_INFO_Click);
            // 
            // TestForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.button_TEST_INFO);
            this.Controls.Add(this.button_STOP);
            this.Controls.Add(this.button_RUNTEST_RESULT);
            this.Controls.Add(this.button_DET);
            this.Controls.Add(this.button_CONFIG);
            this.Controls.Add(this.button_CLEAN);
            this.Controls.Add(this.button_ACT);
            this.Name = "TestForm";
            this.Text = "TestForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.TestForm_FormClosing);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button_ACT;
        private System.Windows.Forms.Button button_CLEAN;
        private System.Windows.Forms.Button button_CONFIG;
        private System.Windows.Forms.Button button_DET;
        private System.Windows.Forms.Button button_RUNTEST_RESULT;
        private System.Windows.Forms.Button button_STOP;
        private System.Windows.Forms.Button button_TEST_INFO;
    }
}