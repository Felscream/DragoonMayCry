using Dalamud.Game.Command;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Hooking;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DragoonMayCry.Configuration;
using DragoonMayCry.Score;
using DragoonMayCry.State;
using DragoonMayCry.UI;
using FFXIVClientStructs.FFXIV.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace DragoonMayCry;

public unsafe class Plugin : IDalamudPlugin
{
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    public static DmcConfiguration? Configuration { get; private set; } = null;

    private const string CommandName = "/dmc";
    private const char SpecialChar = '\u00A7';

    private HashSet<FlyTextKind> _validKinds = new HashSet<FlyTextKind>() {
            FlyTextKind.Damage,
            FlyTextKind.DamageCrit,
            FlyTextKind.DamageDh,
            FlyTextKind.DamageCritDh
        };

    private delegate void AddFlyTextDelegate(
        IntPtr addonFlyText,
        uint actorIndex,
        uint messageMax,
        IntPtr numbers,
        uint offsetNum,
        uint offsetNumMax,
        IntPtr strings,
        uint offsetStr,
        uint offsetStrMax,
        int unknown);
    private readonly Hook<AddFlyTextDelegate> addFlyTextHook;

    public static ScoreManager ScoreManager { get; private set; } = null;
    public static PluginUI PluginUI { get; private set; } = null;

    private static object[] _ftLocks = Enumerable.Repeat(new object(), 50).ToArray();
    private readonly IPluginLog logger;
    private readonly PlayerState playerState;
    private readonly ScoreProgressBar scoreProgressBar;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        PluginInterface.Create<Service>();
        logger = Service.Log;
        Configuration = PluginInterface.GetPluginConfig() as DmcConfiguration ?? new DmcConfiguration();
        
        
        playerState = new();
        
        ScoreManager = new(playerState);
        scoreProgressBar = new(ScoreManager);
        PluginUI = new(playerState, scoreProgressBar);


        Service.ClientState.Logout += ScoreManager.OnLogout;

        Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        try
        {
            var addFlyTextAddress = Service.Scanner.ScanText("E8 ?? ?? ?? ?? FF C7 41 D1 C7");
            addFlyTextHook = Service.Hook.HookFromAddress<AddFlyTextDelegate>(addFlyTextAddress, AddFlyTextDetour);

        }
        catch (Exception ex)
        {
            logger.Error(ex, $"An error occurred loading DragoonMayCry Plugin.");
            logger.Error("Plugin will not be loaded.");

            addFlyTextHook?.Disable();
            addFlyTextHook?.Dispose();

            throw;
        }

        addFlyTextHook?.Enable();
    }

    public void Dispose()
    {
        PluginUI.Dispose();
        playerState.Dispose();
        addFlyTextHook?.Disable();
        addFlyTextHook?.Dispose();
        scoreProgressBar.Dispose();

        Service.ClientState.Logout -= ScoreManager.OnLogout;

        Service.CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // in response to the slash command, just toggle the display status of our main ui
        PluginUI.ToggleConfigUI();
    }

    private void AddFlyTextDetour(
            IntPtr addonFlyText,
            uint actorIndex,
            uint messageMax,
            IntPtr numbers,
            uint offsetNum,
            uint offsetNumMax,
            IntPtr strings,
            uint offsetStr,
            uint offsetStrMax,
            int unknown)
    {
        if (!Configuration.Enabled || actorIndex <= 1 || actorIndex >= 50)
        {
            // don't lock this since locks may not be enough
            addFlyTextHook.Original(
                addonFlyText,
                actorIndex,
                messageMax,
                numbers,
                offsetNum,
                offsetNumMax,
                strings,
                offsetStr,
                offsetStrMax,
                unknown);
            return;
        }
        try
        {
            // Known valid flytext region within the atk arrays
            // actual index
            var strIndex = 27;
            var numIndex = 30;
            var atkArrayDataHolder = ((UIModule*)Service.GameGui.GetUIModule())->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;
            logger.Debug($"addonFlyText: {addonFlyText:X} actorIndex:{actorIndex} offsetNum: {offsetNum} offsetNumMax: {offsetNumMax} offsetStr: {offsetStr} offsetStrMax: {offsetStrMax} unknown:{unknown}");
            try
            {
                var strArray = atkArrayDataHolder._StringArrays[strIndex];
                var flyText1Ptr = strArray->StringArray[offsetStr];
                if (flyText1Ptr == null || (nint)flyText1Ptr == IntPtr.Zero)
                {
                    lock (_ftLocks[actorIndex])
                    {
                        addFlyTextHook.Original(
                            addonFlyText,
                            actorIndex,
                            messageMax,
                            numbers,
                            offsetNum,
                            offsetNumMax,
                            strings,
                            offsetStr,
                            offsetStrMax,
                            unknown);
                    }
                    return;
                }
                var numArray = atkArrayDataHolder._NumberArrays[numIndex];
                var kind = numArray->IntArray[offsetNum + 1];
                var val1 = numArray->IntArray[offsetNum + 2];
                var val2 = numArray->IntArray[offsetNum + 3];
                int damageTypeIcon = numArray->IntArray[offsetNum + 4];
                int color = numArray->IntArray[offsetNum + 6];
                int icon = numArray->IntArray[offsetNum + 7];
                var text1 = Marshal.PtrToStringUTF8((nint)flyText1Ptr);
                var flyText2Ptr = strArray->StringArray[offsetStr + 1];
                var text2 = Marshal.PtrToStringUTF8((nint)flyText2Ptr);
               
                logger.Debug($"text1:{text1} text2:{text2}");
                if (text1 == null || text2 == null)
                {
                    lock (_ftLocks[actorIndex])
                    {
                        addFlyTextHook.Original(
                            addonFlyText,
                            actorIndex,
                            messageMax,
                            numbers,
                            offsetNum,
                            offsetNumMax,
                            strings,
                            offsetStr,
                            offsetStrMax,
                            unknown);
                    }
                    return;
                }
                if (text1.EndsWith(SpecialChar) && text1.Length >= 1)
                {
                    var bytes = Encoding.UTF8.GetBytes(text1.Substring(0, text1.Length - 1));
                    Marshal.WriteByte((nint)flyText1Ptr + bytes.Length, 0);
                    lock (_ftLocks[actorIndex])
                    {
                        addFlyTextHook.Original(
                            addonFlyText,
                            actorIndex,
                            messageMax,
                            numbers,
                            offsetNum,
                            offsetNumMax,
                            strings,
                            offsetStr,
                            offsetStrMax,
                            unknown);
                    }
                    return;
                }

                FlyTextKind flyKind = (FlyTextKind)kind;
                String? shownActionName = null;
                if (text1 != string.Empty)
                {
                    shownActionName = text1;
                }
                if (shownActionName == null || val1 <= 0)
                {
                    logger.Debug($"val1:{val1} is not valid");
                    lock (_ftLocks[actorIndex])
                    {
                        addFlyTextHook.Original(
                            addonFlyText,
                            actorIndex,
                            messageMax,
                            numbers,
                            offsetNum,
                            offsetNumMax,
                            strings,
                            offsetStr,
                            offsetStrMax,
                            unknown);
                    }
                    return;
                }
                logger.Debug($"val1:{val1} val2:{val2} kind:{Enum.GetName(typeof(FlyTextKind), kind)}");
                ScoreManager.AddScore(val1);
            }
            catch (Exception e)
            {
                logger.Error(e, "Skipping");
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "An error has occurred in MultiHit");
        }
        
        addFlyTextHook.Original(
                            addonFlyText,
                            actorIndex,
                            messageMax,
                            numbers,
                            offsetNum,
                            offsetNumMax,
                            strings,
                            offsetStr,
                            offsetStrMax,
                            unknown);
    }
}
