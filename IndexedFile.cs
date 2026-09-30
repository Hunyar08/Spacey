<<<<<<< HEAD
namespace Spacey;

public sealed record IndexedFile(string Path, long Size);

public sealed record ScanResult(
    List<StorageEntry> Entries,
    int FileCount,
    int FolderCount,
    long TotalBytes,
=======
namespace Spacey;

public sealed record IndexedFile(string Path, long Size);

public sealed record ScanResult(
    List<StorageEntry> Entries,
    int FileCount,
    int FolderCount,
    long TotalBytes,
>>>>>>> 8c4b819f7c07a98dbcabcd93df0b6ea17e6b0162
    string Status);