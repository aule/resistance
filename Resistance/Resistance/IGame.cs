using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resistance
{
    public interface IGame
    {
        int PlayerCount { get; }
        IPlayer Leader { get; }
        GameState State { get; }
        //int Successes { get; }
        //int Failures { get; }

        event EventHandler RolesAssigned;
        event EventHandler LeaderChanged;
        event EventHandler OperativesChosen;
        event EventHandler MissionStarting;
        event EventHandler MissionCompleted;
        event EventHandler GameOver;

        bool SelectLeader(IPlayer leader);
        bool SelectSpies(IEnumerable<IPlayer> spies);
        //bool ChooseOperatives(IEnumerable<IPlayer> operatives);


    }
}
