using Dalamud.Interface.Windowing;
using DragoonMayCry.Configuration;
using ImGuiNET;
using System;
using System.Numerics;

namespace DragoonMayCry.UI;

public class ConfigWindow : Window, IDisposable
{
    private DmcConfiguration Configuration;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(DmcConfiguration configuration) : base("A Wonderful Configuration Window###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(0,0);
        SizeCondition = ImGuiCond.Always;

        Configuration = configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var lockScoreWindow = Configuration.StyleRankUiConfiguration.LockScoreWindow;
        if (ImGui.Checkbox("Lock Score Window", ref lockScoreWindow))
        {
            Configuration.StyleRankUiConfiguration.LockScoreWindow = lockScoreWindow;
            Configuration.Save();
        }

        var activeOutsideInstance = Configuration.ActiveOutsideInstance;
        if (ImGui.Checkbox("Active outside instance",
                           ref activeOutsideInstance))
        {
            Configuration.ActiveOutsideInstance = activeOutsideInstance;
            Configuration.Save();
        }

        var playSoundEffects = Configuration.PlaySoundEffects;
        if (ImGui.Checkbox("Play sound effects", ref playSoundEffects))
        {
            Configuration.PlaySoundEffects = playSoundEffects;
            Configuration.Save();
        }

        var soundEffectVolume = Configuration.SfxVolume;
        if (ImGui.SliderInt("Sound effect volume", ref soundEffectVolume, 0,
                            100))
        {
            Configuration.SfxVolume = soundEffectVolume;
            Configuration.Save();
        }

        if (ImGui.Button("Next rank"))
        {
            Plugin.ScoreManager.GoToNextRank(true);
        }

    }
}
