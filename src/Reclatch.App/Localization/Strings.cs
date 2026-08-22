using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Reclatch.App.Localization;

public static class Strings
{
    public const string SourceLanguage = "tr";

    private static readonly Dictionary<string, Dictionary<string, string>> Catalogs = new();
    private static readonly HashSet<string> Reported = new();

    public static string Current { get; private set; } = SourceLanguage;

    public static CultureInfo Culture { get; private set; } = CultureInfo.GetCultureInfo(SourceLanguage);

    public static event Action? LanguageChanged;

    public static void Use(string language)
    {
        if (string.Equals(Current, language, StringComparison.OrdinalIgnoreCase)) return;
        Current = language;
        Culture = CultureInfo.GetCultureInfo(language);
        LanguageChanged?.Invoke();
    }

    public static string Get(string key)
    {
        var value = Lookup(Current, key);
        if (value is not null) return value;

        if (Reported.Add(Current + ':' + key))
            System.Diagnostics.Debug.WriteLine($"locale: '{key}' missing in '{Current}', falling back to '{SourceLanguage}'");

        return Lookup(SourceLanguage, key) ?? key;
    }

    public static string Get(string key, params (string Name, object? Value)[] arguments)
    {
        var text = Get(key);
        foreach (var (name, value) in arguments)
            text = text.Replace("{" + name + "}", value?.ToString() ?? string.Empty, StringComparison.Ordinal);
        return text;
    }

    private static string? Lookup(string language, string key)
        => Catalog(language).TryGetValue(key, out var value) ? value : null;

    private static Dictionary<string, string> Catalog(string language)
    {
        if (Catalogs.TryGetValue(language, out var cached)) return cached;

        var catalog = Load(language) ?? new Dictionary<string, string>();
        Catalogs[language] = catalog;
        return catalog;
    }

    private static Dictionary<string, string>? Load(string language)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var name = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith($"locale.{language}.json", StringComparison.OrdinalIgnoreCase));
        if (name is null) return null;

        using var stream = assembly.GetManifestResourceStream(name);
        if (stream is null) return null;

        using var reader = new StreamReader(stream);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadToEnd());
    }
}
