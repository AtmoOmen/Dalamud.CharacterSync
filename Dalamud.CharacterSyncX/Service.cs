using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.RichPresence.Config;

namespace Dalamud.CharacterSyncX;

/// <summary>
///     Dalamud and plugin services.
/// </summary>
internal class Service
{
    /// <summary>
    ///     Gets or sets the plugin configuration.
    /// </summary>
    internal static CharacterSyncConfig Configuration { get; set; } = null!;

    /// <summary>
    ///     Gets the Dalamud plugin interface.
    /// </summary>
    [PluginService]
    internal static IDalamudPluginInterface Interface { get; private set; } = null!;

    /// <summary>
    ///     Gets the Dalamud client state.
    /// </summary>
    [PluginService]
    internal static IClientState ClientState { get; private set; } = null!;

    /// <summary>
    ///     Gets the Dalamud command manager.
    /// </summary>
    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    /// <summary>
    ///     Gets the Dalamud command manager.
    /// </summary>
    [PluginService]
    internal static IGameInteropProvider Interop { get; private set; } = null!;

    /// <summary>
    ///     Gets the scanner.
    /// </summary>
    [PluginService]
    internal static ISigScanner Scanner { get; private set; } = null!;
}
