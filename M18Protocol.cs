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

        public bool PrintTx { get; set; }
        public bool PrintRx { get; set; }

        private readonly SerialPort _port;

        public M18Protocol(string portName)
        {
            _port = new SerialPort(portName, 4800, Parity.None, 8, StopBits.Two)
            {
                ReadTimeout = 800,
                WriteTimeout = 800
            };

            _port.Open();
            Idle();
        }

        public byte ReverseBits(byte value)
        {
            byte reversed = 0;
            for (int i = 0; i < 8; i++)
            {
                reversed <<= 1;
                reversed |= (byte)((value >> i) & 0x01);
            }

            return reversed;
        }

        public int Checksum(IEnumerable<byte> payload)
        {
            int checksum = 0;
            foreach (var b in payload)
            {
                checksum += b & 0xFFFF;
            }

            return checksum;
        }

        public byte[] AddChecksum(byte[] lsbCommand)
        {
            if (lsbCommand == null)
            {
                throw new ArgumentNullException(nameof(lsbCommand));
            }

            int checksum = Checksum(lsbCommand);
            var withChecksum = new byte[lsbCommand.Length + 2];
            Buffer.BlockCopy(lsbCommand, 0, withChecksum, 0, lsbCommand.Length);

            withChecksum[withChecksum.Length - 2] = (byte)((checksum >> 8) & 0xFF);
            withChecksum[withChecksum.Length - 1] = (byte)(checksum & 0xFF);

            return withChecksum;
        }

        public void Send(byte[] command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            _port.DiscardInBuffer();

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

                Console.WriteLine($"TX: {builder}");
            }

            _port.Write(msb, 0, msb.Length);
        }

        public void SendCommand(byte[] command)
        {
            Send(AddChecksum(command));
        }

        public byte[] ReadResponse(int size)
        {
            int firstByte;
            try
            {
                firstByte = _port.ReadByte();
            }
            catch (TimeoutException)
            {
                throw new InvalidOperationException("Empty response");
            }

            if (firstByte < 0)
            {
                throw new InvalidOperationException("Empty response");
            }

            var msbResponse = new List<byte> { (byte)firstByte };
            int remaining = ReverseBits((byte)firstByte) == 0x82 ? 1 : Math.Max(0, size - 1);

            if (remaining > 0)
            {
                msbResponse.AddRange(ReadAvailable(remaining));
            }

            var lsbResponse = msbResponse.Select(ReverseBits).ToArray();

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

                Console.WriteLine($"RX: {builder}");
            }

            return lsbResponse;
        }

        public bool Reset()
        {
            Acc = 4;

            _port.BreakState = true;
            _port.DtrEnable = true;
            Thread.Sleep(300);

            _port.BreakState = false;
            _port.DtrEnable = false;
            Thread.Sleep(300);

            Send(new[] { SYNC_BYTE });

            try
            {
                var response = ReadResponse(1);
                return response.Length > 0 && response[0] == SYNC_BYTE;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public void Idle()
        {
            _port.BreakState = true;
            _port.DtrEnable = true;
        }

        public void High()
        {
            _port.BreakState = false;
            _port.DtrEnable = false;
        }

        public byte[] Cmd(byte addrMsb, byte addrLsb, byte length, byte command = 0x01)
        {
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
                    }
                }
                catch (TimeoutException)
                {
                    break;
                }
            }

            return buffer.Take(totalRead).ToArray();
        }
    }
}
