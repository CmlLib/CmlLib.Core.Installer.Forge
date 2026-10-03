namespace CmlLib.Core.Installer.Forge.Versions;

/// <summary>
/// Maps catalog release names to version data without creating installers.
/// </summary>
public static class NeoForgeVersionMapper
{
    public static NeoForgeVersion MapVersion(string name) => new(MapMinecraftVersion(name), name);

    public static string MapMinecraftVersion(string name)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));
        var originalName = name;
        if (name.StartsWith("neoforge-", StringComparison.Ordinal))
            name = name.Substring("neoforge-".Length);

        // Special snapshot releases use 0.<Minecraft snapshot ID>.<loader build>.
        if (name.StartsWith("0.", StringComparison.Ordinal))
        {
            var snapshotParts = name.Split('.');
            if (snapshotParts.Length >= 3 && snapshotParts[1].Any(char.IsLetter))
            {
                var build = new string(snapshotParts[2].TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(build, out _))
                    return snapshotParts[1];
            }
            throw new FormatException($"Cannot map NeoForge version '{originalName}' to a Minecraft version.");
        }

        var numberLength = 0;
        while (numberLength < name.Length &&
            ((name[numberLength] >= '0' && name[numberLength] <= '9') || name[numberLength] == '.'))
            numberLength++;

        var parts = name.Substring(0, numberLength).Split('.');
        var numbers = new int[parts.Length];
        for (var index = 0; index < parts.Length; index++)
        {
            if (!int.TryParse(parts[index], out numbers[index]))
                throw new FormatException($"Cannot map NeoForge version '{originalName}' to a Minecraft version.");
        }

        if (numbers.Length >= 3 && numbers[0] is 20 or 21)
            return NormalizeMinecraftVersion($"1.{numbers[0]}.{numbers[1]}");
        if (numbers.Length >= 4 && numbers[0] >= 26)
            return NormalizeMinecraftVersion($"{numbers[0]}.{numbers[1]}.{numbers[2]}");

        throw new FormatException($"Cannot map NeoForge version '{originalName}' to a Minecraft version.");
    }

    public static string NormalizeMinecraftVersion(string version)
    {
        var parts = version.Split('.');
        if (parts.Length == 3 && parts[2] == "0")
            return string.Join(".", parts.Take(2));
        return version;
    }
}
