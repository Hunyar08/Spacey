<<<<<<< HEAD
using System.Data.OleDb;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Spacey;

public sealed class StorageScanner
{
    private const int MaximumResults = 20000;

    public async Task<ScanResult> ScanAsync()
    {
        return await Task.Run(() =>
        {
            IReadOnlyList<IndexedFile> files;
            string status;

            if (EverythingIndex.TryQuery(MaximumResults, out var everythingFiles, out var reason))
            {
                files = everythingFiles;
                status = $"Everything index · {files.Count:N0} files returned";
            }
            else
            {
                try
                {
                    files = WindowsSearchIndex.Query(MaximumResults);
                    status = $"Windows Search index · {files.Count:N0} files returned · Everything unavailable: {reason}";
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"Neither index could be queried. Everything: {reason}. Windows Search: {exception.Message}", exception);
                }
            }

            return BuildResult(files, status);
        });
    }

    private static ScanResult BuildResult(IReadOnlyList<IndexedFile> files, string status)
    {
        var entries = new List<StorageEntry>(files.Count);
        var folderSizes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        long totalBytes = 0;

        foreach (var file in files)
        {
            if (!Path.IsPathFullyQualified(file.Path) || file.Size <= 0)
            {
                continue;
            }

            entries.Add(new StorageEntry
            {
                Name = Path.GetFileName(file.Path),
                Path = file.Path,
                Size = file.Size,
                IsFolder = false
            });
            totalBytes = SaturatingAdd(totalBytes, file.Size);

            var directory = Path.GetDirectoryName(file.Path);
            var root = Path.GetPathRoot(file.Path);
            while (!string.IsNullOrEmpty(directory) && !string.Equals(directory, root, StringComparison.OrdinalIgnoreCase))
            {
                folderSizes[directory] = SaturatingAdd(folderSizes.GetValueOrDefault(directory), file.Size);
                directory = Path.GetDirectoryName(directory);
            }
        }

        foreach (var folder in folderSizes)
        {
            entries.Add(new StorageEntry
            {
                Name = Path.GetFileName(folder.Key.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                Path = folder.Key,
                Size = folder.Value,
                IsFolder = true
            });
        }

        entries.Sort((left, right) => right.Size.CompareTo(left.Size));
        return new ScanResult(entries, files.Count, folderSizes.Count, totalBytes, status);
    }

    private static long SaturatingAdd(long current, long value) =>
        value > long.MaxValue - current ? long.MaxValue : current + value;

    private static class EverythingIndex
    {
        private const uint RequestFullPathAndFileName = 0x00000004;
        private const uint RequestSize = 0x00000010;
        private const uint SortSizeDescending = 6;

        public static bool TryQuery(int maximumResults, out IReadOnlyList<IndexedFile> files, out string reason)
        {
            files = [];
            reason = "Everything is not running.";
            if (!Process.GetProcessesByName("Everything").Any())
            {
                return false;
            }

            try
            {
                SetSearch("file:");
                SetRequestFlags(RequestFullPathAndFileName | RequestSize);
                SetSort(SortSizeDescending);
                SetMax((uint)maximumResults);

                if (!Query(true))
                {
                    reason = "The Everything SDK could not query the running instance.";
                    return false;
                }

                var resultCount = GetNumResults();
                var results = new List<IndexedFile>((int)resultCount);
                var pathBuffer = new StringBuilder(32768);
                for (uint index = 0; index < resultCount; index++)
                {
                    pathBuffer.Clear();
                    if (GetResultFullPathName(index, pathBuffer, (uint)pathBuffer.Capacity) == 0 || !GetResultSize(index, out var size))
                    {
                        continue;
                    }

                    results.Add(new IndexedFile(pathBuffer.ToString(), size > long.MaxValue ? long.MaxValue : (long)size));
                }

                files = results;
                reason = string.Empty;
                return true;
            }
            catch (DllNotFoundException)
            {
                reason = "Everything is running, but Everything64.dll from the Everything SDK is not beside the app.";
                return false;
            }
            catch (Exception exception) when (exception is EntryPointNotFoundException or BadImageFormatException)
            {
                reason = $"The Everything SDK DLL could not be used: {exception.Message}";
                return false;
            }
            finally
            {
                try
                {
                    Reset();
                }
                catch (DllNotFoundException)
                {
                }
                catch (EntryPointNotFoundException)
                {
                }
                catch (BadImageFormatException)
                {
                }
            }
        }

        [DllImport("Everything64.dll", EntryPoint = "Everything_SetSearchW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern void SetSearch(string search);

        [DllImport("Everything64.dll", EntryPoint = "Everything_SetRequestFlags", ExactSpelling = true)]
        private static extern void SetRequestFlags(uint flags);

        [DllImport("Everything64.dll", EntryPoint = "Everything_SetSort", ExactSpelling = true)]
        private static extern void SetSort(uint sort);

        [DllImport("Everything64.dll", EntryPoint = "Everything_SetMax", ExactSpelling = true)]
        private static extern void SetMax(uint maximum);

        [DllImport("Everything64.dll", EntryPoint = "Everything_QueryW", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool Query([MarshalAs(UnmanagedType.Bool)] bool wait);

        [DllImport("Everything64.dll", EntryPoint = "Everything_GetNumResults", ExactSpelling = true)]
        private static extern uint GetNumResults();

        [DllImport("Everything64.dll", EntryPoint = "Everything_GetResultFullPathNameW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern uint GetResultFullPathName(uint index, StringBuilder path, uint pathLength);

        [DllImport("Everything64.dll", EntryPoint = "Everything_GetResultSize", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetResultSize(uint index, out ulong size);

        [DllImport("Everything64.dll", EntryPoint = "Everything_Reset", ExactSpelling = true)]
        private static extern void Reset();
    }

    private static class WindowsSearchIndex
    {
        public static IReadOnlyList<IndexedFile> Query(int maximumResults)
        {
            const string connectionString = "Provider=Search.CollatorDSO;Extended Properties='Application=Windows';";
            const string queryTemplate = "SELECT TOP {0} System.ItemPathDisplay, System.Size FROM SystemIndex WHERE System.IsFolder = FALSE AND System.Size > 0 ORDER BY System.Size DESC";
            using var connection = new OleDbConnection(connectionString);
            connection.Open();
            using var command = new OleDbCommand(string.Format(System.Globalization.CultureInfo.InvariantCulture, queryTemplate, maximumResults), connection);
            using var reader = command.ExecuteReader();
            var files = new List<IndexedFile>();

            while (reader?.Read() == true)
            {
                var path = Convert.ToString(reader.GetValue(0), System.Globalization.CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(path) || reader.IsDBNull(1))
                {
                    continue;
                }

                var size = Convert.ToInt64(reader.GetValue(1), System.Globalization.CultureInfo.InvariantCulture);
                if (size > 0)
                {
                    files.Add(new IndexedFile(path, size));
                }
            }

            return files;
        }
    }
=======
using System.Data.OleDb;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Spacey;

public sealed class StorageScanner
{
    private const int MaximumResults = 20000;

    public async Task<ScanResult> ScanAsync()
    {
        return await Task.Run(() =>
        {
            IReadOnlyList<IndexedFile> files;
            string status;

            if (EverythingIndex.TryQuery(MaximumResults, out var everythingFiles, out var reason))
            {
                files = everythingFiles;
                status = $"Everything index · {files.Count:N0} files returned";
            }
            else
            {
                try
                {
                    files = WindowsSearchIndex.Query(MaximumResults);
                    status = $"Windows Search index · {files.Count:N0} files returned · Everything unavailable: {reason}";
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException($"Neither index could be queried. Everything: {reason}. Windows Search: {exception.Message}", exception);
                }
            }

            return BuildResult(files, status);
        });
    }

    private static ScanResult BuildResult(IReadOnlyList<IndexedFile> files, string status)
    {
        var entries = new List<StorageEntry>(files.Count);
        var folderSizes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        long totalBytes = 0;

        foreach (var file in files)
        {
            if (!Path.IsPathFullyQualified(file.Path) || file.Size <= 0)
            {
                continue;
            }

            entries.Add(new StorageEntry
            {
                Name = Path.GetFileName(file.Path),
                Path = file.Path,
                Size = file.Size,
                IsFolder = false
            });
            totalBytes = SaturatingAdd(totalBytes, file.Size);

            var directory = Path.GetDirectoryName(file.Path);
            var root = Path.GetPathRoot(file.Path);
            while (!string.IsNullOrEmpty(directory) && !string.Equals(directory, root, StringComparison.OrdinalIgnoreCase))
            {
                folderSizes[directory] = SaturatingAdd(folderSizes.GetValueOrDefault(directory), file.Size);
                directory = Path.GetDirectoryName(directory);
            }
        }

        foreach (var folder in folderSizes)
        {
            entries.Add(new StorageEntry
            {
                Name = Path.GetFileName(folder.Key.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                Path = folder.Key,
                Size = folder.Value,
                IsFolder = true
            });
        }

        entries.Sort((left, right) => right.Size.CompareTo(left.Size));
        return new ScanResult(entries, files.Count, folderSizes.Count, totalBytes, status);
    }

    private static long SaturatingAdd(long current, long value) =>
        value > long.MaxValue - current ? long.MaxValue : current + value;

    private static class EverythingIndex
    {
        private const uint RequestFullPathAndFileName = 0x00000004;
        private const uint RequestSize = 0x00000010;
        private const uint SortSizeDescending = 6;

        public static bool TryQuery(int maximumResults, out IReadOnlyList<IndexedFile> files, out string reason)
        {
            files = [];
            reason = "Everything is not running.";
            if (!Process.GetProcessesByName("Everything").Any())
            {
                return false;
            }

            try
            {
                SetSearch("file:");
                SetRequestFlags(RequestFullPathAndFileName | RequestSize);
                SetSort(SortSizeDescending);
                SetMax((uint)maximumResults);

                if (!Query(true))
                {
                    reason = "The Everything SDK could not query the running instance.";
                    return false;
                }

                var resultCount = GetNumResults();
                var results = new List<IndexedFile>((int)resultCount);
                var pathBuffer = new StringBuilder(32768);
                for (uint index = 0; index < resultCount; index++)
                {
                    pathBuffer.Clear();
                    if (GetResultFullPathName(index, pathBuffer, (uint)pathBuffer.Capacity) == 0 || !GetResultSize(index, out var size))
                    {
                        continue;
                    }

                    results.Add(new IndexedFile(pathBuffer.ToString(), size > long.MaxValue ? long.MaxValue : (long)size));
                }

                files = results;
                reason = string.Empty;
                return true;
            }
            catch (DllNotFoundException)
            {
                reason = "Everything is running, but Everything64.dll from the Everything SDK is not beside the app.";
                return false;
            }
            catch (Exception exception) when (exception is EntryPointNotFoundException or BadImageFormatException)
            {
                reason = $"The Everything SDK DLL could not be used: {exception.Message}";
                return false;
            }
            finally
            {
                try
                {
                    Reset();
                }
                catch (DllNotFoundException)
                {
                }
                catch (EntryPointNotFoundException)
                {
                }
                catch (BadImageFormatException)
                {
                }
            }
        }

        [DllImport("Everything64.dll", EntryPoint = "Everything_SetSearchW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern void SetSearch(string search);

        [DllImport("Everything64.dll", EntryPoint = "Everything_SetRequestFlags", ExactSpelling = true)]
        private static extern void SetRequestFlags(uint flags);

        [DllImport("Everything64.dll", EntryPoint = "Everything_SetSort", ExactSpelling = true)]
        private static extern void SetSort(uint sort);

        [DllImport("Everything64.dll", EntryPoint = "Everything_SetMax", ExactSpelling = true)]
        private static extern void SetMax(uint maximum);

        [DllImport("Everything64.dll", EntryPoint = "Everything_QueryW", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool Query([MarshalAs(UnmanagedType.Bool)] bool wait);

        [DllImport("Everything64.dll", EntryPoint = "Everything_GetNumResults", ExactSpelling = true)]
        private static extern uint GetNumResults();

        [DllImport("Everything64.dll", EntryPoint = "Everything_GetResultFullPathNameW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern uint GetResultFullPathName(uint index, StringBuilder path, uint pathLength);

        [DllImport("Everything64.dll", EntryPoint = "Everything_GetResultSize", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetResultSize(uint index, out ulong size);

        [DllImport("Everything64.dll", EntryPoint = "Everything_Reset", ExactSpelling = true)]
        private static extern void Reset();
    }

    private static class WindowsSearchIndex
    {
        public static IReadOnlyList<IndexedFile> Query(int maximumResults)
        {
            const string connectionString = "Provider=Search.CollatorDSO;Extended Properties='Application=Windows';";
            const string queryTemplate = "SELECT TOP {0} System.ItemPathDisplay, System.Size FROM SystemIndex WHERE System.IsFolder = FALSE AND System.Size > 0 ORDER BY System.Size DESC";
            using var connection = new OleDbConnection(connectionString);
            connection.Open();
            using var command = new OleDbCommand(string.Format(System.Globalization.CultureInfo.InvariantCulture, queryTemplate, maximumResults), connection);
            using var reader = command.ExecuteReader();
            var files = new List<IndexedFile>();

            while (reader?.Read() == true)
            {
                var path = Convert.ToString(reader.GetValue(0), System.Globalization.CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(path) || reader.IsDBNull(1))
                {
                    continue;
                }

                var size = Convert.ToInt64(reader.GetValue(1), System.Globalization.CultureInfo.InvariantCulture);
                if (size > 0)
                {
                    files.Add(new IndexedFile(path, size));
                }
            }

            return files;
        }
    }
>>>>>>> 8c4b819f7c07a98dbcabcd93df0b6ea17e6b0162
}