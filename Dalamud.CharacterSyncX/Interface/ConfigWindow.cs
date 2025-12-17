using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace Dalamud.CharacterSyncX.Interface;

internal class ConfigWindow() : Window("Character Sync X 配置界面",
                                       ImGuiWindowFlags.NoCollapse       |
                                       ImGuiWindowFlags.AlwaysAutoResize |
                                       ImGuiWindowFlags.NoScrollbar)
{
    public override void Draw()
    {
        if (Service.ObjectTable.LocalPlayer is not { } localPlayer)
        {
            ImGui.Text("请先登录");
            return;
        }

        using (ImRaii.Group())
        {
            ImGui.TextColored(ImGuiColors.TankBlue, "当前角色:");

            ImGui.SameLine();
            ImGui.Text($"{localPlayer.Name}@{localPlayer.HomeWorld.Value.Name} (FFXIV_CHR{Service.PlayerState.ContentId:X16})");

            using (ImRaii.PushIndent())
            {
                if (ImGui.Button("设置为主角色"))
                {
                    Service.Configuration.Cid     = Service.PlayerState.ContentId;
                    Service.Configuration.SetName = $"{localPlayer.Name}@{localPlayer.HomeWorld.Value.Name}";
                    Service.Configuration.Save();
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("请在设置后尽快重启一次游戏以让设置生效\n你可以使用 /xlrestart 指令或点击右侧按钮快速重启");

                ImGui.SameLine();
                if (ImGui.Button("设置为主角色并重启"))
                {
                    Service.Configuration.Cid     = Service.PlayerState.ContentId;
                    Service.Configuration.SetName = $"{localPlayer.Name}@{localPlayer.HomeWorld.Value.Name}";
                    Service.Configuration.Save();

                    [DllImport("kernel32.dll")]
                    [return: MarshalAs(UnmanagedType.Bool)]
                    static extern void RaiseException(uint dwExceptionCode, uint dwExceptionFlags, uint nNumberOfArguments, IntPtr lpArguments);

                    RaiseException(0x12345678, 0, 0, IntPtr.Zero);
                    Process.GetCurrentProcess().Kill();
                }
            }
        }

        ImGui.Spacing();

        using (ImRaii.Group())
        {
            ImGui.TextColored(ImGuiColors.TankBlue, "主角色:");

            var isMainSet = Service.Configuration.Cid != 0 &&
                            !string.IsNullOrWhiteSpace(Service.Configuration.SetName);
            ImGui.SameLine();
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed, Service.Configuration.Cid == 0))
            {
                ImGui.Text(!isMainSet
                               ? "暂未设置, 请登录至想要设置为主角色的游戏角色后点击上方按钮"
                               : $"{Service.Configuration.SetName} (FFXIV_CHR{Service.Configuration.Cid:X16})");
            }
        }

        ImGui.NewLine();

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.ParsedBlue, "设置");

        ImGui.SameLine();
        if (ImGui.SmallButton("保存"))
            Service.Configuration.Save();

        ImGui.Separator();

        ImGui.Checkbox("同步热键栏",         ref Service.Configuration.SyncHotbars);
        ImGui.Checkbox("同步宏",           ref Service.Configuration.SyncMacro);
        ImGui.Checkbox("同步按键设置",        ref Service.Configuration.SyncKeybind);
        ImGui.Checkbox("同步聊天设置",        ref Service.Configuration.SyncLogfilter);
        ImGui.Checkbox("同步角色设置",        ref Service.Configuration.SyncCharSettings);
        ImGui.Checkbox("同步键盘设置",        ref Service.Configuration.SyncKeyboardSettings);
        ImGui.Checkbox("同步手柄设置",        ref Service.Configuration.SyncGamepadSettings);
        ImGui.Checkbox("同步九宫幻卡与萌宠之王设置", ref Service.Configuration.SyncCardSets);
    }
}
