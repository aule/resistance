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
            Game game = new Game(GenenerateMoqPlayers(10),null);
            Assert.AreEqual(10, game.PlayerCount, "Player count should be set at game start.");
        }

        [TestMethod]
        public void NewGame_ValidPlayerCount_GameStateIsSelectingLeader()
        {
            Game game = new Game(GenenerateMoqPlayers(5), null);
            Assert.AreEqual(GameState.SelectingLeader, game.State, "Initial game state is 'selecting leader'.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void NewGame_FourPlayers_ThrowsArgumentOutOfRangeException()
        {
            Game game = new Game(GenenerateMoqPlayers(4), null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void NewGame_ElevenPlayers_ThrowsArgumentOutOfRangeException()
        {
            Game game = new Game(GenenerateMoqPlayers(11), null);
        }

        [TestMethod]
        public void SelectLeader_StateIsSelectingLeader_ReturnsTrue()
        {
            List<IPlayer> players = GenenerateMoqPlayers(6);
            Game game = new Game(players);
            Assert.AreEqual(GameState.SelectingLeader, game.State);
            Assert.IsTrue(game.SelectLeader(players[5]), null);
        }

        [TestMethod]
        public void SelectLeader_StateIsSelectingLeader_StateChangesToSelectingMissionOperatives()
        {
            List<IPlayer> players = GenenerateMoqPlayers(7);
            Game game = new Game(players, null);
            game.SelectLeader(players[5]);
            Assert.AreEqual(GameState.SelectingMissionOperatives, game.State);
        }

        [TestMethod]
        public void SelectLeader_StateIsNotSelectingLeader_ReturnsFalse()
        {
            List<IPlayer> players = GenenerateMoqPlayers(6);
            Game game = new Game(players, null);
            game.SelectLeader(players[5]);
            Assert.AreNotEqual(GameState.SelectingLeader,game.State);
            Assert.IsFalse(game.SelectLeader(players[5]));
        }

        [TestMethod]
        public void SelectLeader_PlayerIdNotInGame_ReturnsFalse()
        {
            Game game = new Game(GenenerateMoqPlayers(9), null);
            Assert.AreEqual(GameState.SelectingLeader, game.State);
            Assert.IsFalse(game.SelectLeader(GenenerateMoqPlayer()));
        }

        [TestMethod]
        public void SelectLeader_PlayerIdNotInGame_StateIsUnchanged()
        {
            Game game = new Game(GenenerateMoqPlayers(8), null);
            Assert.AreEqual(GameState.SelectingLeader, game.State);
            Assert.IsFalse(game.SelectLeader(GenenerateMoqPlayer()));
            Assert.AreEqual(GameState.SelectingLeader, game.State);
        }

        [TestMethod]
        public void SelectLeader_ValidLeader_LeaderIsSet()
        {
            List<IPlayer> players = GenenerateMoqPlayers(7);
            IPlayer expectedLeader = players[3];
            Game game = new Game(players, null);
            game.SelectLeader(expectedLeader);
            Assert.AreEqual(expectedLeader, game.Leader);
        }

        [TestMethod]
        public void SelectLeader_ValidLeader_LeaderChangedEventFired()
        {
            List<IPlayer> players = GenenerateMoqPlayers(10);
            Game game = new Game(players, null);
            int eventCalled = 0;
            game.LeaderChanged += (o, i) => { eventCalled++; };
            game.SelectLeader(players[7]);
            Assert.AreEqual(1, eventCalled, "Leader changed event should be called once when leader is selected.");
        }

        [TestMethod]
        public void SelectLeader_PlayerIdNotInGame_LeaderChangedEventNotFired()
        {
            List<IPlayer> players = GenenerateMoqPlayers(6);
            Game game = new Game(players, null);
            int eventCalled = 0;
            game.LeaderChanged += (o, i) => { eventCalled++; };
            game.SelectLeader(GenenerateMoqPlayer());
            Assert.AreEqual(0, eventCalled, "Leader changed event should not be called if the player is not in the game.");
        }

    }
}
