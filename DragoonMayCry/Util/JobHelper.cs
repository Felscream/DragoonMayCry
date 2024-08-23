using DragoonMayCry.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FFXIVClientStructs.FFXIV.Client.Game.Character.VfxContainer;

namespace DragoonMayCry.Util
{
    public class JobHelper
    {
        public static JobIds IdToJob(uint job) => job < 19 ? JobIds.OTHER : (JobIds)job;

        public static JobIds GetCurrentJob()
        {
            if (Service.ClientState == null ||
                Service.ClientState.LocalPlayer == null)
            {
                Service.Log.Debug("Cannot find current player job, character not found");
                return JobIds.OTHER;
            }

            return IdToJob(Service.ClientState.LocalPlayer.ClassJob.Id);
        }
    }
}
