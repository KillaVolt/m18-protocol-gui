using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;

namespace M18BatteryInfo
{
    /// <summary>
    /// C# port of the Python M18 protocol implementation.
    /// SerialPort.DtrEnable and SerialPort.BreakState map the DTR and BREAK
    /// control signals; on platforms where these lines are not supported, an
    /// equivalent control-line mechanism must be provided. Timing delays mirror
    /// the original behavior but may require platform-specific adjustments when
    /// interacting with different serial drivers.
    /// </summary>
    public class M18Protocol
    {
        public const byte SYNC_BYTE = 0xAA;
        public const byte CAL_CMD = 0x55;
        public const byte CONF_CMD = 0x60;
        public const byte SNAP_CMD = 0x61;
        public const byte KEEPALIVE_CMD = 0x62;

        public const int CUTOFF_CURRENT = 300;
        public const int MAX_CURRENT = 6000;

        public int Acc { get; private set; } = 4;

        public bool PrintTx { get; set; } = true;
        public bool PrintRx { get; set; } = true;

        public Action<string>? TxLogger { get; set; }
        public Action<string>? RxLogger { get; set; }
        public Action<string>? DebugLogger { get; set; }

        private readonly List<(byte AddrMsb, byte AddrLsb, byte Length)> _dataMatrix = new()
        {
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

        private bool EnsurePortOpen(string operation)
        {
            if (_disposed)
            {
                LogDebug($"{operation} skipped because protocol is disposed.");
                return false;
            }

            if (!_port.IsOpen)
            {
                LogDebug($"{operation} skipped because serial port {_port.PortName} is not open.");
                return false;
            }

            return true;
        }

        public M18Protocol(string portName, Action<string>? debugLogger = null)
        {
            DebugLogger = debugLogger;
            LogDebug($"Initializing protocol for port {portName}.");
            _port = new SerialPort(portName, 4800, Parity.None, 8, StopBits.Two)
            {
                ReadTimeout = 1200,
                WriteTimeout = 800
            };

            LogDebug("Opening serial port...");
            _port.Open();
            LogDebug("Serial port opened. Setting TX to idle state.");
            Idle();
            LogDebug("Protocol initialization complete.");
        }

        public string PortName => _port.PortName;
        public bool IsOpen => _port.IsOpen;

        public byte ReverseBits(byte value)
        {
            LogDebug($"ReverseBits called with value 0x{value:X2}.");
            byte reversed = 0;
            for (int i = 0; i < 8; i++)
            {
                reversed <<= 1;
                reversed |= (byte)((value >> i) & 0x01);
            }

            LogDebug($"ReverseBits returning 0x{reversed:X2}.");
            return reversed;
        }

        public int Checksum(IEnumerable<byte> payload)
        {
            if (payload == null)
            {
                LogDebug("Checksum called with null payload.");
                throw new ArgumentNullException(nameof(payload));
            }

            LogDebug("Checksum calculation started.");
            int checksum = 0;
            foreach (var b in payload)
            {
                checksum += b & 0xFFFF;
            }

            LogDebug($"Checksum calculation complete: {checksum & 0xFFFF}.");
            return checksum;
        }

        public byte[] AddChecksum(byte[] lsbCommand)
        {
            if (lsbCommand == null)
            {
                LogDebug("AddChecksum called with null lsbCommand.");
                throw new ArgumentNullException(nameof(lsbCommand));
            }

            LogDebug($"AddChecksum called for payload length {lsbCommand.Length}.");
            int checksum = Checksum(lsbCommand);
            var withChecksum = new byte[lsbCommand.Length + 2];
            Buffer.BlockCopy(lsbCommand, 0, withChecksum, 0, lsbCommand.Length);

            withChecksum[withChecksum.Length - 2] = (byte)((checksum >> 8) & 0xFF);
            withChecksum[withChecksum.Length - 1] = (byte)(checksum & 0xFF);

            LogDebug($"Checksum {checksum & 0xFFFF} appended. Final payload: {FormatBytes(withChecksum)}.");
            return withChecksum;
        }

        public void Send(byte[] command)
        {
            if (command == null)
            {
                LogDebug("Send called with null command.");
                throw new ArgumentNullException(nameof(command));
            }

            if (!EnsurePortOpen("Send"))
            {
                return;
            }

            LogDebug($"Send called with command length {command.Length}. Raw payload: {FormatBytes(command)}.");

            try
            {
                _port.DiscardInBuffer();
                LogDebug("Input buffer discarded prior to send.");
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ObjectDisposedException)
            {
                LogDebug($"DiscardInBuffer skipped because port is not available: {ex.GetType().Name} - {ex.Message}");
                return;
            }

            var msb = new byte[command.Length];
            for (int i = 0; i < command.Length; i++)
            {
                msb[i] = ReverseBits(command[i]);
            }

            if (PrintTx)
            {
                var builder = new StringBuilder();
                for (int i = 0; i < command.Length; i++)
                {
                    builder.Append(command[i].ToString("X2"));
                    if (i < command.Length - 1)
                    {
                        builder.Append(' ');
                    }
                }

                var logMessage = $"TX: {builder}";
                TxLogger?.Invoke(logMessage);
                Console.WriteLine(logMessage);
            }

            try
            {
                _port.Write(msb, 0, msb.Length);
                LogDebug($"Command sent over serial: {FormatBytes(msb)} (MSB).");
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is ObjectDisposedException)
            {
                LogDebug($"Send aborted because port is not available: {ex.GetType().Name} - {ex.Message}");
            }
        }

        public void SendCommand(byte[] command)
        {
            LogDebug("SendCommand invoked.");
            Send(AddChecksum(command));
        }

        public byte[] ReadResponse(int size)
        {
            if (!EnsurePortOpen("ReadResponse"))
            {
                return Array.Empty<byte>();
            }

            LogDebug($"ReadResponse called with expected size {size}.");
            int firstByte;
            try
            {
                firstByte = _port.ReadByte();
            }
            catch (InvalidOperationException ex)
            {
                LogDebug($"ReadResponse skipped because port is unavailable: {ex.GetType().Name} - {ex.Message}");
                return Array.Empty<byte>();
            }
            catch (TimeoutException)
            {
                LogDebug("ReadResponse timed out waiting for first byte.");
                throw new InvalidOperationException("Empty response");
            }

            if (firstByte < 0)
            {
                LogDebug("ReadResponse encountered invalid first byte (<0).");
                throw new InvalidOperationException("Empty response");
            }

            var msbResponse = new List<byte> { (byte)firstByte };
            int remaining = ReverseBits((byte)firstByte) == 0x82 ? 1 : Math.Max(0, size - 1);

            LogDebug($"First byte received (MSB): 0x{firstByte:X2}. Calculated remaining bytes to read: {remaining}.");

            if (remaining > 0)
            {
                LogDebug($"Reading remaining {remaining} byte(s) from serial port.");
                msbResponse.AddRange(ReadAvailable(remaining));
            }

            var lsbResponse = msbResponse.Select(ReverseBits).ToArray();

            LogDebug($"Full response received (LSB): {FormatBytes(lsbResponse)}.");

            if (PrintRx)
            {
                var builder = new StringBuilder();
                for (int i = 0; i < lsbResponse.Length; i++)
                {
                    builder.Append(lsbResponse[i].ToString("X2"));
                    if (i < lsbResponse.Length - 1)
                    {
                        builder.Append(' ');
                    }
                }

                var logMessage = $"RX: {builder}";
                RxLogger?.Invoke(logMessage);
                Console.WriteLine(logMessage);
            }

            return lsbResponse;
        }

        public bool Reset()
        {
            LogDebug("Reset invoked. Driving control lines to issue reset sequence.");
            Acc = 4;

            if (!EnsurePortOpen("Reset"))
            {
                return false;
            }

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                LogDebug($"Reset attempt {attempt} starting.");

                _port.DiscardInBuffer();
                _port.BreakState = true;
                _port.DtrEnable = true;
                Thread.Sleep(300);

                _port.BreakState = false;
                _port.DtrEnable = false;
                Thread.Sleep(300);

                Send(new[] { SYNC_BYTE });

                try
                {
                    LogDebug("Awaiting reset response after SYNC byte.");
                    var response = ReadResponse(1);
                    var success = response.Length > 0 && response[0] == SYNC_BYTE;
                    LogDebug($"Reset response {(success ? "acknowledged" : "did not match expected SYNC")}.");
                    if (success)
                    {
                        Thread.Sleep(10);
                        return true;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    LogDebug($"Reset attempt {attempt} failed: {ex.GetType().Name} - {ex.Message}");
                }

                Thread.Sleep(50);
            }

            LogDebug("All reset attempts exhausted without a response.");
            return false;
        }

        public void Idle()
        {
            LogDebug("Setting TX to Idle (BreakState=true, DtrEnable=true).");
            if (!EnsurePortOpen("Idle"))
            {
                return;
            }
            _port.BreakState = true;
            _port.DtrEnable = true;
        }

        public void High()
        {
            LogDebug("Setting TX to Active (BreakState=false, DtrEnable=false).");
            if (!EnsurePortOpen("High"))
            {
                return;
            }
            _port.BreakState = false;
            _port.DtrEnable = false;
        }

        public string GetTxStateSummary(string caller)
        {
            try
            {
                var disposedText = _disposed ? "disposed" : "active";
                var openState = _port.IsOpen ? "open" : "closed";
                if (!_port.IsOpen)
                {
                    return $"{caller}: Port {_port.PortName} is {openState} and protocol is {disposedText}; TX state unavailable.";
                }

                return $"{caller}: Port {_port.PortName} is {openState} and protocol is {disposedText}. BreakState={_port.BreakState}, DtrEnable={_port.DtrEnable}.";
            }
            catch (Exception ex)
            {
                LogDebug($"Error while retrieving TX state: {ex.GetType().Name} - {ex.Message}");
                return $"{caller}: TX state unavailable due to error.";
            }
        }

        public string HealthReport()
        {
            if (_disposed)
            {
                return "Protocol disposed; health report unavailable.";
            }

            LogDebug("Generating HealthReport summary.");
            var regList = new List<int>
            {
                4, 28, 25, 26, 12, 13, 18, 29, 39, 40, 41, 42, 43, 33, 32, 31, 35, 36, 38
            };
            regList.AddRange(Enumerable.Range(44, 20));
            regList.AddRange(new[] { 8, 2 });

            TxRxSaveAndSet(false);
            try
            {
                var values = ReadId(regList, true);
                if (values == null || values.Count != regList.Count)
                {
                    return "Health report failed: incomplete data returned.";
                }

                var builder = new StringBuilder();
                builder.AppendLine("Reading battery. This will take 5-10sec\n");

                var serialInfo = values[^1] as string ?? string.Empty;
                var matches = Regex.Matches(serialInfo, "\\d+\\.?\\d*");
                var batType = matches.Count > 0 ? matches[0].Value : "";
                var eSerial = matches.Count > 1 ? matches[1].Value : "";

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

                var batteryDetails = batLookup.TryGetValue(batType, out var details) ? details : (0d, "Unknown");
                builder.AppendLine($"Type: {batType} [{batteryDetails.Item2}]");
                builder.AppendLine($"E-serial: {eSerial} (does NOT match case serial)");

                var batNow = values[^2] as DateTime? ?? DateTime.UtcNow;
                var manufactureDate = values[0] as DateTime?;
                if (manufactureDate.HasValue)
                {
                    builder.AppendLine($"Manufacture date: {manufactureDate:yyyy-MM-dd}");
                }

                builder.AppendLine($"Days since 1st charge: {values[1]}");
                builder.AppendLine($"Days since last tool use: {(batNow - ((DateTime?)values[2] ?? batNow)).Days}");
                builder.AppendLine($"Days since last charge: {(batNow - ((DateTime?)values[3] ?? batNow)).Days}");

                if (values[4] is List<int> cellVoltages)
                {
                    var totalVoltage = cellVoltages.Sum() / 1000.0;
                    builder.AppendLine($"Pack voltage: {totalVoltage}");
                    builder.AppendLine($"Cell Voltages (mV): {string.Join(", ", cellVoltages)}");
                    builder.AppendLine($"Cell Imbalance (mV): {cellVoltages.Max() - cellVoltages.Min()}");
                }

                if (values[5] != null)
                {
                    builder.AppendLine($"Temperature (deg C): {values[5]}");
                }

                if (values[6] != null)
                {
                    builder.AppendLine($"Temperature (deg C): {values[6]}");
                }

                builder.AppendLine("\nCHARGING STATS:");
                builder.AppendLine($"Charge count [Redlink, dumb, (total)]: {values[13]}, {values[14]}, ({values[15]})");
                builder.AppendLine($"Total charge time: {values[16]}");
                builder.AppendLine($"Time idling on charger: {values[17]}");
                builder.AppendLine($"Low-voltage charges (any cell <2.5V): {values[18]}");

                builder.AppendLine("\nTOOL USE STATS:");
                var totalDischarge = Convert.ToDouble(values[7] ?? 0);
                builder.AppendLine($"Total discharge (Ah): {totalDischarge / 3600:0.00}");
                var totalDischargeCycles = batteryDetails.Item1 != 0
                    ? $"{totalDischarge / 3600 / batteryDetails.Item1:0.00}"
                    : "Unknown battery type, unable to calculate";
                builder.AppendLine($"Total discharge cycles: {totalDischargeCycles}");
                builder.AppendLine($"Times discharged to empty: {values[8]}");
                builder.AppendLine($"Times overheated: {values[9]}");
                builder.AppendLine($"Overcurrent events: {values[10]}");
                builder.AppendLine($"Low-voltage events: {values[11]}");
                builder.AppendLine($"Low-voltage bounce/stutter: {values[12]}");

                var toolTime = Enumerable.Range(19, 20).Sum(i => Convert.ToInt32(values[i] ?? 0));
                builder.AppendLine($"Total time on tool (>10A): {TimeSpan.FromSeconds(toolTime)}");

                for (int i = 19; i < 38; i++)
                {
                    var seconds = Convert.ToInt32(values[i] ?? 0);
                    var pct = toolTime > 0 ? Math.Round(seconds / (double)toolTime * 100) : 0;
                    var bar = new string('X', (int)pct);
                    var ampRange = $"{(i - 18) * 10}-{(i - 17) * 10}A";
                    builder.AppendLine($"Time @ {ampRange,8}: {TimeSpan.FromSeconds(seconds)} {pct,2:0}% {bar}");
                }

                var lastSeconds = Convert.ToInt32(values[38] ?? 0);
                var lastPct = toolTime > 0 ? Math.Round(lastSeconds / (double)toolTime * 100) : 0;
                var lastBar = new string('X', (int)lastPct);
                builder.AppendLine($"Time @ {"> 200A",8}: {TimeSpan.FromSeconds(lastSeconds)} {lastPct,2:0}% {lastBar}");

                return builder.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                LogDebug($"HealthReport failed: {ex.GetType().Name} - {ex.Message}");
                return "Health report failed. Check battery is connected and you have correct serial port.";
            }
            finally
            {
                TxRxRestore();
            }
        }

        public List<object?>? ReadId(IEnumerable<int>? idArray, bool forceRefresh)
        {
            var ids = idArray?.ToList() ?? Enumerable.Range(0, _dataId.Count).ToList();
            var results = new List<object?>();

            try
            {
                Reset();

                if (forceRefresh)
                {
                    foreach (var (addrMsb, addrLsb, length) in _dataMatrix)
                    {
                        Cmd(addrMsb, addrLsb, length, (byte)(length + 5));
                    }
                    Idle();
                    Thread.Sleep(100);
                }

                Reset();
                foreach (var i in ids)
                {
                    if (i < 0 || i >= _dataId.Count)
                    {
                        results.Add(null);
                        continue;
                    }

                    var (address, length, dataType, _) = _dataId[i];
                    var addrMsb = (byte)((address >> 8) & 0xFF);
                    var addrLsb = (byte)(address & 0xFF);

                    var response = Cmd(addrMsb, addrLsb, (byte)length, (byte)(length + 5));
                    if (response.Length >= 4 && response[0] == 0x81)
                    {
                        var data = response.Skip(3).Take(length).ToArray();
                        results.Add(ParseData(data, dataType));
                    }
                    else
                    {
                        results.Add(null);
                    }
                }

                Idle();
                return results;
            }
            catch (Exception ex)
            {
                LogDebug($"ReadId failed: {ex.GetType().Name} - {ex.Message}");
                return null;
            }
        }

        public void TxRxSaveAndSet(bool value)
        {
            _savedPrintTx = PrintTx;
            _savedPrintRx = PrintRx;
            PrintTx = value;
            PrintRx = value;
        }

        public void TxRxRestore()
        {
            if (_savedPrintTx.HasValue)
            {
                PrintTx = _savedPrintTx.Value;
                _savedPrintTx = null;
            }

            if (_savedPrintRx.HasValue)
            {
                PrintRx = _savedPrintRx.Value;
                _savedPrintRx = null;
            }
        }

        private object? ParseData(byte[] data, string dataType)
        {
            switch (dataType)
            {
                case "uint":
                    return data.Aggregate(0, (current, b) => (current << 8) + b);
                case "date":
                    var seconds = data.Aggregate(0L, (current, b) => (current << 8) + b);
                    return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
                case "hhmmss":
                    var duration = data.Aggregate(0, (current, b) => (current << 8) + b);
                    var mmss = TimeSpan.FromSeconds(duration);
                    return $"{(int)mmss.TotalHours}:{mmss.Minutes:00}:{mmss.Seconds:00}";
                case "ascii":
                    return Encoding.UTF8.GetString(data);
                case "sn":
                    var btype = (data[0] << 8) + data[1];
                    var serial = (data[2] << 16) + (data[3] << 8) + data[4];
                    return $"Type: {btype,3}, Serial: {serial}";
                case "adc_t":
                    var adcValue = data.Aggregate(0, (current, b) => (current << 8) + b);
                    return CalculateTemperature(adcValue);
                case "dec_t":
                    return Math.Round(data[0] + data[1] / 256.0, 2);
                case "cell_v":
                    var cellVoltages = new List<int>();
                    for (var i = 0; i < data.Length; i += 2)
                    {
                        cellVoltages.Add((data[i] << 8) + data[i + 1]);
                    }
                    return cellVoltages;
                default:
                    return null;
            }
        }

        private double CalculateTemperature(int adcValue)
        {
            const double r1 = 10e3;
            const double r2 = 20e3;
            const double t1 = 50;
            const double t2 = 35;

            const double adc1 = 0x0180;
            const double adc2 = 0x022E;

            var m = (t2 - t1) / (r2 - r1);
            var b = t1 - m * r1;

            var resistance = r1 + (adcValue - adc1) * (r2 - r1) / (adc2 - adc1);
            var temperature = m * resistance + b;

            return Math.Round(temperature, 2);
        }

        public void Close()
        {
            LogDebug("Close invoked. Beginning disposal sequence.");
            if (_disposed)
            {
                LogDebug("Close skipped because object is already disposed.");
                return;
            }

            _disposed = true;

            try
            {
                Idle();
            }
            catch (Exception ex)
            {
                LogDebug($"Error setting idle during close: {ex.GetType().Name} - {ex.Message}");
            }

            try
            {
                _port.Close();
            }
            catch (Exception ex)
            {
                LogDebug($"Error while closing serial port: {ex.GetType().Name} - {ex.Message}");
            }
            finally
            {
                _port.Dispose();
                LogDebug("Serial port disposed.");
            }
        }

        public byte[] Cmd(byte addrMsb, byte addrLsb, byte length, byte command = 0x01)
        {
            LogDebug($"Cmd invoked with addrMsb=0x{addrMsb:X2}, addrLsb=0x{addrLsb:X2}, length={length}, command=0x{command:X2}.");
            SendCommand(new[]
            {
                command,
                (byte)0x04,
                (byte)0x03,
                addrMsb,
                addrLsb,
                length
            });

            return ReadResponse(length);
        }

        private IEnumerable<byte> ReadAvailable(int count)
        {
            LogDebug($"ReadAvailable attempting to read {count} byte(s).");
            var buffer = new byte[count];
            int totalRead = 0;

            while (totalRead < count)
            {
                try
                {
                    int read = _port.Read(buffer, totalRead, count - totalRead);
                    if (read > 0)
                    {
                        totalRead += read;
                        LogDebug($"Read {read} byte(s); total read so far: {totalRead}.");
                      }
                }
                catch (InvalidOperationException ex)
                {
                    LogDebug($"ReadAvailable stopped because port is unavailable: {ex.GetType().Name} - {ex.Message}");
                    break;
                }
                catch (TimeoutException)
                {
                    LogDebug("Timeout encountered while reading available bytes.");
                    break;
                }
            }

            var result = buffer.Take(totalRead).ToArray();
            LogDebug($"ReadAvailable returning {result.Length} byte(s): {FormatBytes(result)}.");
            return buffer.Take(totalRead).ToArray();
        }

        private void LogDebug(string message)
        {
            DebugLogger?.Invoke(message);
        }

        private static string FormatBytes(IEnumerable<byte> bytes)
        {
            if (bytes == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            var array = bytes.ToArray();

            for (int i = 0; i < array.Length; i++)
            {
                builder.Append(array[i].ToString("X2"));
                if (i < array.Length - 1)
                {
                    builder.Append(' ');
                }
            }

            return builder.ToString();
        }
    }
}
