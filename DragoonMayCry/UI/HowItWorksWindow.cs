using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Numerics;

namespace DragoonMayCry.UI
{
    public class HowItWorksWindow : Window, IDisposable
    {
        private readonly Vector4 goldColor = new Vector4(229, 48, 0, 255);
        public HowItWorksWindow(): base("DragoonMayCry - How it works")
        {
            this.Size = new Vector2(800,400);
            this.SizeCondition = ImGuiCond.Appearing;
        }

        public override void Draw()
        {
            ImGui.AlignTextToFramePadding();
            ImGui.PushTextWrapPos(760);
            ImGui.Indent();
            ImGui.Text("\nDragoonMayCry tries to add flavour to the game's combat by ranking you using a Devil May Cry like 'style' system.");
            ImGui.Text("Classes and PvP are not supported.");

            ImGui.TextColored(goldColor, "\nRanking up");
            ImGui.Text("- You fill your style gauge by dealing damage. The value is read from the flying text.");
            ImGui.Text("- DoTs are not currently tracked. Auto-attacks and DoT's that deal no damage on application will not be added to your score.");
            ImGui.Text("- The expected damage output is based on your role and iLvL at the start of the encounter. It is calculated using an exponential regression on the nDPS 80th percentile from FFLogs on Dawntrail and Endwalker content, and some legacy ultimates pulls in 7.05.");
            ImGui.Text("- Limit breaks are cool.");

            ImGui.TextColored(goldColor, "\nDemotions");
            ImGui.Text("- You lose points over time. The higher your rank, the more points you lose.");
            ImGui.Text("- You will be demoted if your style gauge in under a threshold for more than 5 seconds.");
            ImGui.Text("- You won't get demoted if you can't act due to fights mechanics (ex : out of the action, fetters) and the demotion timer didn't already start.");

            ImGui.TextColored(goldColor, "\nBlunders");
            ImGui.Text("Blunders affect your rank significantly.");
            ImGui.Text("- Clipping your GCD will substract points from your current gauge, and heavily reduce points gained for the next 8 seconds.");
            ImGui.Text("- Holding your GCD, if you are not incapacited, will move you to the previous tier. But if you were in tier S or above, you will return to B tier.");
            ImGui.Text("- Receiving a damage down or any other debuffs applied after a failed mechanic resolution will move you to D*. This only applies if you had no other 'blunder' debuffs before.");
            ImGui.Text("- Dying is bad.");

            ImGui.TextColored(goldColor, "\nFinal rank");
            ImGui.Text("- A final rank between D and S is attributed at the end of combat by selecting the tier in which you spent the most time in.");
            ImGui.Text("- Getting demoted to a rank lower than A will substract time spent in S tier.");

            ImGui.Text("\nI'd appreciate any feedback or bug report to improve the experience. You can find me on the XIVLauncher & Dalamud discord server.");

            ImGui.TextColored(new Vector4(.75f, .75f, .75f, 1), "\nTo healers");
            ImGui.TextColored(new Vector4(.75f, .75f, .75f, 1), "Listen people, I don't play the role so I have no idea how the experience is using this plugin as a healer. If you've got some ideas to improve it, send them to me :)");

           ImGui.TextColored(new Vector4(.75f, .75f, .75f, 1), "\n * Not all debuffs across all content have been identified, and some demotions may be applied wrongfully. Don't hesitate to send feedback if you find a debuff that should be removed or added.");

        }

        public void Dispose()
        {

        }
    }
}