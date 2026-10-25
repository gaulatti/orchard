using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace Orchard.Catalog;

internal static class CatalogReferenceAudit
{
    private delegate bool Visitor(nint handle, nint value);
    [DllImport("user32.dll")] private static extern bool EnumWindows(Visitor visitor, nint value);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint process);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint handle);

    internal static int Run(string output)
    {
        var resources = new List<object>(); var guards = 0;
        void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        void Guard()
        {
            guards++; var visible = false; GetWindowThreadProcessId(GetForegroundWindow(), out var foreground);
            EnumWindows((handle, _) => { GetWindowThreadProcessId(handle, out var process); visible |= process == Environment.ProcessId && IsWindowVisible(handle); return true; }, 0);
            Require(!visible && foreground != Environment.ProcessId, "Resource audit left its background boundary.");
        }
        try
        {
            Guard(); var assembly = typeof(App).Assembly;
            foreach (var name in assembly.GetManifestResourceNames().Order(StringComparer.Ordinal))
            {
                using var encoded = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException($"Missing encoded resource: {name}");
                using var decoded = CatalogReferenceResources.Open(assembly, name);
                Require(decoded.CanRead && decoded.CanSeek && !decoded.CanWrite && decoded.Position == 0, $"Resource stream contract changed: {name}");
                var length = decoded.Length; var sha256 = Convert.ToHexStringLower(SHA256.HashData(decoded));
                decoded.Seek(0, SeekOrigin.Begin);
                Require(Convert.ToHexStringLower(SHA256.HashData(decoded)) == sha256, $"Resource rewind changed bytes: {name}");
                decoded.Position = 0; var json = name.StartsWith("Orchard.Catalog.Reference.", StringComparison.Ordinal) && name.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
                if (json) { using var document = JsonDocument.Parse(decoded); }
                resources.Add(new { name, length, encodedBytes = encoded.Length, sha256, jsonParsed = json, seekable = decoded.CanSeek, readOnly = !decoded.CanWrite, rewindVerified = true });
                Guard();
            }
            File.WriteAllText(output, JsonSerializer.Serialize(new { passed = true, resources, guards, nativeArchitecture = RuntimeInformation.ProcessArchitecture.ToString(), hostSha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly.Location))), windowsCreated = false, hardwareInputSent = false, osSettingsChanged = false }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception error)
        {
            File.WriteAllText(output, JsonSerializer.Serialize(new { passed = false, resources, guards, error = error.ToString() }, new JsonSerializerOptions { WriteIndented = true }));
            return 1;
        }
    }
}
