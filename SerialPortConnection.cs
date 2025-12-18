// *************************************************************************************************
// SerialPortConnection.cs
// -----------------------
// Wraps System.IO.Ports.SerialPort with helper methods that mirror the subset of operations needed
// by M18Protocol: opening the port with specific framing, toggling BREAK/DTR, purging buffers, and
// reading/writing exact byte counts. The wrapper also supports an optional raw logger to surface
// low-level serial actions to the UI without exposing SerialPort directly.
// *************************************************************************************************

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;

namespace M18BatteryInfo;

internal sealed class SerialPortConnection : IDisposable
{
    private readonly SerialPort _serialPort;
    private Action<string>? _rawLogger;

    public SerialPortConnection(SerialPortDisplay device, Action<string>? rawLogger)
    {
        Device = device;
        _rawLogger = rawLogger;

        _serialPort = new SerialPort(device.PortName, 4800, Parity.None, 8, StopBits.Two)
        {
            ReadTimeout = 800,
            WriteTimeout = 800,
            DtrEnable = false,
            RtsEnable = false
        };

        Log($"Opening {device.PortName} at 4800 8N2");
        _serialPort.Open();
        PurgeAll();
    }

    public SerialPortDisplay Device { get; }

    public bool IsOpen => _serialPort.IsOpen;

    public void SetBreak(bool enable)
    {
        _serialPort.BreakState = enable;
        Log($"SerialPort.BreakState={(enable ? "On" : "Off")}");
    }

    public void SetDtr(bool enable)
    {
        _serialPort.DtrEnable = enable;
        Log($"SerialPort.DtrEnable={(enable ? "On" : "Off")}");
    }

    public void PurgeAll()
    {
        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();
        Log("SerialPort.DiscardInBuffer/DiscardOutBuffer");
    }

    public void PurgeRx()
    {
        _serialPort.DiscardInBuffer();
        Log("SerialPort.DiscardInBuffer");
    }

    public void WriteBytes(byte[] data)
    {
        _serialPort.Write(data, 0, data.Length);
        Log($"SerialPort.Write {FormatHex(data)}");
    }

    public byte[] ReadBytes(int count)
    {
        var buffer = new byte[count];
        var totalRead = 0;
        try
        {
            while (totalRead < count)
            {
                var bytesRead = _serialPort.Read(buffer, totalRead, count - totalRead);
                totalRead += bytesRead;
            }
        }
        catch (TimeoutException ex)
        {
            throw new InvalidOperationException($"SerialPort.Read timed out after reading {totalRead} of {count} byte(s)", ex);
        }

        Log($"SerialPort.Read count={count}, bytesRead={totalRead}, data={FormatHex(buffer.Take(totalRead))}");

        if (totalRead < count)
        {
            throw new InvalidOperationException($"SerialPort.Read returned {totalRead} of {count} byte(s)");
        }

        return buffer;
    }

    public void Close()
    {
        if (!_serialPort.IsOpen)
        {
            return;
        }

        Log("SerialPort.Close");
        _serialPort.Close();
    }

    public void Dispose()
    {
        Close();
        _serialPort.Dispose();
    }

    public void UpdateLogger(Action<string>? logger)
    {
        _rawLogger = logger;
    }

    private void Log(string message)
    {
        _rawLogger?.Invoke(message);
    }

    private static string FormatHex(IEnumerable<byte> data)
    {
        return string.Join(" ", data.Select(b => $"0x{b:X2}"));
    }
}
