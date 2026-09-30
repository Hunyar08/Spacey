<<<<<<< HEAD
namespace Spacey;

internal enum ThemePreference
{
    System,
    Dark,
    Light
}

internal static class ThemePreferences
{
    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Spacey",
        "theme.txt");

    public static ThemePreference Load()
    {
        try
        {
            return Enum.TryParse<ThemePreference>(File.ReadAllText(SettingsPath), true, out var preference) && Enum.IsDefined(preference)
                ? preference
                : ThemePreference.System;
        }
        catch (IOException)
        {
            return ThemePreference.System;
        }
        catch (UnauthorizedAccessException)
        {
            return ThemePreference.System;
        }
    }

    public static void Save(ThemePreference preference)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, preference.ToString());
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
=======
namespace Spacey;

internal enum ThemePreference
{
    System,
    Dark,
    Light
}

internal static class ThemePreferences
{
    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Spacey",
        "theme.txt");

    public static ThemePreference Load()
    {
        try
        {
            return Enum.TryParse<ThemePreference>(File.ReadAllText(SettingsPath), true, out var preference) && Enum.IsDefined(preference)
                ? preference
                : ThemePreference.System;
        }
        catch (IOException)
        {
            return ThemePreference.System;
        }
        catch (UnauthorizedAccessException)
        {
            return ThemePreference.System;
        }
    }

    public static void Save(ThemePreference preference)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, preference.ToString());
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
>>>>>>> 8c4b819f7c07a98dbcabcd93df0b6ea17e6b0162
}