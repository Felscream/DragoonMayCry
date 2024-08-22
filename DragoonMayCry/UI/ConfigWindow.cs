using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using DragoonMayCry.Configuration;
using ImGuiNET;

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

        Size = new Vector2(232, 120);
        SizeCondition = ImGuiCond.Always;

        Configuration = configuration;
    }

    public void Dispose() { }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        var configValue = Configuration.SomePropertyToBeSavedAndWithADefault;
        if (ImGui.Checkbox("Random Config Bool", ref configValue))
        {
            Configuration.SomePropertyToBeSavedAndWithADefault = configValue;
            // can save immediately on change, if you don't want to provide a "Save and Close" button
            Configuration.Save();
        }

        var lockScoreWindow = Configuration.StyleRankUiConfiguration.LockScoreWindow;
        if (ImGui.Checkbox("Lock Score Window", ref lockScoreWindow))
        {
            Configuration.StyleRankUiConfiguration.LockScoreWindow = lockScoreWindow;
            Configuration.Save();
        }

        if (ImGui.Button("Next rank"))
        {
            Plugin.ScoreManager.GoToNextRank();
        }

    }
}
