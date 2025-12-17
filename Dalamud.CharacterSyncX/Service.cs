using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.RichPresence.Config;

namespace Dalamud.CharacterSyncX;

internal class Service
{
    internal static CharacterSyncConfig Configuration { get; set; } = null!;


    [PluginService] internal static IDalamudPluginInterface Interface      { get; private set; } = null!;
    [PluginService] internal static IPlayerState            PlayerState    { get; private set; } = null!;
    [PluginService] internal static IObjectTable            ObjectTable    { get; private set; } = null!;
    [PluginService] internal static ICommandManager         CommandManager { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider    Interop        { get; private set; } = null!;
}
