using Dalamud.Interface.Windowing;
using DragoonMayCry.Configuration;
using ImGuiNET;
using System;
using System.Numerics;
using DragoonMayCry.Score.Style;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using DragoonMayCry.State;
using KamiLib.Drawing;

namespace DragoonMayCry.UI;

public class ConfigWindow : Window, IDisposable
{
    public EventHandler<bool> ActiveOutsideInstanceChange;
    private readonly DmcConfigurationOne configuration;
    private readonly PluginUI pluginUI;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(PluginUI pluginUi, DmcConfigurationOne configuration) : base("DragoonMayCry - Configuration")
    {

        Size = new Vector2(400,250);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.configuration = configuration;
        this.pluginUI = pluginUi;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (ImGui.Button("How it works"))
        {
            pluginUI.ToggleHowItWorks();
        }

        InfoBox.Instance.AddTitle("General")
            .AddConfigCheckbox("Lock rank window", configuration.LockScoreWindow)
            .AddConfigCheckbox("Active outside instance", configuration.ActiveOutsideInstance)
            .Draw();

        InfoBox.Instance.AddTitle("Audio")
            .AddConfigCheckbox("Play sound effects", configuration.PlaySoundEffects)
            .AddConfigCheckbox("Force sound effect on blunders", configuration.ForceSoundEffectsOnBlunder)
            .AddConfigCheckbox("Apply game volume", configuration.ApplyGameVolume)
            .AddSliderInt("Sound effect volume", configuration.SfxVolume, 0, 100)
            .AddString("Play each unique SFX only once every")
            .SameLine()
            .AddSliderInt("occurrences", configuration.PlaySfxEveryOccurrences, 1, 20)
            .Draw();
    }
}
