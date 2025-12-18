// *************************************************************************************************
// M18Protocol.cs
// --------------
// Implements the Milwaukee M18 battery serial protocol in C#. The class encapsulates all UART
// command composition, bit-level encoding/decoding, and register parsing so that the WinForms UI
// (frmMain in M18AnalyzerMain.cs) can simply call high-level methods like Idle(), High(),
// HealthReport(), and Reset(). Control-line manipulation (BreakState and DtrEnable) is used to
// emulate charger signals electrically on the FT232 USB-UART bridge. Extensive comments explain the
// meaning of each operation, including how bytes flow to and from the hardware.
// *************************************************************************************************

using System; // Core .NET types including DateTime, Array, and Action delegates.
using System.Collections.Generic; // Collections such as List<T> and IEnumerable<T>.
using System.IO.Ports; // SerialPort class for UART communication and control-line toggling.
using System.Linq; // LINQ helpers like Select, Aggregate, and Take for byte processing.
using System.Text; // StringBuilder for efficient log formatting and byte-to-hex conversion.
using System.Threading; // Thread.Sleep for deterministic protocol timing delays.
using System.Text.RegularExpressions; // Regex for parsing serial numbers and hardware IDs from responses.

namespace M18BatteryInfo
{
    /// <summary>
    /// C# port of the Python M18 protocol implementation. Wraps <see cref="SerialPort"/> to expose
    /// high-level methods for toggling TX line states, issuing reset sequences, and reading/writing
    /// registers. Control lines map directly to physical UART pins: <see cref="SerialPort.BreakState"/>
    /// drives the TX line low (space) while <see cref="SerialPort.DtrEnable"/> drives the DTR pin,
    /// both of which connect to the battery's interface header. The class also formats verbose logs
    /// for the WinForms UI to display.
    /// </summary>
    public class M18Protocol
    {
        // Protocol constants mirroring the Python implementation and datasheets.
        public const byte SYNC_BYTE = 0xAA; // Sync byte used to initiate communication and acknowledge reset.
        public const byte CAL_CMD = 0x55; // Calibration command identifier (not fully implemented here).
        public const byte CONF_CMD = 0x60; // Configuration command base value.
        public const byte SNAP_CMD = 0x61; // Snapshot command for specific data reads.
        public const byte KEEPALIVE_CMD = 0x62; // Keepalive command to maintain session.

        // Current limits used when simulating charger load profiles.
        public const int CUTOFF_CURRENT = 300; // Milliamps at which discharge is considered complete.
        public const int MAX_CURRENT = 6000; // Milliamps representing maximum allowable current draw.

        public int Acc { get; private set; } = 4; // Protocol accumulator (used by Python port); kept for compatibility.

        public bool PrintTx { get; set; } = true; // When true, TX bytes are logged through TxLogger.
        public bool PrintRx { get; set; } = true; // When true, RX bytes are logged through RxLogger.

        // Delegates that allow the UI to receive protocol events. frmMain sets these to append to rich text boxes.
        public Action<string>? TxLogger { get; set; }
        public Action<string>? RxLogger { get; set; }
        public Action<string>? DebugLogger { get; set; }

        // Pre-defined register ranges to request during initial read (mirrors Python data_matrix).
        private readonly List<(byte AddrMsb, byte AddrLsb, byte Length)> _dataMatrix = new()
        {
            // Each tuple describes a memory address (MSB/LSB) and how many bytes to read. These are
            // executed by Cmd(addrMsb, addrLsb, length) in ReadId when forceRefresh is requested.
            (0x00, 0x00, 0x02),
            (0x00, 0x02, 0x02),
            (0x00, 0x04, 0x05),
            (0x00, 0x0D, 0x04),
            (0x00, 0x11, 0x04),
            (0x00, 0x15, 0x04),
            (0x00, 0x19, 0x04),
            (0x00, 0x23, 0x14),
            (0x00, 0x37, 0x04),
            (0x00, 0x69, 0x02),
            (0x00, 0x7B, 0x01),
            (0x40, 0x00, 0x04),
            (0x40, 0x0A, 0x0A),
            (0x40, 0x14, 0x02),
            (0x40, 0x16, 0x02),
            (0x40, 0x19, 0x02),
            (0x40, 0x1B, 0x02),
            (0x40, 0x1D, 0x02),
            (0x40, 0x1F, 0x02),
            (0x60, 0x00, 0x02),
            (0x60, 0x02, 0x02),
            (0x60, 0x04, 0x04),
            (0x60, 0x08, 0x04),
            (0x60, 0x0C, 0x02),
            (0x90, 0x00, 0x3A),
            (0x90, 0x3A, 0x3A),
            (0x90, 0x74, 0x3A),
            (0x90, 0xAE, 0x3A),
            (0x90, 0xE8, 0x3A),
            (0x91, 0x22, 0x30),
            (0x91, 0x52, 0x00),
            (0xA0, 0x00, 0x06)
        };

        // Human-readable descriptions for register addresses used by HealthReport and diagnostic reads.
        private readonly List<(ushort Address, byte Length, string Type, string Label)> _dataId = new()
        {
            (0x0000, 2,  "uint",  "Cell type"),
            (0x0002, 2,  "uint",  "Unknown (always 0)"),
            (0x0004, 5,  "sn",    "Capacity & Serial number (?)"),
            (0x000D, 4,  "uint",  "Unknown (4th code?)"),
            (0x0011, 4,  "date",  "Manufacture date"),
            (0x0015, 4,  "date",  "Date of first charge (Forge)"),
            (0x0019, 4,  "date",  "Date of last charge (Forge)"),
            (0x0023, 20, "ascii", "Note (ascii string)"),
            (0x0037, 4,  "date",  "Current date"),
            (0x0069, 2,  "uint",  "Unknown (always 2)"),
            (0x007B, 1,  "uint",  "Unknown (always 0)"),
            (0x4000, 4,  "uint",  "Unknown (Forge)"),
            (0x400A, 10, "cell_v","Cell voltages (mV)"),
            (0x4014, 2,  "adc_t", "Temperature (C) (non-Forge)"),
            (0x4016, 2,  "uint",  "Unknown (Forge)"),
            (0x4019, 2,  "uint",  "Unknown (Forge)"),
            (0x401B, 2,  "uint",  "Unknown (Forge)"),
            (0x401D, 2,  "uint",  "Unknown (Forge)"),
            (0x401F, 2,  "dec_t", "Temperature (C) (Forge)"),
            (0x6000, 2,  "uint",  "Unknown (Forge)"),
            (0x6002, 2,  "uint",  "Unknown (Forge)"),
            (0x6004, 4,  "uint",  "Unknown (Forge)"),
            (0x6008, 4,  "uint",  "Unknown (Forge)"),
            (0x600C, 2,  "uint",  "Unknown (Forge)"),
            (0x9000, 4,  "date",  "Date of first charge (rounded)"),
            (0x9004, 4,  "date",  "Date of last tool use (rounded)"),
            (0x9008, 4,  "date",  "Date of last charge (rounded)"),
            (0x900C, 4,  "date",  "Unknown date (often zero)"),
            (0x9010, 2,  "uint",  "Days since first charge"),
            (0x9012, 4,  "uint",  "Total discharge (amp-sec)"),
            (0x9016, 4,  "uint",  "Total discharge (watt-sec or joules)"),
            (0x901A, 4,  "uint",  "Total charge count"),
            (0x901E, 2,  "uint",  "Dumb charge count (J2>7.1V for >=0.48s)"),
            (0x9020, 2,  "uint",  "Redlink (UART) charge count"),
            (0x9022, 2,  "uint",  "Completed charge count (?)"),
            (0x9024, 4,  "hhmmss","Total charging time (HH:MM:SS)"),
            (0x9028, 4,  "hhmmss","Time on charger whilst full (HH:MM:SS)"),
            (0x902C, 2,  "uint",  "Unknown (another low-voltage charge counter?)"),
            (0x902E, 2,  "uint",  "Charge started with a cell < 2.5V"),
            (0x9030, 2,  "uint",  "Discharge to empty"),
            (0x9032, 2,  "uint",  "Num. overheat on tool (must be > 10A)"),
            (0x9034, 2,  "uint",  "Overcurrent?"),
            (0x9036, 2,  "uint",  "Low voltage events)"),
            (0x9038, 2,  "uint",  "Low-voltage bounce? (4 flashing LEDs)"),
            (0x903A, 2,  "uint",  "Discharge @ 10-20A (seconds)"),
            (0x903C, 2,  "uint",  "          @ 20-30A (could be watts)"),
            (0x903E, 2,  "uint",  "          @ 30-40A      "),
            (0x9040, 2,  "uint",  "          @ 40-50A      "),
            (0x9042, 2,  "uint",  "          @ 50-60A      "),
            (0x9044, 2,  "uint",  "          @ 60-70A      "),
            (0x9046, 2,  "uint",  "          @ 70-80A      "),
            (0x9048, 2,  "uint",  "          @ 80-90A      "),
            (0x904A, 2,  "uint",  "          @ 90-100A     "),
            (0x904C, 2,  "uint",  "          @ 100-110A    "),
            (0x904E, 2,  "uint",  "          @ 110-120A    "),
            (0x9050, 2,  "uint",  "          @ 120-130A    "),
            (0x9052, 2,  "uint",  "          @ 130-140A    "),
            (0x9054, 2,  "uint",  "          @ 140-150A    "),
            (0x9056, 2,  "uint",  "          @ 150-160A    "),
            (0x9058, 2,  "uint",  "          @ 160-170A    "),
            (0x905A, 2,  "uint",  "          @ 170-180A    "),
            (0x905C, 2,  "uint",  "          @ 180-190A    "),
            (0x905E, 2,  "uint",  "          @ 190-200A    "),
            (0x9060, 2,  "uint",  "          @ 200-210A    "),
            (0x9062, 2,  "uint",  "Discharge @ 5-10A (seconds)"),
            (0x9064, 2,  "uint",  "          @ 10-15A (could be watts)"),
            (0x9066, 2,  "uint",  "          @ 15-20A (histo not well understood yet)"),
            (0x9068, 2,  "uint",  "          @ 20-25A      "),
            (0x906A, 2,  "uint",  "          @ 25-30A      "),
            (0x906C, 2,  "uint",  "          @ 30-35A      "),
            (0x906E, 2,  "uint",  "          @ 35-40A      "),
            (0x9070, 2,  "uint",  "          @ 40-45A      "),
            (0x9072, 2,  "uint",  "          @ 45-50A      "),
            (0x9074, 2,  "uint",  "          @ 50-55A      "),
            (0x9076, 2,  "uint",  "          @ 55-60A      "),
            (0x9078, 2,  "uint",  "          @ 60-65A      "),
            (0x907A, 2,  "uint",  "          @ 65-70A      "),
            (0x907C, 2,  "uint",  "          @ 70-65A      "),
            (0x907E, 2,  "uint",  "          @ 75-80A      "),
            (0x9080, 2,  "uint",  "          @ 80-85A      "),
            (0x9082, 2,  "uint",  "          @ 85-90A      "),
            (0x9084, 2,  "uint",  "          @ 90-95A      "),
            (0x9086, 2,  "uint",  "          @ 95-100A     "),
            (0x9088, 2,  "uint",  "          @ 100-105A    "),
            (0x908A, 2,  "uint",  "          @ 105-110A    "),
            (0x908C, 2,  "uint",  "          @ 110-115A    "),
            (0x908E, 2,  "uint",  "          @ 115-120A    "),
            (0x9090, 2,  "uint",  "          @ 120-125A    "),
            (0x9092, 2,  "uint",  "          @ 125-130A    "),
            (0x9094, 2,  "uint",  "          @ 130-135A    "),
            (0x9096, 2,  "uint",  "          @ 135-140A    "),
            (0x9098, 2,  "uint",  "          @ 140-145A    "),
            (0x909A, 2,  "uint",  "          @ 145-150A    "),
            (0x909C, 2,  "uint",  "          @ 150-155A    "),
            (0x909E, 2,  "uint",  "          @ 155-160A    "),
            (0x90A0, 2,  "uint",  "          @ 160-165A    "),
            (0x90A2, 2,  "uint",  "          @ 165-170A    "),
            (0x90A4, 2,  "uint",  "          @ 170-175A    "),
            (0x90A6, 2,  "uint",  "          @ 175-180A    "),
            (0x90A8, 2,  "uint",  "          @ 180-185A    "),
            (0x90AA, 2,  "uint",  "          @ 185-190A    "),
            (0x90AC, 2,  "uint",  "          @ 190-195A    "),
            (0x90AE, 2,  "uint",  "          @ 195-200A    "),
            (0x90B0, 2,  "uint",  "          @ 200A+       "),
            (0x90B2, 2,  "uint",  "Charge started < 17V"),
            (0x90B4, 2,  "uint",  "Charge started 17-18V"),
            (0x90B6, 2,  "uint",  "Charge started 18-19V"),
            (0x90B8, 2,  "uint",  "Charge started 19-20V"),
            (0x90BA, 2,  "uint",  "Charge started 20V+"),
            (0x90BC, 2,  "uint",  "Charge ended < 17V"),
            (0x90BE, 2,  "uint",  "Charge ended 17-18V"),
            (0x90C0, 2,  "uint",  "Charge ended 18-19V"),
            (0x90C2, 2,  "uint",  "Charge ended 19-20V"),
            (0x90C4, 2,  "uint",  "Charge ended 20V+"),
            (0x90C6, 2,  "uint",  "Charge start temp -30C to -20C"),
            (0x90C8, 2,  "uint",  "Charge start temp -20C to -10C"),
            (0x90CA, 2,  "uint",  "Charge start temp -10C to 0C"),
            (0x90CC, 2,  "uint",  "Charge start temp 0C to +10C"),
            (0x90CE, 2,  "uint",  "Charge start temp +10C to +20C"),
            (0x90D0, 2,  "uint",  "Charge start temp +20C to +30C"),
            (0x90D2, 2,  "uint",  "Charge start temp +30C to +40C"),
            (0x90D4, 2,  "uint",  "Charge start temp +40C to +50C"),
            (0x90D6, 2,  "uint",  "Charge start temp +50C to +60C"),
            (0x90D8, 2,  "uint",  "Charge start temp +60C to +70C"),
            (0x90DA, 2,  "uint",  "Charge start temp +70C to +80C"),
            (0x90DC, 2,  "uint",  "Charge start temp +80C and over"),
            (0x90DE, 2,  "uint",  "Charge end temp -30C to -20C"),
            (0x90E0, 2,  "uint",  "Charge end temp -20C to -10C"),
            (0x90E2, 2,  "uint",  "Charge end temp -10C to 0C"),
            (0x90E4, 2,  "uint",  "Charge end temp 0C to +10C"),
            (0x90E6, 2,  "uint",  "Charge end temp +10C to +20C"),
            (0x90E8, 2,  "uint",  "Charge end temp +20C to +30C"),
            (0x90EA, 2,  "uint",  "Charge end temp +30C to +40C"),
            (0x90EC, 2,  "uint",  "Charge end temp +40C to +50C"),
            (0x90EE, 2,  "uint",  "Charge end temp +50C to +60C"),
            (0x90F0, 2,  "uint",  "Charge end temp +60C to +70C"),
            (0x90F2, 2,  "uint",  "Charge end temp +70C to +80C"),
            (0x90F4, 2,  "uint",  "Charge end temp +80C and over"),
            (0xA000, 6,  "uint",  "Unknown (Forge)")
        };

        private readonly SerialPort _port;
        private bool _disposed;
        private bool? _savedPrintTx;
        private bool? _savedPrintRx;

        public SerialPort Port => _port; // Exposes the underlying SerialPort so UI can run diagnostic echo tests.

        /// <summary>
        /// Ensures the serial port is open and the protocol has not been disposed before performing
        /// operations. Returns false and logs a message when actions should be skipped.
        /// </summary>
        private bool EnsurePortOpen(string operation)
        {
            if (_disposed)
            {
                LogDebug($"{operation} skipped because protocol is disposed."); // Warn when caller uses disposed instance.
                return false; // Abort operation.
            }

            if (!_port.IsOpen)
            {
                LogDebug($"{operation} skipped because serial port {_port.PortName} is not open."); // Note closed port state.
                return false; // Abort because control lines cannot be toggled on a closed port.
            }

            return true; // Port is available.
        }

        /// <summary>
        /// Constructs the protocol handler, opens the specified serial port with correct UART
        /// settings (4800 8N2), and immediately drives TX to idle (BreakState/DTR true) to avoid
        /// sending unintended pulses to the battery. The optional <paramref name="debugLogger"/>
        /// allows the UI to capture initialization details.
        /// </summary>
        public M18Protocol(string portName, Action<string>? debugLogger = null)
        {
            DebugLogger = debugLogger; // Store logger delegate so we can write debug lines throughout the class.
            LogDebug($"Initializing protocol for port {portName}."); // Inform caller about port selection.
            _port = new SerialPort(portName, 4800, Parity.None, 8, StopBits.Two) // Configure UART to match battery protocol (4800 baud, 8 data bits, 2 stop bits).
            {
                ReadTimeout = 1200, // Set read timeout so calls to ReadByte() don't hang indefinitely.
                WriteTimeout = 800 // Set write timeout to fail fast if driver stalls.
            };

            LogDebug("Opening serial port..."); // Trace start of open call.
            _port.Open(); // Acquire OS handle; this toggles control lines to default states.
            LogDebug("Serial port opened. Setting TX to idle state."); // Notify that handle is open.
            Idle(); // Immediately assert BreakState/DTR to drive TX low; prevents spurious high pulses on battery pin.
            LogDebug("Protocol initialization complete."); // Signal completion.
        }

        public string PortName => _port.PortName; // Convenience property used by frmMain for display and comparisons.
        public bool IsOpen => _port.IsOpen; // Exposes port state so UI can check connectivity.

        /// <summary>
        /// Reverses the bit order of a byte (LSB to MSB) because the protocol transmits bytes with
        /// bits reversed relative to how they are represented in memory. This mirrors Python's
        /// reverse_bits function and is applied before sending/after receiving.
        /// </summary>
        public byte ReverseBits(byte value)
        {
            LogDebug($"ReverseBits called with value 0x{value:X2}."); // Trace incoming value.
            byte reversed = 0; // Start with zero so we can shift bits in.
            for (int i = 0; i < 8; i++)
            {
                reversed <<= 1; // Make room for next bit by shifting left.
                reversed |= (byte)((value >> i) & 0x01); // Extract bit i from original (LSB-first) and OR into result.
            }

            LogDebug($"ReverseBits returning 0x{reversed:X2}."); // Trace output for debugging.
            return reversed; // Return reversed byte used for MSB-first serial I/O.
        }

        /// <summary>
        /// Computes a simple additive checksum across the provided payload (sum of bytes, mod 2^16).
        /// The M18 protocol appends this 16-bit checksum to commands to help the battery validate
        /// data integrity.
        /// </summary>
        public int Checksum(IEnumerable<byte> payload)
        {
            if (payload == null)
            {
                LogDebug("Checksum called with null payload."); // Guard against null reference misuse.
                throw new ArgumentNullException(nameof(payload)); // Propagate argument error to caller.
            }

            LogDebug("Checksum calculation started."); // Trace operation.
            int checksum = 0; // Initialize accumulator.
            foreach (var b in payload)
            {
                checksum += b & 0xFFFF; // Add each byte value; mask not strictly necessary but keeps parity with Python implementation.
            }

            LogDebug($"Checksum calculation complete: {checksum & 0xFFFF}."); // Trace final checksum (16-bit value).
            return checksum; // Return full integer; caller masks to 16 bits when writing bytes.
        }

        /// <summary>
        /// Appends a 2-byte checksum to the provided payload (LSB first). The checksum is computed
        /// using <see cref="Checksum(IEnumerable{byte})"/> and returned as a new byte array ready for
        /// transmission over the UART.
        /// </summary>
        public byte[] AddChecksum(byte[] lsbCommand)
        {
            if (lsbCommand == null)
            {
                LogDebug("AddChecksum called with null lsbCommand."); // Alert misuse.
                throw new ArgumentNullException(nameof(lsbCommand)); // Throw to fail fast.
            }

            LogDebug($"AddChecksum called for payload length {lsbCommand.Length}."); // Trace size for debugging.
            int checksum = Checksum(lsbCommand); // Compute additive checksum.
            var withChecksum = new byte[lsbCommand.Length + 2]; // Allocate new array with two extra bytes.
            Buffer.BlockCopy(lsbCommand, 0, withChecksum, 0, lsbCommand.Length); // Copy original payload into new array.

            withChecksum[withChecksum.Length - 2] = (byte)((checksum >> 8) & 0xFF); // Append high byte of checksum.
            withChecksum[withChecksum.Length - 1] = (byte)(checksum & 0xFF); // Append low byte of checksum.

            LogDebug($"Checksum {checksum & 0xFFFF} appended. Final payload: {FormatBytes(withChecksum)}."); // Trace final buffer for logging panel.
            return withChecksum; // Return ready-to-send buffer (LSB-ordered before bit reversal).
        }

        /// <summary>
        /// Sends a prepared command (LSB bit order) over the serial port after reversing bits for
        /// MSB-first transmission. Also logs the TX bytes when requested.
        /// </summary>
        public void Send(byte[] command)
        {
            if (command == null)
            {
                LogDebug("Send called with null command."); // Guard against misuse.
                throw new ArgumentNullException(nameof(command)); // Fail fast to avoid null reference.
            }

            if (!EnsurePortOpen("Send"))
            {
                return; // Skip when port unavailable.
            }

            LogDebug($"Send called with command length {command.Length}. Raw payload: {FormatBytes(command)}."); // Trace outgoing payload.

            try
            {
                _port.DiscardInBuffer(); // Clear any stale incoming bytes to avoid mixing responses.
                LogDebug("Input buffer discarded prior to send."); // Confirm buffer clear.
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ObjectDisposedException)
            {
                LogDebug($"DiscardInBuffer skipped because port is not available: {ex.GetType().Name} - {ex.Message}"); // Log and exit if port not usable.
                return;
            }

            var msb = new byte[command.Length]; // Allocate buffer for bit-reversed bytes.
            for (int i = 0; i < command.Length; i++)
            {
                msb[i] = ReverseBits(command[i]); // Reverse each byte so bits are transmitted MSB-first as device expects.
            }

            if (PrintTx)
            {
                var builder = new StringBuilder(); // Build hex string for UI logging.
                for (int i = 0; i < command.Length; i++)
                {
                    builder.Append(command[i].ToString("X2")); // Append LSB-order byte for readability.
                    if (i < command.Length - 1)
                    {
                        builder.Append(' '); // Separate bytes with spaces for clarity.
                    }
                }

                var logMessage = $"TX: {builder}"; // Prefix with TX for disambiguation in UI.
                TxLogger?.Invoke(logMessage); // Notify UI logger delegate to show in rich text box.
                Console.WriteLine(logMessage); // Also emit to console (useful when running headless).
            }

            try
            {
                _port.Write(msb, 0, msb.Length); // Stream bytes over UART using FT232 driver; TX pin toggles bits on physical line.
                LogDebug($"Command sent over serial: {FormatBytes(msb)} (MSB)."); // Trace MSB-encoded payload.
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ObjectDisposedException)
            {
                LogDebug($"Send aborted because port is not available: {ex.GetType().Name} - {ex.Message}"); // Note failure without throwing to allow UI recovery.
            }
        }

        /// <summary>
        /// Adds a checksum to the provided command and transmits it via <see cref="Send(byte[])"/>.
        /// </summary>
        public void SendCommand(byte[] command)
        {
            LogDebug("SendCommand invoked."); // Trace wrapper usage.
            Send(AddChecksum(command)); // Compose checksumed payload then send.
        }

        /// <summary>
        /// Reads a response of the specified size, reversing bits back to LSB order and logging when
        /// enabled. Throws InvalidOperationException on timeout to mirror Python behavior.
        /// </summary>
        public byte[] ReadResponse(int size)
        {
            if (!EnsurePortOpen("ReadResponse"))
            {
                return Array.Empty<byte>(); // Return empty array when port unavailable.
            }

            LogDebug($"ReadResponse called with expected size {size}."); // Trace expected length.
            int firstByte;
            try
            {
                firstByte = _port.ReadByte(); // Block for first byte from RX line.
            }
            catch (InvalidOperationException ex)
            {
                LogDebug($"ReadResponse skipped because port is unavailable: {ex.GetType().Name} - {ex.Message}"); // Log closed/ disposed port.
                return Array.Empty<byte>(); // Return nothing to caller.
            }
            catch (TimeoutException)
            {
                LogDebug("ReadResponse timed out waiting for first byte."); // Trace timeout event.
                throw new InvalidOperationException("Empty response"); // Match Python exception semantics for caller.
            }

            if (firstByte < 0)
            {
                LogDebug("ReadResponse encountered invalid first byte (<0)."); // Negative return indicates stream ended.
                throw new InvalidOperationException("Empty response"); // Propagate error.
            }

            var msbResponse = new List<byte> { (byte)firstByte }; // Seed response with first MSB-ordered byte.
            int remaining = ReverseBits((byte)firstByte) == 0x82 ? 1 : Math.Max(0, size - 1); // If first LSB byte equals 0x82 (after reversing), expect one more byte; otherwise size-1.

            LogDebug($"First byte received (MSB): 0x{firstByte:X2}. Calculated remaining bytes to read: {remaining}."); // Trace first byte and expected remaining count.

            if (remaining > 0)
            {
                LogDebug($"Reading remaining {remaining} byte(s) from serial port."); // Note continuation.
                msbResponse.AddRange(ReadAvailable(remaining)); // Read remaining bytes using buffered helper.
            }

            var lsbResponse = msbResponse.Select(ReverseBits).ToArray(); // Convert MSB-ordered bytes back to LSB representation for processing.

            LogDebug($"Full response received (LSB): {FormatBytes(lsbResponse)}."); // Trace final decoded response.

            if (PrintRx)
            {
                var builder = new StringBuilder(); // Build hex string for logs.
                for (int i = 0; i < lsbResponse.Length; i++)
                {
                    builder.Append(lsbResponse[i].ToString("X2")); // Append each byte in hex.
                    if (i < lsbResponse.Length - 1)
                    {
                        builder.Append(' '); // Separate with space.
                    }
                }

                var logMessage = $"RX: {builder}"; // Prefix to indicate receive direction.
                RxLogger?.Invoke(logMessage); // Notify UI logger delegate.
                Console.WriteLine(logMessage); // Emit to console for CLI scenarios.
            }

            return lsbResponse; // Return decoded payload to caller for further parsing.
        }

        /// <summary>
        /// Issues a reset sequence to the battery by toggling Break/DTR (TX line) low/high and
        /// sending the SYNC byte. Retries up to three times and waits for SYNC acknowledgement.
        /// </summary>
        public bool Reset()
        {
            LogDebug("Reset invoked. Driving control lines to issue reset sequence."); // Trace action.
            Acc = 4; // Reset accumulator to default as in Python implementation.

            if (!EnsurePortOpen("Reset"))
            {
                return false; // Abort if port unavailable.
            }

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                LogDebug($"Reset attempt {attempt} starting."); // Indicate retry count.

                _port.DiscardInBuffer(); // Clear any pending bytes to avoid misinterpreting stale data.
                _port.BreakState = true; // Drive TX low (logic 0) to assert BREAK on physical line.
                _port.DtrEnable = true; // Assert DTR; both pins go low relative to idle high.
                Thread.Sleep(300); // Hold low for 300 ms to meet battery reset timing.

                _port.BreakState = false; // Release BREAK to drive TX high.
                _port.DtrEnable = false; // Release DTR to high.
                Thread.Sleep(300); // Hold high allowing BMS to recognize edge transition.

                Send(new[] { SYNC_BYTE }); // Transmit SYNC byte (0xAA) which the battery should echo.

                try
                {
                    LogDebug("Awaiting reset response after SYNC byte."); // Trace waiting state.
                    var response = ReadResponse(1); // Expect single byte echo.
                    var success = response.Length > 0 && response[0] == SYNC_BYTE; // Evaluate acknowledgement.
                    LogDebug($"Reset response {(success ? \"acknowledged\" : \"did not match expected SYNC\")}."); // Report result.
                    if (success)
                    {
                        Thread.Sleep(10); // Brief pause to stabilize before further commands.
                        return true; // Signal reset success.
                    }
                }
                catch (InvalidOperationException ex)
                {
                    LogDebug($"Reset attempt {attempt} failed: {ex.GetType().Name} - {ex.Message}"); // Report timeout or read issues.
                }

                Thread.Sleep(50); // Small delay before next retry to avoid hammering BMS.
            }

            LogDebug("All reset attempts exhausted without a response."); // Final failure message.
            return false; // Indicate reset failed.
        }

        /// <summary>
        /// Drives TX line low (idle state). On FT232 this sets BreakState=true and DtrEnable=true,
        /// effectively forcing the TX pin to a continuous logic 0 level and asserting DTR.
        /// </summary>
        public void Idle()
        {
            LogDebug("Setting TX to Idle (BreakState=true, DtrEnable=true)."); // Describe electrical intent.
            if (!EnsurePortOpen("Idle"))
            {
                return; // Abort if port not open.
            }
            _port.BreakState = true; // Assert BREAK -> TX held low (space).
            _port.DtrEnable = true; // Assert DTR -> control line low.
        }

        /// <summary>
        /// Drives TX line high (active state). On FT232 this clears BreakState and DtrEnable so the
        /// TX pin idles high (mark) like a charger presenting voltage on the comms pin.
        /// </summary>
        public void High()
        {
            LogDebug("Setting TX to Active (BreakState=false, DtrEnable=false)."); // Describe electrical intent.
            if (!EnsurePortOpen("High"))
            {
                return; // Abort if port not open.
            }
            _port.BreakState = false; // Release BREAK -> TX goes high.
            _port.DtrEnable = false; // Release DTR -> line returns to deasserted state.
        }

        /// <summary>
        /// Returns a textual summary of TX control-line states for debugging, including whether the
        /// serial port is open and whether the protocol has been disposed.
        /// </summary>
        public string GetTxStateSummary(string caller)
        {
            try
            {
                var disposedText = _disposed ? "disposed" : "active"; // Human-readable disposed state.
                var openState = _port.IsOpen ? "open" : "closed"; // Human-readable port state.
                if (!_port.IsOpen)
                {
                    return $"{caller}: Port {_port.PortName} is {openState} and protocol is {disposedText}; TX state unavailable."; // Indicate missing state when port closed.
                }

                return $"{caller}: Port {_port.PortName} is {openState} and protocol is {disposedText}. BreakState={_port.BreakState}, DtrEnable={_port.DtrEnable}."; // Provide detailed control-line flags.
            }
            catch (Exception ex)
            {
                LogDebug($"Error while retrieving TX state: {ex.GetType().Name} - {ex.Message}"); // Handle rare cases where SerialPort throws.
                return $"{caller}: TX state unavailable due to error."; // Graceful fallback.
            }
        }

        /// <summary>
        /// Gathers a detailed human-readable health report by reading multiple registers from the
        /// battery BMS. Temporarily disables TX/RX printing for performance, then restores settings.
        /// </summary>
        public string HealthReport()
        {
            if (_disposed)
            {
                return "Protocol disposed; health report unavailable."; // Prevent operations on disposed object.
            }

            LogDebug("Generating HealthReport summary."); // Trace start.
            var regList = new List<int>
            {
                4, 28, 25, 26, 12, 13, 18, 29, 39, 40, 41, 42, 43, 33, 32, 31, 35, 36, 38 // Predefined register indices capturing manufacturing data, temps, counters.
            };
            regList.AddRange(Enumerable.Range(44, 20)); // Add range covering tool-use histogram registers.
            regList.AddRange(new[] { 8, 2 }); // Append serial number and manufacture date indices.

            TxRxSaveAndSet(false); // Temporarily disable TX/RX logging to reduce noise during bulk reads.
            try
            {
                var values = ReadId(regList, true); // Perform forced refresh using Cmd calls to populate registers.
                if (values == null || values.Count != regList.Count)
                {
                    return "Health report failed: incomplete data returned."; // Early exit when data missing.
                }

                var builder = new StringBuilder();
                builder.AppendLine("Reading battery. This will take 5-10sec\n"); // Explain expected timing to user.

                var serialInfo = values[^1] as string ?? string.Empty; // Retrieve serial number string from last entry.
                var matches = Regex.Matches(serialInfo, "\\d+\\.?\\d*"); // Extract numeric tokens (battery type and e-serial).
                var batType = matches.Count > 0 ? matches[0].Value : ""; // Battery type code from response.
                var eSerial = matches.Count > 1 ? matches[1].Value : ""; // Electronic serial number.

                // Lookup table converting battery type codes to capacities and marketing descriptions.
                var batLookup = new Dictionary<string, (double Capacity, string Description)>
                {
                    {"36", (1.5, "1.5Ah CP (5s1p 18650)")},
                    {"37", (2, "2Ah CP (5s1p 18650)")},
                    {"38", (3, "3Ah XC (5s2p 18650)")},
                    {"39", (4, "4Ah XC (5s2p 18650)")},
                    {"40", (5, "5Ah XC (5s2p 18650) (<= Dec 2018)")},
                    {"165", (5, "5Ah XC (5s2p 18650) (Aug 2019 - Jun 2021)")},
                    {"306", (5, "5Ah XC (5s2p 18650) (Feb 2021 - Jul 2023)")},
                    {"424", (5, "5Ah XC (5s2p 18650) (>= Sep 2023)")},
                    {"46", (6, "6Ah XC (5s2p 18650)")},
                    {"47", (9, "9Ah HD (5s3p 18650)")},
                    {"104", (3, "3Ah HO (5s1p 21700)")},
                    {"106", (6, "6Ah HO (5s2p 21700)")},
                    {"107", (8, "8Ah HO (5s2p 21700)")},
                    {"108", (12, "12Ah HO (5s3p 21700)")},
                    {"383", (8, "8Ah Forge (5s2p 21700 tabless)")},
                    {"384", (12, "12Ah Forge (5s3p 21700 tabless)")}
                };

                var batteryDetails = batLookup.TryGetValue(batType, out var details) ? details : (0d, "Unknown"); // Default to unknown if type not in table.
                builder.AppendLine($"Type: {batType} [{batteryDetails.Item2}]"); // Show battery type and description.
                builder.AppendLine($"E-serial: {eSerial} (does NOT match case serial)"); // Clarify that electronic serial differs from label.

                var batNow = values[^2] as DateTime? ?? DateTime.UtcNow; // Use current timestamp from BMS if available, else fallback to UTC now.
                var manufactureDate = values[0] as DateTime?; // Manufacture date from register 0.
                if (manufactureDate.HasValue)
                {
                    builder.AppendLine($"Manufacture date: {manufactureDate:yyyy-MM-dd}"); // Format date for readability.
                }

                builder.AppendLine($"Days since 1st charge: {values[1]}"); // Raw counter from BMS.
                builder.AppendLine($"Days since last tool use: {(batNow - ((DateTime?)values[2] ?? batNow)).Days}"); // Compute days difference using BMS timestamp.
                builder.AppendLine($"Days since last charge: {(batNow - ((DateTime?)values[3] ?? batNow)).Days}"); // Similar calculation for last charge.

                if (values[4] is List<int> cellVoltages)
                {
                    var totalVoltage = cellVoltages.Sum() / 1000.0; // Convert total mV to V for readability.
                    builder.AppendLine($"Pack voltage: {totalVoltage}"); // Show pack voltage.
                    builder.AppendLine($"Cell Voltages (mV): {string.Join(\", \", cellVoltages)}"); // List individual cell voltages.
                    builder.AppendLine($"Cell Imbalance (mV): {cellVoltages.Max() - cellVoltages.Min()}"); // Compute imbalance as diagnostic metric.
                }

                if (values[5] != null)
                {
                    builder.AppendLine($"Temperature (deg C): {values[5]}"); // First temperature reading (non-Forge).
                }

                if (values[6] != null)
                {
                    builder.AppendLine($"Temperature (deg C): {values[6]}"); // Second temperature reading (Forge).
                }

                builder.AppendLine("\nCHARGING STATS:"); // Section header for charging metrics.
                builder.AppendLine($"Charge count [Redlink, dumb, (total)]: {values[13]}, {values[14]}, ({values[15]})"); // Show charge counters from BMS.
                builder.AppendLine($"Total charge time: {values[16]}"); // Aggregate charging duration.
                builder.AppendLine($"Time idling on charger: {values[17]}"); // Time spent at full charge on charger.
                builder.AppendLine($"Low-voltage charges (any cell <2.5V): {values[18]}"); // Count of deep-charge events.

                builder.AppendLine("\nTOOL USE STATS:"); // Section header for discharge metrics.
                var totalDischarge = Convert.ToDouble(values[7] ?? 0); // Retrieve total discharge in amp-seconds.
                builder.AppendLine($"Total discharge (Ah): {totalDischarge / 3600:0.00}"); // Convert to amp-hours (1 Ah = 3600 As).
                var totalDischargeCycles = batteryDetails.Item1 != 0
                    ? $"{totalDischarge / 3600 / batteryDetails.Item1:0.00}"
                    : "Unknown battery type, unable to calculate"; // Estimate equivalent full cycles using capacity.
                builder.AppendLine($"Total discharge cycles: {totalDischargeCycles}"); // Show calculated cycles or warning.
                builder.AppendLine($"Times discharged to empty: {values[8]}"); // Count of deep discharges.
                builder.AppendLine($"Times overheated: {values[9]}"); // Count of thermal events.
                builder.AppendLine($"Overcurrent events: {values[10]}"); // Count of current-limit trips.
                builder.AppendLine($"Low-voltage events: {values[11]}"); // Count of low-voltage warnings.
                builder.AppendLine($"Low-voltage bounce/stutter: {values[12]}"); // Count of repeated low-voltage flickers.

                var toolTime = Enumerable.Range(19, 20).Sum(i => Convert.ToInt32(values[i] ?? 0)); // Sum histogram buckets to compute total heavy-load time.
                builder.AppendLine($"Total time on tool (>10A): {TimeSpan.FromSeconds(toolTime)}"); // Convert seconds to TimeSpan for readability.

                for (int i = 19; i < 38; i++)
                {
                    var seconds = Convert.ToInt32(values[i] ?? 0); // Seconds spent in current band.
                    var pct = toolTime > 0 ? Math.Round(seconds / (double)toolTime * 100) : 0; // Percent of total tool time.
                    var bar = new string('X', (int)pct); // Simple ASCII bar graph showing relative time share.
                    var ampRange = $"{(i - 18) * 10}-{(i - 17) * 10}A"; // Human-readable current band (e.g., 10-20A).
                    builder.AppendLine($"Time @ {ampRange,8}: {TimeSpan.FromSeconds(seconds)} {pct,2:0}% {bar}"); // Log per-band usage.
                }

                var lastSeconds = Convert.ToInt32(values[38] ?? 0); // >200A bucket seconds.
                var lastPct = toolTime > 0 ? Math.Round(lastSeconds / (double)toolTime * 100) : 0; // Percent for final bucket.
                var lastBar = new string('X', (int)lastPct); // ASCII bar for final bucket.
                builder.AppendLine($"Time @ {\"> 200A\",8}: {TimeSpan.FromSeconds(lastSeconds)} {lastPct,2:0}% {lastBar}"); // Show extreme current usage.

                return builder.ToString().TrimEnd(); // Return compiled report with trailing newline trimmed.
            }
            catch (Exception ex)
            {
                LogDebug($"HealthReport failed: {ex.GetType().Name} - {ex.Message}"); // Trace exception for UI.
                return "Health report failed. Check battery is connected and you have correct serial port."; // Friendly error message.
            }
            finally
            {
                TxRxRestore(); // Restore TX/RX logging flags so UI settings persist.
            }
        }

        /// <summary>
        /// Reads a list of register IDs, optionally forcing a refresh by issuing pre-defined Cmd
        /// calls that load data into the battery's staging registers. Each register is parsed using
        /// metadata in <see cref="_dataId"/>.
        /// </summary>
        public List<object?>? ReadId(IEnumerable<int>? idArray, bool forceRefresh)
        {
            var ids = idArray?.ToList() ?? Enumerable.Range(0, _dataId.Count).ToList(); // Use provided IDs or default to all.
            var results = new List<object?>(); // Holds parsed values in same order as ids.

            try
            {
                Reset(); // Reset communication before reading to ensure clean state.

                if (forceRefresh)
                {
                    foreach (var (addrMsb, addrLsb, length) in _dataMatrix)
                    {
                        Cmd(addrMsb, addrLsb, length, (byte)(length + 5)); // Preload battery staging registers to update telemetry.
                    }
                    Idle(); // Return TX to safe idle state after batch.
                    Thread.Sleep(100); // Small delay to let BMS process staged reads.
                }

                Reset(); // Perform another reset so subsequent reads start from known state.
                foreach (var i in ids)
                {
                    if (i < 0 || i >= _dataId.Count)
                    {
                        results.Add(null); // Insert null for invalid indices to preserve order.
                        continue;
                    }

                    var (address, length, dataType, _) = _dataId[i]; // Retrieve metadata (address, length, type).
                    var addrMsb = (byte)((address >> 8) & 0xFF); // Extract high byte of address.
                    var addrLsb = (byte)(address & 0xFF); // Extract low byte of address.

                    var response = Cmd(addrMsb, addrLsb, (byte)length, (byte)(length + 5)); // Issue command to read register.
                    if (response.Length >= 4 && response[0] == 0x81) // 0x81 indicates valid response header.
                    {
                        var data = response.Skip(3).Take(length).ToArray(); // Skip header/checksum bytes to isolate payload.
                        results.Add(ParseData(data, dataType)); // Parse to appropriate .NET type (date, int, string).
                    }
                    else
                    {
                        results.Add(null); // Insert null when response not valid/complete.
                    }
                }

                Idle(); // Return TX to idle after reads.
                return results; // Deliver parsed list to caller.
            }
            catch (Exception ex)
            {
                LogDebug($"ReadId failed: {ex.GetType().Name} - {ex.Message}"); // Trace failure.
                return null; // Indicate failure to caller.
            }
        }

        /// <summary>
        /// Saves current TX/RX print flags and sets both to the provided value. Used to temporarily
        /// silence logging during bulk operations.
        /// </summary>
        public void TxRxSaveAndSet(bool value)
        {
            _savedPrintTx = PrintTx; // Cache current TX flag.
            _savedPrintRx = PrintRx; // Cache current RX flag.
            PrintTx = value; // Apply new value.
            PrintRx = value; // Apply new value.
        }

        /// <summary>
        /// Restores TX/RX print flags previously captured by <see cref="TxRxSaveAndSet"/>.
        /// </summary>
        public void TxRxRestore()
        {
            if (_savedPrintTx.HasValue)
            {
                PrintTx = _savedPrintTx.Value; // Restore cached TX flag.
                _savedPrintTx = null; // Clear cache.
            }

            if (_savedPrintRx.HasValue)
            {
                PrintRx = _savedPrintRx.Value; // Restore cached RX flag.
                _savedPrintRx = null; // Clear cache.
            }
        }

        /// <summary>
        /// Parses raw byte data returned from the battery according to the specified type string.
        /// Converts to ints, dates, ASCII strings, serial numbers, temperatures, or cell voltage lists.
        /// </summary>
        private object? ParseData(byte[] data, string dataType)
        {
            switch (dataType)
            {
                case "uint":
                    return data.Aggregate(0, (current, b) => (current << 8) + b); // Big-endian integer conversion.
                case "date":
                    var seconds = data.Aggregate(0L, (current, b) => (current << 8) + b); // Convert to Unix epoch seconds.
                    return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime; // Convert to UTC DateTime for display.
                case "hhmmss":
                    var duration = data.Aggregate(0, (current, b) => (current << 8) + b); // Convert to total seconds.
                    var mmss = TimeSpan.FromSeconds(duration); // Create TimeSpan to format HH:MM:SS.
                    return $"{(int)mmss.TotalHours}:{mmss.Minutes:00}:{mmss.Seconds:00}"; // Return formatted string.
                case "ascii":
                    return Encoding.UTF8.GetString(data); // Interpret payload as UTF-8 text.
                case "sn":
                    var btype = (data[0] << 8) + data[1]; // Battery type code from first two bytes.
                    var serial = (data[2] << 16) + (data[3] << 8) + data[4]; // 24-bit serial value from remaining bytes.
                    return $"Type: {btype,3}, Serial: {serial}"; // Compose friendly serial string.
                case "adc_t":
                    var adcValue = data.Aggregate(0, (current, b) => (current << 8) + b); // Convert ADC reading to integer.
                    return CalculateTemperature(adcValue); // Convert ADC to approximate temperature.
                case "dec_t":
                    return Math.Round(data[0] + data[1] / 256.0, 2); // Temperature stored as integer + fractional part (1/256).
                case "cell_v":
                    var cellVoltages = new List<int>(); // Prepare list for each cell voltage.
                    for (var i = 0; i < data.Length; i += 2)
                    {
                        cellVoltages.Add((data[i] << 8) + data[i + 1]); // Convert pair of bytes to mV reading.
                    }
                    return cellVoltages; // Return list of cell voltages.
                default:
                    return null; // Unknown type; caller will treat as missing.
            }
        }

        /// <summary>
        /// Approximates temperature in Celsius from an ADC value using a linear interpolation between
        /// two calibration points derived from the original Python script. Shows how to transform
        /// raw ADC counts into meaningful engineering units.
        /// </summary>
        private double CalculateTemperature(int adcValue)
        {
            const double r1 = 10e3; // Resistance reference point 1 (ohms).
            const double r2 = 20e3; // Resistance reference point 2 (ohms).
            const double t1 = 50; // Temperature corresponding to r1 (degrees Celsius).
            const double t2 = 35; // Temperature corresponding to r2 (degrees Celsius).

            const double adc1 = 0x0180; // ADC reading at r1.
            const double adc2 = 0x022E; // ADC reading at r2.

            var m = (t2 - t1) / (r2 - r1); // Calculate slope of temperature vs resistance.
            var b = t1 - m * r1; // Calculate intercept for linear model.

            var resistance = r1 + (adcValue - adc1) * (r2 - r1) / (adc2 - adc1); // Interpolate resistance from ADC reading.
            var temperature = m * resistance + b; // Convert resistance to temperature via line equation.

            return Math.Round(temperature, 2); // Round for display friendliness.
        }

        /// <summary>
        /// Closes the protocol by driving TX to idle, closing the SerialPort, and disposing managed
        /// resources. Safe to call multiple times thanks to the _disposed flag.
        /// </summary>
        public void Close()
        {
            LogDebug("Close invoked. Beginning disposal sequence."); // Trace start.
            if (_disposed)
            {
                LogDebug("Close skipped because object is already disposed."); // Avoid double disposal.
                return; // No work to do.
            }

            _disposed = true; // Mark disposed to prevent further I/O calls.

            try
            {
                Idle(); // Drive TX to safe idle state before closing port to avoid floating line.
            }
            catch (Exception ex)
            {
                LogDebug($"Error setting idle during close: {ex.GetType().Name} - {ex.Message}"); // Log but continue cleanup.
            }

            try
            {
                _port.Close(); // Close COM port handle.
            }
            catch (Exception ex)
            {
                LogDebug($"Error while closing serial port: {ex.GetType().Name} - {ex.Message}"); // Log any issues closing port.
            }
            finally
            {
                _port.Dispose(); // Release unmanaged resources held by SerialPort.
                LogDebug("Serial port disposed."); // Confirm disposal.
            }
        }

        /// <summary>
        /// Constructs and sends a register read command, then reads the expected number of bytes
        /// back. The command byte defaults to 0x01 but can be overridden for other operations.
        /// </summary>
        public byte[] Cmd(byte addrMsb, byte addrLsb, byte length, byte command = 0x01)
        {
            LogDebug($"Cmd invoked with addrMsb=0x{addrMsb:X2}, addrLsb=0x{addrLsb:X2}, length={length}, command=0x{command:X2}."); // Trace command parameters.
            SendCommand(new[]
            {
                command, // Command type (0x01 for read).
                (byte)0x04, // Unknown protocol constant from Python implementation (likely addressing mode).
                (byte)0x03, // Unknown protocol constant; part of command header.
                addrMsb, // High byte of register address.
                addrLsb, // Low byte of register address.
                length // Number of bytes to read.
            });

            return ReadResponse(length); // Read payload bytes; header/checksum processed in ReadResponse.
        }

        /// <summary>
        /// Reads the specified number of bytes from the SerialPort, aggregating partial reads until
        /// the requested count is met or a timeout/port error occurs.
        /// </summary>
        private IEnumerable<byte> ReadAvailable(int count)
        {
            LogDebug($"ReadAvailable attempting to read {count} byte(s)."); // Trace request.
            var buffer = new byte[count]; // Temporary buffer for incoming bytes.
            int totalRead = 0; // Track how many bytes we have read so far.

            while (totalRead < count)
            {
                try
                {
                    int read = _port.Read(buffer, totalRead, count - totalRead); // Attempt to fill remaining bytes.
                    if (read > 0)
                    {
                        totalRead += read; // Increment total with bytes read this iteration.
                        LogDebug($"Read {read} byte(s); total read so far: {totalRead}."); // Trace progress.
                      }
                }
                catch (InvalidOperationException ex)
                {
                    LogDebug($"ReadAvailable stopped because port is unavailable: {ex.GetType().Name} - {ex.Message}"); // Port closed/disposed; break loop.
                    break;
                }
                catch (TimeoutException)
                {
                    LogDebug("Timeout encountered while reading available bytes."); // Stop reading on timeout to avoid blocking forever.
                    break;
                }
            }

            var result = buffer.Take(totalRead).ToArray(); // Slice buffer down to bytes actually read.
            LogDebug($"ReadAvailable returning {result.Length} byte(s): {FormatBytes(result)}."); // Trace final chunk.
            return buffer.Take(totalRead).ToArray(); // Return trimmed array to caller.
        }

        /// <summary>
        /// Sends debug messages to whichever logger the UI attached. Keeps protocol agnostic of UI
        /// output mechanism.
        /// </summary>
        private void LogDebug(string message)
        {
            DebugLogger?.Invoke(message); // Only log when delegate supplied.
        }

        /// <summary>
        /// Formats a sequence of bytes as a space-delimited hex string (e.g., "AA 55 10").
        /// </summary>
        private static string FormatBytes(IEnumerable<byte> bytes)
        {
            if (bytes == null)
            {
                return string.Empty; // Gracefully handle null sequences.
            }

            var builder = new StringBuilder(); // Efficient string concatenation.
            var array = bytes.ToArray(); // Materialize enumerable to avoid multiple enumerations.

            for (int i = 0; i < array.Length; i++)
            {
                builder.Append(array[i].ToString("X2")); // Append byte as two-digit hex.
                if (i < array.Length - 1)
                {
                    builder.Append(' '); // Add space separator except after last byte.
                }
            }

            return builder.ToString(); // Return formatted string for logs.
        }
    }
}
