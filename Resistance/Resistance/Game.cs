using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resistance
{
    public class Game : IGame
    {

        public event EventHandler RolesAssigned;
        public event EventHandler LeaderChanged;
        public event EventHandler OperativesChosen;
        public event EventHandler MissionStarting;
        public event EventHandler MissionCompleted;
        public event EventHandler GameOver;

        public GameState State { get; private set; }
        public List<IPlayer> Players { get; private set; }
        public List<IMission> Missions { get; private set; }
        public int PlayerCount { get; private set; }

        private IPlayer _leader;
        public IPlayer Leader { get { return _leader; } private set { _leader = value; if (LeaderChanged != null) LeaderChanged(this, EventArgs.Empty); } }

        private List<IPlayer> _spies; 

        public Game(IEnumerable<IPlayer> players, IEnumerable<IMission> missions )
        {
            Players = (players ?? Enumerable.Empty<IPlayer>()).ToList();
            Missions = (missions ?? Enumerable.Empty<IMission>()).ToList();
            PlayerCount = Players.Count();

            State = GameState.NotReady;
        }

        private bool PlayerIsInGame(IPlayer player)
        {
            return Players.Contains(player);
        }

        public bool SelectLeader(IPlayer leader)
        {
            if (State != GameState.SelectingLeader) return false;
            if (!PlayerIsInGame(leader)) return false;

            State = GameState.SelectingMissionOperatives;
            Leader = leader;

            return true;
        }

        private void AnnounceRoles()
        {
            
        }

        public bool SelectSpies(IEnumerable<IPlayer> spies)
        {
            if (State != GameState.NotReady) return false;
            _spies = (spies ?? Enumerable.Empty<IPlayer>()).ToList();
            if(_spies.All(PlayerIsInGame))
            {
                AnnounceRoles();
                State = GameState.SelectingLeader;
            } else
            {
                _spies = null;
                return false;
            }
            return true;
        }
    }
}
