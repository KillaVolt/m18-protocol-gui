// *************************************************************************************************
// FtdiDeviceUtil.cs
// ------------------
// Enumerates FTDI devices using the official D2XX .NET wrapper (FTD2XX_NET.dll). The helper mirrors
// pyserial's list_ports output by pairing each FTDI node with SetupAPI metadata (manufacturer,
// location path, optional COM port mapping) from SerialPortUtil. This ensures the WinForms UI can
// show human-readable device descriptions while all communications are routed through the D2XX API
// rather than System.IO.Ports.SerialPort.
// *************************************************************************************************

using System; // Basic .NET primitives.
using System.Collections.Generic; // List<T> for returning device results.
using System.Linq; // LINQ helpers to correlate FTDI serial numbers with SetupAPI metadata.
using FTD2XX_NET; // Official managed wrapper around the FTDI D2XX driver.

namespace M18BatteryInfo;

/// <summary>
/// Utility that queries the FTDI D2XX driver for attached USB-UART bridges and enriches the
/// results with SetupAPI metadata so the UI can display friendly text. All serial I/O goes through
/// D2XX; this class only discovers devices and never touches System.IO.Ports.SerialPort.
/// </summary>
internal static class FtdiDeviceUtil
{
    /// <summary>
    /// Enumerates FTDI devices using <see cref="FTDI.GetDeviceList"/> and correlates the entries
    /// with SetupAPI metadata for display. Every D2XX call is logged for the optional debug logger
    /// to mimic the verbose output of m18.py.
    /// </summary>
    /// <param name="debugLogger">Optional sink that receives raw D2XX call descriptions.</param>
    /// <returns>Ordered list of FTDI devices present on the system.</returns>
    public static List<FtdiDeviceDisplay> EnumerateDevices(Action<string>? debugLogger = null)
    {
        var devices = new List<FtdiDeviceDisplay>(); // Accumulates enumeration results.
        var ftdi = new FTDI(); // FTDI object used solely for discovery; not kept open.

        // D2XX: query the number of attached FTDI devices. Mirrors pyserial's initial port scan.
        var status = ftdi.GetNumberOfDevices(ref var count);
        debugLogger?.Invoke($"FT_GetNumberOfDevices -> {status} (count={count})");
        if (status != FTDI.FT_STATUS.FT_OK || count == 0)
        {
            return devices; // No devices or driver failure; return empty list.
        }

        // Prepare a buffer and request the full device list from the driver.
        var nodes = new FTDI.FT_DEVICE_INFO_NODE[count];
        status = ftdi.GetDeviceList(nodes);
        debugLogger?.Invoke($"FT_GetDeviceInfoList -> {status}");
        if (status != FTDI.FT_STATUS.FT_OK)
        {
            return devices; // Abort enumeration on error to avoid incomplete data.
        }

        // Pull SetupAPI port metadata to correlate COM names and USB descriptors with D2XX entries.
        var setupApiPorts = SerialPortUtil.EnumerateDetailedPorts(debugLogger);

        for (var i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i]; // Capture current device info node from D2XX.
            var serialNumber = node.SerialNumber ?? string.Empty; // Serial is the stable key.

            // Try to match the FTDI serial number to a SetupAPI COM-port entry for additional labels.
            var matchingPort = setupApiPorts.FirstOrDefault(port =>
                string.Equals(port.UsbSerialNumber, serialNumber, StringComparison.OrdinalIgnoreCase));

            // Compose a record that includes both D2XX and SetupAPI metadata for UI display.
            var display = new FtdiDeviceDisplay(
                Index: (uint)i,
                SerialNumber: serialNumber,
                Description: node.Description ?? "FTDI Device",
                DeviceId: node.ID,
                LocationId: node.LocId,
                DeviceType: node.Type,
                ComPort: matchingPort?.PortName,
                Manufacturer: matchingPort?.Manufacturer,
                FriendlyName: matchingPort?.FriendlyName,
                Source: matchingPort?.Source ?? "D2XX enumeration"
            );

            devices.Add(display); // Store result for caller.

            // Emit a verbose log line to replicate m18.py debug output patterns.
            debugLogger?.Invoke(
                $"FTDI[{i}] Serial={display.SerialNumber}, Desc={display.Description}, COM={display.ComPort ?? "(none)"}, LocId=0x{display.LocationId:X}, Id=0x{display.DeviceId:X}, Source={display.Source}");
        }

        return devices
            .OrderBy(device => device.SerialNumber, StringComparer.OrdinalIgnoreCase) // Stable ordering for UI.
            .ToList();
    }
}

/// <summary>
/// Immutable record describing a single FTDI device, combining D2XX and SetupAPI data so the UI can
/// display a helpful label while the protocol layer opens the device by serial number/index.
/// </summary>
internal sealed record FtdiDeviceDisplay(
    uint Index,
    string SerialNumber,
    string Description,
    uint DeviceId,
    uint LocationId,
    FTDI.FT_DEVICE DeviceType,
    string? ComPort,
    string? Manufacturer,
    string? FriendlyName,
    string? Source)
{
    public string DisplayName
    {
        get
        {
            // Example: "SN:FT123ABC - FT232R USB UART - COM3 - FTDI"
            var parts = new List<string> { $"SN:{SerialNumber}" };

            if (!string.IsNullOrWhiteSpace(Description))
            {
                parts.Add(Description);
            }

            if (!string.IsNullOrWhiteSpace(ComPort))
            {
                parts.Add(ComPort!);
            }

            if (!string.IsNullOrWhiteSpace(Manufacturer))
            {
                parts.Add(Manufacturer!);
            }

            return string.Join(" - ", parts);
        }
    }

    public override string ToString() => DisplayName; // ComboBox uses this for display text.
}
