namespace ModbusRTUTool
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            buttonConnect = new Button();
            comboBoxPort = new ComboBox();
            comboBoxBaudRate = new ComboBox();
            textBoxSend = new TextBox();
            richTextBoxRecv = new RichTextBox();
            buttonSend = new Button();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            SuspendLayout();
            // 
            // buttonConnect
            // 
            buttonConnect.Location = new Point(440, 55);
            buttonConnect.Name = "buttonConnect";
            buttonConnect.Size = new Size(133, 63);
            buttonConnect.TabIndex = 0;
            buttonConnect.Text = "Connect";
            buttonConnect.UseVisualStyleBackColor = true;
            buttonConnect.Click += buttonConnect_Click;
            // 
            // comboBoxPort
            // 
            comboBoxPort.FormattingEnabled = true;
            comboBoxPort.Location = new Point(108, 44);
            comboBoxPort.Name = "comboBoxPort";
            comboBoxPort.Size = new Size(240, 32);
            comboBoxPort.TabIndex = 1;
            // 
            // comboBoxBaudRate
            // 
            comboBoxBaudRate.FormattingEnabled = true;
            comboBoxBaudRate.Location = new Point(108, 106);
            comboBoxBaudRate.Name = "comboBoxBaudRate";
            comboBoxBaudRate.Size = new Size(240, 32);
            comboBoxBaudRate.TabIndex = 2;
            // 
            // textBoxSend
            // 
            textBoxSend.Location = new Point(96, 203);
            textBoxSend.Name = "textBoxSend";
            textBoxSend.Size = new Size(439, 30);
            textBoxSend.TabIndex = 3;
            textBoxSend.Text = "01 03 34 00 00 02";
            textBoxSend.TextChanged += textBoxSend_TextChanged;
            // 
            // richTextBoxRecv
            // 
            richTextBoxRecv.HideSelection = false;
            richTextBoxRecv.Location = new Point(96, 312);
            richTextBoxRecv.Name = "richTextBoxRecv";
            richTextBoxRecv.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBoxRecv.Size = new Size(544, 148);
            richTextBoxRecv.TabIndex = 4;
            richTextBoxRecv.Text = "";
            // 
            // buttonSend
            // 
            buttonSend.Location = new Point(578, 187);
            buttonSend.Name = "buttonSend";
            buttonSend.Size = new Size(133, 63);
            buttonSend.TabIndex = 5;
            buttonSend.Text = "Send";
            buttonSend.UseVisualStyleBackColor = true;
            buttonSend.Click += buttonSend_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(8, 41);
            label1.Name = "label1";
            label1.Size = new Size(46, 24);
            label1.TabIndex = 6;
            label1.Text = "串口";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(8, 109);
            label2.Name = "label2";
            label2.Size = new Size(64, 24);
            label2.TabIndex = 7;
            label2.Text = "波特率";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(8, 206);
            label3.Name = "label3";
            label3.Size = new Size(82, 24);
            label3.TabIndex = 8;
            label3.Text = "发送数据";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(8, 333);
            label4.Name = "label4";
            label4.Size = new Size(82, 24);
            label4.TabIndex = 9;
            label4.Text = "数据响应";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(818, 492);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(buttonSend);
            Controls.Add(richTextBoxRecv);
            Controls.Add(textBoxSend);
            Controls.Add(comboBoxBaudRate);
            Controls.Add(comboBoxPort);
            Controls.Add(buttonConnect);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button buttonConnect;
        private ComboBox comboBoxPort;
        private ComboBox comboBoxBaudRate;
        private TextBox textBoxSend;
        private RichTextBox richTextBoxRecv;
        private Button buttonSend;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
    }
}
