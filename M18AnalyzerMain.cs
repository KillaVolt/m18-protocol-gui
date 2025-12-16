using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
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
                    AppendLogBoth($"Found port {port.DisplayName}{(port.IsLikelyFtdi ? " (FTDI detected)" : string.Empty)}");
                }

                AppendDebugLog($"RefreshSerialPorts(): {ports.Count} port(s) detected.");
                foreach (var port in ports)
                {
                    var sourceLabel = string.IsNullOrWhiteSpace(port.Source) ? "Unknown source" : port.Source;
                    AppendDebugLog($" - {port.DisplayName} (source: {sourceLabel})");
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
                _selectedPortDescription = selected.DisplayName;
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
            }
            catch (Exception ex)
            {
                _protocol = null;
                LogError($"Failed to open {selectedDescription}.", ex);
            }
        }

        private async void btnDisconnect_Click(object? sender, EventArgs e)
        {
            LogDebugAction("Requesting DisconnectAsync().");
            await DisconnectAsync();
        }

        private async Task DisconnectAsync()
        {
            if (_protocol == null)
            {
                AppendLogBoth("Disconnect requested, but no serial port is currently open.");
                MessageBox.Show("No serial port is currently open.", "Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            }
        }

        private async void btnIdle_Click(object? sender, EventArgs e)
        {
            LogDebugAction("Requesting _protocol.Idle().");
            if (!EnsureConnected())
            {
                return;
            }

            AppendDebugLog("Invoking _protocol.Idle() to drive TX low.");
            try
            {
                await Task.Run(() => _protocol!.Idle());
                AppendLogBoth("TX set to Idle (low). Safe to connect or disconnect battery.");
            }
            catch (Exception ex)
            {
                LogError("Failed to set TX to Idle.", ex);
            }
        }

        private async void btnActive_Click(object? sender, EventArgs e)
        {
            LogDebugAction("Requesting _protocol.High().");
            if (!EnsureConnected())
            {
                return;
            }

            AppendDebugLog("Invoking _protocol.High() to drive TX high.");
            try
            {
                await Task.Run(() => _protocol!.High());
                AppendLogBoth("TX set to Active (high). Charger simulation enabled.");
            }
            catch (Exception ex)
            {
                LogError("Failed to set TX to Active (high).", ex);
            }
        }

        private async void btnHealthReport_Click(object? sender, EventArgs e)
        {
            LogDebugAction("Requesting _protocol.HealthReport().");
            if (!EnsureConnected())
            {
                return;
            }

            try
            {
                AppendDebugLog("Starting health report collection (mirrors m18.py health()).");
                var report = await Task.Run(() => _protocol!.HealthReport());
                AppendLogBoth("=== Health report ===");
                foreach (var line in report.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    AppendLogBoth(line);
                }
                AppendLogBoth("Health report complete.");
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
            LogDebugAction("Checking EnsureConnected().");
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
            var portLookup = new Dictionary<string, SerialPortDisplay>(StringComparer.OrdinalIgnoreCase);

            static string CombineSources(string? existingSource, string newSource)
            {
                if (string.IsNullOrWhiteSpace(existingSource))
                {
                    return newSource;
                }

                return existingSource.Contains(newSource, StringComparison.OrdinalIgnoreCase)
                    ? existingSource
                    : $"{existingSource}; {newSource}";
            }

            foreach (var portName in SerialPort.GetPortNames())
            {
                portLookup[portName] = new SerialPortDisplay(portName, null, null, null, "SerialPort.GetPortNames() fallback");
            }

            try
            {
                using var serialSearcher = new ManagementObjectSearcher("SELECT DeviceID, Description, Manufacturer, Name FROM Win32_SerialPort");
                foreach (var obj in serialSearcher.Get().OfType<ManagementObject>())
                {
                    var portName = ExtractPortName(obj["DeviceID"]?.ToString());
                    if (string.IsNullOrWhiteSpace(portName))
                    {
                        continue;
                    }

                    var existing = portLookup.GetValueOrDefault(portName, new SerialPortDisplay(portName, null, null, null, string.Empty));
                    portLookup[portName] = existing with
                    {
                        Description = obj["Description"]?.ToString(),
                        Manufacturer = obj["Manufacturer"]?.ToString(),
                        FriendlyName = obj["Name"]?.ToString(),
                        Source = CombineSources(existing.Source, "WMI: Win32_SerialPort")
                    };
                }
            }
            catch (Exception ex)
            {
                AppendDebugLog($"Win32_SerialPort query failed; using fallback data only. Details: {ex.Message}");
            }

            try
            {
                using var pnpSearcher = new ManagementObjectSearcher("SELECT DeviceID, Caption, Manufacturer, Name FROM Win32_PnPEntity WHERE Caption LIKE 'COM%' OR DeviceID LIKE 'COM%'");
                foreach (var obj in pnpSearcher.Get().OfType<ManagementObject>())
                {
                    var portName = ExtractPortName(obj["Caption"]?.ToString()) ?? ExtractPortName(obj["DeviceID"]?.ToString());
                    if (string.IsNullOrWhiteSpace(portName))
                    {
                        continue;
                    }

                    var existing = portLookup.GetValueOrDefault(portName, new SerialPortDisplay(portName, null, null, null, string.Empty));
                    portLookup[portName] = existing with
                    {
                        Description = existing.Description ?? obj["Caption"]?.ToString(),
                        Manufacturer = existing.Manufacturer ?? obj["Manufacturer"]?.ToString(),
                        FriendlyName = existing.FriendlyName ?? obj["Name"]?.ToString(),
                        Source = CombineSources(existing.Source, "WMI: Win32_PnPEntity")
                    };
                }
            }
            catch (Exception ex)
            {
                AppendDebugLog($"Win32_PnPEntity query failed; using available data. Details: {ex.Message}");
            }

            return portLookup.Values
                .OrderBy(port => port.PortName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string? ExtractPortName(string? source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }

            var match = System.Text.RegularExpressions.Regex.Match(source, @"(COM\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
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

        private void rtbOutput_TextChanged(object sender, EventArgs e)
        {

        }

        private sealed record SerialPortDisplay(string PortName, string? Description, string? Manufacturer, string? FriendlyName, string Source)
        {
            public string DisplayName
            {
                get
                {
                    var coreDescription = FriendlyName ?? Description;

                    if (string.IsNullOrWhiteSpace(coreDescription))
                    {
                        return PortName;
                    }

                    var manufacturerText = string.IsNullOrWhiteSpace(Manufacturer)
                        ? string.Empty
                        : $" ({Manufacturer})";

                    return $"{PortName} — {coreDescription}{manufacturerText}";
                }
            }

            public bool IsLikelyFtdi => new[] { Description, Manufacturer, FriendlyName }
                .Any(value => value != null && value.IndexOf("FTDI", StringComparison.OrdinalIgnoreCase) >= 0);

            public override string ToString()
            {
                return DisplayName;
            }
        }
    }
}
