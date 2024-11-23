using Dalamud.Game.ClientState.Objects;
using Dalamud.Interface;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace DragoonMayCry
{
    public class Service
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [PluginService]
        public static ITextureProvider TextureProvider { get; private set; }

        [PluginService]
        public static IClientState ClientState { get; private set; }

        [PluginService]
        public static ICommandManager CommandManager { get; private set; }

        [PluginService]
        public static IFlyTextGui FlyText { get; private set; }

        [PluginService]
        public static IGameInteropProvider Hook { get; private set; }

        [PluginService]
        public static IPluginLog Log { get; private set; }

        [PluginService]
        public static IGameGui GameGui { get; private set; }

        [PluginService]
        public static ICondition Condition { get; private set; }

        [PluginService]
        public static IFramework Framework { get; private set; }

        [PluginService]
        public static IGameConfig GameConfig { get; private set; }

        [PluginService]
        public static IObjectTable ObjectTable { get; private set; }

        [PluginService]
        public static INotificationManager NotificationManager { get; private set; }

        [PluginService]
        public static IChatGui ChatGui { get; private set; }

        [PluginService]
        public static IDutyState DutyState { get; private set; }

        [PluginService]
        public static IDataManager DataManager { get; private set; }

        [PluginService]
        public static ITargetManager TargetManager { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}
