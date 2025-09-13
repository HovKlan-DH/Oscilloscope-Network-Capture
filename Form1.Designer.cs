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
            this.buttonCaptureOnce = new System.Windows.Forms.Button();
            this.comboBoxRegion = new System.Windows.Forms.ComboBox();
            this.textBoxIp = new System.Windows.Forms.TextBox();
            this.buttonCaptureContinuelsy = new System.Windows.Forms.Button();
            this.richTextBoxLog = new System.Windows.Forms.RichTextBox();
            this.pictureBoxImage = new System.Windows.Forms.PictureBox();
            this.labelIp = new System.Windows.Forms.Label();
            this.labelRegion = new System.Windows.Forms.Label();
            this.textBoxCapturePinStart = new System.Windows.Forms.TextBox();
            this.labelCapturePinStart = new System.Windows.Forms.Label();
            this.labelCapturePinEnd = new System.Windows.Forms.Label();
            this.textBoxCapturePinEnd = new System.Windows.Forms.TextBox();
            this.richTextBoxAction = new System.Windows.Forms.RichTextBox();
            this.labelComponent = new System.Windows.Forms.Label();
            this.textBoxComponent = new System.Windows.Forms.TextBox();
            this.labelAction = new System.Windows.Forms.Label();
            this.buttonCheckScope = new System.Windows.Forms.Button();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageMeasurements = new System.Windows.Forms.TabPage();
            this.buttonOpenFolder = new System.Windows.Forms.Button();
            this.panel4 = new System.Windows.Forms.Panel();
            this.checkBoxBeep = new System.Windows.Forms.CheckBox();
            this.textBoxPort = new System.Windows.Forms.TextBox();
            this.labelPort = new System.Windows.Forms.Label();
            this.panel3 = new System.Windows.Forms.Panel();
            this.textBoxFilenameFormat = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.textBoxCapturePin = new System.Windows.Forms.TextBox();
            this.labelCapturePin = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
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
            this.panel4.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tabPageHelp.SuspendLayout();
            this.tabPageAbout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxIcon)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonCaptureOnce
            // 
            this.buttonCaptureOnce.BackColor = System.Drawing.Color.Cornsilk;
            this.buttonCaptureOnce.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCaptureOnce.Location = new System.Drawing.Point(9, 46);
            this.buttonCaptureOnce.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.buttonCaptureOnce.Name = "buttonCaptureOnce";
            this.buttonCaptureOnce.Size = new System.Drawing.Size(331, 34);
            this.buttonCaptureOnce.TabIndex = 8;
            this.buttonCaptureOnce.Text = "Capture one image only";
            this.buttonCaptureOnce.UseVisualStyleBackColor = false;
            // 
            // comboBoxRegion
            // 
            this.comboBoxRegion.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBoxRegion.FormattingEnabled = true;
            this.comboBoxRegion.Items.AddRange(new object[] {
            "PAL",
            "NTSC"});
            this.comboBoxRegion.Location = new System.Drawing.Point(9, 17);
            this.comboBoxRegion.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.comboBoxRegion.Name = "comboBoxRegion";
            this.comboBoxRegion.Size = new System.Drawing.Size(78, 29);
            this.comboBoxRegion.TabIndex = 4;
            // 
            // textBoxIp
            // 
            this.textBoxIp.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxIp.Location = new System.Drawing.Point(10, 38);
            this.textBoxIp.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxIp.Name = "textBoxIp";
            this.textBoxIp.Size = new System.Drawing.Size(135, 28);
            this.textBoxIp.TabIndex = 1;
            // 
            // buttonCaptureContinuelsy
            // 
            this.buttonCaptureContinuelsy.BackColor = System.Drawing.Color.Cornsilk;
            this.buttonCaptureContinuelsy.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCaptureContinuelsy.Location = new System.Drawing.Point(9, 84);
            this.buttonCaptureContinuelsy.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.buttonCaptureContinuelsy.Name = "buttonCaptureContinuelsy";
            this.buttonCaptureContinuelsy.Size = new System.Drawing.Size(331, 35);
            this.buttonCaptureContinuelsy.TabIndex = 11;
            this.buttonCaptureContinuelsy.Text = "Capture continuesly until end-pin or ESC";
            this.buttonCaptureContinuelsy.UseVisualStyleBackColor = false;
            // 
            // richTextBoxLog
            // 
            this.richTextBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxLog.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.richTextBoxLog.Location = new System.Drawing.Point(364, 8);
            this.richTextBoxLog.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.richTextBoxLog.Name = "richTextBoxLog";
            this.richTextBoxLog.Size = new System.Drawing.Size(527, 361);
            this.richTextBoxLog.TabIndex = 4;
            this.richTextBoxLog.Text = "";
            // 
            // pictureBoxImage
            // 
            this.pictureBoxImage.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pictureBoxImage.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBoxImage.Location = new System.Drawing.Point(364, 370);
            this.pictureBoxImage.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pictureBoxImage.Name = "pictureBoxImage";
            this.pictureBoxImage.Size = new System.Drawing.Size(525, 315);
            this.pictureBoxImage.TabIndex = 5;
            this.pictureBoxImage.TabStop = false;
            // 
            // labelIp
            // 
            this.labelIp.AutoSize = true;
            this.labelIp.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelIp.Location = new System.Drawing.Point(6, 13);
            this.labelIp.Name = "labelIp";
            this.labelIp.Size = new System.Drawing.Size(126, 21);
            this.labelIp.TabIndex = 0;
            this.labelIp.Text = "Scope IP address";
            // 
            // labelRegion
            // 
            this.labelRegion.AutoSize = true;
            this.labelRegion.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelRegion.Location = new System.Drawing.Point(93, 20);
            this.labelRegion.Name = "labelRegion";
            this.labelRegion.Size = new System.Drawing.Size(57, 21);
            this.labelRegion.TabIndex = 0;
            this.labelRegion.Text = "Region";
            // 
            // textBoxCapturePinStart
            // 
            this.textBoxCapturePinStart.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxCapturePinStart.Location = new System.Drawing.Point(10, 12);
            this.textBoxCapturePinStart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxCapturePinStart.Name = "textBoxCapturePinStart";
            this.textBoxCapturePinStart.Size = new System.Drawing.Size(69, 28);
            this.textBoxCapturePinStart.TabIndex = 9;
            this.textBoxCapturePinStart.Text = "1";
            // 
            // labelCapturePinStart
            // 
            this.labelCapturePinStart.AutoSize = true;
            this.labelCapturePinStart.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCapturePinStart.Location = new System.Drawing.Point(85, 15);
            this.labelCapturePinStart.Name = "labelCapturePinStart";
            this.labelCapturePinStart.Size = new System.Drawing.Size(165, 21);
            this.labelCapturePinStart.TabIndex = 0;
            this.labelCapturePinStart.Text = "Start capture from pin";
            // 
            // labelCapturePinEnd
            // 
            this.labelCapturePinEnd.AutoSize = true;
            this.labelCapturePinEnd.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCapturePinEnd.Location = new System.Drawing.Point(85, 51);
            this.labelCapturePinEnd.Name = "labelCapturePinEnd";
            this.labelCapturePinEnd.Size = new System.Drawing.Size(154, 21);
            this.labelCapturePinEnd.TabIndex = 0;
            this.labelCapturePinEnd.Text = "End capture with pin";
            // 
            // textBoxCapturePinEnd
            // 
            this.textBoxCapturePinEnd.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxCapturePinEnd.Location = new System.Drawing.Point(10, 48);
            this.textBoxCapturePinEnd.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxCapturePinEnd.Name = "textBoxCapturePinEnd";
            this.textBoxCapturePinEnd.Size = new System.Drawing.Size(69, 28);
            this.textBoxCapturePinEnd.TabIndex = 10;
            this.textBoxCapturePinEnd.Text = "40";
            // 
            // richTextBoxAction
            // 
            this.richTextBoxAction.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.richTextBoxAction.BackColor = System.Drawing.Color.White;
            this.richTextBoxAction.Font = new System.Drawing.Font("Calibri", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBoxAction.Location = new System.Drawing.Point(6, 569);
            this.richTextBoxAction.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.richTextBoxAction.Name = "richTextBoxAction";
            this.richTextBoxAction.ReadOnly = true;
            this.richTextBoxAction.Size = new System.Drawing.Size(352, 74);
            this.richTextBoxAction.TabIndex = 0;
            this.richTextBoxAction.TabStop = false;
            this.richTextBoxAction.Text = "Ready to capture pin 1 of 40";
            // 
            // labelComponent
            // 
            this.labelComponent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelComponent.AutoSize = true;
            this.labelComponent.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelComponent.Location = new System.Drawing.Point(93, 57);
            this.labelComponent.Name = "labelComponent";
            this.labelComponent.Size = new System.Drawing.Size(135, 21);
            this.labelComponent.TabIndex = 0;
            this.labelComponent.Text = "Component name";
            // 
            // textBoxComponent
            // 
            this.textBoxComponent.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxComponent.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxComponent.Location = new System.Drawing.Point(10, 54);
            this.textBoxComponent.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxComponent.Name = "textBoxComponent";
            this.textBoxComponent.Size = new System.Drawing.Size(77, 28);
            this.textBoxComponent.TabIndex = 5;
            // 
            // labelAction
            // 
            this.labelAction.AutoSize = true;
            this.labelAction.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelAction.Location = new System.Drawing.Point(6, 547);
            this.labelAction.Name = "labelAction";
            this.labelAction.Size = new System.Drawing.Size(54, 21);
            this.labelAction.TabIndex = 0;
            this.labelAction.Text = "Action";
            // 
            // buttonCheckScope
            // 
            this.buttonCheckScope.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCheckScope.BackColor = System.Drawing.Color.Cornsilk;
            this.buttonCheckScope.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCheckScope.Location = new System.Drawing.Point(10, 74);
            this.buttonCheckScope.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.buttonCheckScope.Name = "buttonCheckScope";
            this.buttonCheckScope.Size = new System.Drawing.Size(330, 34);
            this.buttonCheckScope.TabIndex = 3;
            this.buttonCheckScope.Text = "Check oscilloscope connectivity";
            this.buttonCheckScope.UseVisualStyleBackColor = false;
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
            this.tabControl1.Size = new System.Drawing.Size(905, 723);
            this.tabControl1.TabIndex = 17;
            // 
            // tabPageMeasurements
            // 
            this.tabPageMeasurements.Controls.Add(this.buttonOpenFolder);
            this.tabPageMeasurements.Controls.Add(this.panel4);
            this.tabPageMeasurements.Controls.Add(this.panel3);
            this.tabPageMeasurements.Controls.Add(this.panel2);
            this.tabPageMeasurements.Controls.Add(this.panel1);
            this.tabPageMeasurements.Controls.Add(this.richTextBoxLog);
            this.tabPageMeasurements.Controls.Add(this.labelAction);
            this.tabPageMeasurements.Controls.Add(this.pictureBoxImage);
            this.tabPageMeasurements.Controls.Add(this.richTextBoxAction);
            this.tabPageMeasurements.Location = new System.Drawing.Point(4, 30);
            this.tabPageMeasurements.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabPageMeasurements.Name = "tabPageMeasurements";
            this.tabPageMeasurements.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabPageMeasurements.Size = new System.Drawing.Size(897, 689);
            this.tabPageMeasurements.TabIndex = 0;
            this.tabPageMeasurements.Text = "Measurements";
            this.tabPageMeasurements.UseVisualStyleBackColor = true;
            // 
            // buttonOpenFolder
            // 
            this.buttonOpenFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonOpenFolder.BackColor = System.Drawing.Color.Cornsilk;
            this.buttonOpenFolder.Font = new System.Drawing.Font("Calibri", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonOpenFolder.Location = new System.Drawing.Point(6, 650);
            this.buttonOpenFolder.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.buttonOpenFolder.Name = "buttonOpenFolder";
            this.buttonOpenFolder.Size = new System.Drawing.Size(352, 35);
            this.buttonOpenFolder.TabIndex = 12;
            this.buttonOpenFolder.Text = "Open capture folder";
            this.buttonOpenFolder.UseVisualStyleBackColor = false;
            this.buttonOpenFolder.Click += new System.EventHandler(this.buttonOpenFolder_Click);
            // 
            // panel4
            // 
            this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel4.Controls.Add(this.checkBoxBeep);
            this.panel4.Controls.Add(this.textBoxPort);
            this.panel4.Controls.Add(this.labelPort);
            this.panel4.Controls.Add(this.textBoxIp);
            this.panel4.Controls.Add(this.labelIp);
            this.panel4.Controls.Add(this.buttonCheckScope);
            this.panel4.Location = new System.Drawing.Point(6, 7);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(352, 128);
            this.panel4.TabIndex = 22;
            // 
            // checkBoxBeep
            // 
            this.checkBoxBeep.AutoSize = true;
            this.checkBoxBeep.Checked = true;
            this.checkBoxBeep.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxBeep.Location = new System.Drawing.Point(250, 40);
            this.checkBoxBeep.Name = "checkBoxBeep";
            this.checkBoxBeep.Size = new System.Drawing.Size(116, 25);
            this.checkBoxBeep.TabIndex = 4;
            this.checkBoxBeep.Text = "Enable beep";
            this.checkBoxBeep.UseVisualStyleBackColor = true;
            // 
            // textBoxPort
            // 
            this.textBoxPort.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxPort.Location = new System.Drawing.Point(155, 38);
            this.textBoxPort.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxPort.Name = "textBoxPort";
            this.textBoxPort.Size = new System.Drawing.Size(84, 28);
            this.textBoxPort.TabIndex = 2;
            // 
            // labelPort
            // 
            this.labelPort.AutoSize = true;
            this.labelPort.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelPort.Location = new System.Drawing.Point(151, 13);
            this.labelPort.Name = "labelPort";
            this.labelPort.Size = new System.Drawing.Size(115, 21);
            this.labelPort.TabIndex = 0;
            this.labelPort.Text = "Scope TCP port";
            // 
            // panel3
            // 
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.textBoxFilenameFormat);
            this.panel3.Controls.Add(this.label2);
            this.panel3.Controls.Add(this.comboBoxRegion);
            this.panel3.Controls.Add(this.labelRegion);
            this.panel3.Controls.Add(this.textBoxComponent);
            this.panel3.Controls.Add(this.labelComponent);
            this.panel3.Location = new System.Drawing.Point(6, 141);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(352, 137);
            this.panel3.TabIndex = 21;
            // 
            // textBoxFilenameFormat
            // 
            this.textBoxFilenameFormat.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFilenameFormat.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxFilenameFormat.Location = new System.Drawing.Point(9, 90);
            this.textBoxFilenameFormat.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxFilenameFormat.Name = "textBoxFilenameFormat";
            this.textBoxFilenameFormat.Size = new System.Drawing.Size(196, 28);
            this.textBoxFilenameFormat.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(211, 93);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(125, 21);
            this.label2.TabIndex = 0;
            this.label2.Text = "Filename format";
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.buttonCaptureOnce);
            this.panel2.Controls.Add(this.textBoxCapturePin);
            this.panel2.Controls.Add(this.labelCapturePin);
            this.panel2.Location = new System.Drawing.Point(6, 288);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(352, 99);
            this.panel2.TabIndex = 20;
            // 
            // textBoxCapturePin
            // 
            this.textBoxCapturePin.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxCapturePin.Location = new System.Drawing.Point(9, 10);
            this.textBoxCapturePin.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxCapturePin.Name = "textBoxCapturePin";
            this.textBoxCapturePin.Size = new System.Drawing.Size(69, 28);
            this.textBoxCapturePin.TabIndex = 7;
            this.textBoxCapturePin.Text = "1";
            // 
            // labelCapturePin
            // 
            this.labelCapturePin.AutoSize = true;
            this.labelCapturePin.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCapturePin.Location = new System.Drawing.Point(84, 13);
            this.labelCapturePin.Name = "labelCapturePin";
            this.labelCapturePin.Size = new System.Drawing.Size(121, 21);
            this.labelCapturePin.TabIndex = 0;
            this.labelCapturePin.Text = "Capture this pin";
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.buttonCaptureContinuelsy);
            this.panel1.Controls.Add(this.textBoxCapturePinStart);
            this.panel1.Controls.Add(this.labelCapturePinStart);
            this.panel1.Controls.Add(this.textBoxCapturePinEnd);
            this.panel1.Controls.Add(this.labelCapturePinEnd);
            this.panel1.Location = new System.Drawing.Point(6, 402);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(352, 132);
            this.panel1.TabIndex = 19;
            // 
            // tabPageHelp
            // 
            this.tabPageHelp.Controls.Add(this.richTextBoxHelp);
            this.tabPageHelp.Controls.Add(this.label1);
            this.tabPageHelp.Location = new System.Drawing.Point(4, 30);
            this.tabPageHelp.Name = "tabPageHelp";
            this.tabPageHelp.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageHelp.Size = new System.Drawing.Size(897, 689);
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
            this.tabPageAbout.Size = new System.Drawing.Size(897, 689);
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
            this.ClientSize = new System.Drawing.Size(911, 732);
            this.Controls.Add(this.tabControl1);
            this.Font = new System.Drawing.Font("Calibri", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MinimumSize = new System.Drawing.Size(799, 779);
            this.Name = "Form1";
            this.Text = "Oscilloscope Network Capture";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxImage)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPageMeasurements.ResumeLayout(false);
            this.tabPageMeasurements.PerformLayout();
            this.panel4.ResumeLayout(false);
            this.panel4.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
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

        private System.Windows.Forms.Button buttonCaptureOnce;
        private System.Windows.Forms.ComboBox comboBoxRegion;
        private System.Windows.Forms.TextBox textBoxIp;
        private System.Windows.Forms.Button buttonCaptureContinuelsy;
        private System.Windows.Forms.RichTextBox richTextBoxLog;
        private System.Windows.Forms.PictureBox pictureBoxImage;
        private System.Windows.Forms.Label labelIp;
        private System.Windows.Forms.Label labelRegion;
        private System.Windows.Forms.TextBox textBoxCapturePinStart;
        private System.Windows.Forms.Label labelCapturePinStart;
        private System.Windows.Forms.Label labelCapturePinEnd;
        private System.Windows.Forms.TextBox textBoxCapturePinEnd;
        private System.Windows.Forms.RichTextBox richTextBoxAction;
        private System.Windows.Forms.Label labelComponent;
        private System.Windows.Forms.TextBox textBoxComponent;
        private System.Windows.Forms.Label labelAction;
        private System.Windows.Forms.Button buttonCheckScope;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageMeasurements;
        private System.Windows.Forms.TabPage tabPageAbout;
        private System.Windows.Forms.PictureBox pictureBoxIcon;
        private System.Windows.Forms.Label labelProductVersion;
        private System.Windows.Forms.Label labelProductName;
        private System.Windows.Forms.Label labelCapturePin;
        private System.Windows.Forms.TextBox textBoxCapturePin;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button buttonOpenFolder;
        private System.Windows.Forms.TextBox textBoxPort;
        private System.Windows.Forms.Label labelPort;
        private System.Windows.Forms.TextBox textBoxFilenameFormat;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox checkBoxBeep;
        private System.Windows.Forms.TabPage tabPageHelp;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox richTextBoxHelp;
        private System.Windows.Forms.RichTextBox richTextBoxAbout;
    }
}

