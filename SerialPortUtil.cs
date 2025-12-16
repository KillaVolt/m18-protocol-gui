using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace M18BatteryInfo;

/// <summary>
/// Provides serial port enumeration using Windows SetupAPI to capture detailed
/// descriptions similar to Python's pyserial list_ports.comports().
/// </summary>
internal static class SerialPortUtil
{
    // SetupAPI flags and registry property identifiers.
    private const uint DIGCF_PRESENT = 0x00000002; // Return only devices present in the system.

    private const uint SPDRP_DEVICEDESC = 0x00000000; // Device description (REG_SZ).
    private const uint SPDRP_HARDWAREID = 0x00000001; // Hardware IDs (REG_MULTI_SZ).
    private const uint SPDRP_FRIENDLYNAME = 0x0000000C; // Friendly name shown in Device Manager (REG_SZ).
    private const uint SPDRP_MFG = 0x0000000B; // Manufacturer string (REG_SZ).

    private const uint DIREG_DEV = 0x00000001; // Open hardware key for device.
    private const int KEY_QUERY_VALUE = 0x0001; // Access mask for querying registry values.

    private static readonly Guid PortsClassGuid = new("4d36e978-e325-11ce-bfc1-08002be10318");

    /// <summary>
    /// Enumerates serial ports using SetupAPI and falls back to SerialPort.GetPortNames()
    /// when additional details cannot be retrieved.
    /// </summary>
    /// <param name="debugLogger">Optional logger for verbose debug output.</param>
    /// <returns>Ordered list of serial port metadata.</returns>
    public static List<SerialPortDisplay> EnumerateDetailedPorts(Action<string>? debugLogger = null)
    {
        var ports = new Dictionary<string, SerialPortDisplay>(StringComparer.OrdinalIgnoreCase);

        EnumerateViaSetupApi(ports, debugLogger);
        AppendSerialPortFallbacks(ports, debugLogger);

        return ports.Values
            .OrderBy(port => port.PortName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void EnumerateViaSetupApi(IDictionary<string, SerialPortDisplay> ports, Action<string>? log)
    {
        // Acquire a handle to the Ports device class (COM & LPT) containing the serial devices.
        var deviceInfoSet = SetupDiGetClassDevs(ref PortsClassGuid, null, IntPtr.Zero, DIGCF_PRESENT);
        if (deviceInfoSet == IntPtr.Zero || deviceInfoSet == new IntPtr(-1))
        {
            log?.Invoke("SetupAPI enumeration failed to acquire device info set; continuing with fallbacks only.");
            return;
        }

        try
        {
            var devInfoData = new SP_DEVINFO_DATA
            {
                cbSize = (uint)Marshal.SizeOf<SP_DEVINFO_DATA>()
            };

            for (uint index = 0; SetupDiEnumDeviceInfo(deviceInfoSet, index, ref devInfoData); index++)
            {
                try
                {
                    var friendlyName = GetDeviceRegistryProperty(deviceInfoSet, ref devInfoData, SPDRP_FRIENDLYNAME);
                    var description = GetDeviceRegistryProperty(deviceInfoSet, ref devInfoData, SPDRP_DEVICEDESC);
                    var manufacturer = GetDeviceRegistryProperty(deviceInfoSet, ref devInfoData, SPDRP_MFG);
                    var hardwareIds = GetMultiStringProperty(deviceInfoSet, ref devInfoData, SPDRP_HARDWAREID);

                    var portName = ExtractPortName(friendlyName)
                        ?? ExtractPortName(description)
                        ?? TryGetPortNameFromRegistry(deviceInfoSet, ref devInfoData);

                    if (string.IsNullOrWhiteSpace(portName))
                    {
                        log?.Invoke($"SetupAPI device entry missing COM port name. Desc: '{description ?? friendlyName ?? "(none)"}'");
                        continue;
                    }

                    var existing = ports.GetValueOrDefault(portName!, new SerialPortDisplay(portName!, null, null, null, null, string.Empty));
                    var combinedSource = CombineSources(existing.Source, "SetupAPI (Ports class)");

                    ports[portName!] = existing with
                    {
                        Description = string.IsNullOrWhiteSpace(existing.Description) ? description : existing.Description,
                        Manufacturer = string.IsNullOrWhiteSpace(existing.Manufacturer) ? manufacturer : existing.Manufacturer,
                        FriendlyName = string.IsNullOrWhiteSpace(existing.FriendlyName) ? friendlyName : existing.FriendlyName,
                        HardwareIds = string.IsNullOrWhiteSpace(existing.HardwareIds) ? hardwareIds : existing.HardwareIds,
                        Source = combinedSource
                    };

                    log?.Invoke($"SetupAPI found {portName}: desc='{description ?? friendlyName ?? "(none)"}', mfg='{manufacturer ?? "(none)"}', hwid='{hardwareIds ?? "(none)"}'.");
                }
                catch (Exception ex)
                {
                    log?.Invoke($"SetupAPI enumeration error: {ex.Message}");
                }
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }
    }

    private static void AppendSerialPortFallbacks(IDictionary<string, SerialPortDisplay> ports, Action<string>? log)
    {
        try
        {
            foreach (var portName in SerialPort.GetPortNames())
            {
                if (ports.TryGetValue(portName, out var existing))
                {
                    ports[portName] = existing with
                    {
                        Source = CombineSources(existing.Source, "SerialPort.GetPortNames()")
                    };
                }
                else
                {
                    ports[portName] = new SerialPortDisplay(portName, null, null, null, null, "SerialPort.GetPortNames() fallback");
                }

                log?.Invoke($"SerialPort.GetPortNames detected {portName}.");
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"SerialPort.GetPortNames() failed: {ex.Message}");
        }
    }

    private static string CombineSources(string? existingSource, string newSource)
    {
        if (string.IsNullOrWhiteSpace(existingSource))
        {
            return newSource;
        }

        return existingSource.Contains(newSource, StringComparison.OrdinalIgnoreCase)
            ? existingSource
            : $"{existingSource}; {newSource}";
    }

    private static string? GetDeviceRegistryProperty(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, uint property)
    {
        var buffer = new byte[1024];

        if (!SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, property, out _, buffer, (uint)buffer.Length, out var requiredSize) || requiredSize == 0)
        {
            return null;
        }

        return Encoding.Unicode.GetString(buffer, 0, (int)requiredSize).TrimEnd('\0');
    }

    private static string? GetMultiStringProperty(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, uint property)
    {
        var buffer = new byte[2048];

        if (!SetupDiGetDeviceRegistryProperty(deviceInfoSet, ref deviceInfoData, property, out var regType, buffer, (uint)buffer.Length, out var requiredSize) || requiredSize == 0)
        {
            return null;
        }

        // REG_MULTI_SZ is a sequence of null-terminated UTF-16 strings ending with an additional null terminator.
        if (regType != 7) // 7 = REG_MULTI_SZ
        {
            return Encoding.Unicode.GetString(buffer, 0, (int)requiredSize).TrimEnd('\0');
        }

        var multiSz = Encoding.Unicode.GetString(buffer, 0, (int)requiredSize).TrimEnd('\0');
        var entries = multiSz.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
        return entries.Length == 0 ? null : string.Join(", ", entries);
    }

    private static string? TryGetPortNameFromRegistry(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData)
    {
        var regKey = SetupDiOpenDevRegKey(deviceInfoSet, ref deviceInfoData, 0, 0, DIREG_DEV, KEY_QUERY_VALUE);
        if (regKey == IntPtr.Zero || regKey == new IntPtr(-1))
        {
            return null;
        }

        using var safeHandle = new SafeRegistryHandle(regKey, true);
        using var key = RegistryKey.FromHandle(safeHandle);
        return key.GetValue("PortName") as string;
    }

    private static string? ExtractPortName(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        var match = Regex.Match(source, @"(COM\d+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    /// <summary>
    /// Mirrors the native SP_DEVINFO_DATA structure used by SetupAPI to describe a device instance.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct SP_DEVINFO_DATA
    {
        public uint cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    /// <summary>Retrieves a handle to a device information set for a specified class.</summary>
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevs(ref Guid classGuid, string? enumerator, IntPtr hwndParent, uint flags);

    /// <summary>Enumerates device information elements in a device information set.</summary>
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, uint memberIndex, ref SP_DEVINFO_DATA deviceInfoData);

    /// <summary>Destroys a device information set and frees associated memory.</summary>
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    /// <summary>Retrieves a device property from the registry.</summary>
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceRegistryProperty(
        IntPtr deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint property,
        out uint propertyRegDataType,
        byte[] propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize);

    /// <summary>Opens a registry key for a device instance.</summary>
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr SetupDiOpenDevRegKey(
        IntPtr deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        uint scope,
        uint hwProfile,
        uint keyType,
        int samDesired);
}

internal sealed record SerialPortDisplay(
    string PortName,
    string? Description,
    string? Manufacturer,
    string? FriendlyName,
    string? HardwareIds,
    string Source)
{
    public string? DeviceDescription => SanitizeDescription(FriendlyName ?? Description);

    public string DisplayName => string.IsNullOrWhiteSpace(DeviceDescription)
        ? PortName
        : $"{PortName} — {DeviceDescription}";

    public bool IsLikelyFtdi => new[] { Description, Manufacturer, FriendlyName }
        .Any(value => value != null && value.IndexOf("FTDI", StringComparison.OrdinalIgnoreCase) >= 0);

    private string? SanitizeDescription(string? rawDescription)
    {
        if (string.IsNullOrWhiteSpace(rawDescription))
        {
            return null;
        }

        var cleaned = rawDescription
            .Replace($"({PortName})", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(PortName, string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim().Trim('-', '—').Trim();

        return string.IsNullOrWhiteSpace(cleaned) ? rawDescription : cleaned;
    }

    public override string ToString() => DisplayName;
}
