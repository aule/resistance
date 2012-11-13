﻿using System;
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
        private readonly List<IPlayer> _players;
        public IList<IPlayer> Players { get { return _players.AsReadOnly(); } }
        private readonly List<IMission> _missions;
        public IList<IMission> Missions { get { return _missions.AsReadOnly(); } } 
        public int PlayerCount { get; private set; }

        private IPlayer _leader;
        public IPlayer Leader { get { return _leader; } private set { _leader = value; if (LeaderChanged != null) LeaderChanged(this, EventArgs.Empty); } }

        private List<IPlayer> _spies; 

        public Game(IEnumerable<IPlayer> players, IEnumerable<IMission> missions )
        {
            _players = (players ?? Enumerable.Empty<IPlayer>()).ToList();
            _missions = (missions ?? Enumerable.Empty<IMission>()).ToList();
            PlayerCount = _players.Count();

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

            State = GameState.SelectingMissionOperatives;
            Leader = leader;

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
    }
}
