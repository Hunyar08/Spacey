<<<<<<< HEAD
using System.Globalization;

namespace Spacey;

public sealed class StorageEntry
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public bool IsFolder { get; set; }

    public string Kind => IsFolder ? "Folder (estimated)" : "File";
    public string Glyph => IsFolder ? "\uE8B7" : "\uE8A5";
    public string FormattedSize => FormatSize(Size);

    public static string FormatSize(long size)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = size;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value.ToString(unit == 0 ? "N0" : "N1", CultureInfo.CurrentCulture)} {units[unit]}";
    }
=======
using System.Globalization;

namespace Spacey;

public sealed class StorageEntry
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public long Size { get; set; }
    public bool IsFolder { get; set; }

    public string Kind => IsFolder ? "Folder (estimated)" : "File";
    public string Glyph => IsFolder ? "\uE8B7" : "\uE8A5";
    public string FormattedSize => FormatSize(Size);

    public static string FormatSize(long size)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double value = size;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value.ToString(unit == 0 ? "N0" : "N1", CultureInfo.CurrentCulture)} {units[unit]}";
    }
>>>>>>> 8c4b819f7c07a98dbcabcd93df0b6ea17e6b0162
}