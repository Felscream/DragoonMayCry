using Dalamud.Interface.Windowing;
using DragoonMayCry.Configuration;
using ImGuiNET;
using System;
using System.Numerics;

namespace DragoonMayCry.UI;

public class ConfigWindow : Window, IDisposable
{
    private DmcConfiguration configuration;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(DmcConfiguration configuration) : base("A Wonderful Configuration Window###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(0,0);
        SizeCondition = ImGuiCond.Always;

        this.configuration = configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        var lockScoreWindow = configuration.StyleRankUiConfiguration.LockScoreWindow;
        if (ImGui.Checkbox("Lock Score Window", ref lockScoreWindow))
        {
            configuration.StyleRankUiConfiguration.LockScoreWindow = lockScoreWindow;
            configuration.Save();
        }

        var activeOutsideInstance = configuration.ActiveOutsideInstance;
        if (ImGui.Checkbox("Active outside instance",
                           ref activeOutsideInstance))
        {
            configuration.ActiveOutsideInstance = activeOutsideInstance;
            configuration.Save();
        }

        var playSoundEffects = configuration.PlaySoundEffects;
        if (ImGui.Checkbox("Play sound effects", ref playSoundEffects))
        {
            configuration.PlaySoundEffects = playSoundEffects;
            configuration.Save();
        }

        var soundEffectVolume = configuration.SfxVolume;
        if (ImGui.SliderInt("Sound effect volume", ref soundEffectVolume, 0,
                            100))
        {
            configuration.SfxVolume = soundEffectVolume;
            configuration.Save();
        }

#if DEBUG
        if (ImGui.Button("Next rank"))
        {
            Plugin.StyleRankHandler!.GoToNextRank(true, true);
        }
        if (ImGui.Button("Previous rank"))
        {
            Plugin.StyleRankHandler!.ReturnToPreviousRank(false);
        }

        var testing = configuration.StyleRankUiConfiguration.TestRankDisplay;
        if (ImGui.Checkbox("Test rank display", ref testing))
        {
            configuration.StyleRankUiConfiguration.TestRankDisplay = testing;
            configuration.Save();
        }

        if (testing)
        {
            var debugProgressValue = (float)configuration.StyleRankUiConfiguration.DebugProgressValue;
            if (ImGui.SliderFloat("Progress value", ref debugProgressValue, 0,
                                1))
            {
                configuration.StyleRankUiConfiguration.DebugProgressValue = debugProgressValue;
                configuration.Save();
            }

            /*var progressBarTint =
                Configuration.StyleRankUiConfiguration.ProgressBarTint;

            if (ImGui.ColorPicker4("Progress bar tint", ref progressBarTint))
            {
                Configuration.StyleRankUiConfiguration.ProgressBarTint =
                    progressBarTint;
            }*/

        }
#endif
    }
}
