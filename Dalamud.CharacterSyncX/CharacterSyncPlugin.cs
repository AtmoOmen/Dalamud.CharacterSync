using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Dalamud.CharacterSyncX.Interface;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.RichPresence.Config;

namespace Dalamud.CharacterSyncX;

/// <summary>
///     Main plugin class.
/// </summary>
internal partial class CharacterSyncPlugin : IDalamudPlugin
{
    private const string FileInterfaceOpenFileSignature = "E8 ?? ?? ?? ?? 3C 01 0F 85 1C 04 00 00";
    
    public static IPluginLog PluginLog = null!;

    private readonly WindowSystem  windowSystem;
    private readonly ConfigWindow  configWindow;
    
    private readonly Hook<FileInterfaceOpenFileDelegate> openFileHook;

    private readonly Regex saveFolderRegex = MyRegex();

    /// <summary>
    ///     Initializes a new instance of the <see cref="CharacterSyncPlugin" /> class.
    /// </summary>
    public CharacterSyncPlugin(IDalamudPluginInterface interf, IPluginLog pluginLog)
    {
        PluginLog = pluginLog;

        interf.Create<Service>();

        Service.Configuration = Service.Interface.GetPluginConfig() as CharacterSyncConfig ?? new CharacterSyncConfig();

        configWindow  = new();
        windowSystem  = new("CharacterSync");
        windowSystem.AddWindow(configWindow);

        Service.Interface.UiBuilder.Draw         += windowSystem.Draw;
        Service.Interface.UiBuilder.OpenConfigUi += OnOpenConfigUi;

        Service.CommandManager.AddHandler("/pcharsync", new CommandInfo(OnChatCommand)
        {
            HelpMessage = "打开配置界面",
            ShowInHelp  = true
        });

        try
        {
            DoBackup();
        }
        catch (Exception ex)
        {
            PluginLog.Error(ex, "无法备份玩家数据");
        }

        openFileHook =
            Service.Interop.HookFromSignature<FileInterfaceOpenFileDelegate>(
                FileInterfaceOpenFileSignature, OpenFileDetour);
        openFileHook.Enable();
    }

    private delegate IntPtr FileInterfaceOpenFileDelegate(
        IntPtr                                   pFileInterface,
        [MarshalAs(UnmanagedType.LPWStr)] string filepath,
        uint                                     a3);
    
    /// <inheritdoc />
    public void Dispose()
    {
        Service.CommandManager.RemoveHandler("/pcharsync");
        Service.Interface.UiBuilder.Draw -= windowSystem.Draw;
        openFileHook.Dispose();
    }

    private void OnOpenConfigUi()
    {
        configWindow.Toggle();
    }

    private void OnChatCommand(string command, string arguments)
    {
        configWindow.Toggle();
    }

    private static void DoBackup()
    {
        var configFolder = Service.Interface.GetPluginConfigDirectory();
        Directory.CreateDirectory(configFolder);

        var backupFolder = new DirectoryInfo(Path.Combine(configFolder, "backups"));
        Directory.CreateDirectory(backupFolder.FullName);

        var folders = backupFolder.GetDirectories().OrderBy(x => long.Parse(x.Name)).ToArray();
        if (folders.Length > 2) folders.FirstOrDefault()?.Delete(true);

        var thisBackupFolder =
            Path.Combine(backupFolder.FullName, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
        Directory.CreateDirectory(thisBackupFolder);

        var xivFolder = new DirectoryInfo(Path.Combine(
                                              Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                              "My Games",
                                              "FINAL FANTASY XIV - A Realm Reborn"));

        if (!xivFolder.Exists)
        {
            PluginLog.Error("未找到游戏数据文件夹");
            return;
        }

        foreach (var directory in xivFolder.GetDirectories("FFXIV_CHR*"))
        {
            var thisBackupFile = Path.Combine(thisBackupFolder, directory.Name);
            PluginLog.Information(thisBackupFile);
            Directory.CreateDirectory(thisBackupFile);

            foreach (var filePath in directory.GetFiles("*.DAT"))
                File.Copy(filePath.FullName, filePath.FullName.Replace(directory.FullName, thisBackupFile), true);
        }

        PluginLog.Information("已完成备份");
    }

    private IntPtr OpenFileDetour(IntPtr pFileInterface, [MarshalAs(UnmanagedType.LPWStr)] string filepath, uint a3)
    {
        try
        {
            if (Service.Configuration.Cid != 0)
            {
                var match = saveFolderRegex.Match(filepath);
                if (match.Success)
                {
                    var rootPath = match.Groups["path"].Value;
                    var datName  = match.Groups["dat"].Value;

                    if (PerformRewrite(datName))
                        filepath = $"{rootPath}FFXIV_CHR{Service.Configuration.Cid:X16}/{datName}";
                }
            }
        }
        catch (Exception ex) { PluginLog.Error(ex, "尝试拦截游戏文件写入时发生错误"); }

        return openFileHook.Original(pFileInterface, filepath, a3);
    }

    private static bool PerformRewrite(string datName)
    {
        switch (datName)
        {
            case "HOTBAR.DAT" when Service.Configuration.SyncHotbars:
            case "MACRO.DAT" when Service.Configuration.SyncMacro:
            case "KEYBIND.DAT" when Service.Configuration.SyncKeybind:
            case "LOGFLTR.DAT" when Service.Configuration.SyncLogfilter:
            case "COMMON.DAT" when Service.Configuration.SyncCharSettings:
            case "CONTROL0.DAT" when Service.Configuration.SyncKeyboardSettings:
            case "CONTROL1.DAT" when Service.Configuration.SyncGamepadSettings:
            case "GS.DAT" when Service.Configuration.SyncCardSets:
            case "ADDON.DAT":
                return true;
        }

        return false;
    }

    [GeneratedRegex(@"(?<path>.*)FFXIV_CHR(?<cid>.*)\/(?!ITEMODR\.DAT|ITEMFDR\.DAT|GEARSET\.DAT|UISAVE\.DAT|.*\.log)(?<dat>.*)", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex MyRegex();
}
