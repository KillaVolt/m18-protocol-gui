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

            chkbxTXLog.Checked = true;
            chkboxRxLog.Checked = true;

            LogDebug("Application initialized. Default TX/RX logging enabled.");

            toolTipSimpleTab.SetToolTip(btnRefresh, "Refresh the list of available serial ports.");
            toolTipSimpleTab.SetToolTip(btnConnect, "Connect to the selected serial port.");
            toolTipSimpleTab.SetToolTip(btnDisconnect, "Disconnect from the currently connected device.");
            toolTipSimpleTab.SetToolTip(btnIdle, "Drive TX low (idle). Safe for connect/disconnect.");
            toolTipSimpleTab.SetToolTip(btnActive, "Drive TX high (active). Charger simulation.");
            toolTipSimpleTab.SetToolTip(btnHealthReport, "Read and display a basic battery health report.");
            toolTipSimpleTab.SetToolTip(btnReset, "Send a reset signal to the connected battery.");
            toolTipSimpleTab.SetToolTip(btnCopyOutput, "Copy all output log text to the clipboard.");
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
            LogDebug("Refresh button pressed - calling RefreshSerialPorts().");
            RefreshSerialPorts();
        }

        private void RefreshSerialPorts()
        {
            LogDebug("Entering RefreshSerialPorts(). Fetching available serial ports.");
            AppendLog("Refreshing serial port list...");

            try
            {
                var portDescriptions = GetPortDescriptions();
                var portNames = SerialPort.GetPortNames().OrderBy(port => port, StringComparer.OrdinalIgnoreCase).ToList();

                LogDebug($"Detected {portNames.Count} serial port(s).");

                cmbBxSerialPort.Items.Clear();

                foreach (var port in portNames)
                {
                    portDescriptions.TryGetValue(port, out var description);
                    cmbBxSerialPort.Items.Add(new SerialPortDisplay(port, description));
                    LogDebug($"Added port to combo box: {port}{(string.IsNullOrWhiteSpace(description) ? string.Empty : $" - {description}")}");
                    AppendLog($"Found port {port}{(string.IsNullOrWhiteSpace(description) ? string.Empty : $" - {description}")}");
                }

                if (portNames.Count == 0)
                {
                    AppendLog("No serial ports detected.");
                }
                else
                {
                    cmbBxSerialPort.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                LogError("Error while refreshing serial ports", ex);
            }
        }

        private async void btnConnect_Click(object? sender, EventArgs e)
        {
            LogDebug("Connect button pressed - attempting to establish protocol connection.");
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
                    AppendLog($"Port {selectedPort.PortName} is already open. Ignoring duplicate connect request.");
                    MessageBox.Show($"{selectedPort.PortName} is already open.", "Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                AppendLog($"A different port ({_protocol.PortName}) is currently open. Closing it before opening {selectedPort.PortName}...");
                await DisconnectAsync();
            }

            AppendLog($"Attempting to open {selectedPort} with settings: 4800 baud, 8 data bits, parity None, stop bits One, handshake None.");

            try
            {
                await Task.Run(() => _protocol = new M18Protocol(selectedPort.PortName, LogDebug));
                ApplyProtocolLoggingPreferences();
                LogDebug($"Protocol initialized for {selectedPort.PortName}.");
                AppendLog($"{selectedPort} opened successfully.");
            }
            catch (Exception ex)
            {
                _protocol = null;
                LogError($"Failed to open {selectedPort}.", ex);
            }
        }

        private async void btnDisconnect_Click(object? sender, EventArgs e)
        {
            LogDebug("Disconnect button pressed - initiating disconnect sequence.");
            await DisconnectAsync();
        }

        private async Task DisconnectAsync()
        {
            LogDebug("Entering DisconnectAsync().");
            if (_protocol == null)
            {
                AppendLog("Disconnect requested, but no serial port is currently open.");
                MessageBox.Show("No serial port is currently open.", "Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AppendLog($"Closing {_protocol.PortName}...");

            try
            {
                await Task.Run(() => _protocol.Close());
                LogDebug("Protocol closed successfully.");
                AppendLog($"{_protocol.PortName} closed successfully.");
            }
            catch (Exception ex)
            {
                LogError($"Error while closing {_protocol.PortName}.", ex);
            }
            finally
            {
                _protocol = null;
            }
        }

        private async void btnIdle_Click(object? sender, EventArgs e)
        {
            LogDebug("Idle button pressed - requesting _protocol.Idle().");
            if (!EnsureConnected())
            {
                return;
            }

            await Task.Run(() => _protocol!.Idle());
            AppendLog("TX set to Idle (low). Safe to connect or disconnect battery.");
        }

        private async void btnActive_Click(object? sender, EventArgs e)
        {
            LogDebug("Active button pressed - requesting _protocol.High().");
            if (!EnsureConnected())
            {
                return;
            }

            await Task.Run(() => _protocol!.High());
            AppendLog("TX set to Active (high). Charger simulation enabled.");
        }

        private async void btnHealthReport_Click(object? sender, EventArgs e)
        {
            LogDebug("Health Report button pressed - requesting _protocol.HealthReport().");
            if (!EnsureConnected())
            {
                return;
            }

            try
            {
                var report = await Task.Run(() => _protocol!.HealthReport());
                AppendLog("=== Health report ===");
                AppendLog(report);
                AppendLog("Health report complete.");
            }
            catch (Exception ex)
            {
                LogError("Health report failed.", ex);
            }
        }

        private async void btnReset_Click(object? sender, EventArgs e)
        {
            LogDebug("Reset button pressed - requesting _protocol.Reset().");
            if (!EnsureConnected())
            {
                return;
            }

            try
            {
                var success = await Task.Run(() => _protocol!.Reset());
                AppendLog(success ? "Reset command acknowledged by device." : "Reset command did not receive expected response.");
            }
            catch (Exception ex)
            {
                LogError("Reset failed.", ex);
            }
        }

        private void btnCopyOutput_Click(object? sender, EventArgs e)
        {
            LogDebug("Copy Output button pressed - copying rtbOutput content to clipboard.");
            if (string.IsNullOrEmpty(rtbOutput.Text))
            {
                AppendLog("No output to copy.");
                return;
            }

            Clipboard.SetText(rtbOutput.Text);
            AppendLog("Output copied to clipboard.");
        }

        private bool EnsureConnected()
        {
            LogDebug("Checking connection status via EnsureConnected().");
            if (_protocol != null)
            {
                LogDebug("Protocol already connected.");
                return true;
            }

            AppendLog("No active connection. Please connect to a serial port first.");
            MessageBox.Show("Please connect to a serial port first.", "Connection required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private Dictionary<string, string> GetPortDescriptions()
        {
            LogDebug("Attempting to retrieve port descriptions via WMI query.");
            var descriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT Caption, DeviceID FROM Win32_PnPEntity WHERE Caption LIKE 'COM%' OR DeviceID LIKE 'COM%'");
                foreach (var obj in searcher.Get().OfType<System.Management.ManagementObject>())
                {
                    var caption = obj["Caption"]?.ToString();
                    var deviceId = obj["DeviceID"]?.ToString();

                    if (string.IsNullOrWhiteSpace(caption) && string.IsNullOrWhiteSpace(deviceId))
                    {
                        continue;
                    }

                    var combined = caption ?? deviceId ?? string.Empty;
                    var match = System.Text.RegularExpressions.Regex.Match(combined, @"\((COM\d+)\)");
                    var portName = match.Success ? match.Groups[1].Value : SerialPort.GetPortNames().FirstOrDefault(name => combined.Contains(name, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrWhiteSpace(portName) && !descriptions.ContainsKey(portName))
                    {
                        descriptions[portName] = caption ?? deviceId ?? portName;
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to read port descriptions.", ex);
            }

            return descriptions;
        }

        private void AppendLog(string message)
        {
            AppendSimpleLog(FormatLogMessage(message));
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
            LogDebug($"Protocol log forwarded to debug output: {message}");
        }

        private void LogError(string context, Exception exception)
        {
            var fullMessage = $"{context} Error details: {exception}";
            AppendLog(fullMessage);
            LogDebug(fullMessage);
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
            _protocol.DebugLogger = LogDebug;
            _protocol.TxLogger = message =>
            {
                AppendProtocolLog(message);
                LogDebug($"TX log forwarded from protocol: {message}");
            };
            _protocol.RxLogger = message =>
            {
                AppendProtocolLog(message);
                LogDebug($"RX log forwarded from protocol: {message}");
            };
            LogDebug($"Protocol logging preferences applied. PrintTx={_protocol.PrintTx}, PrintRx={_protocol.PrintRx}.");
        }

        private void chkbxTXLog_CheckedChanged(object? sender, EventArgs e)
        {
            if (_protocol != null)
            {
                _protocol.PrintTx = chkbxTXLog.Checked;
                LogDebug($"TX logging preference changed: {_protocol.PrintTx}.");
            }
            else
            {
                LogDebug($"TX logging checkbox changed to {chkbxTXLog.Checked} (protocol not initialized yet).");
            }
        }

        private void chkboxRxLog_CheckedChanged(object? sender, EventArgs e)
        {
            if (_protocol != null)
            {
                _protocol.PrintRx = chkboxRxLog.Checked;
                LogDebug($"RX logging preference changed: {_protocol.PrintRx}.");
            }
            else
            {
                LogDebug($"RX logging checkbox changed to {chkboxRxLog.Checked} (protocol not initialized yet).");
            }
        }

        private void LogDebug(string message)
        {
            if (rtbDebugOutput.InvokeRequired)
            {
                rtbDebugOutput.Invoke(new Action(() => LogDebug(message)));
                return;
            }

            var timestamped = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
            var prefix = _hasAppendedDebugLog ? Environment.NewLine : string.Empty;
            rtbDebugOutput.AppendText($"{prefix}{timestamped}");
            rtbDebugOutput.SelectionStart = rtbDebugOutput.TextLength;
            rtbDebugOutput.ScrollToCaret();
            _hasAppendedDebugLog = true;
        }

        private void rtbOutput_TextChanged(object sender, EventArgs e)
        {

        }

        private sealed record SerialPortDisplay(string PortName, string? Description)
        {
            public override string ToString()
            {
                return string.IsNullOrWhiteSpace(Description) ? PortName : $"{PortName} - {Description}";
            }
        }
    }
}
