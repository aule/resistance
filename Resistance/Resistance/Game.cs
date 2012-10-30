using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resistance
{
    public class Game : IGame
    {

        public GameState State { get; private set; }

        public List<IPlayer> Players { get; private set; }
        public List<IMission> Missions { get; private set; }
        public int PlayerCount { get; private set; }

        private IPlayer _leader;
        public event EventHandler LeaderChanged;
        public IPlayer Leader { get { return _leader; } private set { _leader = value; if (LeaderChanged != null) LeaderChanged(this, EventArgs.Empty); } }

        public Game(IEnumerable<IPlayer> players, IEnumerable<IMission> missions )
        {
            Players = players.ToList();
            Missions = missions.ToList();
            PlayerCount = Players.Count();

            if(PlayerCount<5 || PlayerCount>10)
            {
                throw new ArgumentOutOfRangeException("players", PlayerCount,
                                                      "Player count must be between 5 and 10.");
            }
            State = GameState.SelectingLeader;
        }

        private bool PlayerIsInGame(IPlayer player)
        {
            return Players.Contains(player);
        }

        public bool SelectLeader(IPlayer player)
        {
            if (State != GameState.SelectingLeader) return false;
            if (!PlayerIsInGame(player)) return false;

            State = GameState.SelectingMissionOperatives;
            Leader = player;

            return true;
        }
    }
}
