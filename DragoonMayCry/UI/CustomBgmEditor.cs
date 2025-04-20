#region

using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;

#endregion

namespace DragoonMayCry.UI
{
    public class CustomBgmEditor : Window
    {

        public CustomBgmEditor() : base(
            "DragoonMayCry - BGM Editor ##dmc-bgm-editor") { }

        public override void Draw()
        {
            using var bgmEditorMainTable = ImRaii.Table("BgmEditorList", 2, ImGuiTableFlags.Resizable);
            if (!bgmEditorMainTable)
            {
                return;
            }
            
        }
    }
}
