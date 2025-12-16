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
    private const uint SPDRP_MFG = 0x0000000B; // Manufacturer string (REG_SZ).
    private const uint SPDRP_FRIENDLYNAME = 0x0000000C; // Friendly name shown in Device Manager (REG_SZ).
    private const uint SPDRP_LOCATION_INFORMATION = 0x0000000D; // Location information string (REG_SZ).

    private const uint DEVPROP_TYPE_STRING = 0x12; // DEVPROP_TYPE_STRING
    private const uint DEVPROP_TYPE_STRING_LIST = 0x1012; // DEVPROP_TYPE_STRING | DEVPROP_TYPEMOD_LIST

    private const uint DIREG_DEV = 0x00000001; // Open hardware key for device.
    private const int KEY_QUERY_VALUE = 0x0001; // Access mask for querying registry values.

    private static readonly Guid PortsClassGuid = new("4d36e978-e325-11ce-bfc1-08002be10318");
    private static readonly DEVPROPKEY DEVPKEY_Device_LocationPaths = new()
    {
        fmtid = new Guid("9d7debbc-c85d-4e75-a5f2-0e0e3bfb4ffd"),
        pid = 37
    };

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
        var portsClassGuid = PortsClassGuid;
        var deviceInfoSet = SetupDiGetClassDevs(ref portsClassGuid, null, IntPtr.Zero, DIGCF_PRESENT);
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
                    var locationInformation = GetDeviceRegistryProperty(deviceInfoSet, ref devInfoData, SPDRP_LOCATION_INFORMATION);
                    var locationPaths = GetDevicePropertyMultiString(deviceInfoSet, ref devInfoData, DEVPKEY_Device_LocationPaths);
                    var deviceInstanceId = GetDeviceInstanceId(deviceInfoSet, ref devInfoData);

                    var (portName, portSource) = ExtractBestPortName(friendlyName, description, deviceInfoSet, ref devInfoData, deviceInstanceId);

                    if (string.IsNullOrWhiteSpace(portName))
                    {
                        log?.Invoke($"SetupAPI device entry missing COM port name. Desc: '{description ?? friendlyName ?? "(none)"}'");
                        continue;
                    }

                    ports.TryGetValue(portName!, out var existing);
                    existing ??= SerialPortDisplay.Create(portName!);
                    var combinedSource = CombineSources(existing.Source, "SetupAPI (Ports class)");

                    var parsedUsb = ParseUsbIdentifiers(hardwareIds, deviceInstanceId);
                    var locationPath = locationPaths?.FirstOrDefault() ?? locationInformation;

                    ports[portName!] = existing with
                    {
                        Description = string.IsNullOrWhiteSpace(existing.Description) ? description : existing.Description,
                        Manufacturer = string.IsNullOrWhiteSpace(existing.Manufacturer) ? manufacturer : existing.Manufacturer,
                        FriendlyName = string.IsNullOrWhiteSpace(existing.FriendlyName) ? friendlyName : existing.FriendlyName,
                        HardwareIds = string.IsNullOrWhiteSpace(existing.HardwareIds) ? hardwareIds : existing.HardwareIds,
                        DeviceInstanceId = string.IsNullOrWhiteSpace(existing.DeviceInstanceId) ? deviceInstanceId : existing.DeviceInstanceId,
                        UsbVendorId = string.IsNullOrWhiteSpace(existing.UsbVendorId) ? parsedUsb.Vid : existing.UsbVendorId,
                        UsbProductId = string.IsNullOrWhiteSpace(existing.UsbProductId) ? parsedUsb.Pid : existing.UsbProductId,
                        UsbSerialNumber = string.IsNullOrWhiteSpace(existing.UsbSerialNumber) ? parsedUsb.SerialNumber : existing.UsbSerialNumber,
                        LocationPath = string.IsNullOrWhiteSpace(existing.LocationPath) ? locationPath : existing.LocationPath,
                        Source = combinedSource
                    };

                    LogDevice(log, portName!, new()
                    {
                        ($"FriendlyName", friendlyName, "SetupAPI SPDRP_FRIENDLYNAME"),
                        ($"Description", description, "SetupAPI SPDRP_DEVICEDESC"),
                        ($"Manufacturer", manufacturer, "SetupAPI SPDRP_MFG"),
                        ($"HardwareIds", hardwareIds, "SetupAPI SPDRP_HARDWAREID"),
                        ($"DeviceInstanceId", deviceInstanceId, "SetupDiGetDeviceInstanceId"),
                        ($"LocationPath", locationPath, "DEVPKEY_Device_LocationPaths/SPDRP_LOCATION_INFORMATION"),
                        ($"USB VID", parsedUsb.Vid, "Parsed from hardware IDs"),
                        ($"USB PID", parsedUsb.Pid, "Parsed from hardware IDs"),
                        ($"USB Serial", parsedUsb.SerialNumber, "Parsed from device instance"),
                        ($"Port source", portSource, "FriendlyName/Description/Registry")
                    });
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
                    ports[portName] = SerialPortDisplay.Create(portName) with
                    {
                        Source = "SerialPort.GetPortNames() fallback"
                    };
                }

                log?.Invoke($"SerialPort.GetPortNames detected {portName}.");
            }
        }
        catch (Exception ex)
        {
            log?.Invoke($"SerialPort.GetPortNames() failed: {ex.Message}");
        }
    }

    private static (string? Port, string Source) ExtractBestPortName(string? friendlyName, string? description, IntPtr deviceInfoSet, ref SP_DEVINFO_DATA devInfoData, string? deviceInstanceId)
    {
        var portFromFriendly = ExtractPortName(friendlyName);
        if (!string.IsNullOrWhiteSpace(portFromFriendly))
        {
            return (portFromFriendly, "Friendly name");
        }

        var portFromDescription = ExtractPortName(description);
        if (!string.IsNullOrWhiteSpace(portFromDescription))
        {
            return (portFromDescription, "Device description");
        }

        var registryPort = TryGetPortNameFromRegistry(deviceInfoSet, ref devInfoData);
        if (!string.IsNullOrWhiteSpace(registryPort))
        {
            return (registryPort, "Device registry PortName");
        }

        var enumRegistryPort = TryGetPortNameFromEnumRegistry(deviceInstanceId);
        if (!string.IsNullOrWhiteSpace(enumRegistryPort))
        {
            return (enumRegistryPort, "HKLM\\SYSTEM\\CurrentControlSet\\Enum device parameters");
        }

        return (null, "Unavailable");
    }

    private static void LogDevice(Action<string>? log, string portName, List<(string Name, string? Value, string Source)> values)
    {
        if (log == null)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.Append($"Detected {portName} with: ");
        foreach (var (name, value, source) in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            builder.Append($"{name}='{value}' (from {source}); ");
        }

        log(builder.ToString().Trim());
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

    private static string? GetDeviceInstanceId(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData)
    {
        var buffer = new StringBuilder(512);
        if (SetupDiGetDeviceInstanceId(deviceInfoSet, ref deviceInfoData, buffer, (uint)buffer.Capacity, out var requiredSize))
        {
            return buffer.ToString();
        }

        if (Marshal.GetLastWin32Error() != 122) // ERROR_INSUFFICIENT_BUFFER
        {
            return null;
        }

        buffer = new StringBuilder((int)requiredSize);
        return SetupDiGetDeviceInstanceId(deviceInfoSet, ref deviceInfoData, buffer, (uint)buffer.Capacity, out _)
            ? buffer.ToString()
            : null;
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

    private static List<string>? GetDevicePropertyMultiString(IntPtr deviceInfoSet, ref SP_DEVINFO_DATA deviceInfoData, DEVPROPKEY propertyKey)
    {
        var buffer = new byte[4096];

        if (!SetupDiGetDeviceProperty(deviceInfoSet, ref deviceInfoData, ref propertyKey, out var propertyType, buffer, (uint)buffer.Length, out var requiredSize, 0) || requiredSize == 0)
        {
            return null;
        }

        if (propertyType != DEVPROP_TYPE_STRING_LIST && propertyType != DEVPROP_TYPE_STRING)
        {
            return null;
        }

        var value = Encoding.Unicode.GetString(buffer, 0, (int)requiredSize).TrimEnd('\0');
        var parts = value.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? null : new List<string>(parts);
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

    private static string? TryGetPortNameFromEnumRegistry(string? deviceInstanceId)
    {
        if (string.IsNullOrWhiteSpace(deviceInstanceId))
        {
            return null;
        }

        try
        {
            var path = $"SYSTEM\\CurrentControlSet\\Enum\\{deviceInstanceId}\\Device Parameters";
            using var enumKey = Registry.LocalMachine.OpenSubKey(path);
            return enumKey?.GetValue("PortName") as string;
        }
        catch
        {
            // Registry access can fail due to permissions; ignore and continue.
            return null;
        }
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

    private static (string? Vid, string? Pid, string? SerialNumber) ParseUsbIdentifiers(string? hardwareIds, string? deviceInstanceId)
    {
        string? firstHardwareId = hardwareIds
            ?.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(id => id.Trim())
            .FirstOrDefault();

        var vidMatch = Regex.Match(firstHardwareId ?? string.Empty, "VID_([0-9A-Fa-f]{4})");
        var pidMatch = Regex.Match(firstHardwareId ?? string.Empty, "PID_([0-9A-Fa-f]{4})");

        string? serialNumber = null;
        if (!string.IsNullOrWhiteSpace(deviceInstanceId))
        {
            var segments = deviceInstanceId.Split('\\');
            if (segments.Length >= 3)
            {
                serialNumber = segments[2];
            }
        }

        return (
            vidMatch.Success ? vidMatch.Groups[1].Value.ToUpperInvariant() : null,
            pidMatch.Success ? pidMatch.Groups[1].Value.ToUpperInvariant() : null,
            serialNumber);
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

    /// <summary>Represents a DEVPROPKEY structure used with SetupDiGetDeviceProperty.</summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct DEVPROPKEY
    {
        public Guid fmtid;
        public uint pid;
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

    /// <summary>Retrieves a device instance identifier (e.g., USB\\VID_XXXX&PID_XXXX\\SERIAL).</summary>
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInstanceId(
        IntPtr deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        StringBuilder deviceInstanceId,
        uint deviceInstanceIdSize,
        out uint requiredSize);

    /// <summary>Retrieves device properties such as DEVPKEY_Device_LocationPaths.</summary>
    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceProperty(
        IntPtr deviceInfoSet,
        ref SP_DEVINFO_DATA deviceInfoData,
        ref DEVPROPKEY propertyKey,
        out uint propertyType,
        byte[] propertyBuffer,
        uint propertyBufferSize,
        out uint requiredSize,
        uint flags);

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
    string? DeviceInstanceId,
    string? UsbVendorId,
    string? UsbProductId,
    string? UsbSerialNumber,
    string? LocationPath,
    string Source)
{
    public static SerialPortDisplay Create(string portName) => new(portName, null, null, null, null, null, null, null, null, null, string.Empty);

    public string? DeviceDescription => SanitizeDescription(FriendlyName ?? Description);

    public string DisplayName
    {
        get
        {
            var parts = new List<string> { PortName };

            var ftdiOrUsbSegment = BuildFtdiOrUsbSegment();
            if (!string.IsNullOrWhiteSpace(ftdiOrUsbSegment))
            {
                parts.Add(ftdiOrUsbSegment!);
            }

            if (!string.IsNullOrWhiteSpace(DeviceDescription))
            {
                parts.Add(DeviceDescription!);
            }

            if (!string.IsNullOrWhiteSpace(Manufacturer))
            {
                parts.Add(Manufacturer!);
            }

            return string.Join(" - ", parts);
        }
    }

    public bool IsLikelyFtdi => new[] { Description, Manufacturer, FriendlyName }
        .Any(value => value != null && value.IndexOf("FTDI", StringComparison.OrdinalIgnoreCase) >= 0);

    private string? BuildFtdiOrUsbSegment()
    {
        if (IsLikelyFtdi)
        {
            return "FTDI detected";
        }

        var usbParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(UsbVendorId))
        {
            usbParts.Add($"VID:{UsbVendorId}");
        }

        if (!string.IsNullOrWhiteSpace(UsbProductId))
        {
            usbParts.Add($"PID:{UsbProductId}");
        }

        return usbParts.Count > 0 ? string.Join(" ", usbParts) : null;
    }

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
