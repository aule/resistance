using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Resistance;

namespace ResistanceTests
{
    /// <summary>
    /// Summary description for GameTests
    /// </summary>
    [TestClass]
    public class GameTests
    {
        private Game PrepareFreshGame(IEnumerable<IPlayer> players, IEnumerable<IMission> missions)
        {
            return new Game(players, missions);
        }

        private Game PrepareGameInSelectLeaderState(IEnumerable<IPlayer> players, IEnumerable<IMission> missions, IEnumerable<IPlayer> spies = null )
        {
            Game game = new Game(players, missions);
            game.SelectSpies(spies);
            Assert.AreEqual(GameState.SelectingLeader,game.State);
            return game;
        }

        private IPlayer GenenerateMoqPlayer()
        {
            return new Mock<IPlayer>().Object;
        }

        private List<IPlayer> GenenerateMoqPlayers( int playerCount )
        {
            return Enumerable.Range(1, playerCount).Select(n => GenenerateMoqPlayer()).ToList();
        }
            
        [TestMethod]
        public void NewGame_TenPlayers_PlayerCountSetToTen()
        {
            Game game = PrepareFreshGame(GenenerateMoqPlayers(10), null);
            Assert.AreEqual(10, game.PlayerCount);
        }

        [TestMethod]
        public void NewGame_GameStateIsNotReady()
        {
            Game game = PrepareFreshGame(null, null);
            Assert.AreEqual(GameState.NotReady, game.State);
        }

        [TestMethod]
        public void SelectSpies_ListContainsOnlyNonPlayers_ReturnsFalse()
        {
            List<IPlayer> players = GenenerateMoqPlayers(6);
            Game game = PrepareFreshGame(players, null);
            Assert.IsFalse(game.SelectSpies(Enumerable.Range(1, 3).Select(n => GenenerateMoqPlayer())));
        }

        [TestMethod]
        public void SelectSpies_ListContainsSomeNonPlayers_ReturnsFalse()
        {
            List<IPlayer> players = GenenerateMoqPlayers(6);
            IEnumerable<IPlayer> spies = players.Take(2).Concat(Enumerable.Range(1, 2).Select(n => GenenerateMoqPlayer()));
            Game game = PrepareFreshGame(players, null);
            Assert.IsFalse(game.SelectSpies(spies));
        }

        [TestMethod]
        public void SelectSpies_InvalidSpies_StateRemainsNotReady()
        {
            List<IPlayer> players = GenenerateMoqPlayers(6);
            Game game = PrepareFreshGame(players, null);
            game.SelectSpies(Enumerable.Range(1, 3).Select(n => GenenerateMoqPlayer()));
            Assert.AreEqual(GameState.NotReady,game.State);
        }

        [TestMethod]
        public void SelectSpies_ValidSpies_ReturnsTrue()
        {
            List<IPlayer> players = GenenerateMoqPlayers(8);
            IEnumerable<IPlayer> spies = players.Skip(2).Take(3);
            Game game = PrepareFreshGame(players, null);
            Assert.IsTrue(game.SelectSpies(spies));
        }

        [TestMethod]
        public void SelectSpies_ValidSpies_StateChangesToSelectingLeader()
        {
            List<IPlayer> players = GenenerateMoqPlayers(8);
            IEnumerable<IPlayer> spies = players.Skip(4).Take(2);
            Game game = PrepareFreshGame(players, null);
            game.SelectSpies(spies);
            Assert.AreEqual(GameState.SelectingLeader, game.State);
        }

        [TestMethod]
        public void SelectSpies_StateIsntNotReady_ReturnsFalse()
        {
            Game game = PrepareGameInSelectLeaderState(null, null);
            Assert.AreNotEqual(GameState.NotReady, game.State);
            Assert.IsFalse(game.SelectSpies(null));
        }

        [TestMethod]
        public void SelectLeader_ValidLeader_StateChangesToSelectingMissionOperatives()
        {
            List<IPlayer> players = GenenerateMoqPlayers(7);
            Game game = PrepareGameInSelectLeaderState(players, null);
            game.SelectLeader(players[5]);
            Assert.AreEqual(GameState.SelectingMissionOperatives, game.State);
        }

        [TestMethod]
        public void SelectLeader_ValidLeader_ReturnsTrue()
        {
            List<IPlayer> players = GenenerateMoqPlayers(6);
            Game game = PrepareGameInSelectLeaderState(players, null);
            Assert.AreEqual(GameState.SelectingLeader, game.State);
            Assert.IsTrue(game.SelectLeader(players[5]), null);
        }

        [TestMethod]
        public void SelectLeader_StateIsNotSelectingLeader_ReturnsFalse()
        {
            List<IPlayer> players = GenenerateMoqPlayers(6);
            Game game = PrepareGameInSelectLeaderState(players, null);
            game.SelectLeader(players[5]);
            Assert.AreNotEqual(GameState.SelectingLeader,game.State);
            Assert.IsFalse(game.SelectLeader(players[5]));
        }

        [TestMethod]
        public void SelectLeader_PlayerNotInGame_ReturnsFalse()
        {
            Game game = PrepareGameInSelectLeaderState(GenenerateMoqPlayers(9), null);
            Assert.AreEqual(GameState.SelectingLeader, game.State);
            Assert.IsFalse(game.SelectLeader(GenenerateMoqPlayer()));
        }

        [TestMethod]
        public void SelectLeader_PlayerNotInGame_StateIsUnchanged()
        {
            Game game = PrepareGameInSelectLeaderState(GenenerateMoqPlayers(8), null);
            Assert.AreEqual(GameState.SelectingLeader, game.State);
            Assert.IsFalse(game.SelectLeader(GenenerateMoqPlayer()));
            Assert.AreEqual(GameState.SelectingLeader, game.State);
        }

        [TestMethod]
        public void SelectLeader_ValidLeader_LeaderIsSet()
        {
            List<IPlayer> players = GenenerateMoqPlayers(7);
            IPlayer expectedLeader = players[3];
            Game game = PrepareGameInSelectLeaderState(players, null);
            game.SelectLeader(expectedLeader);
            Assert.AreEqual(expectedLeader, game.Leader);
        }

        [TestMethod]
        public void SelectLeader_ValidLeader_LeaderChangedEventFired()
        {
            List<IPlayer> players = GenenerateMoqPlayers(10);
            Game game = PrepareGameInSelectLeaderState(players, null);
            int eventCalled = 0;
            game.LeaderChanged += (o, i) => { eventCalled++; };
            game.SelectLeader(players[7]);
            Assert.AreEqual(1, eventCalled, "Leader changed event should be called once when leader is selected.");
        }

        [TestMethod]
        public void SelectLeader_PlayerIdNotInGame_LeaderChangedEventNotFired()
        {
            List<IPlayer> players = GenenerateMoqPlayers(6);
            Game game = PrepareGameInSelectLeaderState(players, null);
            int eventCalled = 0;
            game.LeaderChanged += (o, i) => { eventCalled++; };
            game.SelectLeader(GenenerateMoqPlayer());
            Assert.AreEqual(0, eventCalled, "Leader changed event should not be called if the player is not in the game.");
        }

    }
}
