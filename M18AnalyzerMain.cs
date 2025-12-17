using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace M18BatteryInfo
{
    public partial class frmMain : Form
    {
        private M18Protocol? _protocol;
        private bool _hasAppendedLog;
        private bool _hasAppendedAdvancedLog;
        private bool _hasAppendedDebugLog;
        private string? _selectedPortName;
        private string? _selectedPortDescription;

        public frmMain()
        {
            InitializeComponent();

            btnRefresh.Click += btnRefresh_Click;
            btnConnect.Click += btnConnect_Click;
            btnDisconnect.Click += btnDisconnect_Click;
            btnIdle.Click += btnIdle_Click;
            btnActive.Click += btnActive_Click;
            btnHealthReport.Click += btnHealthReport_Click;
            btnReset.Click += btnReset_Click;
            btnCopyOutput.Click += btnCopyOutput_Click;
            chkbxTXLog.CheckedChanged += chkbxTXLog_CheckedChanged;
            chkboxRxLog.CheckedChanged += chkboxRxLog_CheckedChanged;
            btnTestFT232.Click += btnTestFT232_Click;
            cmbBxSerialPort.SelectedIndexChanged += cmbBxSerialPort_SelectedIndexChanged;
            FormClosing += frmMain_FormClosing;

            chkbxTXLog.Checked = true;
            chkboxRxLog.Checked = true;
            toolTipSimpleTab.SetToolTip(btnRefresh, "Refresh the list of available serial ports.");
            toolTipSimpleTab.SetToolTip(btnConnect, "Connect to the selected serial port.");
            toolTipSimpleTab.SetToolTip(btnDisconnect, "Disconnect from the currently connected device.");
            toolTipSimpleTab.SetToolTip(btnIdle, "Drive TX low (idle). Safe for connect/disconnect.");
            toolTipSimpleTab.SetToolTip(btnActive, "Drive TX high (active). Charger simulation.");
            toolTipSimpleTab.SetToolTip(btnHealthReport, "Read and display a basic battery health report.");
            toolTipSimpleTab.SetToolTip(btnReset, "Send a reset signal to the connected battery.");
            toolTipSimpleTab.SetToolTip(btnCopyOutput, "Copy all output log text to the clipboard.");

            UpdateConnectionUi(false);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {

        }

        private void toolTipReset_Popup(object sender, PopupEventArgs e)
        {

        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            RefreshSerialPorts();
        }

        private void lblResponseLength_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

        }

        private void grpBxSimCustomProfile_Enter(object sender, EventArgs e)
        {

        }

        private void btnRefresh_Click(object? sender, EventArgs e)
        {
            LogDebugAction("Requesting RefreshSerialPorts().");
            RefreshSerialPorts();
        }

        private void RefreshSerialPorts()
        {
            AppendLogBoth("Refreshing serial port list...");

            try
            {
                var ports = GetSerialPortInfos();

                cmbBxSerialPort.Items.Clear();
                foreach (var port in ports)
                {
                    cmbBxSerialPort.Items.Add(port);
                    AppendLogBoth($"Found port {port.DisplayName}");
                }

                AppendDebugLog($"RefreshSerialPorts(): {ports.Count} port(s) detected.");
                foreach (var port in ports)
                {
                    var sourceLabel = string.IsNullOrWhiteSpace(port.Source) ? "Unknown source" : port.Source;
                    AppendDebugLog($" - {port.DisplayName} (source: {sourceLabel})");
                    AppendDebugLog($"   Details: description='{port.DeviceDescription ?? "(none)"}', manufacturer='{port.Manufacturer ?? "(none)"}', hwid='{port.HardwareIds ?? "(none)"}'");
                }

                if (ports.Count == 0)
                {
                    AppendLogBoth("No serial ports detected.");
                }
                else
                {
                    cmbBxSerialPort.SelectedIndex = 0;
                }

                if (cmbBxSerialPort.SelectedItem is null)
                {
                    _selectedPortName = null;
                    _selectedPortDescription = null;
                }
            }
            catch (Exception ex)
            {
                LogError("Error while refreshing serial ports", ex);
            }
        }

        private void cmbBxSerialPort_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbBxSerialPort.SelectedItem is SerialPortDisplay selected)
            {
                _selectedPortName = selected.PortName;
                _selectedPortDescription = selected.DeviceDescription ?? selected.DisplayName;
                AppendDebugLog($"Selected port set to {selected.DisplayName} (source: {selected.Source}).");
            }
            else
            {
                _selectedPortName = null;
                _selectedPortDescription = null;
            }
        }

        private async void btnConnect_Click(object? sender, EventArgs e)
        {
            LogDebugAction("Requesting Connect().");
            if (cmbBxSerialPort.SelectedItem is not SerialPortDisplay selectedPort)
            {
                AppendLog("No serial port selected. Please choose a port before connecting.");
                MessageBox.Show("Please select a serial port before connecting.", "Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_protocol != null)
            {
                if (string.Equals(_protocol.PortName, selectedPort.PortName, StringComparison.OrdinalIgnoreCase))
                {
                    AppendLogBoth($"Port {selectedPort.PortName} is already open. Ignoring duplicate connect request.");
                    MessageBox.Show($"{selectedPort.PortName} is already open.", "Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                AppendLogBoth($"A different port ({_protocol.PortName}) is currently open. Closing it before opening {selectedPort.PortName}...");
                await DisconnectAsync();
            }

            var selectedDescription = _selectedPortDescription ?? selectedPort.DisplayName;

            AppendLogBoth($"Attempting to open {selectedDescription} with settings: 4800 baud, 8 data bits, parity None, stop bits Two, handshake None.");
            AppendDebugLog("Serial connection will set TX low (idle) after open.");

            try
            {
                await Task.Run(() => _protocol = new M18Protocol(selectedPort.PortName, AppendDebugLog));
                ApplyProtocolLoggingPreferences();
                AppendLogBoth($"{selectedDescription} opened successfully.");
                UpdateConnectionUi(true);
            }
            catch (Exception ex)
            {
                _protocol = null;
                LogError($"Failed to open {selectedDescription}.", ex);
                UpdateConnectionUi(false);
            }
        }

        private async void btnDisconnect_Click(object? sender, EventArgs e)
        {
            LogDebugAction(FormatLogMessage("btnDisconnect pressed - requesting DisconnectAsync()."));
            await DisconnectAsync();
        }

        private async Task DisconnectAsync(bool showUserMessages = true)
        {
            if (_protocol == null)
            {
                var message = "Disconnect requested, but no serial port is currently open.";
                AppendDebugLog(FormatLogMessage(message));
                if (showUserMessages)
                {
                    AppendLog(message);
                    MessageBox.Show(message, "Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                UpdateConnectionUi(false);
                return;
            }

            AppendLogBoth($"Closing {_protocol.PortName}...");

            try
            {
                await Task.Run(() => _protocol.Close());
                AppendLogBoth($"{_protocol.PortName} closed successfully.");
            }
            catch (Exception ex)
            {
                LogError($"Error while closing {_protocol.PortName}.", ex);
            }
            finally
            {
                _protocol = null;
                UpdateConnectionUi(false);
            }
        }

        private async void btnIdle_Click(object? sender, EventArgs e)
        {
            AppendDebugLog(FormatLogMessage("btnIdle pressed - calling _protocol.Idle()."));
            if (!EnsureConnected())
            {
                return;
            }

            AppendDebugLog(FormatLogMessage("Invoking _protocol.Idle() to drive TX low."));
            try
            {
                await Task.Run(() => _protocol!.Idle());
                AppendLogBoth("TX set to Idle (low). Safe to connect or disconnect battery.");
                AppendDebugLog(FormatLogMessage(_protocol!.GetTxStateSummary("Idle")));
            }
            catch (Exception ex)
            {
                LogError("Failed to set TX to Idle.", ex);
            }
        }

        private async void btnActive_Click(object? sender, EventArgs e)
        {
            AppendDebugLog(FormatLogMessage("btnActive pressed - calling _protocol.High()."));
            if (!EnsureConnected())
            {
                return;
            }

            AppendDebugLog(FormatLogMessage("Invoking _protocol.High() to drive TX high."));
            try
            {
                await Task.Run(() => _protocol!.High());
                AppendLogBoth("TX set to Active (high). Charger simulation enabled.");
                AppendDebugLog(FormatLogMessage(_protocol!.GetTxStateSummary("High")));
            }
            catch (Exception ex)
            {
                LogError("Failed to set TX to Active (high).", ex);
            }
        }

        private async void btnHealthReport_Click(object? sender, EventArgs e)
        {
            AppendDebugLog(FormatLogMessage("btnHealthReport pressed - calling _protocol.HealthReport()."));
            if (!EnsureConnected())
            {
                return;
            }

            try
            {
                AppendDebugLog(FormatLogMessage("Starting health report collection (mirrors m18.py health())."));
                var report = await Task.Run(() => _protocol!.HealthReport());
                AppendLog("=== Health report ===");
                foreach (var line in report.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    AppendLog(line);
                }
                AppendLog("Health report complete.");
                AppendDebugLog(FormatLogMessage("Health report appended to output."));
            }
            catch (Exception ex)
            {
                LogError("Health report failed.", ex);
            }
        }

        private async void btnReset_Click(object? sender, EventArgs e)
        {
            LogDebugAction("Requesting _protocol.Reset().");
            if (!EnsureConnected())
            {
                return;
            }

            try
            {
                AppendDebugLog("Sending reset sequence (BREAK/DTR + SYNC).");
                var success = await Task.Run(() => _protocol!.Reset());
                AppendLogBoth(success ? "Reset command acknowledged by device." : "Reset command did not receive expected response.");
            }
            catch (Exception ex)
            {
                LogError("Reset failed.", ex);
            }
        }

        private void btnCopyOutput_Click(object? sender, EventArgs e)
        {
            LogDebugAction("Copying output via btnCopyOutput_Click().");
            if (string.IsNullOrEmpty(rtbOutput.Text))
            {
                AppendLogBoth("No output to copy.");
                return;
            }

            Clipboard.SetText(rtbOutput.Text);
            AppendLogBoth("Output copied to clipboard.");
        }

        private bool EnsureConnected()
        {
            LogDebugAction(FormatLogMessage("Checking EnsureConnected()."));
            if (_protocol != null)
            {
                return true;
            }

            AppendLog("No active connection. Please connect to a serial port first.");
            MessageBox.Show("Please connect to a serial port first.", "Connection required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private List<SerialPortDisplay> GetSerialPortInfos()
        {
            return SerialPortUtil.EnumerateDetailedPorts(AppendDebugLog);
        }

        private async void btnTestFT232_Click(object? sender, EventArgs e)
        {
            LogDebugAction("Requesting Test FT232 operation.");

            if (cmbBxSerialPort.SelectedItem is not SerialPortDisplay selectedPort)
            {
                AppendLog("No serial port selected. Please choose a port before testing.");
                MessageBox.Show("Please select a serial port before testing.", "Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedDescription = _selectedPortDescription ?? selectedPort.DisplayName;

            AppendLog($"Testing FT232 on {selectedDescription}...");
            LogDebugAction($"Testing FT232 on {selectedDescription}.");

            var testResult = await Task.Run(() => TestSerialDevice(selectedPort));

            if (testResult.Success)
            {
                AppendLog($"Device responded successfully on {selectedPort.PortName}.");
                LogDebugAction($"Device responded successfully on {selectedPort.PortName}.");
            }
            else
            {
                AppendLog($"No response / failed to communicate with device on {selectedPort.PortName}{(string.IsNullOrWhiteSpace(testResult.ErrorMessage) ? string.Empty : $": {testResult.ErrorMessage}")}.");
                LogDebugAction($"No response / failed to communicate with device on {selectedPort.PortName}{(string.IsNullOrWhiteSpace(testResult.ErrorMessage) ? string.Empty : $": {testResult.ErrorMessage}")}.");
            }
        }

        private static (bool Success, string? ErrorMessage) TestSerialDevice(SerialPortDisplay port)
        {
            try
            {
                using var serialPort = new SerialPort(port.PortName, 4800, Parity.None, 8, StopBits.Two)
                {
                    ReadTimeout = 500,
                    WriteTimeout = 500,
                    DtrEnable = true,
                    RtsEnable = true
                };

                serialPort.Open();

                serialPort.DtrEnable = !serialPort.DtrEnable;
                serialPort.RtsEnable = !serialPort.RtsEnable;

                serialPort.Write(new byte[] { 0x00 }, 0, 1);

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        private void AppendLog(string message)
        {
            AppendSimpleLog(FormatLogMessage(message));
        }

        private void AppendLogBoth(string message)
        {
            var formatted = FormatLogMessage(message);
            AppendSimpleLog(formatted);
            AppendDebugLog(formatted);
        }

        private void AppendSimpleLog(string formattedMessage)
        {
            if (rtbOutput.InvokeRequired)
            {
                rtbOutput.Invoke(new Action(() => AppendSimpleLog(formattedMessage)));
                return;
            }

            var prefix = _hasAppendedLog ? Environment.NewLine : string.Empty;
            rtbOutput.AppendText($"{prefix}{formattedMessage}");
            rtbOutput.SelectionStart = rtbOutput.TextLength;
            rtbOutput.ScrollToCaret();
            _hasAppendedLog = true;
        }

        private void AppendAdvancedLog(string formattedMessage)
        {
            if (rtbAdvOutput.InvokeRequired)
            {
                rtbAdvOutput.Invoke(new Action(() => AppendAdvancedLog(formattedMessage)));
                return;
            }

            var prefix = _hasAppendedAdvancedLog ? Environment.NewLine : string.Empty;
            rtbAdvOutput.AppendText($"{prefix}{formattedMessage}");
            rtbAdvOutput.SelectionStart = rtbAdvOutput.TextLength;
            rtbAdvOutput.ScrollToCaret();
            _hasAppendedAdvancedLog = true;
        }

        private string FormatLogMessage(string message)
        {
            return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
        }

        private void AppendProtocolLog(string message)
        {
            var formattedMessage = FormatLogMessage(message);
            AppendSimpleLog(formattedMessage);
            AppendAdvancedLog(formattedMessage);
            AppendDebugLog(formattedMessage);
        }

        private void LogError(string context, Exception exception)
        {
            var fullMessage = $"{context} Error details: {exception}";
            AppendLogBoth(fullMessage);
            MessageBox.Show(fullMessage, "Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ApplyProtocolLoggingPreferences()
        {
            if (_protocol == null)
            {
                return;
            }

            _protocol.PrintTx = chkbxTXLog.Checked;
            _protocol.PrintRx = chkboxRxLog.Checked;
            _protocol.TxLogger = message =>
            {
                AppendProtocolLog(message);
            };
            _protocol.RxLogger = message =>
            {
                AppendProtocolLog(message);
            };
        }

        private void chkbxTXLog_CheckedChanged(object? sender, EventArgs e)
        {
            if (_protocol != null)
            {
                _protocol.PrintTx = chkbxTXLog.Checked;
            }
        }

        private void chkboxRxLog_CheckedChanged(object? sender, EventArgs e)
        {
            if (_protocol != null)
            {
                _protocol.PrintRx = chkboxRxLog.Checked;
            }
        }

        private void LogDebugAction(string message)
        {
            AppendDebugLog(message);
        }

        private void AppendDebugLog(string message)
        {
            if (rtbDebugOutput.InvokeRequired)
            {
                rtbDebugOutput.Invoke(new Action(() => AppendDebugLog(message)));
                return;
            }

            var prefix = _hasAppendedDebugLog ? Environment.NewLine : string.Empty;
            var formatted = message.Contains("- ") ? message : FormatLogMessage(message);
            rtbDebugOutput.AppendText($"{prefix}{formatted}");
            rtbDebugOutput.SelectionStart = rtbDebugOutput.TextLength;
            rtbDebugOutput.ScrollToCaret();
            _hasAppendedDebugLog = true;
        }

        private void UpdateConnectionUi(bool connected)
        {
            btnConnect.Enabled = !connected;
            btnDisconnect.Enabled = connected;
            btnIdle.Enabled = connected;
            btnActive.Enabled = connected;
            btnHealthReport.Enabled = connected;
            btnReset.Enabled = connected;
            btnCopyOutput.Enabled = true;
            btnTestFT232.Enabled = !connected;
            cmbBxSerialPort.Enabled = !connected;
            btnRefresh.Enabled = !connected;
        }

        private async void frmMain_FormClosing(object? sender, FormClosingEventArgs e)
        {
            AppendDebugLog(FormatLogMessage("Form closing requested - attempting clean disconnect."));

            try
            {
                await DisconnectAsync(false);
            }
            catch (Exception ex)
            {
                AppendDebugLog(FormatLogMessage($"Form closing disconnect encountered an error: {ex.GetType().Name} - {ex.Message}"));
            }
        }

        private void rtbOutput_TextChanged(object sender, EventArgs e)
        {

        }

        private void cmbBxSerialPort_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
        private void btnTestEcho_Click(object sender, EventArgs e)
{
    rtbDebugOutput.AppendText($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - Starting raw echo test on COM{_protocol?.PortName ?? "??"}\n");
    try
    {
        if (_protocol?.Port.IsOpen != true)
        {
            rtbDebugOutput.AppendText($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - Serial port not open. Connect first.\n");
            return;
        }

        // Clear input buffer to avoid stale data
        _protocol.Port.DiscardInBuffer();

        // Send 0xAA
        byte[] send = { 0xAA };
        _protocol.Port.Write(send, 0, 1);
        rtbDebugOutput.AppendText($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - Sent byte 0xAA.\n");

        // Try to read one byte, with timeout
        int response = _protocol.Port.ReadByte(); // This blocks up to ReadTimeout
        if (response >= 0)
        {
            rtbDebugOutput.AppendText($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - Received byte: 0x{response:X2}\n");
        }
        else
        {
            rtbDebugOutput.AppendText($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - No byte received (timeout).\n");
        }
    }
    catch (Exception ex)
    {
        rtbDebugOutput.AppendText($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - Exception: {ex.Message}\n");
    }
}

    }
}
