namespace M18BatteryInfo
{
    partial class frmMain
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            grpOutput = new GroupBox();
            btnCopyOutput = new Button();
            btnReset = new Button();
            btnHealthReport = new Button();
            btnActive = new Button();
            rtbOutput = new RichTextBox();
            btnIdle = new Button();
            btnDisconnect = new Button();
            btnConnect = new Button();
            btnRefresh = new Button();
            lblSerialPort = new Label();
            comboBox1 = new ComboBox();
            tabAdvanced = new TabPage();
            btnRestoreTxRxState = new Button();
            grpTryCmd = new GroupBox();
            txtTryCmdLength = new TextBox();
            txtAddressLow = new TextBox();
            txtAddressHigh = new TextBox();
            lblTryCmdLength = new Label();
            lblAddressLow = new Label();
            lblAddrHigh = new Label();
            lblCommand = new Label();
            txtCommand = new TextBox();
            grpbxDebugCmd = new GroupBox();
            txtResponseLength = new TextBox();
            txtDebugCmdLength = new TextBox();
            txtAddrLSB = new TextBox();
            lblResponseLength = new Label();
            lblDebugCmdLength = new Label();
            lblLSB = new Label();
            lblMSB = new Label();
            txtAddrMSB = new TextBox();
            btnFullBrute = new Button();
            btnBruteAddr = new Button();
            btnSaveTxRxState = new Button();
            chkboxRxLog = new CheckBox();
            chkbxTXLog = new CheckBox();
            txtWriteMessage = new TextBox();
            btnWriteMessage = new Button();
            btnReadIDLabelled = new Button();
            btnReadIDRaw = new Button();
            btnReadAllSpreadsheet = new Button();
            btnReadAllRegisters = new Button();
            rtbAdvOutput = new RichTextBox();
            tabPage3 = new TabPage();
            comboBox4 = new ComboBox();
            comboBox3 = new ComboBox();
            cmbDuration = new ComboBox();
            tabDiagnostics = new TabPage();
            label6 = new Label();
            label5 = new Label();
            label4 = new Label();
            label3 = new Label();
            label2 = new Label();
            label1 = new Label();
            richTextBox1 = new RichTextBox();
            textBox6 = new TextBox();
            textBox5 = new TextBox();
            textBox4 = new TextBox();
            textBox3 = new TextBox();
            textBox2 = new TextBox();
            textBox1 = new TextBox();
            btnSubmitForm = new Button();
            tabAbout = new TabPage();
            toolTipSimpleTab = new ToolTip(components);
            lblDuration = new Label();
            lblBaudRate = new Label();
            lblSimProfile = new Label();
            btnStartSim = new Button();
            btnStopSim = new Button();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            grpOutput.SuspendLayout();
            tabAdvanced.SuspendLayout();
            grpTryCmd.SuspendLayout();
            grpbxDebugCmd.SuspendLayout();
            tabPage3.SuspendLayout();
            tabDiagnostics.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabAdvanced);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Controls.Add(tabDiagnostics);
            tabControl1.Controls.Add(tabAbout);
            tabControl1.Location = new Point(8, 8);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(908, 484);
            tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(grpOutput);
            tabPage1.Controls.Add(btnDisconnect);
            tabPage1.Controls.Add(btnConnect);
            tabPage1.Controls.Add(btnRefresh);
            tabPage1.Controls.Add(lblSerialPort);
            tabPage1.Controls.Add(comboBox1);
            tabPage1.Location = new Point(4, 34);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(900, 446);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Simple";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // grpOutput
            // 
            grpOutput.Controls.Add(btnCopyOutput);
            grpOutput.Controls.Add(btnReset);
            grpOutput.Controls.Add(btnHealthReport);
            grpOutput.Controls.Add(btnActive);
            grpOutput.Controls.Add(rtbOutput);
            grpOutput.Controls.Add(btnIdle);
            grpOutput.Location = new Point(6, 88);
            grpOutput.Name = "grpOutput";
            grpOutput.Size = new Size(882, 352);
            grpOutput.TabIndex = 6;
            grpOutput.TabStop = false;
            grpOutput.Text = "Output";
            // 
            // btnCopyOutput
            // 
            btnCopyOutput.Location = new Point(728, 144);
            btnCopyOutput.Name = "btnCopyOutput";
            btnCopyOutput.Size = new Size(148, 34);
            btnCopyOutput.TabIndex = 11;
            btnCopyOutput.Text = "Copy Output";
            toolTipSimpleTab.SetToolTip(btnCopyOutput, "Send 0xAA to battery. \r\nReturn true if battery replies wih 0xAA");
            btnCopyOutput.UseVisualStyleBackColor = true;
            // 
            // btnReset
            // 
            btnReset.Location = new Point(728, 304);
            btnReset.Name = "btnReset";
            btnReset.Size = new Size(148, 34);
            btnReset.TabIndex = 10;
            btnReset.Text = "Reset";
            toolTipSimpleTab.SetToolTip(btnReset, "Send 0xAA to battery. \r\nReturn true if battery replies wih 0xAA");
            btnReset.UseVisualStyleBackColor = true;
            // 
            // btnHealthReport
            // 
            btnHealthReport.Location = new Point(728, 104);
            btnHealthReport.Name = "btnHealthReport";
            btnHealthReport.Size = new Size(148, 34);
            btnHealthReport.TabIndex = 9;
            btnHealthReport.Text = "Health Report";
            toolTipSimpleTab.SetToolTip(btnHealthReport, "Print simple health report on the connected battery.");
            btnHealthReport.UseVisualStyleBackColor = true;
            // 
            // btnActive
            // 
            btnActive.Location = new Point(728, 64);
            btnActive.Name = "btnActive";
            btnActive.Size = new Size(148, 34);
            btnActive.TabIndex = 8;
            btnActive.Text = "Active (Tx High)";
            toolTipSimpleTab.SetToolTip(btnActive, "Charger simulation active.\r\nMay increase battery charge counter.\r\nNot safe to connect or disconnect battery during this state.");
            btnActive.UseVisualStyleBackColor = true;
            // 
            // rtbOutput
            // 
            rtbOutput.Location = new Point(6, 30);
            rtbOutput.Name = "rtbOutput";
            rtbOutput.Size = new Size(714, 314);
            rtbOutput.TabIndex = 0;
            rtbOutput.Text = "Step 1: Set Idle.\nStep 2: Connect Battery.\nStep 3: Set Active.";
            // 
            // btnIdle
            // 
            btnIdle.Location = new Point(728, 24);
            btnIdle.Name = "btnIdle";
            btnIdle.Size = new Size(148, 34);
            btnIdle.TabIndex = 7;
            btnIdle.Text = "Idle (Tx Low)";
            toolTipSimpleTab.SetToolTip(btnIdle, "Does not increase charge count.\r\nSafe to connect or disconnect battery.");
            btnIdle.UseVisualStyleBackColor = true;
            // 
            // btnDisconnect
            // 
            btnDisconnect.Location = new Point(344, 48);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(112, 34);
            btnDisconnect.TabIndex = 5;
            btnDisconnect.Text = "Disconnect";
            btnDisconnect.UseVisualStyleBackColor = true;
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(224, 48);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(112, 34);
            btnConnect.TabIndex = 4;
            btnConnect.Text = "Connect";
            toolTipSimpleTab.SetToolTip(btnConnect, "Connect to the selected serial port.");
            btnConnect.UseVisualStyleBackColor = true;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(104, 48);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(112, 34);
            btnRefresh.TabIndex = 3;
            btnRefresh.Text = "Refresh";
            toolTipSimpleTab.SetToolTip(btnRefresh, "Refresh the serial port list.");
            btnRefresh.UseVisualStyleBackColor = true;
            // 
            // lblSerialPort
            // 
            lblSerialPort.AutoSize = true;
            lblSerialPort.Location = new Point(6, 14);
            lblSerialPort.Name = "lblSerialPort";
            lblSerialPort.Size = new Size(95, 25);
            lblSerialPort.TabIndex = 2;
            lblSerialPort.Text = "Serial Port:";
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(107, 11);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(781, 33);
            comboBox1.TabIndex = 1;
            // 
            // tabAdvanced
            // 
            tabAdvanced.Controls.Add(btnRestoreTxRxState);
            tabAdvanced.Controls.Add(grpTryCmd);
            tabAdvanced.Controls.Add(grpbxDebugCmd);
            tabAdvanced.Controls.Add(btnFullBrute);
            tabAdvanced.Controls.Add(btnBruteAddr);
            tabAdvanced.Controls.Add(btnSaveTxRxState);
            tabAdvanced.Controls.Add(chkboxRxLog);
            tabAdvanced.Controls.Add(chkbxTXLog);
            tabAdvanced.Controls.Add(txtWriteMessage);
            tabAdvanced.Controls.Add(btnWriteMessage);
            tabAdvanced.Controls.Add(btnReadIDLabelled);
            tabAdvanced.Controls.Add(btnReadIDRaw);
            tabAdvanced.Controls.Add(btnReadAllSpreadsheet);
            tabAdvanced.Controls.Add(btnReadAllRegisters);
            tabAdvanced.Controls.Add(rtbAdvOutput);
            tabAdvanced.Location = new Point(4, 34);
            tabAdvanced.Name = "tabAdvanced";
            tabAdvanced.Padding = new Padding(3);
            tabAdvanced.Size = new Size(900, 446);
            tabAdvanced.TabIndex = 1;
            tabAdvanced.Text = "Advanced";
            tabAdvanced.UseVisualStyleBackColor = true;
            // 
            // btnRestoreTxRxState
            // 
            btnRestoreTxRxState.Location = new Point(8, 232);
            btnRestoreTxRxState.Name = "btnRestoreTxRxState";
            btnRestoreTxRxState.Size = new Size(208, 34);
            btnRestoreTxRxState.TabIndex = 15;
            btnRestoreTxRxState.Text = "Restore TxRx State";
            btnRestoreTxRxState.UseVisualStyleBackColor = true;
            // 
            // grpTryCmd
            // 
            grpTryCmd.Controls.Add(txtTryCmdLength);
            grpTryCmd.Controls.Add(txtAddressLow);
            grpTryCmd.Controls.Add(txtAddressHigh);
            grpTryCmd.Controls.Add(lblTryCmdLength);
            grpTryCmd.Controls.Add(lblAddressLow);
            grpTryCmd.Controls.Add(lblAddrHigh);
            grpTryCmd.Controls.Add(lblCommand);
            grpTryCmd.Controls.Add(txtCommand);
            grpTryCmd.Location = new Point(560, 8);
            grpTryCmd.Name = "grpTryCmd";
            grpTryCmd.Size = new Size(328, 184);
            grpTryCmd.TabIndex = 14;
            grpTryCmd.TabStop = false;
            grpTryCmd.Text = "Try Command";
            // 
            // txtTryCmdLength
            // 
            txtTryCmdLength.Location = new Point(160, 144);
            txtTryCmdLength.Name = "txtTryCmdLength";
            txtTryCmdLength.Size = new Size(160, 31);
            txtTryCmdLength.TabIndex = 14;
            // 
            // txtAddressLow
            // 
            txtAddressLow.Location = new Point(160, 104);
            txtAddressLow.Name = "txtAddressLow";
            txtAddressLow.Size = new Size(160, 31);
            txtAddressLow.TabIndex = 13;
            // 
            // txtAddressHigh
            // 
            txtAddressHigh.Location = new Point(160, 64);
            txtAddressHigh.Name = "txtAddressHigh";
            txtAddressHigh.Size = new Size(160, 31);
            txtAddressHigh.TabIndex = 12;
            // 
            // lblTryCmdLength
            // 
            lblTryCmdLength.AutoSize = true;
            lblTryCmdLength.Location = new Point(8, 152);
            lblTryCmdLength.Name = "lblTryCmdLength";
            lblTryCmdLength.Size = new Size(66, 25);
            lblTryCmdLength.TabIndex = 11;
            lblTryCmdLength.Text = "Length";
            // 
            // lblAddressLow
            // 
            lblAddressLow.AutoSize = true;
            lblAddressLow.Location = new Point(8, 112);
            lblAddressLow.Name = "lblAddressLow";
            lblAddressLow.Size = new Size(114, 25);
            lblAddressLow.TabIndex = 10;
            lblAddressLow.Text = "Address Low";
            // 
            // lblAddrHigh
            // 
            lblAddrHigh.AutoSize = true;
            lblAddrHigh.Location = new Point(8, 72);
            lblAddrHigh.Name = "lblAddrHigh";
            lblAddrHigh.Size = new Size(120, 25);
            lblAddrHigh.TabIndex = 9;
            lblAddrHigh.Text = "Address High";
            // 
            // lblCommand
            // 
            lblCommand.AutoSize = true;
            lblCommand.Location = new Point(8, 32);
            lblCommand.Name = "lblCommand";
            lblCommand.Size = new Size(96, 25);
            lblCommand.TabIndex = 8;
            lblCommand.Text = "Command";
            // 
            // txtCommand
            // 
            txtCommand.Location = new Point(160, 24);
            txtCommand.Name = "txtCommand";
            txtCommand.Size = new Size(160, 31);
            txtCommand.TabIndex = 7;
            // 
            // grpbxDebugCmd
            // 
            grpbxDebugCmd.Controls.Add(txtResponseLength);
            grpbxDebugCmd.Controls.Add(txtDebugCmdLength);
            grpbxDebugCmd.Controls.Add(txtAddrLSB);
            grpbxDebugCmd.Controls.Add(lblResponseLength);
            grpbxDebugCmd.Controls.Add(lblDebugCmdLength);
            grpbxDebugCmd.Controls.Add(lblLSB);
            grpbxDebugCmd.Controls.Add(lblMSB);
            grpbxDebugCmd.Controls.Add(txtAddrMSB);
            grpbxDebugCmd.Location = new Point(224, 8);
            grpbxDebugCmd.Name = "grpbxDebugCmd";
            grpbxDebugCmd.Size = new Size(328, 184);
            grpbxDebugCmd.TabIndex = 13;
            grpbxDebugCmd.TabStop = false;
            grpbxDebugCmd.Text = "Debug Command";
            // 
            // txtResponseLength
            // 
            txtResponseLength.Location = new Point(160, 144);
            txtResponseLength.Name = "txtResponseLength";
            txtResponseLength.Size = new Size(160, 31);
            txtResponseLength.TabIndex = 14;
            // 
            // txtDebugCmdLength
            // 
            txtDebugCmdLength.Location = new Point(160, 104);
            txtDebugCmdLength.Name = "txtDebugCmdLength";
            txtDebugCmdLength.Size = new Size(160, 31);
            txtDebugCmdLength.TabIndex = 13;
            // 
            // txtAddrLSB
            // 
            txtAddrLSB.Location = new Point(160, 64);
            txtAddrLSB.Name = "txtAddrLSB";
            txtAddrLSB.Size = new Size(160, 31);
            txtAddrLSB.TabIndex = 12;
            // 
            // lblResponseLength
            // 
            lblResponseLength.AutoSize = true;
            lblResponseLength.Location = new Point(8, 152);
            lblResponseLength.Name = "lblResponseLength";
            lblResponseLength.Size = new Size(147, 25);
            lblResponseLength.TabIndex = 11;
            lblResponseLength.Text = "Response Length";
            lblResponseLength.Click += lblResponseLength_Click;
            // 
            // lblDebugCmdLength
            // 
            lblDebugCmdLength.AutoSize = true;
            lblDebugCmdLength.Location = new Point(8, 112);
            lblDebugCmdLength.Name = "lblDebugCmdLength";
            lblDebugCmdLength.Size = new Size(66, 25);
            lblDebugCmdLength.TabIndex = 10;
            lblDebugCmdLength.Text = "Length";
            // 
            // lblLSB
            // 
            lblLSB.AutoSize = true;
            lblLSB.Location = new Point(8, 72);
            lblLSB.Name = "lblLSB";
            lblLSB.Size = new Size(110, 25);
            lblLSB.TabIndex = 9;
            lblLSB.Text = "LSB Address";
            // 
            // lblMSB
            // 
            lblMSB.AutoSize = true;
            lblMSB.Location = new Point(8, 32);
            lblMSB.Name = "lblMSB";
            lblMSB.Size = new Size(118, 25);
            lblMSB.TabIndex = 8;
            lblMSB.Text = "MSB Address";
            // 
            // txtAddrMSB
            // 
            txtAddrMSB.Location = new Point(160, 24);
            txtAddrMSB.Name = "txtAddrMSB";
            txtAddrMSB.Size = new Size(160, 31);
            txtAddrMSB.TabIndex = 7;
            // 
            // btnFullBrute
            // 
            btnFullBrute.Location = new Point(8, 320);
            btnFullBrute.Name = "btnFullBrute";
            btnFullBrute.Size = new Size(208, 34);
            btnFullBrute.TabIndex = 12;
            btnFullBrute.Text = "Brute Force All";
            btnFullBrute.UseVisualStyleBackColor = true;
            // 
            // btnBruteAddr
            // 
            btnBruteAddr.Location = new Point(8, 280);
            btnBruteAddr.Name = "btnBruteAddr";
            btnBruteAddr.Size = new Size(208, 34);
            btnBruteAddr.TabIndex = 11;
            btnBruteAddr.Text = "Brute Force Addresses";
            btnBruteAddr.UseVisualStyleBackColor = true;
            // 
            // btnSaveTxRxState
            // 
            btnSaveTxRxState.Location = new Point(8, 192);
            btnSaveTxRxState.Name = "btnSaveTxRxState";
            btnSaveTxRxState.Size = new Size(208, 34);
            btnSaveTxRxState.TabIndex = 10;
            btnSaveTxRxState.Text = "Save && Set TxRx State";
            btnSaveTxRxState.UseVisualStyleBackColor = true;
            // 
            // chkboxRxLog
            // 
            chkboxRxLog.AutoSize = true;
            chkboxRxLog.Location = new Point(368, 376);
            chkboxRxLog.Name = "chkboxRxLog";
            chkboxRxLog.Size = new Size(149, 29);
            chkboxRxLog.TabIndex = 8;
            chkboxRxLog.Text = "Enable Rx Log";
            chkboxRxLog.UseVisualStyleBackColor = true;
            // 
            // chkbxTXLog
            // 
            chkbxTXLog.AutoSize = true;
            chkbxTXLog.Location = new Point(224, 376);
            chkbxTXLog.Name = "chkbxTXLog";
            chkbxTXLog.Size = new Size(150, 29);
            chkbxTXLog.TabIndex = 7;
            chkbxTXLog.Text = "Enable TX Log";
            chkbxTXLog.UseVisualStyleBackColor = true;
            // 
            // txtWriteMessage
            // 
            txtWriteMessage.Location = new Point(224, 408);
            txtWriteMessage.Name = "txtWriteMessage";
            txtWriteMessage.Size = new Size(664, 31);
            txtWriteMessage.TabIndex = 6;
            txtWriteMessage.Text = "Write ASCII To Register. 20 Characters Max.";
            // 
            // btnWriteMessage
            // 
            btnWriteMessage.Location = new Point(8, 408);
            btnWriteMessage.Name = "btnWriteMessage";
            btnWriteMessage.Size = new Size(208, 34);
            btnWriteMessage.TabIndex = 5;
            btnWriteMessage.Text = "Write Message";
            btnWriteMessage.UseVisualStyleBackColor = true;
            // 
            // btnReadIDLabelled
            // 
            btnReadIDLabelled.Location = new Point(8, 144);
            btnReadIDLabelled.Name = "btnReadIDLabelled";
            btnReadIDLabelled.Size = new Size(208, 34);
            btnReadIDLabelled.TabIndex = 4;
            btnReadIDLabelled.Text = "Read ID (Labelled)";
            btnReadIDLabelled.UseVisualStyleBackColor = true;
            // 
            // btnReadIDRaw
            // 
            btnReadIDRaw.Location = new Point(8, 104);
            btnReadIDRaw.Name = "btnReadIDRaw";
            btnReadIDRaw.Size = new Size(208, 34);
            btnReadIDRaw.TabIndex = 3;
            btnReadIDRaw.Text = "Read ID (Raw)";
            btnReadIDRaw.UseVisualStyleBackColor = true;
            // 
            // btnReadAllSpreadsheet
            // 
            btnReadAllSpreadsheet.Location = new Point(8, 48);
            btnReadAllSpreadsheet.Name = "btnReadAllSpreadsheet";
            btnReadAllSpreadsheet.Size = new Size(208, 34);
            btnReadAllSpreadsheet.TabIndex = 2;
            btnReadAllSpreadsheet.Text = "Read All Spreadsheet";
            btnReadAllSpreadsheet.UseVisualStyleBackColor = true;
            // 
            // btnReadAllRegisters
            // 
            btnReadAllRegisters.Location = new Point(8, 8);
            btnReadAllRegisters.Name = "btnReadAllRegisters";
            btnReadAllRegisters.Size = new Size(208, 34);
            btnReadAllRegisters.TabIndex = 1;
            btnReadAllRegisters.Text = "Read All Registers";
            btnReadAllRegisters.UseVisualStyleBackColor = true;
            // 
            // rtbAdvOutput
            // 
            rtbAdvOutput.Location = new Point(224, 200);
            rtbAdvOutput.Name = "rtbAdvOutput";
            rtbAdvOutput.Size = new Size(664, 176);
            rtbAdvOutput.TabIndex = 0;
            rtbAdvOutput.Text = "";
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(btnStopSim);
            tabPage3.Controls.Add(btnStartSim);
            tabPage3.Controls.Add(lblSimProfile);
            tabPage3.Controls.Add(lblBaudRate);
            tabPage3.Controls.Add(lblDuration);
            tabPage3.Controls.Add(comboBox4);
            tabPage3.Controls.Add(comboBox3);
            tabPage3.Controls.Add(cmbDuration);
            tabPage3.Location = new Point(4, 34);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(900, 446);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Simulation";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // comboBox4
            // 
            comboBox4.FormattingEnabled = true;
            comboBox4.Location = new Point(152, 88);
            comboBox4.Name = "comboBox4";
            comboBox4.Size = new Size(232, 33);
            comboBox4.TabIndex = 2;
            // 
            // comboBox3
            // 
            comboBox3.FormattingEnabled = true;
            comboBox3.Location = new Point(152, 48);
            comboBox3.Name = "comboBox3";
            comboBox3.Size = new Size(232, 33);
            comboBox3.TabIndex = 1;
            // 
            // cmbDuration
            // 
            cmbDuration.FormattingEnabled = true;
            cmbDuration.Location = new Point(152, 8);
            cmbDuration.Name = "cmbDuration";
            cmbDuration.Size = new Size(232, 33);
            cmbDuration.TabIndex = 0;
            // 
            // tabDiagnostics
            // 
            tabDiagnostics.Controls.Add(label6);
            tabDiagnostics.Controls.Add(label5);
            tabDiagnostics.Controls.Add(label4);
            tabDiagnostics.Controls.Add(label3);
            tabDiagnostics.Controls.Add(label2);
            tabDiagnostics.Controls.Add(label1);
            tabDiagnostics.Controls.Add(richTextBox1);
            tabDiagnostics.Controls.Add(textBox6);
            tabDiagnostics.Controls.Add(textBox5);
            tabDiagnostics.Controls.Add(textBox4);
            tabDiagnostics.Controls.Add(textBox3);
            tabDiagnostics.Controls.Add(textBox2);
            tabDiagnostics.Controls.Add(textBox1);
            tabDiagnostics.Controls.Add(btnSubmitForm);
            tabDiagnostics.Location = new Point(4, 34);
            tabDiagnostics.Name = "tabDiagnostics";
            tabDiagnostics.Padding = new Padding(3);
            tabDiagnostics.Size = new Size(900, 446);
            tabDiagnostics.TabIndex = 3;
            tabDiagnostics.Text = "Submit Diagnostics";
            tabDiagnostics.UseVisualStyleBackColor = true;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(24, 208);
            label6.Name = "label6";
            label6.Size = new Size(59, 25);
            label6.TabIndex = 22;
            label6.Text = "label6";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(24, 168);
            label5.Name = "label5";
            label5.Size = new Size(59, 25);
            label5.TabIndex = 21;
            label5.Text = "label5";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(24, 128);
            label4.Name = "label4";
            label4.Size = new Size(59, 25);
            label4.TabIndex = 20;
            label4.Text = "label4";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(24, 88);
            label3.Name = "label3";
            label3.Size = new Size(59, 25);
            label3.TabIndex = 19;
            label3.Text = "label3";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(24, 48);
            label2.Name = "label2";
            label2.Size = new Size(59, 25);
            label2.TabIndex = 18;
            label2.Text = "label2";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(24, 16);
            label1.Name = "label1";
            label1.Size = new Size(59, 25);
            label1.TabIndex = 17;
            label1.Text = "label1";
            // 
            // richTextBox1
            // 
            richTextBox1.Location = new Point(8, 296);
            richTextBox1.Name = "richTextBox1";
            richTextBox1.Size = new Size(248, 144);
            richTextBox1.TabIndex = 16;
            richTextBox1.Text = "";
            // 
            // textBox6
            // 
            textBox6.Location = new Point(96, 48);
            textBox6.Name = "textBox6";
            textBox6.Size = new Size(150, 31);
            textBox6.TabIndex = 15;
            // 
            // textBox5
            // 
            textBox5.Location = new Point(96, 88);
            textBox5.Name = "textBox5";
            textBox5.Size = new Size(150, 31);
            textBox5.TabIndex = 14;
            // 
            // textBox4
            // 
            textBox4.Location = new Point(96, 128);
            textBox4.Name = "textBox4";
            textBox4.Size = new Size(150, 31);
            textBox4.TabIndex = 13;
            // 
            // textBox3
            // 
            textBox3.Location = new Point(96, 208);
            textBox3.Name = "textBox3";
            textBox3.Size = new Size(150, 31);
            textBox3.TabIndex = 12;
            // 
            // textBox2
            // 
            textBox2.Location = new Point(96, 168);
            textBox2.Name = "textBox2";
            textBox2.Size = new Size(150, 31);
            textBox2.TabIndex = 11;
            // 
            // textBox1
            // 
            textBox1.Location = new Point(96, 8);
            textBox1.Name = "textBox1";
            textBox1.Size = new Size(150, 31);
            textBox1.TabIndex = 10;
            // 
            // btnSubmitForm
            // 
            btnSubmitForm.Location = new Point(696, 400);
            btnSubmitForm.Name = "btnSubmitForm";
            btnSubmitForm.Size = new Size(192, 34);
            btnSubmitForm.TabIndex = 9;
            btnSubmitForm.Text = "Submit Form";
            btnSubmitForm.UseVisualStyleBackColor = true;
            // 
            // tabAbout
            // 
            tabAbout.Location = new Point(4, 34);
            tabAbout.Name = "tabAbout";
            tabAbout.Padding = new Padding(3);
            tabAbout.Size = new Size(900, 446);
            tabAbout.TabIndex = 4;
            tabAbout.Text = "About";
            tabAbout.UseVisualStyleBackColor = true;
            // 
            // lblDuration
            // 
            lblDuration.AutoSize = true;
            lblDuration.Location = new Point(8, 8);
            lblDuration.Name = "lblDuration";
            lblDuration.Size = new Size(81, 25);
            lblDuration.TabIndex = 3;
            lblDuration.Text = "Duration";
            // 
            // lblBaudRate
            // 
            lblBaudRate.AutoSize = true;
            lblBaudRate.Location = new Point(8, 48);
            lblBaudRate.Name = "lblBaudRate";
            lblBaudRate.Size = new Size(92, 25);
            lblBaudRate.TabIndex = 4;
            lblBaudRate.Text = "Baud Rate";
            // 
            // lblSimProfile
            // 
            lblSimProfile.AutoSize = true;
            lblSimProfile.Location = new Point(8, 88);
            lblSimProfile.Name = "lblSimProfile";
            lblSimProfile.Size = new Size(129, 25);
            lblSimProfile.TabIndex = 5;
            lblSimProfile.Text = "Charger Profile";
            // 
            // btnStartSim
            // 
            btnStartSim.Location = new Point(152, 128);
            btnStartSim.Name = "btnStartSim";
            btnStartSim.Size = new Size(112, 34);
            btnStartSim.TabIndex = 1;
            btnStartSim.Text = "Start";
            btnStartSim.UseVisualStyleBackColor = true;
            // 
            // btnStopSim
            // 
            btnStopSim.Location = new Point(272, 128);
            btnStopSim.Name = "btnStopSim";
            btnStopSim.Size = new Size(112, 34);
            btnStopSim.TabIndex = 6;
            btnStopSim.Text = "Stop";
            btnStopSim.UseVisualStyleBackColor = true;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(928, 499);
            Controls.Add(tabControl1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "frmMain";
            ShowIcon = false;
            Text = "M18 Pack Analyzer";
            Load += frmMain_Load;
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage1.PerformLayout();
            grpOutput.ResumeLayout(false);
            tabAdvanced.ResumeLayout(false);
            tabAdvanced.PerformLayout();
            grpTryCmd.ResumeLayout(false);
            grpTryCmd.PerformLayout();
            grpbxDebugCmd.ResumeLayout(false);
            grpbxDebugCmd.PerformLayout();
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            tabDiagnostics.ResumeLayout(false);
            tabDiagnostics.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabAdvanced;
        private ComboBox comboBox1;
        private RichTextBox rtbOutput;
        private TabPage tabPage3;
        private TabPage tabDiagnostics;
        private Button btnConnect;
        private Button btnRefresh;
        private Label lblSerialPort;
        private GroupBox grpOutput;
        private Button btnDisconnect;
        private Button btnIdle;
        private Button btnActive;
        private ToolTip toolTipSimpleTab;
        private Button btnHealthReport;
        private Button btnReset;
        private Button btnCopyOutput;
        private Button btnReadAllSpreadsheet;
        private Button btnReadAllRegisters;
        private RichTextBox rtbAdvOutput;
        private TextBox txtWriteMessage;
        private Button btnWriteMessage;
        private Button btnReadIDLabelled;
        private Button btnReadIDRaw;
        private TabPage tabAbout;
        private CheckBox chkboxRxLog;
        private CheckBox chkbxTXLog;
        private Button btnSaveTxRxState;
        private Button btnBruteAddr;
        private Button btnFullBrute;
        private GroupBox grpbxDebugCmd;
        private TextBox txtAddrMSB;
        private Label lblResponseLength;
        private Label lblDebugCmdLength;
        private Label lblLSB;
        private Label lblMSB;
        private TextBox txtResponseLength;
        private TextBox txtDebugCmdLength;
        private TextBox txtAddrLSB;
        private GroupBox grpTryCmd;
        private TextBox txtTryCmdLength;
        private TextBox txtAddressLow;
        private TextBox txtAddressHigh;
        private Label lblTryCmdLength;
        private Label lblAddressLow;
        private Label lblAddrHigh;
        private Label lblCommand;
        private TextBox txtCommand;
        private ComboBox comboBox4;
        private ComboBox comboBox3;
        private ComboBox cmbDuration;
        private Button btnRestoreTxRxState;
        private Button btnSubmitForm;
        private Label label6;
        private Label label5;
        private Label label4;
        private Label label3;
        private Label label2;
        private Label label1;
        private RichTextBox richTextBox1;
        private TextBox textBox6;
        private TextBox textBox5;
        private TextBox textBox4;
        private TextBox textBox3;
        private TextBox textBox2;
        private TextBox textBox1;
        private Button btnStartSim;
        private Label lblSimProfile;
        private Label lblBaudRate;
        private Label lblDuration;
        private Button btnStopSim;
    }
}
