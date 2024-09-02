using Dalamud.Interface.Windowing;
using DragoonMayCry.Configuration;
using ImGuiNET;
using System;
using System.Numerics;
using DragoonMayCry.Score.Style;

namespace DragoonMayCry.UI;

public class ConfigWindow : Window, IDisposable
{
    private readonly DmcConfiguration configuration;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(DmcConfiguration configuration) : base("Dragoon May Cry###DmC")
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
        if (ImGui.Checkbox("Lock score window", ref lockScoreWindow))
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

        var playSoundEffectsOnBlunder = configuration.PlaySoundEffectsOnBlunder;
        if (ImGui.Checkbox("Play sound effect on blunders", ref playSoundEffectsOnBlunder))
        {
            configuration.PlaySoundEffectsOnBlunder = playSoundEffectsOnBlunder;
            configuration.Save();
        }

        var applyGameVolume = configuration.ApplyGameVolume;
        if (ImGui.Checkbox("Apply game volume", ref applyGameVolume))
        {
            configuration.ApplyGameVolume = applyGameVolume;
            configuration.Save();
        }

        var soundEffectVolume = configuration.SfxVolume;
        if (ImGui.SliderInt("Sound effect volume", ref soundEffectVolume, 0,
                            100))
        {
            configuration.SfxVolume = soundEffectVolume;
            configuration.Save();
        }

        var announcerCooldown = configuration.PlaySfxEveryOccurrences;
        ImGui.Text("Play each unique SFX only once every");
        if (ImGui.SliderInt("occurrences", ref announcerCooldown, 1, 20))
        {
            configuration.PlaySfxEveryOccurrences = announcerCooldown;
            configuration.Save();
        }
    }
}
