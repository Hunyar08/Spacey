<<<<<<< HEAD
using System.Collections.ObjectModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Spacey;

public sealed partial class MainWindow : Window
{
    private readonly StorageScanner _scanner = new();
    private List<StorageEntry> _allEntries = [];
    private bool _isLoadingTheme = true;

    public ObservableCollection<StorageEntry> Entries { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Spacey.ico"));
        var preference = ThemePreferences.Load();
        ThemeSelector.SelectedIndex = (int)preference;
        ApplyTheme(preference);
        _isLoadingTheme = false;
        _ = ScanAsync();
    }

    private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingTheme || ThemeSelector.SelectedIndex < 0)
        {
            return;
        }

        var preference = (ThemePreference)ThemeSelector.SelectedIndex;
        ApplyTheme(preference);
        ThemePreferences.Save(preference);
    }

    private void ApplyTheme(ThemePreference preference)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = preference switch
            {
                ThemePreference.Dark => ElementTheme.Dark,
                ThemePreference.Light => ElementTheme.Light,
                _ => ElementTheme.Default
            };
        }

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 26100))
        {
            AppWindow.TitleBar.PreferredTheme = preference switch
            {
                ThemePreference.Dark => TitleBarTheme.Dark,
                ThemePreference.Light => TitleBarTheme.Light,
                _ => TitleBarTheme.UseDefaultAppMode
            };
        }
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        await ScanAsync();
    }

    private async Task ScanAsync()
    {
        ScanProgress.IsActive = true;
        ScanProgress.Visibility = Visibility.Visible;
        SourceText.Text = "Querying Everything, then Windows Search if needed...";

        try
        {
            var result = await _scanner.ScanAsync();
            _allEntries = result.Entries;
            FilesCountText.Text = result.FileCount.ToString("N0");
            FoldersCountText.Text = result.FolderCount.ToString("N0");
            TotalSizeText.Text = StorageEntry.FormatSize(result.TotalBytes);
            SourceText.Text = result.Status;
            ApplyFilters();
        }
        catch (Exception exception)
        {
            _allEntries = [];
            Entries.Clear();
            FilesCountText.Text = "0";
            FoldersCountText.Text = "0";
            TotalSizeText.Text = "—";
            SourceText.Text = exception.Message;
        }
        finally
        {
            ScanProgress.IsActive = false;
            ScanProgress.Visibility = Visibility.Collapsed;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

    private void FilterTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

    private void ApplyFilters()
    {
        if (Entries is null || SearchBox is null || FilterTypeBox is null)
        {
            return;
        }

        var query = SearchBox.Text.Trim();
        var selectedType = FilterTypeBox.SelectedIndex;
        var filtered = _allEntries.Where(entry =>
            (query.Length == 0 || entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || entry.Path.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (selectedType == 0 || (selectedType == 1 && !entry.IsFolder) || (selectedType == 2 && entry.IsFolder)));

        Entries.Clear();
        foreach (var entry in filtered)
        {
            Entries.Add(entry);
        }
    }

    private void OpenLocation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: StorageEntry entry } || !System.IO.Path.IsPathFullyQualified(entry.Path))
        {
            return;
        }

        var arguments = entry.IsFolder ? $"\"{entry.Path}\"" : $"/select,\"{entry.Path}\"";
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", arguments)
        {
            UseShellExecute = true
        });
    }
=======
using System.Collections.ObjectModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Spacey;

public sealed partial class MainWindow : Window
{
    private readonly StorageScanner _scanner = new();
    private List<StorageEntry> _allEntries = [];
    private bool _isLoadingTheme = true;

    public ObservableCollection<StorageEntry> Entries { get; } = [];

    public MainWindow()
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Spacey.ico"));
        var preference = ThemePreferences.Load();
        ThemeSelector.SelectedIndex = (int)preference;
        ApplyTheme(preference);
        _isLoadingTheme = false;
        _ = ScanAsync();
    }

    private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoadingTheme || ThemeSelector.SelectedIndex < 0)
        {
            return;
        }

        var preference = (ThemePreference)ThemeSelector.SelectedIndex;
        ApplyTheme(preference);
        ThemePreferences.Save(preference);
    }

    private void ApplyTheme(ThemePreference preference)
    {
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = preference switch
            {
                ThemePreference.Dark => ElementTheme.Dark,
                ThemePreference.Light => ElementTheme.Light,
                _ => ElementTheme.Default
            };
        }

        if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 26100))
        {
            AppWindow.TitleBar.PreferredTheme = preference switch
            {
                ThemePreference.Dark => TitleBarTheme.Dark,
                ThemePreference.Light => TitleBarTheme.Light,
                _ => TitleBarTheme.UseDefaultAppMode
            };
        }
    }

    private async void Scan_Click(object sender, RoutedEventArgs e)
    {
        await ScanAsync();
    }

    private async Task ScanAsync()
    {
        ScanProgress.IsActive = true;
        ScanProgress.Visibility = Visibility.Visible;
        SourceText.Text = "Querying Everything, then Windows Search if needed...";

        try
        {
            var result = await _scanner.ScanAsync();
            _allEntries = result.Entries;
            FilesCountText.Text = result.FileCount.ToString("N0");
            FoldersCountText.Text = result.FolderCount.ToString("N0");
            TotalSizeText.Text = StorageEntry.FormatSize(result.TotalBytes);
            SourceText.Text = result.Status;
            ApplyFilters();
        }
        catch (Exception exception)
        {
            _allEntries = [];
            Entries.Clear();
            FilesCountText.Text = "0";
            FoldersCountText.Text = "0";
            TotalSizeText.Text = "—";
            SourceText.Text = exception.Message;
        }
        finally
        {
            ScanProgress.IsActive = false;
            ScanProgress.Visibility = Visibility.Collapsed;
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilters();

    private void FilterTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

    private void ApplyFilters()
    {
        if (Entries is null || SearchBox is null || FilterTypeBox is null)
        {
            return;
        }

        var query = SearchBox.Text.Trim();
        var selectedType = FilterTypeBox.SelectedIndex;
        var filtered = _allEntries.Where(entry =>
            (query.Length == 0 || entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase) || entry.Path.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (selectedType == 0 || (selectedType == 1 && !entry.IsFolder) || (selectedType == 2 && entry.IsFolder)));

        Entries.Clear();
        foreach (var entry in filtered)
        {
            Entries.Add(entry);
        }
    }

    private void OpenLocation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: StorageEntry entry } || !System.IO.Path.IsPathFullyQualified(entry.Path))
        {
            return;
        }

        var arguments = entry.IsFolder ? $"\"{entry.Path}\"" : $"/select,\"{entry.Path}\"";
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", arguments)
        {
            UseShellExecute = true
        });
    }
>>>>>>> 8c4b819f7c07a98dbcabcd93df0b6ea17e6b0162
}