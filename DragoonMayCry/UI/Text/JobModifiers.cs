namespace DragoonMayCry.UI.Text
{
    internal class JobModifiers
    {
        public static string BrdModifiers = "- Refreshing your two DoTs with Iron Jaws before they fade on a target, or with your raid buffs active will grant you bonus points.";
        public static string HealerModifiers = "- Refreshing your DoT before it fades on a target will grant you bonus points.";
        public static string ScholarModifiers = HealerModifiers + "\n- Energy Drain grants bonus points.";
        public static string WhiteMageModifiers = HealerModifiers + "\n- Using Afflatus Solace or Afflatus Rapture, while an enemy is targetable, will grant points scaling with the number of targets hit.";
        public static string AstrologianModifiers = HealerModifiers + "\n- Using cards grants you points. \n- Lady of the Crown scales with the number of targets hit when an enemy is targetable.";
        public static string SageModifiers = HealerModifiers + "\n- Using Addersgall-based abilities will grant you points if an aenemy target is present.\n- Ixochole scales with the number of targets hit.";
    }
}
