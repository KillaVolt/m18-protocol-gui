// *************************************************************************************************
// FtdiD2xxPort.cs
// ----------------
// Thin wrapper around the FTDI D2XX .NET API (FTD2XX_NET.dll) that mirrors the behaviour of
// Python's pyserial.Serial object used in m18.py. Every control-line toggle, baud configuration,
// purge, read, and write operation is forwarded to the equivalent D2XX call while emitting verbose
// log messages so the WinForms UI can show a "Raw D2XX Log" tab. The goal is a drop-in replacement
// for SerialPort that preserves timing and side effects of the original Python implementation.
// *************************************************************************************************

using System; // Provides basic types and exceptions.
using System.Collections.Generic; // Enables List<T> usage for byte collections.
using System.Linq; // LINQ helpers for hex string formatting.
using FTD2XX_NET; // Official FTDI D2XX managed wrapper used for all USB-UART interactions.

namespace M18BatteryInfo;

/// <summary>
/// Wraps an FTDI device opened through the D2XX API and exposes SerialPort-like methods used by the
/// protocol layer. Each method logs the corresponding D2XX call to maintain transparency between the
/// C# port and the reference m18.py behaviour.
/// </summary>
internal sealed class FtdiD2xxPort : IDisposable
{
    private readonly FTDI _ftdi = new(); // Underlying D2XX object controlling the FT232.
    private Action<string>? _rawLogger; // Optional logger to display each D2XX call.

    public FtdiDeviceDisplay Device { get; } // Selected FTDI device metadata used for logging.

    public bool IsOpen { get; private set; } // Tracks whether FT_Open succeeded.

    /// <summary>
    /// Constructs the port wrapper, opens the FTDI device by serial number, and configures the
    /// communication parameters (4800 8N2) to mirror pyserial settings in m18.py.
    /// </summary>
    public FtdiD2xxPort(FtdiDeviceDisplay device, Action<string>? rawLogger)
    {
        Device = device; // Store device metadata for logs and future reference.
        _rawLogger = rawLogger; // Save logger delegate for raw D2XX log tab.

        Log($"FT_OpenEx (by serial) -> {device.SerialNumber}"); // Trace open request.
        EnsureStatus(_ftdi.OpenBySerialNumber(device.SerialNumber), "FT_OpenEx"); // Open FTDI handle.
        IsOpen = true; // Mark handle as active so Dispose knows to close it.

        ConfigureForM18(); // Apply baud, framing, flow control, and timeouts identical to pyserial settings.
    }

    /// <summary>
    /// Sets baud rate, data characteristics, flow control, and timeouts to match the original
    /// Python Serial() constructor (4800 baud, 8 data bits, no parity, two stop bits, 800ms timeouts).
    /// </summary>
    private void ConfigureForM18()
    {
        Log("FT_SetBaudRate(4800)");
        EnsureStatus(_ftdi.SetBaudRate(4800), "FT_SetBaudRate(4800)");

        Log("FT_SetDataCharacteristics(8 data bits, 2 stop bits, no parity)");
        EnsureStatus(
            _ftdi.SetDataCharacteristics(
                FTDI.FT_DATA_BITS.FT_BITS_8,
                FTDI.FT_STOP_BITS.FT_STOP_BITS_2,
                FTDI.FT_PARITY.FT_PARITY_NONE),
            "FT_SetDataCharacteristics(8N2)");

        Log("FT_SetFlowControl(NONE)");
        EnsureStatus(_ftdi.SetFlowControl(FTDI.FT_FLOW_CONTROL.FT_FLOW_NONE, 0, 0), "FT_SetFlowControl(NONE)");

        Log("FT_SetTimeouts(Read=800ms, Write=800ms)");
        EnsureStatus(_ftdi.SetTimeouts(800, 800), "FT_SetTimeouts(800ms)");

        PurgeAll(); // Clear any residual data to mimic SerialPort.DiscardInBuffer/DiscardOutBuffer.
    }

    /// <summary>
    /// Drives BREAK on (TX low) or off (TX released) to mirror SerialPort.BreakState mutations.
    /// </summary>
    public void SetBreak(bool enable)
    {
        Log(enable ? "FT_SetBreakOn" : "FT_SetBreakOff");
        EnsureStatus(_ftdi.SetBreak(enable), enable ? "FT_SetBreakOn" : "FT_SetBreakOff");
    }

    /// <summary>
    /// Asserts or clears DTR using the explicit D2XX functions that map to SerialPort.DtrEnable.
    /// </summary>
    public void SetDtr(bool enable)
    {
        if (enable)
        {
            Log("FT_SetDtr");
            EnsureStatus(_ftdi.SetDTR(), "FT_SetDtr");
        }
        else
        {
            Log("FT_ClrDtr");
            EnsureStatus(_ftdi.ClrDTR(), "FT_ClrDtr");
        }
    }

    /// <summary>
    /// Purges both RX and TX buffers, matching SerialPort.DiscardInBuffer/DiscardOutBuffer.
    /// </summary>
    public void PurgeAll()
    {
        Log("FT_Purge(RX|TX)");
        EnsureStatus(_ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX), "FT_Purge(RX|TX)");
    }

    /// <summary>
    /// Purges only the receive buffer to clear stale bytes before a new transaction (send()).
    /// </summary>
    public void PurgeRx()
    {
        Log("FT_Purge(RX)");
        EnsureStatus(_ftdi.Purge(FTDI.FT_PURGE.FT_PURGE_RX), "FT_Purge(RX)");
    }

    /// <summary>
    /// Writes the provided bytes to the FT232 with the same ordering and timing as pyserial.write().
    /// </summary>
    public void WriteBytes(byte[] data)
    {
        uint bytesWritten = 0; // Will hold the number of bytes actually written by FT_Write.
        Log($"FT_Write {FormatHex(data)}");
        EnsureStatus(_ftdi.Write(data, data.Length, ref bytesWritten), "FT_Write");
        if (bytesWritten != data.Length)
        {
            throw new InvalidOperationException($"FT_Write wrote {bytesWritten} of {data.Length} bytes");
        }
    }

    /// <summary>
    /// Reads exactly <paramref name="count"/> bytes, respecting the configured 800ms timeout to
    /// mirror SerialPort.Read behaviour in the Python reference implementation.
    /// </summary>
    public byte[] ReadBytes(int count)
    {
        var buffer = new byte[count]; // Allocate destination buffer identical to SerialPort.Read(byte[],...).
        uint bytesRead = 0; // Tracks how many bytes the driver returned.
        var status = _ftdi.Read(buffer, count, ref bytesRead); // Perform blocking read via D2XX.
        Log($"FT_Read(count={count}) -> status={status}, bytesRead={bytesRead}, data={FormatHex(buffer.Take((int)bytesRead))}");
        if (status != FTDI.FT_STATUS.FT_OK || bytesRead == 0)
        {
            throw new InvalidOperationException($"FT_Read failed or returned zero bytes (status={status})");
        }

        if (bytesRead < count)
        {
            // Partial reads mirror SerialPort's ValueError condition in m18.py when response is shorter than expected.
            throw new InvalidOperationException($"FT_Read returned {bytesRead} of {count} bytes");
        }

        return buffer; // Return the full buffer to the caller.
    }

    /// <summary>
    /// Closes the FTDI handle, logging the underlying FT_Close call to keep parity with m18.py exit
    /// behaviour. Idempotent so callers can safely invoke multiple times.
    /// </summary>
    public void Close()
    {
        if (!IsOpen)
        {
            return; // Already closed or never opened.
        }

        Log("FT_Close");
        EnsureStatus(_ftdi.Close(), "FT_Close");
        IsOpen = false; // Mark handle closed to prevent duplicate closes.
    }

    /// <summary>
    /// Disposes the FTDI handle by calling <see cref="Close"/>.
    /// </summary>
    public void Dispose()
    {
        Close(); // Close FTDI handle when wrapper is disposed.
    }

    private void EnsureStatus(FTDI.FT_STATUS status, string operation)
    {
        if (status != FTDI.FT_STATUS.FT_OK)
        {
            throw new InvalidOperationException($"{operation} failed with status {status}");
        }
    }

    private void Log(string message)
    {
        _rawLogger?.Invoke(message); // Emit raw D2XX trace when logger provided.
    }

    /// <summary>
    /// Updates the raw logger delegate so callers can redirect FTDI call traces at runtime.
    /// </summary>
    public void UpdateLogger(Action<string>? logger)
    {
        _rawLogger = logger; // Swap logger used by Log().
    }

    private static string FormatHex(IEnumerable<byte> data)
    {
        return string.Join(" ", data.Select(b => $"0x{b:X2}")); // Formats byte arrays similar to m18.py debug.
    }
}
