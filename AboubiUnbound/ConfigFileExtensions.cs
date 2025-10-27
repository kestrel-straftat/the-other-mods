using System;
using System.Linq;
using BepInEx.Configuration;

namespace AboubiUnbound;

internal static class ConfigFileExtensions
{
    public static SyncedConfigEntry<T> BindSynced<T>(this ConfigFile file, ConfigDefinition configDefinition, T defaultValue, ConfigDescription configDescription = null) {
        if (!TomlTypeConverter.CanConvert(typeof(T))) {
            throw new ArgumentException($"Type {typeof (T)} is not supported by the config system. Supported types: {string.Join(", ", TomlTypeConverter.GetSupportedTypes().Select(x => x.Name).ToArray())}");
        }

        lock (file._ioLock) {
            if (file.Entries.TryGetValue(configDefinition, out var rawEntry)) {
                return (SyncedConfigEntry<T>) rawEntry;
            }

            var entry = new SyncedConfigEntry<T>(file, configDefinition, defaultValue, configDescription);
            
            file.Entries[configDefinition] = entry;

            if (file.OrphanedEntries.TryGetValue(configDefinition, out var homelessValue)) {
                entry.SetSerializedValue(homelessValue);
                file.OrphanedEntries.Remove(configDefinition);
            }

            if (file.SaveOnConfigSet) {
                file.Save();
            }
            
            return entry;
        }
    }

    public static SyncedConfigEntry<T> BindSynced<T>(this ConfigFile file, string section, string key, T defaultValue, ConfigDescription configDescription = null)
        => BindSynced(file, new ConfigDefinition(section, key), defaultValue, configDescription);
    
    public static SyncedConfigEntry<T> BindSynced<T>(this ConfigFile file, string section, string key, T defaultValue, string description)
        => BindSynced(file, new ConfigDefinition(section, key), defaultValue, new ConfigDescription(description));
}