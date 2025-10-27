using System;
using BepInEx.Configuration;
using MyceliumNetworking;
using Steamworks;

namespace AboubiUnbound;

// Will be released as a standalone library when i can be bothered
public class SyncedConfigEntry<T> : ConfigEntryBase
{
    public T Value {
        get => m_isOverriden ? m_overridenValue : field;
        set {
            if (m_isOverriden) throw new InvalidOperationException("Synced config entries' values cannot be changed when they are overriden by a value from the host!");

            value = ClampValue(value);
            if (Equals(field, value)) return;

            field = value;
            OnSettingChanged(this);
        }
    }

    public override object BoxedValue {
        get => Value;
        set => Value = (T)value;
    }

    public event EventHandler SettingChanged;

    private bool m_isOverriden;
    private T m_overridenValue;
    private int m_myceliumMask;

    internal SyncedConfigEntry(
        ConfigFile configFile,
        ConfigDefinition definition,
        T defaultValue,
        ConfigDescription configDescription)
        : base(configFile, definition, typeof(T), defaultValue, configDescription) {
        m_myceliumMask = string.Join(',', definition.Key, definition.Section).GetHashCode();
        MyceliumNetwork.RegisterNetworkObject(this, Plugin.c_myceliumID, m_myceliumMask);

        MyceliumNetwork.LobbyLeft += ResetOverriden;
        MyceliumNetwork.PlayerEntered += SyncIfHost;

        configFile.SettingChanged += (sender, args) => {
            if (args.ChangedSetting != this) return;

            SettingChanged?.Invoke(sender, args);
            SyncIfHost();
        };
    }

    ~SyncedConfigEntry() {
        MyceliumNetwork.DeregisterNetworkObject(this, Plugin.c_myceliumID, m_myceliumMask);
        MyceliumNetwork.LobbyLeft -= ResetOverriden;
        MyceliumNetwork.PlayerEntered -= SyncIfHost;
    }

    private void SetOverriden() {
        m_isOverriden = true;
    }

    private void ResetOverriden() {
        m_isOverriden = false;
    }

    private void SyncIfHost(CSteamID target) {
        if (!MyceliumNetwork.InLobby || !MyceliumNetwork.IsHost) return;

        MyceliumNetwork.RPCTargetMasked(Plugin.c_myceliumID, nameof(RPCSyncValue), target, ReliableType.Reliable, m_myceliumMask, BoxedValue);
    }

    private void SyncIfHost() {
        if (!MyceliumNetwork.InLobby || !MyceliumNetwork.IsHost) return;

        MyceliumNetwork.RPCMasked(Plugin.c_myceliumID, nameof(RPCSyncValue), ReliableType.Reliable, m_myceliumMask, BoxedValue);
    }

    [CustomRPC]
    public void RPCSyncValue(T value) {
        if (MyceliumNetwork.IsHost) return;

        SetOverriden();
        m_overridenValue = value;
    }
}