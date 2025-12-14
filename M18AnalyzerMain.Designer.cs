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
            tabControlM18Main = new TabControl();
            tabSimple = new TabPage();
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
            cmbBxSerialPort = new ComboBox();
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
            tabSimulation = new TabPage();
            grpBxSimCustomProfile = new GroupBox();
            lblCutoffRaw = new Label();
            lblMaxCurrAmps = new Label();
            txtMaxCurrAmps = new TextBox();
            lblMaxCurrRaw = new Label();
            txtCutoffAmps = new TextBox();
            lblCutoffAmps = new Label();
            txtMaxCurrRaw = new TextBox();
            txtCutoffRaw = new TextBox();
            txtSimDuration = new TextBox();
            btnStopSim = new Button();
            btnStartSim = new Button();
            lblSimProfile = new Label();
            lblBaudRate = new Label();
            lblDuration = new Label();
            cmbBxChgProfile = new ComboBox();
            cmbBxBaudRate = new ComboBox();
            tabDiagnostics = new TabPage();
            btnClearDiagForm = new Button();
            grpboxDiagOutput = new GroupBox();
            rtbSubmitDiagReadOnly = new RichTextBox();
            lblType = new Label();
            lblStickerInfo = new Label();
            lblToolSerialNum = new Label();
            lblSubmitDiagDate = new Label();
            lblOneKeyID = new Label();
            txtSubmitDiagDate = new TextBox();
            txtSubmitDiagSerial = new TextBox();
            txtSubmitDiagSticker = new TextBox();
            txtSubmitDiagType = new TextBox();
            txtOneKeyID = new TextBox();
            btnSubmitDiagForm = new Button();
            tabAbout = new TabPage();
            linkLabelKillaVolt = new LinkLabel();
            lblKillaVoltAbout = new Label();
            linkLabelMartin = new LinkLabel();
            lblMartin = new Label();
            lblAboutTitle = new Label();
            toolTipSimpleTab = new ToolTip(components);
            rtbDebugOutput = new RichTextBox();
            btnTestFT232 = new Button();
            tabControlM18Main.SuspendLayout();
            tabSimple.SuspendLayout();
            grpOutput.SuspendLayout();
            tabAdvanced.SuspendLayout();
            grpTryCmd.SuspendLayout();
            grpbxDebugCmd.SuspendLayout();
            tabSimulation.SuspendLayout();
            grpBxSimCustomProfile.SuspendLayout();
            tabDiagnostics.SuspendLayout();
            grpboxDiagOutput.SuspendLayout();
            tabAbout.SuspendLayout();
            SuspendLayout();
            // 
            // tabControlM18Main
            // 
            tabControlM18Main.Controls.Add(tabSimple);
            tabControlM18Main.Controls.Add(tabAdvanced);
            tabControlM18Main.Controls.Add(tabSimulation);
            tabControlM18Main.Controls.Add(tabDiagnostics);
            tabControlM18Main.Controls.Add(tabAbout);
            tabControlM18Main.Location = new Point(8, 8);
            tabControlM18Main.Name = "tabControlM18Main";
            tabControlM18Main.SelectedIndex = 0;
            tabControlM18Main.Size = new Size(908, 512);
            tabControlM18Main.TabIndex = 0;
            // 
            // tabSimple
            // 
            tabSimple.Controls.Add(btnTestFT232);
            tabSimple.Controls.Add(grpOutput);
            tabSimple.Controls.Add(btnDisconnect);
            tabSimple.Controls.Add(btnConnect);
            tabSimple.Controls.Add(btnRefresh);
            tabSimple.Controls.Add(lblSerialPort);
            tabSimple.Controls.Add(cmbBxSerialPort);
            tabSimple.Location = new Point(4, 34);
            tabSimple.Name = "tabSimple";
            tabSimple.Padding = new Padding(3);
            tabSimple.Size = new Size(900, 474);
            tabSimple.TabIndex = 0;
            tabSimple.Text = "Simple";
            tabSimple.UseVisualStyleBackColor = true;
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
            rtbOutput.BackColor = Color.Black;
            rtbOutput.ForeColor = Color.Lime;
            rtbOutput.Location = new Point(6, 30);
            rtbOutput.Name = "rtbOutput";
            rtbOutput.Size = new Size(714, 314);
            rtbOutput.TabIndex = 0;
            rtbOutput.Text = resources.GetString("rtbOutput.Text");
            rtbOutput.TextChanged += rtbOutput_TextChanged;
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
            btnDisconnect.Location = new Point(464, 48);
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
            // cmbBxSerialPort
            // 
            cmbBxSerialPort.FormattingEnabled = true;
            cmbBxSerialPort.Location = new Point(107, 11);
            cmbBxSerialPort.Name = "cmbBxSerialPort";
            cmbBxSerialPort.Size = new Size(781, 33);
            cmbBxSerialPort.TabIndex = 1;
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
            tabAdvanced.Size = new Size(900, 474);
            tabAdvanced.TabIndex = 1;
            tabAdvanced.Text = "Advanced";
            tabAdvanced.UseVisualStyleBackColor = true;
            // 
            // btnRestoreTxRxState
            // 
            btnRestoreTxRxState.Location = new Point(8, 248);
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
            lblResponseLength.Location = new Point(8, 144);
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
            btnFullBrute.Location = new Point(8, 352);
            btnFullBrute.Name = "btnFullBrute";
            btnFullBrute.Size = new Size(208, 34);
            btnFullBrute.TabIndex = 12;
            btnFullBrute.Text = "Brute Force All";
            btnFullBrute.UseVisualStyleBackColor = true;
            // 
            // btnBruteAddr
            // 
            btnBruteAddr.Location = new Point(8, 312);
            btnBruteAddr.Name = "btnBruteAddr";
            btnBruteAddr.Size = new Size(208, 34);
            btnBruteAddr.TabIndex = 11;
            btnBruteAddr.Text = "Brute Force Addresses";
            btnBruteAddr.UseVisualStyleBackColor = true;
            // 
            // btnSaveTxRxState
            // 
            btnSaveTxRxState.Location = new Point(8, 208);
            btnSaveTxRxState.Name = "btnSaveTxRxState";
            btnSaveTxRxState.Size = new Size(208, 34);
            btnSaveTxRxState.TabIndex = 10;
            btnSaveTxRxState.Text = "Save && Set TxRx State";
            btnSaveTxRxState.UseVisualStyleBackColor = true;
            // 
            // chkboxRxLog
            // 
            chkboxRxLog.AutoSize = true;
            chkboxRxLog.Checked = true;
            chkboxRxLog.CheckState = CheckState.Checked;
            chkboxRxLog.Location = new Point(736, 376);
            chkboxRxLog.Name = "chkboxRxLog";
            chkboxRxLog.Size = new Size(149, 29);
            chkboxRxLog.TabIndex = 8;
            chkboxRxLog.Text = "Enable Rx Log";
            chkboxRxLog.UseVisualStyleBackColor = true;
            // 
            // chkbxTXLog
            // 
            chkbxTXLog.AutoSize = true;
            chkbxTXLog.Checked = true;
            chkbxTXLog.CheckState = CheckState.Checked;
            chkbxTXLog.Location = new Point(576, 376);
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
            btnReadAllSpreadsheet.Location = new Point(8, 64);
            btnReadAllSpreadsheet.Name = "btnReadAllSpreadsheet";
            btnReadAllSpreadsheet.Size = new Size(208, 34);
            btnReadAllSpreadsheet.TabIndex = 2;
            btnReadAllSpreadsheet.Text = "Read All Spreadsheet";
            btnReadAllSpreadsheet.UseVisualStyleBackColor = true;
            // 
            // btnReadAllRegisters
            // 
            btnReadAllRegisters.Location = new Point(8, 24);
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
            // tabSimulation
            // 
            tabSimulation.Controls.Add(grpBxSimCustomProfile);
            tabSimulation.Controls.Add(txtSimDuration);
            tabSimulation.Controls.Add(btnStopSim);
            tabSimulation.Controls.Add(btnStartSim);
            tabSimulation.Controls.Add(lblSimProfile);
            tabSimulation.Controls.Add(lblBaudRate);
            tabSimulation.Controls.Add(lblDuration);
            tabSimulation.Controls.Add(cmbBxChgProfile);
            tabSimulation.Controls.Add(cmbBxBaudRate);
            tabSimulation.Location = new Point(4, 34);
            tabSimulation.Name = "tabSimulation";
            tabSimulation.Padding = new Padding(3);
            tabSimulation.Size = new Size(900, 474);
            tabSimulation.TabIndex = 2;
            tabSimulation.Text = "Simulation";
            tabSimulation.UseVisualStyleBackColor = true;
            // 
            // grpBxSimCustomProfile
            // 
            grpBxSimCustomProfile.Controls.Add(lblCutoffRaw);
            grpBxSimCustomProfile.Controls.Add(lblMaxCurrAmps);
            grpBxSimCustomProfile.Controls.Add(txtMaxCurrAmps);
            grpBxSimCustomProfile.Controls.Add(lblMaxCurrRaw);
            grpBxSimCustomProfile.Controls.Add(txtCutoffAmps);
            grpBxSimCustomProfile.Controls.Add(lblCutoffAmps);
            grpBxSimCustomProfile.Controls.Add(txtMaxCurrRaw);
            grpBxSimCustomProfile.Controls.Add(txtCutoffRaw);
            grpBxSimCustomProfile.Location = new Point(296, 8);
            grpBxSimCustomProfile.Name = "grpBxSimCustomProfile";
            grpBxSimCustomProfile.Size = new Size(336, 200);
            grpBxSimCustomProfile.TabIndex = 16;
            grpBxSimCustomProfile.TabStop = false;
            grpBxSimCustomProfile.Text = "Custom Charger Profile";
            grpBxSimCustomProfile.Enter += grpBxSimCustomProfile_Enter;
            // 
            // lblCutoffRaw
            // 
            lblCutoffRaw.AutoSize = true;
            lblCutoffRaw.Location = new Point(8, 32);
            lblCutoffRaw.Name = "lblCutoffRaw";
            lblCutoffRaw.Size = new Size(110, 25);
            lblCutoffRaw.TabIndex = 12;
            lblCutoffRaw.Text = "Cutoff (Raw)";
            // 
            // lblMaxCurrAmps
            // 
            lblMaxCurrAmps.AutoSize = true;
            lblMaxCurrAmps.Location = new Point(8, 152);
            lblMaxCurrAmps.Name = "lblMaxCurrAmps";
            lblMaxCurrAmps.Size = new Size(174, 25);
            lblMaxCurrAmps.TabIndex = 15;
            lblMaxCurrAmps.Text = "Max. Current (Amps)";
            // 
            // txtMaxCurrAmps
            // 
            txtMaxCurrAmps.Location = new Point(184, 152);
            txtMaxCurrAmps.Name = "txtMaxCurrAmps";
            txtMaxCurrAmps.Size = new Size(134, 31);
            txtMaxCurrAmps.TabIndex = 8;
            // 
            // lblMaxCurrRaw
            // 
            lblMaxCurrRaw.AutoSize = true;
            lblMaxCurrRaw.Location = new Point(8, 112);
            lblMaxCurrRaw.Name = "lblMaxCurrRaw";
            lblMaxCurrRaw.Size = new Size(160, 25);
            lblMaxCurrRaw.TabIndex = 14;
            lblMaxCurrRaw.Text = "Max. Current (Raw)";
            // 
            // txtCutoffAmps
            // 
            txtCutoffAmps.Location = new Point(184, 72);
            txtCutoffAmps.Name = "txtCutoffAmps";
            txtCutoffAmps.Size = new Size(134, 31);
            txtCutoffAmps.TabIndex = 9;
            // 
            // lblCutoffAmps
            // 
            lblCutoffAmps.AutoSize = true;
            lblCutoffAmps.Location = new Point(8, 72);
            lblCutoffAmps.Name = "lblCutoffAmps";
            lblCutoffAmps.Size = new Size(124, 25);
            lblCutoffAmps.TabIndex = 13;
            lblCutoffAmps.Text = "Cutoff (Amps)";
            // 
            // txtMaxCurrRaw
            // 
            txtMaxCurrRaw.Location = new Point(184, 112);
            txtMaxCurrRaw.Name = "txtMaxCurrRaw";
            txtMaxCurrRaw.Size = new Size(134, 31);
            txtMaxCurrRaw.TabIndex = 10;
            // 
            // txtCutoffRaw
            // 
            txtCutoffRaw.Location = new Point(184, 32);
            txtCutoffRaw.Name = "txtCutoffRaw";
            txtCutoffRaw.Size = new Size(134, 31);
            txtCutoffRaw.TabIndex = 11;
            // 
            // txtSimDuration
            // 
            txtSimDuration.Location = new Point(168, 8);
            txtSimDuration.Name = "txtSimDuration";
            txtSimDuration.Size = new Size(120, 31);
            txtSimDuration.TabIndex = 7;
            // 
            // btnStopSim
            // 
            btnStopSim.Location = new Point(8, 128);
            btnStopSim.Name = "btnStopSim";
            btnStopSim.Size = new Size(112, 34);
            btnStopSim.TabIndex = 6;
            btnStopSim.Text = "Stop";
            btnStopSim.UseVisualStyleBackColor = true;
            // 
            // btnStartSim
            // 
            btnStartSim.Location = new Point(176, 128);
            btnStartSim.Name = "btnStartSim";
            btnStartSim.Size = new Size(112, 34);
            btnStartSim.TabIndex = 1;
            btnStartSim.Text = "Start";
            btnStartSim.UseVisualStyleBackColor = true;
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
            // lblBaudRate
            // 
            lblBaudRate.AutoSize = true;
            lblBaudRate.Location = new Point(8, 48);
            lblBaudRate.Name = "lblBaudRate";
            lblBaudRate.Size = new Size(92, 25);
            lblBaudRate.TabIndex = 4;
            lblBaudRate.Text = "Baud Rate";
            // 
            // lblDuration
            // 
            lblDuration.AutoSize = true;
            lblDuration.Location = new Point(8, 8);
            lblDuration.Name = "lblDuration";
            lblDuration.Size = new Size(163, 25);
            lblDuration.TabIndex = 3;
            lblDuration.Text = "Duration (Seconds)";
            // 
            // cmbBxChgProfile
            // 
            cmbBxChgProfile.FormattingEnabled = true;
            cmbBxChgProfile.Location = new Point(168, 88);
            cmbBxChgProfile.Name = "cmbBxChgProfile";
            cmbBxChgProfile.Size = new Size(120, 33);
            cmbBxChgProfile.TabIndex = 2;
            // 
            // cmbBxBaudRate
            // 
            cmbBxBaudRate.FormattingEnabled = true;
            cmbBxBaudRate.Location = new Point(168, 48);
            cmbBxBaudRate.Name = "cmbBxBaudRate";
            cmbBxBaudRate.Size = new Size(120, 33);
            cmbBxBaudRate.TabIndex = 1;
            // 
            // tabDiagnostics
            // 
            tabDiagnostics.Controls.Add(btnClearDiagForm);
            tabDiagnostics.Controls.Add(grpboxDiagOutput);
            tabDiagnostics.Controls.Add(lblType);
            tabDiagnostics.Controls.Add(lblStickerInfo);
            tabDiagnostics.Controls.Add(lblToolSerialNum);
            tabDiagnostics.Controls.Add(lblSubmitDiagDate);
            tabDiagnostics.Controls.Add(lblOneKeyID);
            tabDiagnostics.Controls.Add(txtSubmitDiagDate);
            tabDiagnostics.Controls.Add(txtSubmitDiagSerial);
            tabDiagnostics.Controls.Add(txtSubmitDiagSticker);
            tabDiagnostics.Controls.Add(txtSubmitDiagType);
            tabDiagnostics.Controls.Add(txtOneKeyID);
            tabDiagnostics.Controls.Add(btnSubmitDiagForm);
            tabDiagnostics.Location = new Point(4, 34);
            tabDiagnostics.Name = "tabDiagnostics";
            tabDiagnostics.Padding = new Padding(3);
            tabDiagnostics.Size = new Size(900, 474);
            tabDiagnostics.TabIndex = 3;
            tabDiagnostics.Text = "Submit Diagnostics";
            tabDiagnostics.UseVisualStyleBackColor = true;
            // 
            // btnClearDiagForm
            // 
            btnClearDiagForm.Location = new Point(576, 216);
            btnClearDiagForm.Name = "btnClearDiagForm";
            btnClearDiagForm.Size = new Size(152, 34);
            btnClearDiagForm.TabIndex = 23;
            btnClearDiagForm.Text = "Clear Form";
            btnClearDiagForm.UseVisualStyleBackColor = true;
            // 
            // grpboxDiagOutput
            // 
            grpboxDiagOutput.Controls.Add(rtbSubmitDiagReadOnly);
            grpboxDiagOutput.Location = new Point(296, 8);
            grpboxDiagOutput.Name = "grpboxDiagOutput";
            grpboxDiagOutput.Size = new Size(592, 200);
            grpboxDiagOutput.TabIndex = 22;
            grpboxDiagOutput.TabStop = false;
            grpboxDiagOutput.Text = "Diagnostic Output";
            // 
            // rtbSubmitDiagReadOnly
            // 
            rtbSubmitDiagReadOnly.Location = new Point(8, 32);
            rtbSubmitDiagReadOnly.Name = "rtbSubmitDiagReadOnly";
            rtbSubmitDiagReadOnly.Size = new Size(576, 160);
            rtbSubmitDiagReadOnly.TabIndex = 16;
            rtbSubmitDiagReadOnly.Text = "";
            // 
            // lblType
            // 
            lblType.AutoSize = true;
            lblType.Location = new Point(16, 176);
            lblType.Name = "lblType";
            lblType.Size = new Size(49, 25);
            lblType.TabIndex = 21;
            lblType.Text = "Type";
            // 
            // lblStickerInfo
            // 
            lblStickerInfo.AutoSize = true;
            lblStickerInfo.Location = new Point(16, 136);
            lblStickerInfo.Name = "lblStickerInfo";
            lblStickerInfo.Size = new Size(63, 25);
            lblStickerInfo.TabIndex = 20;
            lblStickerInfo.Text = "Sticker";
            // 
            // lblToolSerialNum
            // 
            lblToolSerialNum.AutoSize = true;
            lblToolSerialNum.Location = new Point(16, 96);
            lblToolSerialNum.Name = "lblToolSerialNum";
            lblToolSerialNum.Size = new Size(70, 25);
            lblToolSerialNum.TabIndex = 19;
            lblToolSerialNum.Text = "Serial #";
            // 
            // lblSubmitDiagDate
            // 
            lblSubmitDiagDate.AutoSize = true;
            lblSubmitDiagDate.Location = new Point(16, 56);
            lblSubmitDiagDate.Name = "lblSubmitDiagDate";
            lblSubmitDiagDate.Size = new Size(49, 25);
            lblSubmitDiagDate.TabIndex = 18;
            lblSubmitDiagDate.Text = "Date";
            // 
            // lblOneKeyID
            // 
            lblOneKeyID.AutoSize = true;
            lblOneKeyID.Location = new Point(16, 16);
            lblOneKeyID.Name = "lblOneKeyID";
            lblOneKeyID.Size = new Size(103, 25);
            lblOneKeyID.TabIndex = 17;
            lblOneKeyID.Text = "One-Key ID";
            // 
            // txtSubmitDiagDate
            // 
            txtSubmitDiagDate.Location = new Point(120, 56);
            txtSubmitDiagDate.Name = "txtSubmitDiagDate";
            txtSubmitDiagDate.Size = new Size(150, 31);
            txtSubmitDiagDate.TabIndex = 15;
            // 
            // txtSubmitDiagSerial
            // 
            txtSubmitDiagSerial.Location = new Point(120, 96);
            txtSubmitDiagSerial.Name = "txtSubmitDiagSerial";
            txtSubmitDiagSerial.Size = new Size(150, 31);
            txtSubmitDiagSerial.TabIndex = 14;
            // 
            // txtSubmitDiagSticker
            // 
            txtSubmitDiagSticker.Location = new Point(120, 136);
            txtSubmitDiagSticker.Name = "txtSubmitDiagSticker";
            txtSubmitDiagSticker.Size = new Size(150, 31);
            txtSubmitDiagSticker.TabIndex = 13;
            // 
            // txtSubmitDiagType
            // 
            txtSubmitDiagType.Location = new Point(120, 176);
            txtSubmitDiagType.Name = "txtSubmitDiagType";
            txtSubmitDiagType.Size = new Size(150, 31);
            txtSubmitDiagType.TabIndex = 11;
            // 
            // txtOneKeyID
            // 
            txtOneKeyID.Location = new Point(120, 16);
            txtOneKeyID.Name = "txtOneKeyID";
            txtOneKeyID.Size = new Size(150, 31);
            txtOneKeyID.TabIndex = 10;
            // 
            // btnSubmitDiagForm
            // 
            btnSubmitDiagForm.Location = new Point(736, 216);
            btnSubmitDiagForm.Name = "btnSubmitDiagForm";
            btnSubmitDiagForm.Size = new Size(152, 34);
            btnSubmitDiagForm.TabIndex = 9;
            btnSubmitDiagForm.Text = "Submit Form";
            btnSubmitDiagForm.UseVisualStyleBackColor = true;
            // 
            // tabAbout
            // 
            tabAbout.Controls.Add(linkLabelKillaVolt);
            tabAbout.Controls.Add(lblKillaVoltAbout);
            tabAbout.Controls.Add(linkLabelMartin);
            tabAbout.Controls.Add(lblMartin);
            tabAbout.Controls.Add(lblAboutTitle);
            tabAbout.Location = new Point(4, 34);
            tabAbout.Name = "tabAbout";
            tabAbout.Padding = new Padding(3);
            tabAbout.Size = new Size(900, 474);
            tabAbout.TabIndex = 4;
            tabAbout.Text = "About";
            tabAbout.UseVisualStyleBackColor = true;
            // 
            // linkLabelKillaVolt
            // 
            linkLabelKillaVolt.AutoSize = true;
            linkLabelKillaVolt.LinkArea = new LinkArea(13, 60);
            linkLabelKillaVolt.Location = new Point(272, 176);
            linkLabelKillaVolt.Name = "linkLabelKillaVolt";
            linkLabelKillaVolt.Size = new Size(347, 30);
            linkLabelKillaVolt.TabIndex = 4;
            linkLabelKillaVolt.TabStop = true;
            linkLabelKillaVolt.Text = "GitHub Repo: https://github.com/KillaVolt";
            linkLabelKillaVolt.UseCompatibleTextRendering = true;
            // 
            // lblKillaVoltAbout
            // 
            lblKillaVoltAbout.AutoSize = true;
            lblKillaVoltAbout.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblKillaVoltAbout.Location = new Point(320, 144);
            lblKillaVoltAbout.Name = "lblKillaVoltAbout";
            lblKillaVoltAbout.Size = new Size(258, 28);
            lblKillaVoltAbout.TabIndex = 3;
            lblKillaVoltAbout.Text = "GUI designed By: KillaVolt";
            // 
            // linkLabelMartin
            // 
            linkLabelMartin.AutoSize = true;
            linkLabelMartin.LinkArea = new LinkArea(13, 60);
            linkLabelMartin.Location = new Point(200, 112);
            linkLabelMartin.Name = "linkLabelMartin";
            linkLabelMartin.Size = new Size(514, 30);
            linkLabelMartin.TabIndex = 2;
            linkLabelMartin.TabStop = true;
            linkLabelMartin.Text = "GitHub Repo: https://github.com/mnh-jansson/m18-protocol/";
            linkLabelMartin.UseCompatibleTextRendering = true;
            linkLabelMartin.LinkClicked += linkLabel1_LinkClicked;
            // 
            // lblMartin
            // 
            lblMartin.AutoSize = true;
            lblMartin.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblMartin.Location = new Point(224, 80);
            lblMartin.Name = "lblMartin";
            lblMartin.Size = new Size(459, 28);
            lblMartin.TabIndex = 1;
            lblMartin.Text = "Original M18 Protocol code by: Martin Jansson";
            // 
            // lblAboutTitle
            // 
            lblAboutTitle.Font = new Font("Segoe UI Black", 26F, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Point, 0);
            lblAboutTitle.Image = (Image)resources.GetObject("lblAboutTitle.Image");
            lblAboutTitle.ImageAlign = ContentAlignment.MiddleLeft;
            lblAboutTitle.Location = new Point(144, 8);
            lblAboutTitle.Name = "lblAboutTitle";
            lblAboutTitle.Size = new Size(632, 80);
            lblAboutTitle.TabIndex = 0;
            lblAboutTitle.Text = "         Pack Analyzer GUI";
            lblAboutTitle.TextAlign = ContentAlignment.TopCenter;
            // 
            // rtbDebugOutput
            // 
            rtbDebugOutput.Location = new Point(8, 520);
            rtbDebugOutput.Name = "rtbDebugOutput";
            rtbDebugOutput.Size = new Size(904, 168);
            rtbDebugOutput.TabIndex = 1;
            rtbDebugOutput.Text = "";
            // 
            // btnTestFT232
            // 
            btnTestFT232.Location = new Point(342, 48);
            btnTestFT232.Name = "btnTestFT232";
            btnTestFT232.Size = new Size(112, 34);
            btnTestFT232.TabIndex = 7;
            btnTestFT232.Text = "Test Device";
            btnTestFT232.UseVisualStyleBackColor = true;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(928, 715);
            Controls.Add(rtbDebugOutput);
            Controls.Add(tabControlM18Main);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "frmMain";
            ShowIcon = false;
            Text = "M18 Pack Analyzer";
            Load += frmMain_Load;
            tabControlM18Main.ResumeLayout(false);
            tabSimple.ResumeLayout(false);
            tabSimple.PerformLayout();
            grpOutput.ResumeLayout(false);
            tabAdvanced.ResumeLayout(false);
            tabAdvanced.PerformLayout();
            grpTryCmd.ResumeLayout(false);
            grpTryCmd.PerformLayout();
            grpbxDebugCmd.ResumeLayout(false);
            grpbxDebugCmd.PerformLayout();
            tabSimulation.ResumeLayout(false);
            tabSimulation.PerformLayout();
            grpBxSimCustomProfile.ResumeLayout(false);
            grpBxSimCustomProfile.PerformLayout();
            tabDiagnostics.ResumeLayout(false);
            tabDiagnostics.PerformLayout();
            grpboxDiagOutput.ResumeLayout(false);
            tabAbout.ResumeLayout(false);
            tabAbout.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TabControl tabControlM18Main;
        private TabPage tabSimple;
        private TabPage tabAdvanced;
        private ComboBox cmbBxSerialPort;
        private RichTextBox rtbOutput;
        private TabPage tabSimulation;
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
        private ComboBox cmbBxChgProfile;
        private ComboBox cmbBxBaudRate;
        private Button btnRestoreTxRxState;
        private Button btnSubmitDiagForm;
        private Label label6;
        private Label lblType;
        private Label lblStickerInfo;
        private Label lblToolSerialNum;
        private Label lblSubmitDiagDate;
        private Label lblOneKeyID;
        private RichTextBox rtbSubmitDiagReadOnly;
        private TextBox txtSubmitDiagDate;
        private TextBox txtSubmitDiagSerial;
        private TextBox txtSubmitDiagSticker;
        private TextBox textBox3;
        private TextBox txtSubmitDiagType;
        private TextBox txtOneKeyID;
        private Button btnStartSim;
        private Label lblSimProfile;
        private Label lblBaudRate;
        private Label lblDuration;
        private Button btnStopSim;
        private TextBox txtSimDuration;
        private TextBox txtCutoffRaw;
        private TextBox txtMaxCurrRaw;
        private TextBox txtCutoffAmps;
        private TextBox txtMaxCurrAmps;
        private Label lblMaxCurrAmps;
        private Label lblMaxCurrRaw;
        private Label lblCutoffAmps;
        private Label lblCutoffRaw;
        private GroupBox grpboxDiagOutput;
        private Button btnClearDiagForm;
        private GroupBox grpBxSimCustomProfile;
        private Label lblAboutTitle;
        private LinkLabel linkLabelMartin;
        private Label lblMartin;
        private LinkLabel linkLabelKillaVolt;
        private Label lblKillaVoltAbout;
        private RichTextBox rtbDebugOutput;
        private Button btnTestFT232;
    }
}
