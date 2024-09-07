using Dalamud.Interface.Windowing;
using DragoonMayCry.Configuration;
using ImGuiNET;
using System;
using System.Numerics;
using DragoonMayCry.Score.Style;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using DragoonMayCry.State;
using KamiLib.Drawing;
using KamiLib;

namespace DragoonMayCry.UI;

public class ConfigWindow : Window, IDisposable
{
    public EventHandler<bool>? ActiveOutsideInstanceChange;
    private readonly DmcConfigurationOne configuration;

    // We give this window a constant ID using ###
    // This allows for labels being dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(PluginUI pluginUi, DmcConfigurationOne configuration) : base("DragoonMayCry - Configuration")
    {

        Size = new Vector2(400,250);
        SizeCondition = ImGuiCond.FirstUseEver;

        this.configuration = configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        InfoBox.Instance.AddTitle("General")
            .AddConfigCheckbox("Lock rank window", configuration.LockScoreWindow)
            //.AddConfigCheckbox("Active outside instance", configuration.ActiveOutsideInstance)
            .AddAction(() =>
            {
                if(ImGui.Checkbox("Active outside instance", ref configuration.ActiveOutsideInstance.Value)){
                    KamiCommon.SaveConfiguration();
                    ActiveOutsideInstanceChange?.Invoke(this, configuration.ActiveOutsideInstance.Value);
                }
            })
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
