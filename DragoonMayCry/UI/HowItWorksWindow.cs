#region

using Dalamud.Interface.Windowing;
using DragoonMayCry.UI.Text;
using ImGuiNET;
using System.Numerics;

#endregion

namespace DragoonMayCry.UI
{
    public class HowItWorksWindow : Window
    {
        private readonly Vector4 goldColor = new(229, 48, 0, 255);
        public HowItWorksWindow() : base("DragoonMayCry - How it works")
        {
            Size = new Vector2(790, 400);
            SizeCondition = ImGuiCond.Appearing;
            Flags = ImGuiWindowFlags.HorizontalScrollbar;
        }

        public override void Draw()
        {
            if (ImGui.BeginTabBar("HIW"))
            {
                if (ImGui.BeginTabItem("General"))
                {
                    DrawGeneralDescription();
                    ImGui.EndTabItem();
                }
                ImGui.PushTextWrapPos(760);
                if (ImGui.BeginTabItem("AST"))
                {
                    DrawJobModifiers("Astrologian", JobModifiers.AstrologianModifiers);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("BRD"))
                {
                    DrawJobModifiers("Bard", JobModifiers.BrdModifiers);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("SCH"))
                {
                    DrawJobModifiers("Scholar", JobModifiers.ScholarModifiers);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("SGE"))
                {
                    DrawJobModifiers("Sage", JobModifiers.SageModifiers);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("WHM"))
                {
                    DrawJobModifiers("White Mage", JobModifiers.WhiteMageModifiers);
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }
        }

        private void DrawJobModifiers(string job, string modifiers)
        {
            ImGui.Indent();
            ImGui.TextColored(goldColor, $"\n{job}");
            ImGui.TextWrapped(modifiers);
        }

        private void DrawGeneralDescription()
        {
            ImGui.PushTextWrapPos(760);
            ImGui.Indent();
            ImGui.Text(
                "\nDragoonMayCry tries to add flavour to the game's combat by ranking you using a Devil May Cry like 'style' system.");
            ImGui.Text("Classes and PvP are not supported.");

            ImGui.TextColored(goldColor, "\nRanking up");
            ImGui.Text("- You fill your style gauge by dealing damage. The value is read from the flying text.");
            ImGui.Text(
                "- The expected damage output is based on your role and iLvL at the start of the encounter. It is calculated using an exponential regression on the nDPS 80th percentile from FFLogs on Dawntrail and Endwalker content, and some legacy ultimates pulls in 7.05.");
            ImGui.Text("- Critical Direct hits will stop the gauge decay for 2 seconds.");
            ImGui.Text("- Limit breaks are cool.");

            ImGui.TextColored(goldColor, "\nDemotions");
            ImGui.Text("- You lose points over time. The higher your rank, the more points you lose.");
            ImGui.Text("- You will be demoted if your style gauge in under a threshold for more than 5 seconds.");
            ImGui.Text(
                "- You won't get demoted if you can't act due to fights mechanics (ex : out of the action, fetters) and the demotion timer didn't already start.");

            ImGui.TextColored(goldColor, "\nBlunders");
            ImGui.Text("Blunders affect your rank significantly.");
            ImGui.Text(
                "- Clipping your GCD for more than 0.2s will substract points from your current gauge, and heavily reduce points gained for the next 8 seconds.");
            ImGui.Text(
                "- Holding your GCD for more than 0.2s, if you are not incapacited, will move you to the previous tier. But if you were in tier S or above, you will return to B tier.");
            ImGui.Text(
                "- Receiving a damage down or any other debuffs applied after a failed mechanic resolution will move you to D*. This only applies if you had no other 'blunder' debuffs before.");
            ImGui.Text("- Dying is bad.");

            ImGui.TextColored(goldColor, "\nFinal rank");
            ImGui.Text(
                "A final rank between D and S is attributed at the end of combat depending on your GCD uptime :");
            ImGui.Indent();
            ImGui.TextUnformatted("- Above 95%  S");
            ImGui.TextUnformatted("- 90% to 95%: A");
            ImGui.TextUnformatted("- 85% to 90%: B");
            ImGui.TextUnformatted("- 75% to 85%: C");
            ImGui.TextUnformatted("- Below 75%: D");
            ImGui.Text("Failing a mechanic incurs a 3 seconds uptime penalty :");
            ImGui.Unindent();

            ImGui.TextColored(goldColor, "\nEstinien Must Die");
            ImGui.Text("Estinien Must Die adds some modifiers");
            ImGui.TextUnformatted("- Rank thresholds are generally 20 % higher");
            ImGui.Text("- You have no breathing room : you MUST press your GCD if an enemy target is available");
            ImGui.Text("- Clipping your GCD will rank you down");
            ImGui.Text("- Final rank GCD uptime thresholds are :");
            ImGui.Indent();
            ImGui.TextUnformatted("- Above 98%: S");
            ImGui.TextUnformatted("- 96% to 98%: A");
            ImGui.TextUnformatted("- 93% to 96%: B");
            ImGui.TextUnformatted("- 88% to 93%: C");
            ImGui.TextUnformatted("- Below 88%: D");
            ImGui.Unindent();

            ImGui.TextColored(goldColor, "\nRecords");
            ImGui.Text("Records are saved on a character per character basis.");
            ImGui.Text("You must complete the duty level synced");
            ImGui.Text(
                "At the end of a successful duty completion, your final rank is processed for your current character, with the current job.");
            ImGui.Text("Only your best rank on the selected job is kept.");
            ImGui.Text("Only duties available in the 'Character records' window are tracked");

            ImGui.Text(
                "\nI'd appreciate any feedback or bug report to improve the experience. You can find me on the XIVLauncher & Dalamud discord server.");

            ImGui.TextColored(new Vector4(.75f, .75f, .75f, 1), "\nTo healers");
            ImGui.TextColored(new Vector4(.75f, .75f, .75f, 1),
                              "Listen people, I don't play the role so I have no idea how the experience is using this plugin as a healer. If you've got some ideas to improve it, send them to me :)");

            ImGui.TextColored(new Vector4(.75f, .75f, .75f, 1),
                              "\n * Not all debuffs across all content have been identified, and some demotions may be applied wrongfully. Don't hesitate to send feedback if you find a debuff that should be removed or added.");
        }
    }


}
