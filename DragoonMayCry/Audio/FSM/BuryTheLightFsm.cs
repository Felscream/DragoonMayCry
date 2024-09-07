using Dalamud.Plugin.Services;
using DragoonMayCry.Audio.FSM.States;
using DragoonMayCry.Audio.FSM.States.BuryTheLight;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
namespace DragoonMayCry.Audio.FSM
{
    public class BuryTheLightFsm : IDisposable
    {
        protected Dictionary<BgmState, FsmState> states;
        // The current state.
        protected FsmState currentState;
        private AudioService audioService;
        private IFramework framework;
        private bool isActive;
        private Stopwatch bgmTimer;
        private bool isGameBgmActive;
        public BuryTheLightFsm(AudioService audioService, IFramework framework)
        {
            this.framework = framework;
            this.audioService = audioService;
            bgmTimer = new Stopwatch();
            FsmState intro = new BTLIntro(BgmState.Intro, audioService);
            states = new Dictionary<BgmState, FsmState>();
            states.Add(BgmState.Intro, intro);
            currentState = intro;

            this.framework.Update += Update;
        }

        public static string GetPathToAudio(string name)
        {
            return Path.Combine(
                Plugin.PluginInterface.AssemblyLocation.Directory?.FullName!,
                $"Assets\\Audio\\bury_the_light\\{name}");
        }

        public void Start()
        {
            if (isActive)
            {
                return;
            }
            if(Service.GameConfig.TryGet(Dalamud.Game.Config.SystemConfigOption.IsSndBgm, out bool val))
            {
                isGameBgmActive = val;
            }
            DisableGameBgm();
            isActive = true;
            currentState.Start();
        }

        public void Update(IFramework framework)
        {
            currentState.Update();
        }

        public void CacheBgm()
        {
            foreach(KeyValuePair<BgmState, FsmState> entry in states){
                var bgmsParts = entry.Value.GetBgmPaths();
                foreach(KeyValuePair<BgmId, string> parts in bgmsParts)
                {
                    audioService.RegisterBgmPart(parts.Key, parts.Value);
                }
            }
        }

        public void Dispose()
        {
            ResetGameBgm();
            this.framework.Update -= Update;
        }

        public void Reset()
        {
            isActive = false;
            foreach(KeyValuePair<BgmState, FsmState> entry in states)
            {
                entry.Value.Reset();
            }
            ResetGameBgm();
        }

        private void DisableGameBgm()
        {
            Service.GameConfig.Set(Dalamud.Game.Config.SystemConfigOption.IsSndBgm, true);
        }

        private void ResetGameBgm()
        {
            Service.GameConfig.Set(Dalamud.Game.Config.SystemConfigOption.IsSndBgm, isGameBgmActive);
        }
    }


}
