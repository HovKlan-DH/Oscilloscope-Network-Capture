namespace Oscilloscope_Network_Capture
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
            this.comboBoxRegion = new System.Windows.Forms.ComboBox();
            this.buttonCaptureContinuelsy = new System.Windows.Forms.Button();
            this.richTextBoxLog = new System.Windows.Forms.RichTextBox();
            this.pictureBoxImage = new System.Windows.Forms.PictureBox();
            this.labelRegion = new System.Windows.Forms.Label();
            this.textBoxCaptureNumberStart = new System.Windows.Forms.TextBox();
            this.labelCaptureNumberStart = new System.Windows.Forms.Label();
            this.richTextBoxAction = new System.Windows.Forms.RichTextBox();
            this.labelComponent = new System.Windows.Forms.Label();
            this.textBoxComponent = new System.Windows.Forms.TextBox();
            this.labelAction = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageMeasurements = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.labelNewVersionAvailable = new System.Windows.Forms.Label();
            this.panel4 = new System.Windows.Forms.Panel();
            this.checkBoxDebug = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.checkBoxForceAcquisition = new System.Windows.Forms.CheckBox();
            this.checkBoxBeep = new System.Windows.Forms.CheckBox();
            this.textBoxPort = new System.Windows.Forms.TextBox();
            this.labelPort = new System.Windows.Forms.Label();
            this.textBoxIp = new System.Windows.Forms.TextBox();
            this.labelIp = new System.Windows.Forms.Label();
            this.buttonCheckScope = new System.Windows.Forms.Button();
            this.panel5 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonOpenFolder = new System.Windows.Forms.Button();
            this.textBoxCaptureFolder = new System.Windows.Forms.TextBox();
            this.textBoxFilenameFormat = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label6 = new System.Windows.Forms.Label();
            this.tabPageHelp = new System.Windows.Forms.TabPage();
            this.richTextBoxHelp = new System.Windows.Forms.RichTextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.tabPageAbout = new System.Windows.Forms.TabPage();
            this.richTextBoxAbout = new System.Windows.Forms.RichTextBox();
            this.labelProductVersion = new System.Windows.Forms.Label();
            this.labelProductName = new System.Windows.Forms.Label();
            this.pictureBoxIcon = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxImage)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPageMeasurements.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel4.SuspendLayout();
            this.panel5.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tabPageHelp.SuspendLayout();
            this.tabPageAbout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // comboBoxRegion
            // 
            this.comboBoxRegion.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBoxRegion.FormattingEnabled = true;
            this.comboBoxRegion.Items.AddRange(new object[] {
            "PAL",
            "NTSC"});
            this.comboBoxRegion.Location = new System.Drawing.Point(6, 36);
            this.comboBoxRegion.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.comboBoxRegion.Name = "comboBoxRegion";
            this.comboBoxRegion.Size = new System.Drawing.Size(69, 29);
            this.comboBoxRegion.TabIndex = 10;
            // 
            // buttonCaptureContinuelsy
            // 
            this.buttonCaptureContinuelsy.BackColor = System.Drawing.Color.Gold;
            this.buttonCaptureContinuelsy.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCaptureContinuelsy.Location = new System.Drawing.Point(6, 138);
            this.buttonCaptureContinuelsy.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.buttonCaptureContinuelsy.Name = "buttonCaptureContinuelsy";
            this.buttonCaptureContinuelsy.Size = new System.Drawing.Size(344, 27);
            this.buttonCaptureContinuelsy.TabIndex = 14;
            this.buttonCaptureContinuelsy.Text = "Capture continuesly";
            this.buttonCaptureContinuelsy.UseVisualStyleBackColor = false;
            // 
            // richTextBoxLog
            // 
            this.richTextBoxLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBoxLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBoxLog.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxLog.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.richTextBoxLog.Name = "richTextBoxLog";
            this.richTextBoxLog.Size = new System.Drawing.Size(488, 325);
            this.richTextBoxLog.TabIndex = 100;
            this.richTextBoxLog.TabStop = false;
            this.richTextBoxLog.Text = "";
            // 
            // pictureBoxImage
            // 
            this.pictureBoxImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxImage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pictureBoxImage.Location = new System.Drawing.Point(0, 0);
            this.pictureBoxImage.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBoxImage.Name = "pictureBoxImage";
            this.pictureBoxImage.Size = new System.Drawing.Size(488, 294);
            this.pictureBoxImage.TabIndex = 5;
            this.pictureBoxImage.TabStop = false;
            // 
            // labelRegion
            // 
            this.labelRegion.AutoSize = true;
            this.labelRegion.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelRegion.Location = new System.Drawing.Point(81, 39);
            this.labelRegion.Name = "labelRegion";
            this.labelRegion.Size = new System.Drawing.Size(126, 21);
            this.labelRegion.TabIndex = 100;
            this.labelRegion.Text = "Region (variable)";
            // 
            // textBoxCaptureNumberStart
            // 
            this.textBoxCaptureNumberStart.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxCaptureNumberStart.Location = new System.Drawing.Point(6, 103);
            this.textBoxCaptureNumberStart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxCaptureNumberStart.Name = "textBoxCaptureNumberStart";
            this.textBoxCaptureNumberStart.Size = new System.Drawing.Size(69, 28);
            this.textBoxCaptureNumberStart.TabIndex = 12;
            this.textBoxCaptureNumberStart.Text = "1";
            // 
            // labelCaptureNumberStart
            // 
            this.labelCaptureNumberStart.AutoSize = true;
            this.labelCaptureNumberStart.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCaptureNumberStart.Location = new System.Drawing.Point(81, 106);
            this.labelCaptureNumberStart.Name = "labelCaptureNumberStart";
            this.labelCaptureNumberStart.Size = new System.Drawing.Size(267, 21);
            this.labelCaptureNumberStart.TabIndex = 100;
            this.labelCaptureNumberStart.Text = "Start capture from number (variable)";
            // 
            // richTextBoxAction
            // 
            this.richTextBoxAction.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.richTextBoxAction.BackColor = System.Drawing.Color.WhiteSmoke;
            this.richTextBoxAction.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBoxAction.Font = new System.Drawing.Font("Calibri", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBoxAction.Location = new System.Drawing.Point(7, 25);
            this.richTextBoxAction.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.richTextBoxAction.Name = "richTextBoxAction";
            this.richTextBoxAction.ReadOnly = true;
            this.richTextBoxAction.Size = new System.Drawing.Size(344, 48);
            this.richTextBoxAction.TabIndex = 100;
            this.richTextBoxAction.TabStop = false;
            this.richTextBoxAction.Text = "Ready to capture; number 1 of 40";
            // 
            // labelComponent
            // 
            this.labelComponent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelComponent.AutoSize = true;
            this.labelComponent.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelComponent.Location = new System.Drawing.Point(81, 73);
            this.labelComponent.Name = "labelComponent";
            this.labelComponent.Size = new System.Drawing.Size(204, 21);
            this.labelComponent.TabIndex = 100;
            this.labelComponent.Text = "Component name (variable)";
            // 
            // textBoxComponent
            // 
            this.textBoxComponent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxComponent.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxComponent.Location = new System.Drawing.Point(6, 70);
            this.textBoxComponent.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxComponent.Name = "textBoxComponent";
            this.textBoxComponent.Size = new System.Drawing.Size(69, 28);
            this.textBoxComponent.TabIndex = 11;
            // 
            // labelAction
            // 
            this.labelAction.AutoSize = true;
            this.labelAction.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAction.Location = new System.Drawing.Point(2, 5);
            this.labelAction.Name = "labelAction";
            this.labelAction.Size = new System.Drawing.Size(55, 21);
            this.labelAction.TabIndex = 100;
            this.labelAction.Text = "Action";
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPageMeasurements);
            this.tabControl1.Controls.Add(this.tabPageHelp);
            this.tabControl1.Controls.Add(this.tabPageAbout);
            this.tabControl1.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tabControl1.Location = new System.Drawing.Point(4, 4);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(860, 657);
            this.tabControl1.TabIndex = 17;
            // 
            // tabPageMeasurements
            // 
            this.tabPageMeasurements.Controls.Add(this.splitContainer1);
            this.tabPageMeasurements.Controls.Add(this.panel4);
            this.tabPageMeasurements.Controls.Add(this.panel5);
            this.tabPageMeasurements.Controls.Add(this.panel3);
            this.tabPageMeasurements.Controls.Add(this.panel1);
            this.tabPageMeasurements.Location = new System.Drawing.Point(4, 30);
            this.tabPageMeasurements.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabPageMeasurements.Name = "tabPageMeasurements";
            this.tabPageMeasurements.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabPageMeasurements.Size = new System.Drawing.Size(852, 623);
            this.tabPageMeasurements.TabIndex = 0;
            this.tabPageMeasurements.Text = "Measurements";
            this.tabPageMeasurements.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(364, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.labelNewVersionAvailable);
            this.splitContainer1.Panel1.Controls.Add(this.richTextBoxLog);
            this.splitContainer1.Panel1MinSize = 50;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.pictureBoxImage);
            this.splitContainer1.Panel2MinSize = 50;
            this.splitContainer1.Size = new System.Drawing.Size(488, 623);
            this.splitContainer1.SplitterDistance = 325;
            this.splitContainer1.TabIndex = 26;
            // 
            // labelNewVersionAvailable
            // 
            this.labelNewVersionAvailable.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelNewVersionAvailable.AutoSize = true;
            this.labelNewVersionAvailable.BackColor = System.Drawing.Color.IndianRed;
            this.labelNewVersionAvailable.ForeColor = System.Drawing.Color.White;
            this.labelNewVersionAvailable.Location = new System.Drawing.Point(28, 2);
            this.labelNewVersionAvailable.Name = "labelNewVersionAvailable";
            this.labelNewVersionAvailable.Size = new System.Drawing.Size(460, 21);
            this.labelNewVersionAvailable.TabIndex = 100;
            this.labelNewVersionAvailable.Text = "There is a newer version available; view \"About\" for GitHub page";
            this.labelNewVersionAvailable.Visible = false;
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel4.Controls.Add(this.checkBoxDebug);
            this.panel4.Controls.Add(this.label7);
            this.panel4.Controls.Add(this.checkBoxForceAcquisition);
            this.panel4.Controls.Add(this.checkBoxBeep);
            this.panel4.Controls.Add(this.textBoxPort);
            this.panel4.Controls.Add(this.labelPort);
            this.panel4.Controls.Add(this.textBoxIp);
            this.panel4.Controls.Add(this.labelIp);
            this.panel4.Controls.Add(this.buttonCheckScope);
            this.panel4.Location = new System.Drawing.Point(3, 0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(358, 172);
            this.panel4.TabIndex = 25;
            // 
            // checkBoxDebug
            // 
            this.checkBoxDebug.AutoSize = true;
            this.checkBoxDebug.Location = new System.Drawing.Point(155, 85);
            this.checkBoxDebug.Name = "checkBoxDebug";
            this.checkBoxDebug.Size = new System.Drawing.Size(125, 25);
            this.checkBoxDebug.TabIndex = 101;
            this.checkBoxDebug.Text = "Enable debug";
            this.checkBoxDebug.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label7.Location = new System.Drawing.Point(2, 5);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(105, 21);
            this.label7.TabIndex = 100;
            this.label7.Text = "Configuration";
            // 
            // checkBoxForceAcquisition
            // 
            this.checkBoxForceAcquisition.AutoSize = true;
            this.checkBoxForceAcquisition.Location = new System.Drawing.Point(7, 108);
            this.checkBoxForceAcquisition.Name = "checkBoxForceAcquisition";
            this.checkBoxForceAcquisition.Size = new System.Drawing.Size(242, 25);
            this.checkBoxForceAcquisition.TabIndex = 3;
            this.checkBoxForceAcquisition.Text = "Force acquisition after capture";
            this.checkBoxForceAcquisition.UseVisualStyleBackColor = true;
            // 
            // checkBoxBeep
            // 
            this.checkBoxBeep.AutoSize = true;
            this.checkBoxBeep.Checked = true;
            this.checkBoxBeep.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxBeep.Location = new System.Drawing.Point(7, 85);
            this.checkBoxBeep.Name = "checkBoxBeep";
            this.checkBoxBeep.Size = new System.Drawing.Size(116, 25);
            this.checkBoxBeep.TabIndex = 4;
            this.checkBoxBeep.Text = "Enable beep";
            this.checkBoxBeep.UseVisualStyleBackColor = true;
            // 
            // textBoxPort
            // 
            this.textBoxPort.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxPort.Location = new System.Drawing.Point(155, 50);
            this.textBoxPort.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new System.Drawing.Size(84, 28);
            this.textBoxPort.TabIndex = 2;
            // 
            // labelPort
            // 
            this.labelPort.AutoSize = true;
            this.labelPort.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPort.Location = new System.Drawing.Point(151, 32);
            this.labelPort.Name = "labelPort";
            this.labelPort.Size = new System.Drawing.Size(115, 21);
            this.labelPort.TabIndex = 100;
            this.labelPort.Text = "Scope TCP port";
            // 
            // textBoxIp
            // 
            this.textBoxIp.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxIp.Location = new System.Drawing.Point(6, 50);
            this.textBoxIp.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxIp.Name = "textBoxIp";
            this.textBoxIp.Size = new System.Drawing.Size(135, 28);
            this.textBoxIp.TabIndex = 1;
            // 
            // labelIp
            // 
            this.labelIp.AutoSize = true;
            this.labelIp.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelIp.Location = new System.Drawing.Point(3, 32);
            this.labelIp.Name = "labelIp";
            this.labelIp.Size = new System.Drawing.Size(126, 21);
            this.labelIp.TabIndex = 100;
            this.labelIp.Text = "Scope IP address";
            // 
            // buttonCheckScope
            // 
            this.buttonCheckScope.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCheckScope.BackColor = System.Drawing.Color.Cornsilk;
            this.buttonCheckScope.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCheckScope.Location = new System.Drawing.Point(6, 136);
            this.buttonCheckScope.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.buttonCheckScope.Name = "buttonCheckScope";
            this.buttonCheckScope.Size = new System.Drawing.Size(344, 27);
            this.buttonCheckScope.TabIndex = 5;
            this.buttonCheckScope.Text = "Check oscilloscope connectivity";
            this.buttonCheckScope.UseVisualStyleBackColor = false;
            // 
            // panel5
            // 
            this.panel5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.panel5.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panel5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel5.Controls.Add(this.richTextBoxAction);
            this.panel5.Controls.Add(this.labelAction);
            this.panel5.Location = new System.Drawing.Point(3, 544);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(358, 79);
            this.panel5.TabIndex = 24;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.label4);
            this.panel3.Controls.Add(this.label3);
            this.panel3.Controls.Add(this.buttonOpenFolder);
            this.panel3.Controls.Add(this.textBoxCaptureFolder);
            this.panel3.Controls.Add(this.textBoxFilenameFormat);
            this.panel3.Controls.Add(this.label2);
            this.panel3.Location = new System.Drawing.Point(3, 178);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(358, 178);
            this.panel3.TabIndex = 21;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(2, 5);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(75, 21);
            this.label4.TabIndex = 100;
            this.label4.Text = "Filename";
            // 
            // label3
            // 
            this.label3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(2, 88);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(110, 21);
            this.label3.TabIndex = 100;
            this.label3.Text = "Capture folder";
            // 
            // buttonOpenFolder
            // 
            this.buttonOpenFolder.BackColor = System.Drawing.Color.Cornsilk;
            this.buttonOpenFolder.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonOpenFolder.Location = new System.Drawing.Point(7, 140);
            this.buttonOpenFolder.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.buttonOpenFolder.Name = "buttonOpenFolder";
            this.buttonOpenFolder.Size = new System.Drawing.Size(343, 27);
            this.buttonOpenFolder.TabIndex = 8;
            this.buttonOpenFolder.Text = "Open capture folder";
            this.buttonOpenFolder.UseVisualStyleBackColor = false;
            this.buttonOpenFolder.Click += new System.EventHandler(this.buttonOpenFolder_Click);
            // 
            // textBoxCaptureFolder
            // 
            this.textBoxCaptureFolder.BackColor = System.Drawing.Color.White;
            this.textBoxCaptureFolder.Location = new System.Drawing.Point(7, 105);
            this.textBoxCaptureFolder.Name = "textBoxCaptureFolder";
            this.textBoxCaptureFolder.ReadOnly = true;
            this.textBoxCaptureFolder.Size = new System.Drawing.Size(343, 28);
            this.textBoxCaptureFolder.TabIndex = 7;
            this.textBoxCaptureFolder.Click += new System.EventHandler(this.textBoxCaptureFolder_Click);
            // 
            // textBoxFilenameFormat
            // 
            this.textBoxFilenameFormat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFilenameFormat.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxFilenameFormat.Location = new System.Drawing.Point(7, 51);
            this.textBoxFilenameFormat.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxFilenameFormat.Name = "textBoxFilenameFormat";
            this.textBoxFilenameFormat.Size = new System.Drawing.Size(343, 28);
            this.textBoxFilenameFormat.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(2, 33);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(226, 21);
            this.label2.TabIndex = 100;
            this.label2.Text = "Filename format with variables";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.label6);
            this.panel1.Controls.Add(this.buttonCaptureContinuelsy);
            this.panel1.Controls.Add(this.textBoxCaptureNumberStart);
            this.panel1.Controls.Add(this.labelCaptureNumberStart);
            this.panel1.Controls.Add(this.comboBoxRegion);
            this.panel1.Controls.Add(this.labelRegion);
            this.panel1.Controls.Add(this.textBoxComponent);
            this.panel1.Controls.Add(this.labelComponent);
            this.panel1.Location = new System.Drawing.Point(3, 362);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(358, 176);
            this.panel1.TabIndex = 19;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.Location = new System.Drawing.Point(2, 5);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(78, 21);
            this.label6.TabIndex = 100;
            this.label6.Text = "Capturing";
            // 
            // tabPageHelp
            // 
            this.tabPageHelp.Controls.Add(this.richTextBoxHelp);
            this.tabPageHelp.Controls.Add(this.label1);
            this.tabPageHelp.Location = new System.Drawing.Point(4, 30);
            this.tabPageHelp.Name = "tabPageHelp";
            this.tabPageHelp.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageHelp.Size = new System.Drawing.Size(852, 623);
            this.tabPageHelp.TabIndex = 2;
            this.tabPageHelp.Text = "Help";
            this.tabPageHelp.UseVisualStyleBackColor = true;
            // 
            // richTextBoxHelp
            // 
            this.richTextBoxHelp.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBoxHelp.Location = new System.Drawing.Point(11, 63);
            this.richTextBoxHelp.Name = "richTextBoxHelp";
            this.richTextBoxHelp.Size = new System.Drawing.Size(866, 620);
            this.richTextBoxHelp.TabIndex = 2;
            this.richTextBoxHelp.Text = "";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Calibri", 16.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(6, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 35);
            this.label1.TabIndex = 0;
            this.label1.Text = "Help";
            // 
            // tabPageAbout
            // 
            this.tabPageAbout.Controls.Add(this.richTextBoxAbout);
            this.tabPageAbout.Controls.Add(this.labelProductVersion);
            this.tabPageAbout.Controls.Add(this.labelProductName);
            this.tabPageAbout.Controls.Add(this.pictureBoxIcon);
            this.tabPageAbout.Location = new System.Drawing.Point(4, 30);
            this.tabPageAbout.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabPageAbout.Name = "tabPageAbout";
            this.tabPageAbout.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabPageAbout.Size = new System.Drawing.Size(852, 623);
            this.tabPageAbout.TabIndex = 1;
            this.tabPageAbout.Text = "About";
            this.tabPageAbout.UseVisualStyleBackColor = true;
            // 
            // richTextBoxAbout
            // 
            this.richTextBoxAbout.BackColor = System.Drawing.Color.White;
            this.richTextBoxAbout.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBoxAbout.Location = new System.Drawing.Point(19, 221);
            this.richTextBoxAbout.Name = "richTextBoxAbout";
            this.richTextBoxAbout.ReadOnly = true;
            this.richTextBoxAbout.Size = new System.Drawing.Size(855, 416);
            this.richTextBoxAbout.TabIndex = 4;
            this.richTextBoxAbout.Text = resources.GetString("richTextBoxAbout.Text");
            // 
            // labelProductVersion
            // 
            this.labelProductVersion.AutoSize = true;
            this.labelProductVersion.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelProductVersion.Location = new System.Drawing.Point(179, 111);
            this.labelProductVersion.Name = "labelProductVersion";
            this.labelProductVersion.Size = new System.Drawing.Size(239, 24);
            this.labelProductVersion.TabIndex = 2;
            this.labelProductVersion.Text = "Version 2025-September-11";
            // 
            // labelProductName
            // 
            this.labelProductName.AutoSize = true;
            this.labelProductName.Font = new System.Drawing.Font("Calibri", 25.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelProductName.Location = new System.Drawing.Point(175, 71);
            this.labelProductName.Name = "labelProductName";
            this.labelProductName.Size = new System.Drawing.Size(564, 53);
            this.labelProductName.TabIndex = 1;
            this.labelProductName.Text = "Oscilloscope Network Capture";
            // 
            // pictureBoxIcon
            // 
            this.pictureBoxIcon.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxIcon.Image")));
            this.pictureBoxIcon.Location = new System.Drawing.Point(19, 18);
            this.pictureBoxIcon.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBoxIcon.Name = "pictureBoxIcon";
            this.pictureBoxIcon.Size = new System.Drawing.Size(144, 168);
            this.pictureBoxIcon.TabIndex = 0;
            this.pictureBoxIcon.TabStop = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(866, 666);
            this.Controls.Add(this.tabControl1);
            this.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "Form1";
            this.Text = "Oscilloscope Network Capture";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxImage)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPageMeasurements.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.panel5.ResumeLayout(false);
            this.panel5.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.tabPageHelp.ResumeLayout(false);
            this.tabPageHelp.PerformLayout();
            this.tabPageAbout.ResumeLayout(false);
            this.tabPageAbout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ComboBox comboBoxRegion;
        private System.Windows.Forms.Button buttonCaptureContinuelsy;
        private System.Windows.Forms.RichTextBox richTextBoxLog;
        private System.Windows.Forms.PictureBox pictureBoxImage;
        private System.Windows.Forms.Label labelRegion;
        private System.Windows.Forms.TextBox textBoxCaptureNumberStart;
        private System.Windows.Forms.Label labelCaptureNumberStart;
        private System.Windows.Forms.RichTextBox richTextBoxAction;
        private System.Windows.Forms.Label labelComponent;
        private System.Windows.Forms.TextBox textBoxComponent;
        private System.Windows.Forms.Label labelAction;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageMeasurements;
        private System.Windows.Forms.TabPage tabPageAbout;
        private System.Windows.Forms.PictureBox pictureBoxIcon;
        private System.Windows.Forms.Label labelProductVersion;
        private System.Windows.Forms.Label labelProductName;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button buttonOpenFolder;
        private System.Windows.Forms.TextBox textBoxFilenameFormat;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TabPage tabPageHelp;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox richTextBoxHelp;
        private System.Windows.Forms.RichTextBox richTextBoxAbout;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxCaptureFolder;
        private System.Windows.Forms.Label labelNewVersionAvailable;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.CheckBox checkBoxForceAcquisition;
        private System.Windows.Forms.CheckBox checkBoxBeep;
        private System.Windows.Forms.TextBox textBoxPort;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.TextBox textBoxIp;
        private System.Windows.Forms.Label labelIp;
        private System.Windows.Forms.Button buttonCheckScope;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.CheckBox checkBoxDebug;
    }
}

