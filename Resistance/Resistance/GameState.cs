using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resistance
{

    public enum GameState
    {
        NotReady,
        SelectingLeader,
        WaitingForMission,
        SelectingMissionOperatives,
        Voting,
        Mission,
        End
    }
}
