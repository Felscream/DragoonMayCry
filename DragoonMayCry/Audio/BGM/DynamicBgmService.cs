using DragoonMayCry.Audio.BGM.FSM;
using DragoonMayCry.Score.Style.Rank;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragoonMayCry.Audio.BGM
{
    class DynamicBgmService : IDisposable
    {
        private readonly DynamicBgmFsm bgmFsm;
        public DynamicBgmService(StyleRankHandler styleRankHandler)
        {
            bgmFsm = new DynamicBgmFsm(styleRankHandler);
        }

        
        public DynamicBgmFsm GetFsm()
        {
            return bgmFsm;
        }

        public void Dispose()
        {
            bgmFsm.Dispose();
        }

        public void ToggleDynamicBgmActivation()
        {

        }
    }
}
