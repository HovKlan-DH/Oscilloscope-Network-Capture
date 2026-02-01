namespace Oscilloscope_Network_Capture
{
    partial class Main
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListView listViewShareFiles;
        private System.Windows.Forms.ColumnHeader columnHeaderShareFilename;
        private System.Windows.Forms.ColumnHeader columnHeaderShareSize;
        private System.Windows.Forms.Button buttonShareSelectAll;
        private System.Windows.Forms.Button buttonShareSelectNone;
        private System.Windows.Forms.Button buttonShareUpload;
        private System.Windows.Forms.Label labelShareStatus;
        private System.Windows.Forms.Button buttonShareCopyId;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.cboVendor = new System.Windows.Forms.ComboBox();
            this.cboModel = new System.Windows.Forms.ComboBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.lblVendor = new System.Windows.Forms.Label();
            this.lblModel = new System.Windows.Forms.Label();
            this.lblIp = new System.Windows.Forms.Label();
            this.lblPort = new System.Windows.Forms.Label();
            this.txtIp = new System.Windows.Forms.TextBox();
            this.numPort = new System.Windows.Forms.NumericUpDown();
            this.tabMain = new System.Windows.Forms.TabControl();
            this.tabConfiguration = new System.Windows.Forms.TabPage();
            this.txtVoltsDivValues = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.lblStatusVoltsDivSet = new System.Windows.Forms.Label();
            this.btnTestVoltsDivSet = new System.Windows.Forms.Button();
            this.txtCmdVoltsDivSet = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.lblStatusVoltsDivQ = new System.Windows.Forms.Label();
            this.btnTestVoltsDivQ = new System.Windows.Forms.Button();
            this.txtCmdVoltsDivQ = new System.Windows.Forms.TextBox();
            this.label21 = new System.Windows.Forms.Label();
            this.txtTimeDivValues = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.labelNewVersion1 = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblStatusOpc = new System.Windows.Forms.Label();
            this.btnTestOpc = new System.Windows.Forms.Button();
            this.txtCmdOpc = new System.Windows.Forms.TextBox();
            this.lblCmdOpc = new System.Windows.Forms.Label();
            this.lblStatusSysErr = new System.Windows.Forms.Label();
            this.btnTestSysErr = new System.Windows.Forms.Button();
            this.txtCmdSysErr = new System.Windows.Forms.TextBox();
            this.lblCmdSysErr = new System.Windows.Forms.Label();
            this.lblStatusDumpImage = new System.Windows.Forms.Label();
            this.btnTestDumpImage = new System.Windows.Forms.Button();
            this.txtCmdDumpImage = new System.Windows.Forms.TextBox();
            this.lblCmdDumpImage = new System.Windows.Forms.Label();
            this.lblStatusTimeDivSet = new System.Windows.Forms.Label();
            this.btnTestTimeDivSet = new System.Windows.Forms.Button();
            this.txtCmdTimeDivSet = new System.Windows.Forms.TextBox();
            this.lblCmdTimeDivSet = new System.Windows.Forms.Label();
            this.lblStatusTimeDivQ = new System.Windows.Forms.Label();
            this.btnTestTimeDivQ = new System.Windows.Forms.Button();
            this.txtCmdTimeDivQ = new System.Windows.Forms.TextBox();
            this.lblCmdTimeDivQ = new System.Windows.Forms.Label();
            this.lblStatusTrigLevelSet = new System.Windows.Forms.Label();
            this.btnTestTrigLevelSet = new System.Windows.Forms.Button();
            this.txtCmdTrigLevelSet = new System.Windows.Forms.TextBox();
            this.lblCmdTrigLevelSet = new System.Windows.Forms.Label();
            this.lblStatusTrigLevelQ = new System.Windows.Forms.Label();
            this.btnTestTrigLevelQ = new System.Windows.Forms.Button();
            this.txtCmdTrigLevelQ = new System.Windows.Forms.TextBox();
            this.lblCmdTrigLevelQ = new System.Windows.Forms.Label();
            this.lblStatusTrigMode = new System.Windows.Forms.Label();
            this.btnTestTrigMode = new System.Windows.Forms.Button();
            this.txtCmdTrigMode = new System.Windows.Forms.TextBox();
            this.lblCmdTrigMode = new System.Windows.Forms.Label();
            this.lblStatusSingle = new System.Windows.Forms.Label();
            this.btnTestSingle = new System.Windows.Forms.Button();
            this.txtCmdSingle = new System.Windows.Forms.TextBox();
            this.lblCmdSingle = new System.Windows.Forms.Label();
            this.lblStatusRun = new System.Windows.Forms.Label();
            this.btnTestRun = new System.Windows.Forms.Button();
            this.txtCmdRun = new System.Windows.Forms.TextBox();
            this.lblCmdRun = new System.Windows.Forms.Label();
            this.lblStatusStop = new System.Windows.Forms.Label();
            this.btnTestStop = new System.Windows.Forms.Button();
            this.txtCmdStop = new System.Windows.Forms.TextBox();
            this.lblCmdStop = new System.Windows.Forms.Label();
            this.lblStatusActiveTrig = new System.Windows.Forms.Label();
            this.btnTestActiveTrig = new System.Windows.Forms.Button();
            this.txtCmdActiveTrig = new System.Windows.Forms.TextBox();
            this.lblCmdActiveTrig = new System.Windows.Forms.Label();
            this.lblStatusClearStats = new System.Windows.Forms.Label();
            this.btnTestClearStats = new System.Windows.Forms.Button();
            this.txtCmdClearStats = new System.Windows.Forms.TextBox();
            this.lblCmdClearStats = new System.Windows.Forms.Label();
            this.lblStatusIdentify = new System.Windows.Forms.Label();
            this.btnTestIdentify = new System.Windows.Forms.Button();
            this.txtCmdIdentify = new System.Windows.Forms.TextBox();
            this.lblCmdIdentify = new System.Windows.Forms.Label();
            this.tabSettings = new System.Windows.Forms.TabPage();
            this.checkBoxDoNotClearWhenStop = new System.Windows.Forms.CheckBox();
            this.checkBoxMaskSerial = new System.Windows.Forms.CheckBox();
            this.checkBoxEnableDelete = new System.Windows.Forms.CheckBox();
            this.label14 = new System.Windows.Forms.Label();
            this.comboBoxAdjustToGrid = new System.Windows.Forms.ComboBox();
            this.labelNewVersion6 = new System.Windows.Forms.Label();
            this.checkBoxTrimUnderscore = new System.Windows.Forms.CheckBox();
            this.label9 = new System.Windows.Forms.Label();
            this.checkBoxDeleteDoubleUnderscore = new System.Windows.Forms.CheckBox();
            this.label7 = new System.Windows.Forms.Label();
            this.numericUpDownDelayMs = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.numericUpDownVariables = new System.Windows.Forms.NumericUpDown();
            this.checkBoxForceClear = new System.Windows.Forms.CheckBox();
            this.checkBoxForceAcquisition = new System.Windows.Forms.CheckBox();
            this.checkBoxEnableBeep = new System.Windows.Forms.CheckBox();
            this.tabCapturing = new System.Windows.Forms.TabPage();
            this.labelCaptureModeInactive = new System.Windows.Forms.Label();
            this.labelCaptureModeActive = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.textBoxFilenameFormat = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.buttonOpenFolder1 = new System.Windows.Forms.Button();
            this.buttonCaptureStart = new System.Windows.Forms.Button();
            this.richTextBoxAction = new System.Windows.Forms.RichTextBox();
            this.textBoxCaptureOutputFolder = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.labelNewVersion2 = new System.Windows.Forms.Label();
            this.picScreen = new System.Windows.Forms.PictureBox();
            this.tabDebug = new System.Windows.Forms.TabPage();
            this.buttonDebugClear = new System.Windows.Forms.Button();
            this.labelNewVersion3 = new System.Windows.Forms.Label();
            this.rtbLog = new System.Windows.Forms.RichTextBox();
            this.tabShare = new System.Windows.Forms.TabPage();
            this.buttonRefreshFolder = new System.Windows.Forms.Button();
            this.buttonOpenFolder2 = new System.Windows.Forms.Button();
            this.labelFilesSelected = new System.Windows.Forms.Label();
            this.labelFilesTotal = new System.Windows.Forms.Label();
            this.buttonShareDeleteSelected = new System.Windows.Forms.Button();
            this.richTextBoxGalleryUrl = new System.Windows.Forms.RichTextBox();
            this.label22 = new System.Windows.Forms.Label();
            this.label20 = new System.Windows.Forms.Label();
            this.buttonShareCopyId = new System.Windows.Forms.Button();
            this.labelShareStatus = new System.Windows.Forms.Label();
            this.buttonShareUpload = new System.Windows.Forms.Button();
            this.buttonShareSelectNone = new System.Windows.Forms.Button();
            this.buttonShareSelectAll = new System.Windows.Forms.Button();
            this.listViewShareFiles = new System.Windows.Forms.ListView();
            this.columnHeaderShareFilename = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderShareSize = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.tabFeedback = new System.Windows.Forms.TabPage();
            this.checkBoxFeedbackAttachConfig = new System.Windows.Forms.CheckBox();
            this.checkBoxFeedbackAttachDebug = new System.Windows.Forms.CheckBox();
            this.label18 = new System.Windows.Forms.Label();
            this.label17 = new System.Windows.Forms.Label();
            this.textBoxFeedback = new System.Windows.Forms.TextBox();
            this.buttonSendToDeveloper = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxEmail = new System.Windows.Forms.TextBox();
            this.tabHelp = new System.Windows.Forms.TabPage();
            this.richTextBoxHelp = new System.Windows.Forms.RichTextBox();
            this.labelNewVersion4 = new System.Windows.Forms.Label();
            this.tabAbout = new System.Windows.Forms.TabPage();
            this.labelVersion = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.richTextBoxAbout = new System.Windows.Forms.RichTextBox();
            this.labelNewVersion5 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).BeginInit();
            this.tabMain.SuspendLayout();
            this.tabConfiguration.SuspendLayout();
            this.tabSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDelayMs)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownVariables)).BeginInit();
            this.tabCapturing.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picScreen)).BeginInit();
            this.tabDebug.SuspendLayout();
            this.tabShare.SuspendLayout();
            this.tabFeedback.SuspendLayout();
            this.tabHelp.SuspendLayout();
            this.tabAbout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // cboVendor
            // 
            this.cboVendor.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboVendor.FormattingEnabled = true;
            this.cboVendor.Location = new System.Drawing.Point(107, 47);
            this.cboVendor.Margin = new System.Windows.Forms.Padding(4);
            this.cboVendor.Name = "cboVendor";
            this.cboVendor.Size = new System.Drawing.Size(239, 28);
            this.cboVendor.TabIndex = 1;
            // 
            // cboModel
            // 
            this.cboModel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboModel.FormattingEnabled = true;
            this.cboModel.Location = new System.Drawing.Point(107, 81);
            this.cboModel.Margin = new System.Windows.Forms.Padding(4);
            this.cboModel.Name = "cboModel";
            this.cboModel.Size = new System.Drawing.Size(239, 28);
            this.cboModel.TabIndex = 2;
            // 
            // btnConnect
            // 
            this.btnConnect.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnConnect.Location = new System.Drawing.Point(371, 47);
            this.btnConnect.Margin = new System.Windows.Forms.Padding(4);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(203, 57);
            this.btnConnect.TabIndex = 5;
            this.btnConnect.Text = "Connect to oscilloscope and start capture mode";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // lblVendor
            // 
            this.lblVendor.AutoSize = true;
            this.lblVendor.Location = new System.Drawing.Point(4, 50);
            this.lblVendor.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblVendor.Name = "lblVendor";
            this.lblVendor.Size = new System.Drawing.Size(56, 20);
            this.lblVendor.TabIndex = 1000;
            this.lblVendor.Text = "Vendor";
            // 
            // lblModel
            // 
            this.lblModel.AutoSize = true;
            this.lblModel.Location = new System.Drawing.Point(4, 84);
            this.lblModel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblModel.Name = "lblModel";
            this.lblModel.Size = new System.Drawing.Size(93, 20);
            this.lblModel.TabIndex = 1000;
            this.lblModel.Text = "Model series";
            // 
            // lblIp
            // 
            this.lblIp.AutoSize = true;
            this.lblIp.Location = new System.Drawing.Point(4, 119);
            this.lblIp.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblIp.Name = "lblIp";
            this.lblIp.Size = new System.Drawing.Size(76, 20);
            this.lblIp.TabIndex = 11;
            this.lblIp.Text = "IP address";
            // 
            // lblPort
            // 
            this.lblPort.AutoSize = true;
            this.lblPort.Location = new System.Drawing.Point(4, 152);
            this.lblPort.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPort.Name = "lblPort";
            this.lblPort.Size = new System.Drawing.Size(35, 20);
            this.lblPort.TabIndex = 12;
            this.lblPort.Text = "Port";
            // 
            // txtIp
            // 
            this.txtIp.Location = new System.Drawing.Point(107, 115);
            this.txtIp.Margin = new System.Windows.Forms.Padding(4);
            this.txtIp.Name = "txtIp";
            this.txtIp.Size = new System.Drawing.Size(239, 27);
            this.txtIp.TabIndex = 3;
            // 
            // numPort
            // 
            this.numPort.Location = new System.Drawing.Point(107, 148);
            this.numPort.Margin = new System.Windows.Forms.Padding(4);
            this.numPort.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.numPort.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numPort.Name = "numPort";
            this.numPort.Size = new System.Drawing.Size(107, 27);
            this.numPort.TabIndex = 4;
            this.numPort.Value = new decimal(new int[] {
            5025,
            0,
            0,
            0});
            // 
            // tabMain
            // 
            this.tabMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabMain.Controls.Add(this.tabConfiguration);
            this.tabMain.Controls.Add(this.tabSettings);
            this.tabMain.Controls.Add(this.tabCapturing);
            this.tabMain.Controls.Add(this.tabDebug);
            this.tabMain.Controls.Add(this.tabShare);
            this.tabMain.Controls.Add(this.tabFeedback);
            this.tabMain.Controls.Add(this.tabHelp);
            this.tabMain.Controls.Add(this.tabAbout);
            this.tabMain.Location = new System.Drawing.Point(3, 5);
            this.tabMain.Margin = new System.Windows.Forms.Padding(4);
            this.tabMain.Name = "tabMain";
            this.tabMain.SelectedIndex = 0;
            this.tabMain.Size = new System.Drawing.Size(1065, 764);
            this.tabMain.TabIndex = 100;
            // 
            // tabConfiguration
            // 
            this.tabConfiguration.Controls.Add(this.txtVoltsDivValues);
            this.tabConfiguration.Controls.Add(this.label5);
            this.tabConfiguration.Controls.Add(this.lblStatusVoltsDivSet);
            this.tabConfiguration.Controls.Add(this.btnTestVoltsDivSet);
            this.tabConfiguration.Controls.Add(this.txtCmdVoltsDivSet);
            this.tabConfiguration.Controls.Add(this.label19);
            this.tabConfiguration.Controls.Add(this.lblStatusVoltsDivQ);
            this.tabConfiguration.Controls.Add(this.btnTestVoltsDivQ);
            this.tabConfiguration.Controls.Add(this.txtCmdVoltsDivQ);
            this.tabConfiguration.Controls.Add(this.label21);
            this.tabConfiguration.Controls.Add(this.txtTimeDivValues);
            this.tabConfiguration.Controls.Add(this.label13);
            this.tabConfiguration.Controls.Add(this.label11);
            this.tabConfiguration.Controls.Add(this.label8);
            this.tabConfiguration.Controls.Add(this.labelNewVersion1);
            this.tabConfiguration.Controls.Add(this.lblStatus);
            this.tabConfiguration.Controls.Add(this.lblStatusOpc);
            this.tabConfiguration.Controls.Add(this.btnTestOpc);
            this.tabConfiguration.Controls.Add(this.txtCmdOpc);
            this.tabConfiguration.Controls.Add(this.lblCmdOpc);
            this.tabConfiguration.Controls.Add(this.lblStatusSysErr);
            this.tabConfiguration.Controls.Add(this.btnTestSysErr);
            this.tabConfiguration.Controls.Add(this.txtCmdSysErr);
            this.tabConfiguration.Controls.Add(this.lblCmdSysErr);
            this.tabConfiguration.Controls.Add(this.lblStatusDumpImage);
            this.tabConfiguration.Controls.Add(this.btnTestDumpImage);
            this.tabConfiguration.Controls.Add(this.txtCmdDumpImage);
            this.tabConfiguration.Controls.Add(this.lblCmdDumpImage);
            this.tabConfiguration.Controls.Add(this.lblStatusTimeDivSet);
            this.tabConfiguration.Controls.Add(this.btnTestTimeDivSet);
            this.tabConfiguration.Controls.Add(this.txtCmdTimeDivSet);
            this.tabConfiguration.Controls.Add(this.lblCmdTimeDivSet);
            this.tabConfiguration.Controls.Add(this.lblStatusTimeDivQ);
            this.tabConfiguration.Controls.Add(this.btnTestTimeDivQ);
            this.tabConfiguration.Controls.Add(this.txtCmdTimeDivQ);
            this.tabConfiguration.Controls.Add(this.lblCmdTimeDivQ);
            this.tabConfiguration.Controls.Add(this.lblStatusTrigLevelSet);
            this.tabConfiguration.Controls.Add(this.btnTestTrigLevelSet);
            this.tabConfiguration.Controls.Add(this.txtCmdTrigLevelSet);
            this.tabConfiguration.Controls.Add(this.lblCmdTrigLevelSet);
            this.tabConfiguration.Controls.Add(this.lblStatusTrigLevelQ);
            this.tabConfiguration.Controls.Add(this.btnTestTrigLevelQ);
            this.tabConfiguration.Controls.Add(this.txtCmdTrigLevelQ);
            this.tabConfiguration.Controls.Add(this.lblCmdTrigLevelQ);
            this.tabConfiguration.Controls.Add(this.lblStatusTrigMode);
            this.tabConfiguration.Controls.Add(this.btnTestTrigMode);
            this.tabConfiguration.Controls.Add(this.txtCmdTrigMode);
            this.tabConfiguration.Controls.Add(this.lblCmdTrigMode);
            this.tabConfiguration.Controls.Add(this.lblStatusSingle);
            this.tabConfiguration.Controls.Add(this.btnTestSingle);
            this.tabConfiguration.Controls.Add(this.txtCmdSingle);
            this.tabConfiguration.Controls.Add(this.lblCmdSingle);
            this.tabConfiguration.Controls.Add(this.lblStatusRun);
            this.tabConfiguration.Controls.Add(this.btnTestRun);
            this.tabConfiguration.Controls.Add(this.txtCmdRun);
            this.tabConfiguration.Controls.Add(this.lblCmdRun);
            this.tabConfiguration.Controls.Add(this.lblStatusStop);
            this.tabConfiguration.Controls.Add(this.btnTestStop);
            this.tabConfiguration.Controls.Add(this.txtCmdStop);
            this.tabConfiguration.Controls.Add(this.lblCmdStop);
            this.tabConfiguration.Controls.Add(this.lblStatusActiveTrig);
            this.tabConfiguration.Controls.Add(this.btnTestActiveTrig);
            this.tabConfiguration.Controls.Add(this.txtCmdActiveTrig);
            this.tabConfiguration.Controls.Add(this.lblCmdActiveTrig);
            this.tabConfiguration.Controls.Add(this.lblStatusClearStats);
            this.tabConfiguration.Controls.Add(this.btnTestClearStats);
            this.tabConfiguration.Controls.Add(this.txtCmdClearStats);
            this.tabConfiguration.Controls.Add(this.lblCmdClearStats);
            this.tabConfiguration.Controls.Add(this.lblStatusIdentify);
            this.tabConfiguration.Controls.Add(this.btnTestIdentify);
            this.tabConfiguration.Controls.Add(this.txtCmdIdentify);
            this.tabConfiguration.Controls.Add(this.lblCmdIdentify);
            this.tabConfiguration.Controls.Add(this.numPort);
            this.tabConfiguration.Controls.Add(this.txtIp);
            this.tabConfiguration.Controls.Add(this.lblPort);
            this.tabConfiguration.Controls.Add(this.lblIp);
            this.tabConfiguration.Controls.Add(this.lblModel);
            this.tabConfiguration.Controls.Add(this.lblVendor);
            this.tabConfiguration.Controls.Add(this.btnConnect);
            this.tabConfiguration.Controls.Add(this.cboModel);
            this.tabConfiguration.Controls.Add(this.cboVendor);
            this.tabConfiguration.Location = new System.Drawing.Point(4, 29);
            this.tabConfiguration.Margin = new System.Windows.Forms.Padding(4);
            this.tabConfiguration.Name = "tabConfiguration";
            this.tabConfiguration.Padding = new System.Windows.Forms.Padding(4);
            this.tabConfiguration.Size = new System.Drawing.Size(1057, 731);
            this.tabConfiguration.TabIndex = 0;
            this.tabConfiguration.Text = "Configuration";
            this.tabConfiguration.UseVisualStyleBackColor = true;
            // 
            // txtVoltsDivValues
            // 
            this.txtVoltsDivValues.Location = new System.Drawing.Point(232, 688);
            this.txtVoltsDivValues.Margin = new System.Windows.Forms.Padding(4);
            this.txtVoltsDivValues.Name = "txtVoltsDivValues";
            this.txtVoltsDivValues.Size = new System.Drawing.Size(302, 27);
            this.txtVoltsDivValues.TabIndex = 1009;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(16, 692);
            this.label5.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(215, 20);
            this.label5.TabIndex = 1010;
            this.label5.Text = "VOLTS/DIV values and notation";
            // 
            // lblStatusVoltsDivSet
            // 
            this.lblStatusVoltsDivSet.AutoSize = true;
            this.lblStatusVoltsDivSet.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusVoltsDivSet.Location = new System.Drawing.Point(634, 610);
            this.lblStatusVoltsDivSet.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusVoltsDivSet.Name = "lblStatusVoltsDivSet";
            this.lblStatusVoltsDivSet.Size = new System.Drawing.Size(274, 20);
            this.lblStatusVoltsDivSet.TabIndex = 1001;
            this.lblStatusVoltsDivSet.Text = "Not tested - first run \"Query VOLTS/DIV\"";
            // 
            // btnTestVoltsDivSet
            // 
            this.btnTestVoltsDivSet.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestVoltsDivSet.Location = new System.Drawing.Point(546, 606);
            this.btnTestVoltsDivSet.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestVoltsDivSet.Name = "btnTestVoltsDivSet";
            this.btnTestVoltsDivSet.Size = new System.Drawing.Size(80, 22);
            this.btnTestVoltsDivSet.TabIndex = 1008;
            this.btnTestVoltsDivSet.Text = "Test";
            this.btnTestVoltsDivSet.UseVisualStyleBackColor = true;
            this.btnTestVoltsDivSet.Click += new System.EventHandler(this.btnTestVoltsDivSet_Click);
            // 
            // txtCmdVoltsDivSet
            // 
            this.txtCmdVoltsDivSet.Location = new System.Drawing.Point(232, 606);
            this.txtCmdVoltsDivSet.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdVoltsDivSet.Name = "txtCmdVoltsDivSet";
            this.txtCmdVoltsDivSet.Size = new System.Drawing.Size(302, 27);
            this.txtCmdVoltsDivSet.TabIndex = 1007;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(16, 610);
            this.label19.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(106, 20);
            this.label19.TabIndex = 1002;
            this.label19.Text = "Set VOLTS/DIV";
            // 
            // lblStatusVoltsDivQ
            // 
            this.lblStatusVoltsDivQ.AutoSize = true;
            this.lblStatusVoltsDivQ.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusVoltsDivQ.Location = new System.Drawing.Point(634, 582);
            this.lblStatusVoltsDivQ.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusVoltsDivQ.Name = "lblStatusVoltsDivQ";
            this.lblStatusVoltsDivQ.Size = new System.Drawing.Size(79, 20);
            this.lblStatusVoltsDivQ.TabIndex = 1003;
            this.lblStatusVoltsDivQ.Text = "Not tested";
            // 
            // btnTestVoltsDivQ
            // 
            this.btnTestVoltsDivQ.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestVoltsDivQ.Location = new System.Drawing.Point(546, 579);
            this.btnTestVoltsDivQ.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestVoltsDivQ.Name = "btnTestVoltsDivQ";
            this.btnTestVoltsDivQ.Size = new System.Drawing.Size(80, 22);
            this.btnTestVoltsDivQ.TabIndex = 1006;
            this.btnTestVoltsDivQ.Text = "Test";
            this.btnTestVoltsDivQ.UseVisualStyleBackColor = true;
            this.btnTestVoltsDivQ.Click += new System.EventHandler(this.btnTestVoltsDivQ_Click);
            // 
            // txtCmdVoltsDivQ
            // 
            this.txtCmdVoltsDivQ.Location = new System.Drawing.Point(232, 579);
            this.txtCmdVoltsDivQ.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdVoltsDivQ.Name = "txtCmdVoltsDivQ";
            this.txtCmdVoltsDivQ.Size = new System.Drawing.Size(302, 27);
            this.txtCmdVoltsDivQ.TabIndex = 1005;
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(16, 583);
            this.label21.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(124, 20);
            this.label21.TabIndex = 1004;
            this.label21.Text = "Query VOLTS/DIV";
            // 
            // txtTimeDivValues
            // 
            this.txtTimeDivValues.Location = new System.Drawing.Point(232, 661);
            this.txtTimeDivValues.Margin = new System.Windows.Forms.Padding(4);
            this.txtTimeDivValues.Name = "txtTimeDivValues";
            this.txtTimeDivValues.Size = new System.Drawing.Size(302, 27);
            this.txtTimeDivValues.TabIndex = 34;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(16, 665);
            this.label13.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(206, 20);
            this.label13.TabIndex = 60;
            this.label13.Text = "TIME/DIV values and notation";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label11.Location = new System.Drawing.Point(16, 200);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(427, 20);
            this.label11.TabIndex = 58;
            this.label11.Text = "Test-suites for SCPI commands specifically for Rigol Generic";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label8.Location = new System.Drawing.Point(3, 6);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(143, 28);
            this.label8.TabIndex = 1000;
            this.label8.Text = "Configuration";
            // 
            // labelNewVersion1
            // 
            this.labelNewVersion1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelNewVersion1.AutoSize = true;
            this.labelNewVersion1.BackColor = System.Drawing.Color.IndianRed;
            this.labelNewVersion1.ForeColor = System.Drawing.Color.White;
            this.labelNewVersion1.Location = new System.Drawing.Point(773, 0);
            this.labelNewVersion1.Name = "labelNewVersion1";
            this.labelNewVersion1.Size = new System.Drawing.Size(281, 20);
            this.labelNewVersion1.TabIndex = 56;
            this.labelNewVersion1.Text = "New version available - view \"About\" tab";
            this.labelNewVersion1.Visible = false;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.ForeColor = System.Drawing.Color.Gray;
            this.lblStatus.Location = new System.Drawing.Point(371, 109);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(232, 20);
            this.lblStatus.TabIndex = 13;
            this.lblStatus.Text = "Network connectivity not checked";
            // 
            // lblStatusOpc
            // 
            this.lblStatusOpc.AutoSize = true;
            this.lblStatusOpc.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusOpc.Location = new System.Drawing.Point(634, 285);
            this.lblStatusOpc.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusOpc.Name = "lblStatusOpc";
            this.lblStatusOpc.Size = new System.Drawing.Size(79, 20);
            this.lblStatusOpc.TabIndex = 0;
            this.lblStatusOpc.Text = "Not tested";
            // 
            // btnTestOpc
            // 
            this.btnTestOpc.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestOpc.Location = new System.Drawing.Point(546, 281);
            this.btnTestOpc.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestOpc.Name = "btnTestOpc";
            this.btnTestOpc.Size = new System.Drawing.Size(80, 22);
            this.btnTestOpc.TabIndex = 11;
            this.btnTestOpc.Text = "Test";
            this.btnTestOpc.UseVisualStyleBackColor = true;
            this.btnTestOpc.Click += new System.EventHandler(this.btnTestOpc_Click);
            // 
            // txtCmdOpc
            // 
            this.txtCmdOpc.Location = new System.Drawing.Point(232, 281);
            this.txtCmdOpc.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdOpc.Name = "txtCmdOpc";
            this.txtCmdOpc.Size = new System.Drawing.Size(302, 27);
            this.txtCmdOpc.TabIndex = 10;
            // 
            // lblCmdOpc
            // 
            this.lblCmdOpc.AutoSize = true;
            this.lblCmdOpc.Location = new System.Drawing.Point(16, 285);
            this.lblCmdOpc.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCmdOpc.Name = "lblCmdOpc";
            this.lblCmdOpc.Size = new System.Drawing.Size(200, 20);
            this.lblCmdOpc.TabIndex = 3;
            this.lblCmdOpc.Text = "Query \"Operation Complete\"";
            // 
            // lblStatusSysErr
            // 
            this.lblStatusSysErr.AutoSize = true;
            this.lblStatusSysErr.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusSysErr.Location = new System.Drawing.Point(634, 256);
            this.lblStatusSysErr.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusSysErr.Name = "lblStatusSysErr";
            this.lblStatusSysErr.Size = new System.Drawing.Size(79, 20);
            this.lblStatusSysErr.TabIndex = 4;
            this.lblStatusSysErr.Text = "Not tested";
            // 
            // btnTestSysErr
            // 
            this.btnTestSysErr.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestSysErr.Location = new System.Drawing.Point(546, 253);
            this.btnTestSysErr.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestSysErr.Name = "btnTestSysErr";
            this.btnTestSysErr.Size = new System.Drawing.Size(80, 22);
            this.btnTestSysErr.TabIndex = 9;
            this.btnTestSysErr.Text = "Test";
            this.btnTestSysErr.UseVisualStyleBackColor = true;
            this.btnTestSysErr.Click += new System.EventHandler(this.btnTestSysErr_Click);
            // 
            // txtCmdSysErr
            // 
            this.txtCmdSysErr.Location = new System.Drawing.Point(232, 253);
            this.txtCmdSysErr.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdSysErr.Name = "txtCmdSysErr";
            this.txtCmdSysErr.Size = new System.Drawing.Size(302, 27);
            this.txtCmdSysErr.TabIndex = 8;
            // 
            // lblCmdSysErr
            // 
            this.lblCmdSysErr.AutoSize = true;
            this.lblCmdSysErr.Location = new System.Drawing.Point(16, 257);
            this.lblCmdSysErr.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCmdSysErr.Name = "lblCmdSysErr";
            this.lblCmdSysErr.Size = new System.Drawing.Size(245, 20);
            this.lblCmdSysErr.TabIndex = 7;
            this.lblCmdSysErr.Text = "Query and drain system error queue";
            // 
            // lblStatusDumpImage
            // 
            this.lblStatusDumpImage.AutoSize = true;
            this.lblStatusDumpImage.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusDumpImage.Location = new System.Drawing.Point(634, 637);
            this.lblStatusDumpImage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusDumpImage.Name = "lblStatusDumpImage";
            this.lblStatusDumpImage.Size = new System.Drawing.Size(79, 20);
            this.lblStatusDumpImage.TabIndex = 8;
            this.lblStatusDumpImage.Text = "Not tested";
            // 
            // btnTestDumpImage
            // 
            this.btnTestDumpImage.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestDumpImage.Location = new System.Drawing.Point(546, 634);
            this.btnTestDumpImage.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestDumpImage.Name = "btnTestDumpImage";
            this.btnTestDumpImage.Size = new System.Drawing.Size(80, 22);
            this.btnTestDumpImage.TabIndex = 33;
            this.btnTestDumpImage.Text = "Test";
            this.btnTestDumpImage.UseVisualStyleBackColor = true;
            this.btnTestDumpImage.Click += new System.EventHandler(this.btnTestDumpImage_Click);
            // 
            // txtCmdDumpImage
            // 
            this.txtCmdDumpImage.Location = new System.Drawing.Point(232, 634);
            this.txtCmdDumpImage.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdDumpImage.Name = "txtCmdDumpImage";
            this.txtCmdDumpImage.Size = new System.Drawing.Size(302, 27);
            this.txtCmdDumpImage.TabIndex = 32;
            // 
            // lblCmdDumpImage
            // 
            this.lblCmdDumpImage.AutoSize = true;
            this.lblCmdDumpImage.Location = new System.Drawing.Point(16, 638);
            this.lblCmdDumpImage.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCmdDumpImage.Name = "lblCmdDumpImage";
            this.lblCmdDumpImage.Size = new System.Drawing.Size(137, 20);
            this.lblCmdDumpImage.TabIndex = 11;
            this.lblCmdDumpImage.Text = "Query dump image";
            // 
            // lblStatusTimeDivSet
            // 
            this.lblStatusTimeDivSet.AutoSize = true;
            this.lblStatusTimeDivSet.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusTimeDivSet.Location = new System.Drawing.Point(634, 557);
            this.lblStatusTimeDivSet.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusTimeDivSet.Name = "lblStatusTimeDivSet";
            this.lblStatusTimeDivSet.Size = new System.Drawing.Size(265, 20);
            this.lblStatusTimeDivSet.TabIndex = 12;
            this.lblStatusTimeDivSet.Text = "Not tested - first run \"Query TIME/DIV\"";
            // 
            // btnTestTimeDivSet
            // 
            this.btnTestTimeDivSet.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestTimeDivSet.Location = new System.Drawing.Point(546, 553);
            this.btnTestTimeDivSet.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestTimeDivSet.Name = "btnTestTimeDivSet";
            this.btnTestTimeDivSet.Size = new System.Drawing.Size(80, 22);
            this.btnTestTimeDivSet.TabIndex = 31;
            this.btnTestTimeDivSet.Text = "Test";
            this.btnTestTimeDivSet.UseVisualStyleBackColor = true;
            this.btnTestTimeDivSet.Click += new System.EventHandler(this.btnTestTimeDivSet_Click);
            // 
            // txtCmdTimeDivSet
            // 
            this.txtCmdTimeDivSet.Location = new System.Drawing.Point(232, 553);
            this.txtCmdTimeDivSet.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdTimeDivSet.Name = "txtCmdTimeDivSet";
            this.txtCmdTimeDivSet.Size = new System.Drawing.Size(302, 27);
            this.txtCmdTimeDivSet.TabIndex = 30;
            // 
            // lblCmdTimeDivSet
            // 
            this.lblCmdTimeDivSet.AutoSize = true;
            this.lblCmdTimeDivSet.Location = new System.Drawing.Point(16, 557);
            this.lblCmdTimeDivSet.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCmdTimeDivSet.Name = "lblCmdTimeDivSet";
            this.lblCmdTimeDivSet.Size = new System.Drawing.Size(97, 20);
            this.lblCmdTimeDivSet.TabIndex = 15;
            this.lblCmdTimeDivSet.Text = "Set TIME/DIV";
            // 
            // lblStatusTimeDivQ
            // 
            this.lblStatusTimeDivQ.AutoSize = true;
            this.lblStatusTimeDivQ.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusTimeDivQ.Location = new System.Drawing.Point(634, 529);
            this.lblStatusTimeDivQ.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusTimeDivQ.Name = "lblStatusTimeDivQ";
            this.lblStatusTimeDivQ.Size = new System.Drawing.Size(79, 20);
            this.lblStatusTimeDivQ.TabIndex = 16;
            this.lblStatusTimeDivQ.Text = "Not tested";
            // 
            // btnTestTimeDivQ
            // 
            this.btnTestTimeDivQ.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestTimeDivQ.Location = new System.Drawing.Point(546, 526);
            this.btnTestTimeDivQ.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestTimeDivQ.Name = "btnTestTimeDivQ";
            this.btnTestTimeDivQ.Size = new System.Drawing.Size(80, 22);
            this.btnTestTimeDivQ.TabIndex = 29;
            this.btnTestTimeDivQ.Text = "Test";
            this.btnTestTimeDivQ.UseVisualStyleBackColor = true;
            this.btnTestTimeDivQ.Click += new System.EventHandler(this.btnTestTimeDivQ_Click);
            // 
            // txtCmdTimeDivQ
            // 
            this.txtCmdTimeDivQ.Location = new System.Drawing.Point(232, 526);
            this.txtCmdTimeDivQ.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdTimeDivQ.Name = "txtCmdTimeDivQ";
            this.txtCmdTimeDivQ.Size = new System.Drawing.Size(302, 27);
            this.txtCmdTimeDivQ.TabIndex = 28;
            // 
            // lblCmdTimeDivQ
            // 
            this.lblCmdTimeDivQ.AutoSize = true;
            this.lblCmdTimeDivQ.Location = new System.Drawing.Point(16, 530);
            this.lblCmdTimeDivQ.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCmdTimeDivQ.Name = "lblCmdTimeDivQ";
            this.lblCmdTimeDivQ.Size = new System.Drawing.Size(115, 20);
            this.lblCmdTimeDivQ.TabIndex = 19;
            this.lblCmdTimeDivQ.Text = "Query TIME/DIV";
            // 
            // lblStatusTrigLevelSet
            // 
            this.lblStatusTrigLevelSet.AutoSize = true;
            this.lblStatusTrigLevelSet.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusTrigLevelSet.Location = new System.Drawing.Point(634, 502);
            this.lblStatusTrigLevelSet.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusTrigLevelSet.Name = "lblStatusTrigLevelSet";
            this.lblStatusTrigLevelSet.Size = new System.Drawing.Size(282, 20);
            this.lblStatusTrigLevelSet.TabIndex = 20;
            this.lblStatusTrigLevelSet.Text = "Not tested - first run \"Query trigger level\"";
            // 
            // btnTestTrigLevelSet
            // 
            this.btnTestTrigLevelSet.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestTrigLevelSet.Location = new System.Drawing.Point(546, 499);
            this.btnTestTrigLevelSet.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestTrigLevelSet.Name = "btnTestTrigLevelSet";
            this.btnTestTrigLevelSet.Size = new System.Drawing.Size(80, 22);
            this.btnTestTrigLevelSet.TabIndex = 27;
            this.btnTestTrigLevelSet.Text = "Test";
            this.btnTestTrigLevelSet.UseVisualStyleBackColor = true;
            this.btnTestTrigLevelSet.Click += new System.EventHandler(this.btnTestTrigLevelSet_Click);
            // 
            // txtCmdTrigLevelSet
            // 
            this.txtCmdTrigLevelSet.Location = new System.Drawing.Point(232, 499);
            this.txtCmdTrigLevelSet.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdTrigLevelSet.Name = "txtCmdTrigLevelSet";
            this.txtCmdTrigLevelSet.Size = new System.Drawing.Size(302, 27);
            this.txtCmdTrigLevelSet.TabIndex = 26;
            // 
            // lblCmdTrigLevelSet
            // 
            this.lblCmdTrigLevelSet.AutoSize = true;
            this.lblCmdTrigLevelSet.Location = new System.Drawing.Point(16, 503);
            this.lblCmdTrigLevelSet.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCmdTrigLevelSet.Name = "lblCmdTrigLevelSet";
            this.lblCmdTrigLevelSet.Size = new System.Drawing.Size(114, 20);
            this.lblCmdTrigLevelSet.TabIndex = 23;
            this.lblCmdTrigLevelSet.Text = "Set trigger level";
            // 
            // lblStatusTrigLevelQ
            // 
            this.lblStatusTrigLevelQ.AutoSize = true;
            this.lblStatusTrigLevelQ.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusTrigLevelQ.Location = new System.Drawing.Point(634, 476);
            this.lblStatusTrigLevelQ.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusTrigLevelQ.Name = "lblStatusTrigLevelQ";
            this.lblStatusTrigLevelQ.Size = new System.Drawing.Size(79, 20);
            this.lblStatusTrigLevelQ.TabIndex = 24;
            this.lblStatusTrigLevelQ.Text = "Not tested";
            // 
            // btnTestTrigLevelQ
            // 
            this.btnTestTrigLevelQ.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestTrigLevelQ.Location = new System.Drawing.Point(546, 472);
            this.btnTestTrigLevelQ.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestTrigLevelQ.Name = "btnTestTrigLevelQ";
            this.btnTestTrigLevelQ.Size = new System.Drawing.Size(80, 22);
            this.btnTestTrigLevelQ.TabIndex = 25;
            this.btnTestTrigLevelQ.Text = "Test";
            this.btnTestTrigLevelQ.UseVisualStyleBackColor = true;
            this.btnTestTrigLevelQ.Click += new System.EventHandler(this.btnTestTrigLevelQ_Click);
            // 
            // txtCmdTrigLevelQ
            // 
            this.txtCmdTrigLevelQ.Location = new System.Drawing.Point(232, 472);
            this.txtCmdTrigLevelQ.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdTrigLevelQ.Name = "txtCmdTrigLevelQ";
            this.txtCmdTrigLevelQ.Size = new System.Drawing.Size(302, 27);
            this.txtCmdTrigLevelQ.TabIndex = 24;
            // 
            // lblCmdTrigLevelQ
            // 
            this.lblCmdTrigLevelQ.AutoSize = true;
            this.lblCmdTrigLevelQ.Location = new System.Drawing.Point(16, 476);
            this.lblCmdTrigLevelQ.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCmdTrigLevelQ.Name = "lblCmdTrigLevelQ";
            this.lblCmdTrigLevelQ.Size = new System.Drawing.Size(132, 20);
            this.lblCmdTrigLevelQ.TabIndex = 27;
            this.lblCmdTrigLevelQ.Text = "Query trigger level";
            // 
            // lblStatusTrigMode
            // 
            this.lblStatusTrigMode.AutoSize = true;
            this.lblStatusTrigMode.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusTrigMode.Location = new System.Drawing.Point(634, 448);
            this.lblStatusTrigMode.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusTrigMode.Name = "lblStatusTrigMode";
            this.lblStatusTrigMode.Size = new System.Drawing.Size(79, 20);
            this.lblStatusTrigMode.TabIndex = 28;
            this.lblStatusTrigMode.Text = "Not tested";
            // 
            // btnTestTrigMode
            // 
            this.btnTestTrigMode.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestTrigMode.Location = new System.Drawing.Point(546, 445);
            this.btnTestTrigMode.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestTrigMode.Name = "btnTestTrigMode";
            this.btnTestTrigMode.Size = new System.Drawing.Size(80, 22);
            this.btnTestTrigMode.TabIndex = 23;
            this.btnTestTrigMode.Text = "Test";
            this.btnTestTrigMode.UseVisualStyleBackColor = true;
            this.btnTestTrigMode.Click += new System.EventHandler(this.btnTestTrigMode_Click);
            // 
            // txtCmdTrigMode
            // 
            this.txtCmdTrigMode.Location = new System.Drawing.Point(232, 445);
            this.txtCmdTrigMode.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdTrigMode.Name = "txtCmdTrigMode";
            this.txtCmdTrigMode.Size = new System.Drawing.Size(302, 27);
            this.txtCmdTrigMode.TabIndex = 22;
            // 
            // lblCmdTrigMode
            // 
            this.lblCmdTrigMode.AutoSize = true;
            this.lblCmdTrigMode.Location = new System.Drawing.Point(16, 449);
            this.lblCmdTrigMode.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCmdTrigMode.Name = "lblCmdTrigMode";
            this.lblCmdTrigMode.Size = new System.Drawing.Size(140, 20);
            this.lblCmdTrigMode.TabIndex = 31;
            this.lblCmdTrigMode.Text = "Query trigger mode";
            // 
            // lblStatusSingle
            // 
            this.lblStatusSingle.AutoSize = true;
            this.lblStatusSingle.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusSingle.Location = new System.Drawing.Point(634, 394);
            this.lblStatusSingle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusSingle.Name = "lblStatusSingle";
            this.lblStatusSingle.Size = new System.Drawing.Size(79, 20);
            this.lblStatusSingle.TabIndex = 32;
            this.lblStatusSingle.Text = "Not tested";
            // 
            // btnTestSingle
            // 
            this.btnTestSingle.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestSingle.Location = new System.Drawing.Point(546, 391);
            this.btnTestSingle.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestSingle.Name = "btnTestSingle";
            this.btnTestSingle.Size = new System.Drawing.Size(80, 22);
            this.btnTestSingle.TabIndex = 19;
            this.btnTestSingle.Text = "Test";
            this.btnTestSingle.UseVisualStyleBackColor = true;
            this.btnTestSingle.Click += new System.EventHandler(this.btnTestSingle_Click);
            // 
            // txtCmdSingle
            // 
            this.txtCmdSingle.Location = new System.Drawing.Point(232, 391);
            this.txtCmdSingle.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdSingle.Name = "txtCmdSingle";
            this.txtCmdSingle.Size = new System.Drawing.Size(302, 27);
            this.txtCmdSingle.TabIndex = 18;
            // 
            // lblCmdSingle
            // 
            this.lblCmdSingle.AutoSize = true;
            this.lblCmdSingle.Location = new System.Drawing.Point(16, 421);
            this.lblCmdSingle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCmdSingle.Name = "lblCmdSingle";
            this.lblCmdSingle.Size = new System.Drawing.Size(114, 20);
            this.lblCmdSingle.TabIndex = 35;
            this.lblCmdSingle.Text = "Set \"Run\" mode";
            // 
            // lblStatusRun
            // 
            this.lblStatusRun.AutoSize = true;
            this.lblStatusRun.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusRun.Location = new System.Drawing.Point(634, 421);
            this.lblStatusRun.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusRun.Name = "lblStatusRun";
            this.lblStatusRun.Size = new System.Drawing.Size(79, 20);
            this.lblStatusRun.TabIndex = 36;
            this.lblStatusRun.Text = "Not tested";
            // 
            // btnTestRun
            // 
            this.btnTestRun.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestRun.Location = new System.Drawing.Point(546, 418);
            this.btnTestRun.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestRun.Name = "btnTestRun";
            this.btnTestRun.Size = new System.Drawing.Size(80, 22);
            this.btnTestRun.TabIndex = 21;
            this.btnTestRun.Text = "Test";
            this.btnTestRun.UseVisualStyleBackColor = true;
            this.btnTestRun.Click += new System.EventHandler(this.btnTestRun_Click);
            // 
            // txtCmdRun
            // 
            this.txtCmdRun.Location = new System.Drawing.Point(232, 418);
            this.txtCmdRun.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdRun.Name = "txtCmdRun";
            this.txtCmdRun.Size = new System.Drawing.Size(302, 27);
            this.txtCmdRun.TabIndex = 20;
            // 
            // lblCmdRun
            // 
            this.lblCmdRun.AutoSize = true;
            this.lblCmdRun.Location = new System.Drawing.Point(16, 394);
            this.lblCmdRun.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCmdRun.Name = "lblCmdRun";
            this.lblCmdRun.Size = new System.Drawing.Size(130, 20);
            this.lblCmdRun.TabIndex = 39;
            this.lblCmdRun.Text = "Set \"Single\" mode";
            // 
            // lblStatusStop
            // 
            this.lblStatusStop.AutoSize = true;
            this.lblStatusStop.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusStop.Location = new System.Drawing.Point(634, 367);
            this.lblStatusStop.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusStop.Name = "lblStatusStop";
            this.lblStatusStop.Size = new System.Drawing.Size(79, 20);
            this.lblStatusStop.TabIndex = 40;
            this.lblStatusStop.Text = "Not tested";
            // 
            // btnTestStop
            // 
            this.btnTestStop.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestStop.Location = new System.Drawing.Point(546, 364);
            this.btnTestStop.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestStop.Name = "btnTestStop";
            this.btnTestStop.Size = new System.Drawing.Size(80, 22);
            this.btnTestStop.TabIndex = 17;
            this.btnTestStop.Text = "Test";
            this.btnTestStop.UseVisualStyleBackColor = true;
            this.btnTestStop.Click += new System.EventHandler(this.btnTestStop_Click);
            // 
            // txtCmdStop
            // 
            this.txtCmdStop.Location = new System.Drawing.Point(232, 364);
            this.txtCmdStop.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdStop.Name = "txtCmdStop";
            this.txtCmdStop.Size = new System.Drawing.Size(302, 27);
            this.txtCmdStop.TabIndex = 16;
            // 
            // lblCmdStop
            // 
            this.lblCmdStop.AutoSize = true;
            this.lblCmdStop.Location = new System.Drawing.Point(16, 367);
            this.lblCmdStop.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCmdStop.Name = "lblCmdStop";
            this.lblCmdStop.Size = new System.Drawing.Size(120, 20);
            this.lblCmdStop.TabIndex = 43;
            this.lblCmdStop.Text = "Set \"Stop\" mode";
            // 
            // lblStatusActiveTrig
            // 
            this.lblStatusActiveTrig.AutoSize = true;
            this.lblStatusActiveTrig.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusActiveTrig.Location = new System.Drawing.Point(634, 340);
            this.lblStatusActiveTrig.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusActiveTrig.Name = "lblStatusActiveTrig";
            this.lblStatusActiveTrig.Size = new System.Drawing.Size(79, 20);
            this.lblStatusActiveTrig.TabIndex = 44;
            this.lblStatusActiveTrig.Text = "Not tested";
            // 
            // btnTestActiveTrig
            // 
            this.btnTestActiveTrig.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestActiveTrig.Location = new System.Drawing.Point(546, 337);
            this.btnTestActiveTrig.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestActiveTrig.Name = "btnTestActiveTrig";
            this.btnTestActiveTrig.Size = new System.Drawing.Size(80, 22);
            this.btnTestActiveTrig.TabIndex = 15;
            this.btnTestActiveTrig.Text = "Test";
            this.btnTestActiveTrig.UseVisualStyleBackColor = true;
            this.btnTestActiveTrig.Click += new System.EventHandler(this.btnTestActiveTrig_Click);
            // 
            // txtCmdActiveTrig
            // 
            this.txtCmdActiveTrig.Location = new System.Drawing.Point(232, 337);
            this.txtCmdActiveTrig.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdActiveTrig.Name = "txtCmdActiveTrig";
            this.txtCmdActiveTrig.Size = new System.Drawing.Size(302, 27);
            this.txtCmdActiveTrig.TabIndex = 14;
            // 
            // lblCmdActiveTrig
            // 
            this.lblCmdActiveTrig.AutoSize = true;
            this.lblCmdActiveTrig.Location = new System.Drawing.Point(16, 340);
            this.lblCmdActiveTrig.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCmdActiveTrig.Name = "lblCmdActiveTrig";
            this.lblCmdActiveTrig.Size = new System.Drawing.Size(140, 20);
            this.lblCmdActiveTrig.TabIndex = 47;
            this.lblCmdActiveTrig.Text = "Query active trigger";
            // 
            // lblStatusClearStats
            // 
            this.lblStatusClearStats.AutoSize = true;
            this.lblStatusClearStats.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusClearStats.Location = new System.Drawing.Point(634, 312);
            this.lblStatusClearStats.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusClearStats.Name = "lblStatusClearStats";
            this.lblStatusClearStats.Size = new System.Drawing.Size(79, 20);
            this.lblStatusClearStats.TabIndex = 48;
            this.lblStatusClearStats.Text = "Not tested";
            // 
            // btnTestClearStats
            // 
            this.btnTestClearStats.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestClearStats.Location = new System.Drawing.Point(546, 309);
            this.btnTestClearStats.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestClearStats.Name = "btnTestClearStats";
            this.btnTestClearStats.Size = new System.Drawing.Size(80, 22);
            this.btnTestClearStats.TabIndex = 13;
            this.btnTestClearStats.Text = "Test";
            this.btnTestClearStats.UseVisualStyleBackColor = true;
            this.btnTestClearStats.Click += new System.EventHandler(this.btnTestClearStats_Click);
            // 
            // txtCmdClearStats
            // 
            this.txtCmdClearStats.Location = new System.Drawing.Point(232, 309);
            this.txtCmdClearStats.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdClearStats.Name = "txtCmdClearStats";
            this.txtCmdClearStats.Size = new System.Drawing.Size(302, 27);
            this.txtCmdClearStats.TabIndex = 12;
            // 
            // lblCmdClearStats
            // 
            this.lblCmdClearStats.AutoSize = true;
            this.lblCmdClearStats.Location = new System.Drawing.Point(16, 313);
            this.lblCmdClearStats.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCmdClearStats.Name = "lblCmdClearStats";
            this.lblCmdClearStats.Size = new System.Drawing.Size(142, 20);
            this.lblCmdClearStats.TabIndex = 51;
            this.lblCmdClearStats.Text = "Set \"Clear Statistics\"";
            // 
            // lblStatusIdentify
            // 
            this.lblStatusIdentify.AutoSize = true;
            this.lblStatusIdentify.ForeColor = System.Drawing.Color.Gray;
            this.lblStatusIdentify.Location = new System.Drawing.Point(634, 229);
            this.lblStatusIdentify.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatusIdentify.Name = "lblStatusIdentify";
            this.lblStatusIdentify.Size = new System.Drawing.Size(79, 20);
            this.lblStatusIdentify.TabIndex = 52;
            this.lblStatusIdentify.Text = "Not tested";
            // 
            // btnTestIdentify
            // 
            this.btnTestIdentify.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTestIdentify.Location = new System.Drawing.Point(546, 225);
            this.btnTestIdentify.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestIdentify.Name = "btnTestIdentify";
            this.btnTestIdentify.Size = new System.Drawing.Size(80, 22);
            this.btnTestIdentify.TabIndex = 7;
            this.btnTestIdentify.Text = "Test";
            this.btnTestIdentify.UseVisualStyleBackColor = true;
            this.btnTestIdentify.Click += new System.EventHandler(this.btnTestIdentify_Click);
            // 
            // txtCmdIdentify
            // 
            this.txtCmdIdentify.Location = new System.Drawing.Point(232, 225);
            this.txtCmdIdentify.Margin = new System.Windows.Forms.Padding(4);
            this.txtCmdIdentify.Name = "txtCmdIdentify";
            this.txtCmdIdentify.Size = new System.Drawing.Size(302, 27);
            this.txtCmdIdentify.TabIndex = 6;
            // 
            // lblCmdIdentify
            // 
            this.lblCmdIdentify.AutoSize = true;
            this.lblCmdIdentify.Location = new System.Drawing.Point(16, 229);
            this.lblCmdIdentify.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCmdIdentify.Name = "lblCmdIdentify";
            this.lblCmdIdentify.Size = new System.Drawing.Size(217, 20);
            this.lblCmdIdentify.TabIndex = 55;
            this.lblCmdIdentify.Text = "Query and identify oscilloscope";
            // 
            // tabSettings
            // 
            this.tabSettings.Controls.Add(this.checkBoxDoNotClearWhenStop);
            this.tabSettings.Controls.Add(this.checkBoxMaskSerial);
            this.tabSettings.Controls.Add(this.checkBoxEnableDelete);
            this.tabSettings.Controls.Add(this.label14);
            this.tabSettings.Controls.Add(this.comboBoxAdjustToGrid);
            this.tabSettings.Controls.Add(this.labelNewVersion6);
            this.tabSettings.Controls.Add(this.checkBoxTrimUnderscore);
            this.tabSettings.Controls.Add(this.label9);
            this.tabSettings.Controls.Add(this.checkBoxDeleteDoubleUnderscore);
            this.tabSettings.Controls.Add(this.label7);
            this.tabSettings.Controls.Add(this.numericUpDownDelayMs);
            this.tabSettings.Controls.Add(this.label6);
            this.tabSettings.Controls.Add(this.numericUpDownVariables);
            this.tabSettings.Controls.Add(this.checkBoxForceClear);
            this.tabSettings.Controls.Add(this.checkBoxForceAcquisition);
            this.tabSettings.Controls.Add(this.checkBoxEnableBeep);
            this.tabSettings.Location = new System.Drawing.Point(4, 29);
            this.tabSettings.Name = "tabSettings";
            this.tabSettings.Size = new System.Drawing.Size(1057, 731);
            this.tabSettings.TabIndex = 5;
            this.tabSettings.Text = "Settings";
            this.tabSettings.UseVisualStyleBackColor = true;
            // 
            // checkBoxDoNotClearWhenStop
            // 
            this.checkBoxDoNotClearWhenStop.AutoSize = true;
            this.checkBoxDoNotClearWhenStop.Checked = true;
            this.checkBoxDoNotClearWhenStop.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxDoNotClearWhenStop.Location = new System.Drawing.Point(13, 129);
            this.checkBoxDoNotClearWhenStop.Name = "checkBoxDoNotClearWhenStop";
            this.checkBoxDoNotClearWhenStop.Size = new System.Drawing.Size(378, 24);
            this.checkBoxDoNotClearWhenStop.TabIndex = 4;
            this.checkBoxDoNotClearWhenStop.Text = "Do not \"Clear Statistics\" when already in STOP mode";
            this.checkBoxDoNotClearWhenStop.UseVisualStyleBackColor = true;
            // 
            // checkBoxMaskSerial
            // 
            this.checkBoxMaskSerial.AutoSize = true;
            this.checkBoxMaskSerial.Checked = true;
            this.checkBoxMaskSerial.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxMaskSerial.Location = new System.Drawing.Point(13, 245);
            this.checkBoxMaskSerial.Name = "checkBoxMaskSerial";
            this.checkBoxMaskSerial.Size = new System.Drawing.Size(348, 24);
            this.checkBoxMaskSerial.TabIndex = 8;
            this.checkBoxMaskSerial.Text = "Mask oscilloscope serial number in \"Debug\" tab";
            this.checkBoxMaskSerial.UseVisualStyleBackColor = true;
            // 
            // checkBoxEnableDelete
            // 
            this.checkBoxEnableDelete.AutoSize = true;
            this.checkBoxEnableDelete.Location = new System.Drawing.Point(13, 219);
            this.checkBoxEnableDelete.Name = "checkBoxEnableDelete";
            this.checkBoxEnableDelete.Size = new System.Drawing.Size(603, 24);
            this.checkBoxEnableDelete.TabIndex = 7;
            this.checkBoxEnableDelete.Text = "Keyboard [DELETE] or [BACKSPACE] will delete last saved file when in \"Capture mod" +
    "e\"";
            this.checkBoxEnableDelete.UseVisualStyleBackColor = true;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(80, 192);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(198, 20);
            this.label14.TabIndex = 62;
            this.label14.Text = "Snap to grid for trigger level";
            // 
            // comboBoxAdjustToGrid
            // 
            this.comboBoxAdjustToGrid.FormattingEnabled = true;
            this.comboBoxAdjustToGrid.Items.AddRange(new object[] {
            "0.1V",
            "0.25V",
            "0.5V",
            "1V"});
            this.comboBoxAdjustToGrid.Location = new System.Drawing.Point(13, 189);
            this.comboBoxAdjustToGrid.Name = "comboBoxAdjustToGrid";
            this.comboBoxAdjustToGrid.Size = new System.Drawing.Size(61, 28);
            this.comboBoxAdjustToGrid.TabIndex = 6;
            this.comboBoxAdjustToGrid.Text = "0.25V";
            // 
            // labelNewVersion6
            // 
            this.labelNewVersion6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelNewVersion6.AutoSize = true;
            this.labelNewVersion6.BackColor = System.Drawing.Color.IndianRed;
            this.labelNewVersion6.ForeColor = System.Drawing.Color.White;
            this.labelNewVersion6.Location = new System.Drawing.Point(773, 0);
            this.labelNewVersion6.Name = "labelNewVersion6";
            this.labelNewVersion6.Size = new System.Drawing.Size(281, 20);
            this.labelNewVersion6.TabIndex = 60;
            this.labelNewVersion6.Text = "New version available - view \"About\" tab";
            this.labelNewVersion6.Visible = false;
            // 
            // checkBoxTrimUnderscore
            // 
            this.checkBoxTrimUnderscore.AutoSize = true;
            this.checkBoxTrimUnderscore.Checked = true;
            this.checkBoxTrimUnderscore.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxTrimUnderscore.Location = new System.Drawing.Point(13, 395);
            this.checkBoxTrimUnderscore.Name = "checkBoxTrimUnderscore";
            this.checkBoxTrimUnderscore.Size = new System.Drawing.Size(504, 24);
            this.checkBoxTrimUnderscore.TabIndex = 21;
            this.checkBoxTrimUnderscore.Text = "Delete underscore/whitespace in start/end from filename before saving";
            this.checkBoxTrimUnderscore.UseVisualStyleBackColor = true;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label9.Location = new System.Drawing.Point(3, 6);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(89, 28);
            this.label9.TabIndex = 58;
            this.label9.Text = "Settings";
            // 
            // checkBoxDeleteDoubleUnderscore
            // 
            this.checkBoxDeleteDoubleUnderscore.AutoSize = true;
            this.checkBoxDeleteDoubleUnderscore.Checked = true;
            this.checkBoxDeleteDoubleUnderscore.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxDeleteDoubleUnderscore.Location = new System.Drawing.Point(13, 365);
            this.checkBoxDeleteDoubleUnderscore.Name = "checkBoxDeleteDoubleUnderscore";
            this.checkBoxDeleteDoubleUnderscore.Size = new System.Drawing.Size(487, 24);
            this.checkBoxDeleteDoubleUnderscore.TabIndex = 20;
            this.checkBoxDeleteDoubleUnderscore.Text = "Delete double underscores/whitespaces from filename before saving";
            this.checkBoxDeleteDoubleUnderscore.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(80, 158);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(379, 20);
            this.label7.TabIndex = 6;
            this.label7.Text = "Delay in milliseconds to settle on new statistics (0-5000)";
            // 
            // numericUpDownDelayMs
            // 
            this.numericUpDownDelayMs.Location = new System.Drawing.Point(13, 156);
            this.numericUpDownDelayMs.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.numericUpDownDelayMs.Name = "numericUpDownDelayMs";
            this.numericUpDownDelayMs.Size = new System.Drawing.Size(61, 27);
            this.numericUpDownDelayMs.TabIndex = 5;
            this.numericUpDownDelayMs.Value = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(9, 291);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(244, 20);
            this.label6.TabIndex = 4;
            this.label6.Text = "Number of variables available (0-5)";
            // 
            // numericUpDownVariables
            // 
            this.numericUpDownVariables.Location = new System.Drawing.Point(13, 314);
            this.numericUpDownVariables.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numericUpDownVariables.Name = "numericUpDownVariables";
            this.numericUpDownVariables.Size = new System.Drawing.Size(120, 27);
            this.numericUpDownVariables.TabIndex = 10;
            // 
            // checkBoxForceClear
            // 
            this.checkBoxForceClear.AutoSize = true;
            this.checkBoxForceClear.Location = new System.Drawing.Point(13, 102);
            this.checkBoxForceClear.Name = "checkBoxForceClear";
            this.checkBoxForceClear.Size = new System.Drawing.Size(281, 24);
            this.checkBoxForceClear.TabIndex = 3;
            this.checkBoxForceClear.Text = "Force \"Clear Statistics\" before capture";
            this.checkBoxForceClear.UseVisualStyleBackColor = true;
            // 
            // checkBoxForceAcquisition
            // 
            this.checkBoxForceAcquisition.AutoSize = true;
            this.checkBoxForceAcquisition.Checked = true;
            this.checkBoxForceAcquisition.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxForceAcquisition.Location = new System.Drawing.Point(13, 76);
            this.checkBoxForceAcquisition.Name = "checkBoxForceAcquisition";
            this.checkBoxForceAcquisition.Size = new System.Drawing.Size(232, 24);
            this.checkBoxForceAcquisition.TabIndex = 2;
            this.checkBoxForceAcquisition.Text = "Force acquisition after capture";
            this.checkBoxForceAcquisition.UseVisualStyleBackColor = true;
            // 
            // checkBoxEnableBeep
            // 
            this.checkBoxEnableBeep.AutoSize = true;
            this.checkBoxEnableBeep.Checked = true;
            this.checkBoxEnableBeep.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxEnableBeep.Location = new System.Drawing.Point(13, 50);
            this.checkBoxEnableBeep.Name = "checkBoxEnableBeep";
            this.checkBoxEnableBeep.Size = new System.Drawing.Size(198, 24);
            this.checkBoxEnableBeep.TabIndex = 1;
            this.checkBoxEnableBeep.Text = "Enable beep at capturing";
            this.checkBoxEnableBeep.UseVisualStyleBackColor = true;
            // 
            // tabCapturing
            // 
            this.tabCapturing.Controls.Add(this.labelCaptureModeInactive);
            this.tabCapturing.Controls.Add(this.labelCaptureModeActive);
            this.tabCapturing.Controls.Add(this.label15);
            this.tabCapturing.Controls.Add(this.textBoxFilenameFormat);
            this.tabCapturing.Controls.Add(this.label3);
            this.tabCapturing.Controls.Add(this.label12);
            this.tabCapturing.Controls.Add(this.label10);
            this.tabCapturing.Controls.Add(this.buttonOpenFolder1);
            this.tabCapturing.Controls.Add(this.buttonCaptureStart);
            this.tabCapturing.Controls.Add(this.richTextBoxAction);
            this.tabCapturing.Controls.Add(this.textBoxCaptureOutputFolder);
            this.tabCapturing.Controls.Add(this.label4);
            this.tabCapturing.Controls.Add(this.label2);
            this.tabCapturing.Controls.Add(this.numericUpDown1);
            this.tabCapturing.Controls.Add(this.labelNewVersion2);
            this.tabCapturing.Controls.Add(this.picScreen);
            this.tabCapturing.Location = new System.Drawing.Point(4, 29);
            this.tabCapturing.Margin = new System.Windows.Forms.Padding(4);
            this.tabCapturing.Name = "tabCapturing";
            this.tabCapturing.Padding = new System.Windows.Forms.Padding(4);
            this.tabCapturing.Size = new System.Drawing.Size(1057, 731);
            this.tabCapturing.TabIndex = 1;
            this.tabCapturing.Text = "Capturing";
            this.tabCapturing.UseVisualStyleBackColor = true;
            // 
            // labelCaptureModeInactive
            // 
            this.labelCaptureModeInactive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelCaptureModeInactive.BackColor = System.Drawing.Color.IndianRed;
            this.labelCaptureModeInactive.ForeColor = System.Drawing.Color.White;
            this.labelCaptureModeInactive.Location = new System.Drawing.Point(8, 553);
            this.labelCaptureModeInactive.Name = "labelCaptureModeInactive";
            this.labelCaptureModeInactive.Size = new System.Drawing.Size(284, 83);
            this.labelCaptureModeInactive.TabIndex = 66;
            this.labelCaptureModeInactive.Text = "Capture mode inactive and oscilloscope is not connected.\n\\r\\nClick the \"Connect t" +
    "o oscilloscope\" button.";
            this.labelCaptureModeInactive.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelCaptureModeActive
            // 
            this.labelCaptureModeActive.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelCaptureModeActive.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.labelCaptureModeActive.ForeColor = System.Drawing.Color.Black;
            this.labelCaptureModeActive.Location = new System.Drawing.Point(8, 553);
            this.labelCaptureModeActive.Name = "labelCaptureModeActive";
            this.labelCaptureModeActive.Size = new System.Drawing.Size(284, 83);
            this.labelCaptureModeActive.TabIndex = 65;
            this.labelCaptureModeActive.Text = "Capture mode active\r\nPress [ENTER] to capture";
            this.labelCaptureModeActive.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelCaptureModeActive.Visible = false;
            // 
            // label15
            // 
            this.label15.ForeColor = System.Drawing.Color.Gray;
            this.label15.Location = new System.Drawing.Point(7, 346);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(285, 62);
            this.label15.TabIndex = 64;
            this.label15.Text = "Network connectivity not checked";
            // 
            // textBoxFilenameFormat
            // 
            this.textBoxFilenameFormat.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxFilenameFormat.Location = new System.Drawing.Point(8, 165);
            this.textBoxFilenameFormat.Name = "textBoxFilenameFormat";
            this.textBoxFilenameFormat.Size = new System.Drawing.Size(284, 24);
            this.textBoxFilenameFormat.TabIndex = 3;
            this.textBoxFilenameFormat.Text = "{COMPONENT}_{NUMBER}_{VAR2}_{DATE}_{TIME}";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(4, 146);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(213, 20);
            this.label3.TabIndex = 62;
            this.label3.Text = "Filename format with variables";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label12.Location = new System.Drawing.Point(3, 6);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(105, 28);
            this.label12.TabIndex = 59;
            this.label12.Text = "Capturing";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(8, 210);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(69, 20);
            this.label10.TabIndex = 12;
            this.label10.Text = "Variables";
            // 
            // buttonOpenFolder1
            // 
            this.buttonOpenFolder1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonOpenFolder1.Location = new System.Drawing.Point(8, 100);
            this.buttonOpenFolder1.Name = "buttonOpenFolder1";
            this.buttonOpenFolder1.Size = new System.Drawing.Size(284, 28);
            this.buttonOpenFolder1.TabIndex = 2;
            this.buttonOpenFolder1.Text = "Open capture folder";
            this.buttonOpenFolder1.UseVisualStyleBackColor = true;
            this.buttonOpenFolder1.Click += new System.EventHandler(this.buttonOpenFolder_Click);
            // 
            // buttonCaptureStart
            // 
            this.buttonCaptureStart.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonCaptureStart.Location = new System.Drawing.Point(8, 274);
            this.buttonCaptureStart.Name = "buttonCaptureStart";
            this.buttonCaptureStart.Size = new System.Drawing.Size(284, 52);
            this.buttonCaptureStart.TabIndex = 20;
            this.buttonCaptureStart.Text = "Connect to oscilloscope and start capture mode";
            this.buttonCaptureStart.UseVisualStyleBackColor = true;
            // 
            // richTextBoxAction
            // 
            this.richTextBoxAction.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxAction.BackColor = System.Drawing.Color.White;
            this.richTextBoxAction.Font = new System.Drawing.Font("Segoe UI", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBoxAction.Location = new System.Drawing.Point(299, 553);
            this.richTextBoxAction.Name = "richTextBoxAction";
            this.richTextBoxAction.ReadOnly = true;
            this.richTextBoxAction.Size = new System.Drawing.Size(758, 83);
            this.richTextBoxAction.TabIndex = 30;
            this.richTextBoxAction.TabStop = false;
            this.richTextBoxAction.Text = "You should start capture mode";
            // 
            // textBoxCaptureOutputFolder
            // 
            this.textBoxCaptureOutputFolder.Location = new System.Drawing.Point(8, 71);
            this.textBoxCaptureOutputFolder.Name = "textBoxCaptureOutputFolder";
            this.textBoxCaptureOutputFolder.Size = new System.Drawing.Size(284, 27);
            this.textBoxCaptureOutputFolder.TabIndex = 1;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 50);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(105, 20);
            this.label4.TabIndex = 6;
            this.label4.Text = "Capture folder";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(90, 233);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(80, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "{NUMBER}";
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.Location = new System.Drawing.Point(11, 231);
            this.numericUpDown1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(73, 27);
            this.numericUpDown1.TabIndex = 4;
            this.numericUpDown1.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // labelNewVersion2
            // 
            this.labelNewVersion2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelNewVersion2.AutoSize = true;
            this.labelNewVersion2.BackColor = System.Drawing.Color.IndianRed;
            this.labelNewVersion2.ForeColor = System.Drawing.Color.White;
            this.labelNewVersion2.Location = new System.Drawing.Point(776, 0);
            this.labelNewVersion2.Name = "labelNewVersion2";
            this.labelNewVersion2.Size = new System.Drawing.Size(281, 20);
            this.labelNewVersion2.TabIndex = 1;
            this.labelNewVersion2.Text = "New version available - view \"About\" tab";
            this.labelNewVersion2.Visible = false;
            // 
            // picScreen
            // 
            this.picScreen.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.picScreen.BackColor = System.Drawing.Color.Black;
            this.picScreen.Location = new System.Drawing.Point(299, 0);
            this.picScreen.Margin = new System.Windows.Forms.Padding(4);
            this.picScreen.Name = "picScreen";
            this.picScreen.Size = new System.Drawing.Size(758, 546);
            this.picScreen.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picScreen.TabIndex = 0;
            this.picScreen.TabStop = false;
            // 
            // tabDebug
            // 
            this.tabDebug.Controls.Add(this.buttonDebugClear);
            this.tabDebug.Controls.Add(this.labelNewVersion3);
            this.tabDebug.Controls.Add(this.rtbLog);
            this.tabDebug.Location = new System.Drawing.Point(4, 29);
            this.tabDebug.Margin = new System.Windows.Forms.Padding(4);
            this.tabDebug.Name = "tabDebug";
            this.tabDebug.Padding = new System.Windows.Forms.Padding(4);
            this.tabDebug.Size = new System.Drawing.Size(1057, 731);
            this.tabDebug.TabIndex = 2;
            this.tabDebug.Text = "Debug";
            this.tabDebug.UseVisualStyleBackColor = true;
            // 
            // buttonDebugClear
            // 
            this.buttonDebugClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonDebugClear.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonDebugClear.Location = new System.Drawing.Point(957, 681);
            this.buttonDebugClear.Name = "buttonDebugClear";
            this.buttonDebugClear.Size = new System.Drawing.Size(75, 27);
            this.buttonDebugClear.TabIndex = 5;
            this.buttonDebugClear.Text = "Clear";
            this.buttonDebugClear.UseVisualStyleBackColor = true;
            this.buttonDebugClear.Click += new System.EventHandler(this.buttonDebugClear_Click);
            // 
            // labelNewVersion3
            // 
            this.labelNewVersion3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelNewVersion3.AutoSize = true;
            this.labelNewVersion3.BackColor = System.Drawing.Color.IndianRed;
            this.labelNewVersion3.ForeColor = System.Drawing.Color.White;
            this.labelNewVersion3.Location = new System.Drawing.Point(776, 0);
            this.labelNewVersion3.Name = "labelNewVersion3";
            this.labelNewVersion3.Size = new System.Drawing.Size(281, 20);
            this.labelNewVersion3.TabIndex = 4;
            this.labelNewVersion3.Text = "New version available - view \"About\" tab";
            this.labelNewVersion3.Visible = false;
            // 
            // rtbLog
            // 
            this.rtbLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbLog.BackColor = System.Drawing.Color.Black;
            this.rtbLog.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rtbLog.ForeColor = System.Drawing.Color.LightGreen;
            this.rtbLog.Location = new System.Drawing.Point(-4, 0);
            this.rtbLog.Margin = new System.Windows.Forms.Padding(4);
            this.rtbLog.Name = "rtbLog";
            this.rtbLog.ReadOnly = true;
            this.rtbLog.Size = new System.Drawing.Size(1065, 727);
            this.rtbLog.TabIndex = 0;
            this.rtbLog.Text = "";
            // 
            // tabShare
            // 
            this.tabShare.Controls.Add(this.buttonRefreshFolder);
            this.tabShare.Controls.Add(this.buttonOpenFolder2);
            this.tabShare.Controls.Add(this.labelFilesSelected);
            this.tabShare.Controls.Add(this.labelFilesTotal);
            this.tabShare.Controls.Add(this.buttonShareDeleteSelected);
            this.tabShare.Controls.Add(this.richTextBoxGalleryUrl);
            this.tabShare.Controls.Add(this.label22);
            this.tabShare.Controls.Add(this.label20);
            this.tabShare.Controls.Add(this.buttonShareCopyId);
            this.tabShare.Controls.Add(this.labelShareStatus);
            this.tabShare.Controls.Add(this.buttonShareUpload);
            this.tabShare.Controls.Add(this.buttonShareSelectNone);
            this.tabShare.Controls.Add(this.buttonShareSelectAll);
            this.tabShare.Controls.Add(this.listViewShareFiles);
            this.tabShare.Location = new System.Drawing.Point(4, 29);
            this.tabShare.Name = "tabShare";
            this.tabShare.Size = new System.Drawing.Size(1057, 731);
            this.tabShare.TabIndex = 7;
            this.tabShare.Text = "Share";
            this.tabShare.UseVisualStyleBackColor = true;
            // 
            // buttonRefreshFolder
            // 
            this.buttonRefreshFolder.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonRefreshFolder.Location = new System.Drawing.Point(8, 131);
            this.buttonRefreshFolder.Name = "buttonRefreshFolder";
            this.buttonRefreshFolder.Size = new System.Drawing.Size(166, 28);
            this.buttonRefreshFolder.TabIndex = 1011;
            this.buttonRefreshFolder.Text = "Refresh";
            this.buttonRefreshFolder.UseVisualStyleBackColor = true;
            // 
            // buttonOpenFolder2
            // 
            this.buttonOpenFolder2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonOpenFolder2.Location = new System.Drawing.Point(8, 97);
            this.buttonOpenFolder2.Name = "buttonOpenFolder2";
            this.buttonOpenFolder2.Size = new System.Drawing.Size(166, 28);
            this.buttonOpenFolder2.TabIndex = 1010;
            this.buttonOpenFolder2.Text = "Open capture folder";
            this.buttonOpenFolder2.UseVisualStyleBackColor = true;
            this.buttonOpenFolder2.Click += new System.EventHandler(this.buttonOpenFolder_Click);
            // 
            // labelFilesSelected
            // 
            this.labelFilesSelected.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelFilesSelected.AutoSize = true;
            this.labelFilesSelected.ForeColor = System.Drawing.Color.Black;
            this.labelFilesSelected.Location = new System.Drawing.Point(176, 629);
            this.labelFilesSelected.Name = "labelFilesSelected";
            this.labelFilesSelected.Size = new System.Drawing.Size(120, 20);
            this.labelFilesSelected.TabIndex = 1009;
            this.labelFilesSelected.Text = "Files selected: 23";
            // 
            // labelFilesTotal
            // 
            this.labelFilesTotal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelFilesTotal.AutoSize = true;
            this.labelFilesTotal.ForeColor = System.Drawing.Color.Black;
            this.labelFilesTotal.Location = new System.Drawing.Point(176, 609);
            this.labelFilesTotal.Name = "labelFilesTotal";
            this.labelFilesTotal.Size = new System.Drawing.Size(112, 20);
            this.labelFilesTotal.TabIndex = 1008;
            this.labelFilesTotal.Text = "Files in total: 24";
            // 
            // buttonShareDeleteSelected
            // 
            this.buttonShareDeleteSelected.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonShareDeleteSelected.Location = new System.Drawing.Point(8, 374);
            this.buttonShareDeleteSelected.Name = "buttonShareDeleteSelected";
            this.buttonShareDeleteSelected.Size = new System.Drawing.Size(166, 29);
            this.buttonShareDeleteSelected.TabIndex = 1007;
            this.buttonShareDeleteSelected.Text = "Delete selected";
            this.buttonShareDeleteSelected.UseVisualStyleBackColor = true;
            // 
            // richTextBoxGalleryUrl
            // 
            this.richTextBoxGalleryUrl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.richTextBoxGalleryUrl.BackColor = System.Drawing.Color.White;
            this.richTextBoxGalleryUrl.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBoxGalleryUrl.Cursor = System.Windows.Forms.Cursors.Hand;
            this.richTextBoxGalleryUrl.Font = new System.Drawing.Font("Segoe UI", 10.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBoxGalleryUrl.Location = new System.Drawing.Point(322, 680);
            this.richTextBoxGalleryUrl.Multiline = false;
            this.richTextBoxGalleryUrl.Name = "richTextBoxGalleryUrl";
            this.richTextBoxGalleryUrl.ReadOnly = true;
            this.richTextBoxGalleryUrl.Size = new System.Drawing.Size(723, 29);
            this.richTextBoxGalleryUrl.TabIndex = 1005;
            this.richTextBoxGalleryUrl.Text = "Gallery URL";
            // 
            // label22
            // 
            this.label22.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label22.Location = new System.Drawing.Point(12, 37);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(1033, 57);
            this.label22.TabIndex = 1004;
            this.label22.Text = resources.GetString("label22.Text");
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label20.Location = new System.Drawing.Point(3, 5);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(65, 28);
            this.label20.TabIndex = 1003;
            this.label20.Text = "Share";
            // 
            // buttonShareCopyId
            // 
            this.buttonShareCopyId.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonShareCopyId.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonShareCopyId.Location = new System.Drawing.Point(180, 676);
            this.buttonShareCopyId.Name = "buttonShareCopyId";
            this.buttonShareCopyId.Size = new System.Drawing.Size(136, 29);
            this.buttonShareCopyId.TabIndex = 5;
            this.buttonShareCopyId.Text = "Copy gallery URL";
            this.buttonShareCopyId.UseVisualStyleBackColor = true;
            // 
            // labelShareStatus
            // 
            this.labelShareStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.labelShareStatus.BackColor = System.Drawing.Color.IndianRed;
            this.labelShareStatus.ForeColor = System.Drawing.Color.White;
            this.labelShareStatus.Location = new System.Drawing.Point(874, 609);
            this.labelShareStatus.Name = "labelShareStatus";
            this.labelShareStatus.Size = new System.Drawing.Size(171, 20);
            this.labelShareStatus.TabIndex = 1001;
            this.labelShareStatus.Text = "Status";
            this.labelShareStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonShareUpload
            // 
            this.buttonShareUpload.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonShareUpload.Location = new System.Drawing.Point(8, 304);
            this.buttonShareUpload.Name = "buttonShareUpload";
            this.buttonShareUpload.Size = new System.Drawing.Size(166, 29);
            this.buttonShareUpload.TabIndex = 3;
            this.buttonShareUpload.Text = "Upload";
            this.buttonShareUpload.UseVisualStyleBackColor = true;
            // 
            // buttonShareSelectNone
            // 
            this.buttonShareSelectNone.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonShareSelectNone.Location = new System.Drawing.Point(8, 234);
            this.buttonShareSelectNone.Name = "buttonShareSelectNone";
            this.buttonShareSelectNone.Size = new System.Drawing.Size(166, 29);
            this.buttonShareSelectNone.TabIndex = 2;
            this.buttonShareSelectNone.Text = "Select NONE";
            this.buttonShareSelectNone.UseVisualStyleBackColor = true;
            // 
            // buttonShareSelectAll
            // 
            this.buttonShareSelectAll.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonShareSelectAll.Location = new System.Drawing.Point(8, 199);
            this.buttonShareSelectAll.Name = "buttonShareSelectAll";
            this.buttonShareSelectAll.Size = new System.Drawing.Size(166, 29);
            this.buttonShareSelectAll.TabIndex = 1;
            this.buttonShareSelectAll.Text = "Select ALL";
            this.buttonShareSelectAll.UseVisualStyleBackColor = true;
            // 
            // listViewShareFiles
            // 
            this.listViewShareFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewShareFiles.CheckBoxes = true;
            this.listViewShareFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderShareFilename,
            this.columnHeaderShareSize});
            this.listViewShareFiles.FullRowSelect = true;
            this.listViewShareFiles.HideSelection = false;
            this.listViewShareFiles.Location = new System.Drawing.Point(180, 97);
            this.listViewShareFiles.MultiSelect = false;
            this.listViewShareFiles.Name = "listViewShareFiles";
            this.listViewShareFiles.Size = new System.Drawing.Size(865, 509);
            this.listViewShareFiles.TabIndex = 0;
            this.listViewShareFiles.UseCompatibleStateImageBehavior = false;
            this.listViewShareFiles.View = System.Windows.Forms.View.Details;
            this.listViewShareFiles.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listViewShareFiles_MouseUp);
            // 
            // columnHeaderShareFilename
            // 
            this.columnHeaderShareFilename.Text = "Filename";
            this.columnHeaderShareFilename.Width = 432;
            // 
            // columnHeaderShareSize
            // 
            this.columnHeaderShareSize.Text = "Size";
            this.columnHeaderShareSize.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeaderShareSize.Width = 120;
            // 
            // tabFeedback
            // 
            this.tabFeedback.Controls.Add(this.checkBoxFeedbackAttachConfig);
            this.tabFeedback.Controls.Add(this.checkBoxFeedbackAttachDebug);
            this.tabFeedback.Controls.Add(this.label18);
            this.tabFeedback.Controls.Add(this.label17);
            this.tabFeedback.Controls.Add(this.textBoxFeedback);
            this.tabFeedback.Controls.Add(this.buttonSendToDeveloper);
            this.tabFeedback.Controls.Add(this.label1);
            this.tabFeedback.Controls.Add(this.textBoxEmail);
            this.tabFeedback.Location = new System.Drawing.Point(4, 29);
            this.tabFeedback.Name = "tabFeedback";
            this.tabFeedback.Size = new System.Drawing.Size(1057, 731);
            this.tabFeedback.TabIndex = 6;
            this.tabFeedback.Text = "Feedback";
            this.tabFeedback.UseVisualStyleBackColor = true;
            // 
            // checkBoxFeedbackAttachConfig
            // 
            this.checkBoxFeedbackAttachConfig.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxFeedbackAttachConfig.AutoSize = true;
            this.checkBoxFeedbackAttachConfig.Location = new System.Drawing.Point(24, 421);
            this.checkBoxFeedbackAttachConfig.Name = "checkBoxFeedbackAttachConfig";
            this.checkBoxFeedbackAttachConfig.Size = new System.Drawing.Size(192, 24);
            this.checkBoxFeedbackAttachConfig.TabIndex = 62;
            this.checkBoxFeedbackAttachConfig.Text = "Attach configuration file";
            this.checkBoxFeedbackAttachConfig.UseVisualStyleBackColor = true;
            // 
            // checkBoxFeedbackAttachDebug
            // 
            this.checkBoxFeedbackAttachDebug.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxFeedbackAttachDebug.AutoSize = true;
            this.checkBoxFeedbackAttachDebug.Checked = true;
            this.checkBoxFeedbackAttachDebug.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxFeedbackAttachDebug.Location = new System.Drawing.Point(23, 391);
            this.checkBoxFeedbackAttachDebug.Name = "checkBoxFeedbackAttachDebug";
            this.checkBoxFeedbackAttachDebug.Size = new System.Drawing.Size(147, 24);
            this.checkBoxFeedbackAttachDebug.TabIndex = 61;
            this.checkBoxFeedbackAttachDebug.Text = "Attach debug log";
            this.checkBoxFeedbackAttachDebug.UseVisualStyleBackColor = true;
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label18.Location = new System.Drawing.Point(3, 5);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(226, 28);
            this.label18.TabIndex = 60;
            this.label18.Text = "Feedback to developer";
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(19, 134);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(527, 20);
            this.label17.TabIndex = 12;
            this.label17.Text = "Your feedback to developer (please explain debug or provide your comments)";
            // 
            // textBoxFeedback
            // 
            this.textBoxFeedback.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFeedback.Location = new System.Drawing.Point(24, 157);
            this.textBoxFeedback.Multiline = true;
            this.textBoxFeedback.Name = "textBoxFeedback";
            this.textBoxFeedback.Size = new System.Drawing.Size(1002, 222);
            this.textBoxFeedback.TabIndex = 11;
            // 
            // buttonSendToDeveloper
            // 
            this.buttonSendToDeveloper.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonSendToDeveloper.Cursor = System.Windows.Forms.Cursors.Hand;
            this.buttonSendToDeveloper.Location = new System.Drawing.Point(23, 465);
            this.buttonSendToDeveloper.Name = "buttonSendToDeveloper";
            this.buttonSendToDeveloper.Size = new System.Drawing.Size(259, 27);
            this.buttonSendToDeveloper.TabIndex = 10;
            this.buttonSendToDeveloper.Text = "Send feedback to developer";
            this.buttonSendToDeveloper.UseVisualStyleBackColor = true;
            this.buttonSendToDeveloper.Click += new System.EventHandler(this.buttonSendToDeveloper_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 70);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(454, 20);
            this.label1.TabIndex = 9;
            this.label1.Text = "Your email address (optional, but needed if you want any response)";
            // 
            // textBoxEmail
            // 
            this.textBoxEmail.Location = new System.Drawing.Point(24, 93);
            this.textBoxEmail.Name = "textBoxEmail";
            this.textBoxEmail.Size = new System.Drawing.Size(1002, 27);
            this.textBoxEmail.TabIndex = 8;
            // 
            // tabHelp
            // 
            this.tabHelp.Controls.Add(this.richTextBoxHelp);
            this.tabHelp.Controls.Add(this.labelNewVersion4);
            this.tabHelp.Location = new System.Drawing.Point(4, 29);
            this.tabHelp.Margin = new System.Windows.Forms.Padding(4);
            this.tabHelp.Name = "tabHelp";
            this.tabHelp.Padding = new System.Windows.Forms.Padding(4);
            this.tabHelp.Size = new System.Drawing.Size(1057, 731);
            this.tabHelp.TabIndex = 3;
            this.tabHelp.Text = "Help";
            this.tabHelp.UseVisualStyleBackColor = true;
            // 
            // richTextBoxHelp
            // 
            this.richTextBoxHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxHelp.BackColor = System.Drawing.Color.White;
            this.richTextBoxHelp.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBoxHelp.Location = new System.Drawing.Point(8, 23);
            this.richTextBoxHelp.Name = "richTextBoxHelp";
            this.richTextBoxHelp.ReadOnly = true;
            this.richTextBoxHelp.Size = new System.Drawing.Size(1048, 678);
            this.richTextBoxHelp.TabIndex = 61;
            this.richTextBoxHelp.Text = "Text built dynamically ...";
            // 
            // labelNewVersion4
            // 
            this.labelNewVersion4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelNewVersion4.AutoSize = true;
            this.labelNewVersion4.BackColor = System.Drawing.Color.IndianRed;
            this.labelNewVersion4.ForeColor = System.Drawing.Color.White;
            this.labelNewVersion4.Location = new System.Drawing.Point(775, 0);
            this.labelNewVersion4.Name = "labelNewVersion4";
            this.labelNewVersion4.Size = new System.Drawing.Size(281, 20);
            this.labelNewVersion4.TabIndex = 0;
            this.labelNewVersion4.Text = "New version available - view \"About\" tab";
            this.labelNewVersion4.Visible = false;
            // 
            // tabAbout
            // 
            this.tabAbout.Controls.Add(this.labelVersion);
            this.tabAbout.Controls.Add(this.label16);
            this.tabAbout.Controls.Add(this.pictureBox1);
            this.tabAbout.Controls.Add(this.richTextBoxAbout);
            this.tabAbout.Controls.Add(this.labelNewVersion5);
            this.tabAbout.Location = new System.Drawing.Point(4, 29);
            this.tabAbout.Margin = new System.Windows.Forms.Padding(4);
            this.tabAbout.Name = "tabAbout";
            this.tabAbout.Padding = new System.Windows.Forms.Padding(4);
            this.tabAbout.Size = new System.Drawing.Size(1057, 731);
            this.tabAbout.TabIndex = 4;
            this.tabAbout.Text = "About";
            this.tabAbout.UseVisualStyleBackColor = true;
            // 
            // labelVersion
            // 
            this.labelVersion.AutoSize = true;
            this.labelVersion.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelVersion.Location = new System.Drawing.Point(160, 84);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(224, 28);
            this.labelVersion.TabIndex = 4;
            this.labelVersion.Text = "Version 2025-October-2";
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Font = new System.Drawing.Font("Segoe UI", 22.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label16.Location = new System.Drawing.Point(156, 44);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(549, 50);
            this.label16.TabIndex = 3;
            this.label16.Text = "Oscilloscope Network Capture";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(16, 18);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(128, 128);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // richTextBoxAbout
            // 
            this.richTextBoxAbout.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxAbout.BackColor = System.Drawing.Color.White;
            this.richTextBoxAbout.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.richTextBoxAbout.Location = new System.Drawing.Point(7, 172);
            this.richTextBoxAbout.Name = "richTextBoxAbout";
            this.richTextBoxAbout.ReadOnly = true;
            this.richTextBoxAbout.Size = new System.Drawing.Size(1044, 469);
            this.richTextBoxAbout.TabIndex = 1;
            this.richTextBoxAbout.Text = "Text built dynamically ...";
            // 
            // labelNewVersion5
            // 
            this.labelNewVersion5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelNewVersion5.AutoSize = true;
            this.labelNewVersion5.BackColor = System.Drawing.Color.IndianRed;
            this.labelNewVersion5.ForeColor = System.Drawing.Color.White;
            this.labelNewVersion5.Location = new System.Drawing.Point(892, 4);
            this.labelNewVersion5.Name = "labelNewVersion5";
            this.labelNewVersion5.Size = new System.Drawing.Size(158, 20);
            this.labelNewVersion5.TabIndex = 0;
            this.labelNewVersion5.Text = "New version available ";
            this.labelNewVersion5.Visible = false;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1067, 763);
            this.Controls.Add(this.tabMain);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(1085, 810);
            this.Name = "Main";
            this.Text = "Oscilloscope Network Capture";
            this.Load += new System.EventHandler(this.Main_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numPort)).EndInit();
            this.tabMain.ResumeLayout(false);
            this.tabConfiguration.ResumeLayout(false);
            this.tabConfiguration.PerformLayout();
            this.tabSettings.ResumeLayout(false);
            this.tabSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownDelayMs)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownVariables)).EndInit();
            this.tabCapturing.ResumeLayout(false);
            this.tabCapturing.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picScreen)).EndInit();
            this.tabDebug.ResumeLayout(false);
            this.tabDebug.PerformLayout();
            this.tabShare.ResumeLayout(false);
            this.tabShare.PerformLayout();
            this.tabFeedback.ResumeLayout(false);
            this.tabFeedback.PerformLayout();
            this.tabHelp.ResumeLayout(false);
            this.tabHelp.PerformLayout();
            this.tabAbout.ResumeLayout(false);
            this.tabAbout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox cboVendor;
        private System.Windows.Forms.ComboBox cboModel;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Label lblVendor;
        private System.Windows.Forms.Label lblModel;
        private System.Windows.Forms.Label lblIp;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtIp;
        private System.Windows.Forms.NumericUpDown numPort;
        private System.Windows.Forms.TabControl tabMain;
        private System.Windows.Forms.TabPage tabConfiguration;
        private System.Windows.Forms.TabPage tabCapturing;
        private System.Windows.Forms.TabPage tabDebug;
        private System.Windows.Forms.TabPage tabHelp;
        private System.Windows.Forms.TabPage tabAbout;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.PictureBox picScreen;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.Label lblCmdIdentify; private System.Windows.Forms.TextBox txtCmdIdentify; private System.Windows.Forms.Button btnTestIdentify; private System.Windows.Forms.Label lblStatusIdentify;
        private System.Windows.Forms.Label lblCmdClearStats; private System.Windows.Forms.TextBox txtCmdClearStats; private System.Windows.Forms.Button btnTestClearStats; private System.Windows.Forms.Label lblStatusClearStats;
        private System.Windows.Forms.Label lblCmdActiveTrig; private System.Windows.Forms.TextBox txtCmdActiveTrig; private System.Windows.Forms.Button btnTestActiveTrig; private System.Windows.Forms.Label lblStatusActiveTrig;
        private System.Windows.Forms.Label lblCmdStop; private System.Windows.Forms.TextBox txtCmdStop; private System.Windows.Forms.Button btnTestStop; private System.Windows.Forms.Label lblStatusStop;
        private System.Windows.Forms.Label lblCmdRun; private System.Windows.Forms.TextBox txtCmdRun; private System.Windows.Forms.Button btnTestRun; private System.Windows.Forms.Label lblStatusRun;
        private System.Windows.Forms.Label lblCmdSingle; private System.Windows.Forms.TextBox txtCmdSingle; private System.Windows.Forms.Button btnTestSingle; private System.Windows.Forms.Label lblStatusSingle;
        private System.Windows.Forms.Label lblCmdTrigMode; private System.Windows.Forms.TextBox txtCmdTrigMode; private System.Windows.Forms.Button btnTestTrigMode; private System.Windows.Forms.Label lblStatusTrigMode;
        private System.Windows.Forms.Label lblCmdTrigLevelQ; private System.Windows.Forms.TextBox txtCmdTrigLevelQ; private System.Windows.Forms.Button btnTestTrigLevelQ; private System.Windows.Forms.Label lblStatusTrigLevelQ;
        private System.Windows.Forms.Label lblCmdTrigLevelSet; private System.Windows.Forms.TextBox txtCmdTrigLevelSet; private System.Windows.Forms.Button btnTestTrigLevelSet; private System.Windows.Forms.Label lblStatusTrigLevelSet;
        private System.Windows.Forms.Label lblCmdTimeDivQ; private System.Windows.Forms.TextBox txtCmdTimeDivQ; private System.Windows.Forms.Button btnTestTimeDivQ; private System.Windows.Forms.Label lblStatusTimeDivQ;
        private System.Windows.Forms.Label lblCmdTimeDivSet; private System.Windows.Forms.TextBox txtCmdTimeDivSet; private System.Windows.Forms.Button btnTestTimeDivSet; private System.Windows.Forms.Label lblStatusTimeDivSet;
        private System.Windows.Forms.Label lblCmdDumpImage; private System.Windows.Forms.TextBox txtCmdDumpImage; private System.Windows.Forms.Button btnTestDumpImage; private System.Windows.Forms.Label lblStatusDumpImage;
        private System.Windows.Forms.Label lblCmdSysErr; private System.Windows.Forms.TextBox txtCmdSysErr; private System.Windows.Forms.Button btnTestSysErr; private System.Windows.Forms.Label lblStatusSysErr;
        private System.Windows.Forms.Label lblCmdOpc; private System.Windows.Forms.TextBox txtCmdOpc; private System.Windows.Forms.Button btnTestOpc; private System.Windows.Forms.Label lblStatusOpc;
        private System.Windows.Forms.Label labelNewVersion1;
        private System.Windows.Forms.Label labelNewVersion2;
        private System.Windows.Forms.Label labelNewVersion3;
        private System.Windows.Forms.Label labelNewVersion4;
        private System.Windows.Forms.Label labelNewVersion5;
        private System.Windows.Forms.RichTextBox richTextBoxAbout;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.TextBox textBoxCaptureOutputFolder;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RichTextBox richTextBoxAction;
        private System.Windows.Forms.TabPage tabSettings;
        private System.Windows.Forms.Button buttonCaptureStart;
        private System.Windows.Forms.NumericUpDown numericUpDownVariables;
        private System.Windows.Forms.CheckBox checkBoxForceClear;
        private System.Windows.Forms.CheckBox checkBoxForceAcquisition;
        private System.Windows.Forms.CheckBox checkBoxEnableBeep;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button buttonOpenFolder1;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.NumericUpDown numericUpDownDelayMs;
        private System.Windows.Forms.CheckBox checkBoxDeleteDoubleUnderscore;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.CheckBox checkBoxTrimUnderscore;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox textBoxFilenameFormat;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label labelNewVersion6;
        private System.Windows.Forms.Button buttonDebugClear;
        private System.Windows.Forms.ComboBox comboBoxAdjustToGrid;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.CheckBox checkBoxEnableDelete;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.RichTextBox richTextBoxHelp;
        private System.Windows.Forms.TextBox txtTimeDivValues;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.CheckBox checkBoxMaskSerial;
        private System.Windows.Forms.TabPage tabFeedback;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.TextBox textBoxFeedback;
        private System.Windows.Forms.Button buttonSendToDeveloper;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxEmail;
        private System.Windows.Forms.CheckBox checkBoxFeedbackAttachConfig;
        private System.Windows.Forms.CheckBox checkBoxFeedbackAttachDebug;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.CheckBox checkBoxDoNotClearWhenStop;
        private System.Windows.Forms.Label labelCaptureModeActive;
        private System.Windows.Forms.Label labelCaptureModeInactive;
        private System.Windows.Forms.Label lblStatusVoltsDivSet;
        private System.Windows.Forms.Button btnTestVoltsDivSet;
        private System.Windows.Forms.TextBox txtCmdVoltsDivSet;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.Label lblStatusVoltsDivQ;
        private System.Windows.Forms.Button btnTestVoltsDivQ;
        private System.Windows.Forms.TextBox txtCmdVoltsDivQ;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.TextBox txtVoltsDivValues;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TabPage tabShare;
        private System.Windows.Forms.Label label22;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.RichTextBox richTextBoxGalleryUrl;
        private System.Windows.Forms.Button buttonShareDeleteSelected;
        private System.Windows.Forms.Label labelFilesSelected;
        private System.Windows.Forms.Label labelFilesTotal;
        private System.Windows.Forms.Button buttonOpenFolder2;
        private System.Windows.Forms.Button buttonRefreshFolder;
    }
}

