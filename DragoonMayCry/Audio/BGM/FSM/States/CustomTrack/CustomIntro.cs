using System.Collections.Generic;
using DragoonMayCry.Audio.BGM.Editor.Model;

namespace DragoonMayCry.Audio.BGM.FSM.States.CustomTrack;

public class CustomIntro : IFsmState
{
    public BgmState ID
    {
        get { return BgmState.Intro; }
    }

    public CustomIntro(DynamicTrack track) { }

    public Dictionary<BgmId, string> GetBgmPaths()
    {
        throw new System.NotImplementedException();
    }

    public void Enter(bool fromLoop)
    {
        throw new System.NotImplementedException();
    }

    public void Update()
    {
        throw new System.NotImplementedException();
    }

    public void Reset()
    {
        throw new System.NotImplementedException();
    }

    public int Exit(ExitType exit)
    {
        throw new System.NotImplementedException();
    }

    public bool CancelExit()
    {
        throw new System.NotImplementedException();
    }
}
