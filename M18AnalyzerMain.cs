using System.IO.Ports;

namespace M18BatteryInfo
{
    public partial class frmMain : Form
    {
        private SerialPort? _serialPort;
        private bool _hasAppendedLog;

        public frmMain()
        {
            InitializeComponent();

            btnRefresh.Click += btnRefresh_Click;
            btnConnect.Click += btnConnect_Click;
            btnDisconnect.Click += btnDisconnect_Click;
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
            RefreshSerialPorts();
        }

        private void RefreshSerialPorts()
        {
            AppendLog("Refreshing serial port list...");

            try
            {
                var portDescriptions = GetPortDescriptions();
                var portNames = SerialPort.GetPortNames().OrderBy(port => port, StringComparer.OrdinalIgnoreCase).ToList();

                cmbBxSerialPort.Items.Clear();

                foreach (var port in portNames)
                {
                    portDescriptions.TryGetValue(port, out var description);
                    cmbBxSerialPort.Items.Add(new SerialPortDisplay(port, description));
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

        private void btnConnect_Click(object? sender, EventArgs e)
        {
            if (cmbBxSerialPort.SelectedItem is not SerialPortDisplay selectedPort)
            {
                AppendLog("No serial port selected. Please choose a port before connecting.");
                MessageBox.Show("Please select a serial port before connecting.", "Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_serialPort?.IsOpen == true)
            {
                if (string.Equals(_serialPort.PortName, selectedPort.PortName, StringComparison.OrdinalIgnoreCase))
                {
                    AppendLog($"Port {selectedPort.PortName} is already open. Ignoring duplicate connect request.");
                    MessageBox.Show($"{selectedPort.PortName} is already open.", "Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                AppendLog($"A different port ({_serialPort.PortName}) is currently open. Closing it before opening {selectedPort.PortName}...");
                btnDisconnect_Click(sender, e);
            }

            AppendLog($"Attempting to open {selectedPort} with settings: 4800 baud, 8 data bits, parity None, stop bits One, handshake None.");

            _serialPort = new SerialPort(selectedPort.PortName, 4800, Parity.None, 8, StopBits.One)
            {
                Handshake = Handshake.None,
                ReadTimeout = 500,
                WriteTimeout = 500
            };

            try
            {
                _serialPort.Open();
                AppendLog($"{selectedPort} opened successfully.");
            }
            catch (Exception ex)
            {
                LogError($"Failed to open {selectedPort}.", ex);
            }
        }

        private void btnDisconnect_Click(object? sender, EventArgs e)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
            {
                AppendLog("Disconnect requested, but no serial port is currently open.");
                MessageBox.Show("No serial port is currently open.", "Serial Port", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            AppendLog($"Closing {_serialPort.PortName}...");

            try
            {
                _serialPort.Close();
                AppendLog($"{_serialPort.PortName} closed successfully.");
            }
            catch (Exception ex)
            {
                LogError($"Error while closing {_serialPort.PortName}.", ex);
            }
            finally
            {
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        private Dictionary<string, string> GetPortDescriptions()
        {
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
            var timestampedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}";
            var prefix = _hasAppendedLog ? $"{Environment.NewLine}{Environment.NewLine}" : string.Empty;

            rtbOutput.AppendText($"{prefix}{timestampedMessage}");
            rtbOutput.SelectionStart = rtbOutput.TextLength;
            rtbOutput.ScrollToCaret();

            _hasAppendedLog = true;
        }

        private void LogError(string context, Exception exception)
        {
            var fullMessage = $"{context} Error details: {exception}";
            AppendLog(fullMessage);
            MessageBox.Show(fullMessage, "Serial Port Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
