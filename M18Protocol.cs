using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;

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

        private readonly SerialPort _port;
        private bool _disposed;

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
                ReadTimeout = 800,
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
                return success;
            }
            catch (InvalidOperationException ex)
            {
                LogDebug($"Reset failed with exception: {ex.GetType().Name} - {ex.Message}");
                return false;
            }
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
            var builder = new StringBuilder();

            builder.AppendLine($"Port: {_port.PortName}");
            builder.AppendLine($"Port open: {_port.IsOpen}");
            builder.AppendLine("Basic health report functionality is limited in this build.");
            builder.AppendLine("Connect to a battery pack to retrieve detailed statistics.");

            return builder.ToString().TrimEnd();
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
                catch (ObjectDisposedException ex)
                {
                    LogDebug($"ReadAvailable stopped because port is disposed: {ex.GetType().Name} - {ex.Message}");
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
