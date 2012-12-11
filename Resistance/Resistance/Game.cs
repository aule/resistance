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
        public event EventHandler TeamRejected;
        public event EventHandler MissionStarting;
        public event EventHandler MissionCompleted;
        public event EventHandler GameOver;

        public GameState State { get; private set; }
        private readonly List<IPlayer> _players;
        public IEnumerable<IPlayer> Players { get { return _players.AsReadOnly(); } }
        private readonly List<IMission> _missions;
        public IEnumerable<IMission> Missions { get { return _missions.AsReadOnly(); } }
        public IMission CurrentMission { get; private set; }
        public int PlayerCount { get; private set; }
        public int RejectedTeamsThisRound { get; private set; }

        private IPlayer _leader;
        public IPlayer Leader { get { return _leader; } private set { _leader = value; if (LeaderChanged != null) LeaderChanged(this, EventArgs.Empty); } }

        private List<IPlayer> _spies;
        private IVote _votingSystem;

        public Game(IEnumerable<IPlayer> players, IEnumerable<IMission> missions, IVote votingSystem )
        {
            _players = (players ?? Enumerable.Empty<IPlayer>()).ToList();
            _missions = (missions ?? Enumerable.Empty<IMission>()).ToList();
            PlayerCount = _players.Count();
            _votingSystem = votingSystem;

            State = GameState.NotReady;
        }

        private bool PlayerIsInGame(IPlayer player)
        {
            return _players.Contains(player);
        }

        public bool SelectLeader(IPlayer leader)
        {
            if (State != GameState.SelectingLeader) return false;
            if (!PlayerIsInGame(leader)) return false;

            Leader = leader;
            State = GameState.WaitingForMission;

            return true;
        }

        private void AnnounceRoles()
        {
            foreach (IPlayer player in _players)
            {
                if( _spies.Contains(player) )
                {
                    player.AssignedToSpies(_spies.AsReadOnly());
                } else
                {
                    player.AssignedToResistance();
                }
            }
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

        public bool StartMission(IMission nextMission)
        {
            if (State != GameState.WaitingForMission) return false;
            if (!_missions.Remove(nextMission)) return false;

            CurrentMission = nextMission;
            State = GameState.SelectingMissionOperatives;
            Task<IEnumerable<IPlayer>> task = Leader.ChooseOperatives(CurrentMission);
            if (task!=null)
            {
                task.ContinueWith(completeTask => HandleOperativesChosen(completeTask.Result));
            }

            return true;
        }

        private void HandleOperativesChosen(IEnumerable<IPlayer> operatives)
        {
            State = GameState.Voting;
            if (OperativesChosen != null)
            {
                OperativesChosen(this, EventArgs.Empty);
            }
            Task<bool> task = _votingSystem.CallVote(Players);
            if (task != null)
            {
                task.ContinueWith(completeTask =>
                                      {
                                          if (completeTask.Result) 
                                              VotePassed(operatives);
                                          else 
                                              VoteFailed();
                                      });
            }
        }

        private void VoteFailed()
        {
            RejectedTeamsThisRound++;
            State = GameState.SelectingLeader;
            _missions.Insert(0,CurrentMission);
            if (TeamRejected != null)
            {
                TeamRejected(this, EventArgs.Empty);
            }
        }

        private void VotePassed(IEnumerable<IPlayer> operatives)
        {
            RejectedTeamsThisRound = 0;
            State = GameState.Mission;
            if (MissionStarting != null)
            {
                MissionStarting(this, EventArgs.Empty);
            }
        }
    }
}
