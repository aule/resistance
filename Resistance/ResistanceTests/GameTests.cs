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

        private IPlayer GeneneratePlayerStub()
        {
            return new Mock<IPlayer>().Object;
        }

        private List<IPlayer> GeneneratePlayerStubs(int playerCount)
        {
            return Enumerable.Range(1, playerCount).Select(n => GeneneratePlayerStub()).ToList();
        }

        private List<Mock<IPlayer>> GeneneratePlayerMocks(int playerCount)
        {
            return Enumerable.Range(1, playerCount).Select(n => new Mock<IPlayer>()).ToList();
        }

        private IMission GenenerateMissionStub()
        {
            return new Mock<IMission>().Object;
        }

        private List<IMission> GenenerateMissionStubs(int missionCount)
        {
            return Enumerable.Range(1, missionCount).Select(n => GenenerateMissionStub()).ToList();
        }

        [TestMethod]
        public void PlayerCount_NewGame_MatchesNumberOfPlayers()
        {
            const int playerCount = 10;
            Game game = PrepareFreshGame(GeneneratePlayerStubs(playerCount), null);
            Assert.AreEqual(playerCount, game.PlayerCount);
        }

        [TestMethod]
        public void Players_NewGame_MatchesPlayersInConstructor()
        {
            List<IPlayer> players = GeneneratePlayerStubs(8);
            Game game = PrepareFreshGame(players, null);
            CollectionAssert.AreEquivalent(players, game.Players.ToList());
        }

        [TestMethod]
        public void Missions_NewGame_MatchesMissionsInConstructor()
        {
            List<IMission> missions = GenenerateMissionStubs(5);
            Game game = PrepareFreshGame(null, missions);
            CollectionAssert.AreEquivalent(missions, game.Missions.ToList());
        }

        [TestMethod]
        public void GameState_NewGame_SetToNotReady()
        {
            Game game = PrepareFreshGame(null, null);
            Assert.AreEqual(GameState.NotReady, game.State);
        }

        [TestMethod]
        public void SelectSpies_ListContainsOnlyNonPlayers_ReturnsFalse()
        {
            List<IPlayer> players = GeneneratePlayerStubs(6);
            Game game = PrepareFreshGame(players, null);
            Assert.IsFalse(game.SelectSpies(Enumerable.Range(1, 3).Select(n => GeneneratePlayerStub())));
        }

        [TestMethod]
        public void SelectSpies_ListContainsSomeNonPlayers_ReturnsFalse()
        {
            List<IPlayer> players = GeneneratePlayerStubs(6);
            IEnumerable<IPlayer> spies = players.Take(2).Concat(Enumerable.Range(1, 2).Select(n => GeneneratePlayerStub()));
            Game game = PrepareFreshGame(players, null);
            Assert.IsFalse(game.SelectSpies(spies));
        }

        [TestMethod]
        public void SelectSpies_InvalidSpies_StateRemainsNotReady()
        {
            List<IPlayer> players = GeneneratePlayerStubs(6);
            Game game = PrepareFreshGame(players, null);
            game.SelectSpies(Enumerable.Range(1, 3).Select(n => GeneneratePlayerStub()));
            Assert.AreEqual(GameState.NotReady,game.State);
        }

        [TestMethod]
        public void SelectSpies_ValidSpies_ReturnsTrue()
        {
            List<IPlayer> players = GeneneratePlayerStubs(8);
            IEnumerable<IPlayer> spies = players.Skip(2).Take(3);
            Game game = PrepareFreshGame(players, null);
            Assert.IsTrue(game.SelectSpies(spies));
        }

        [TestMethod]
        public void SelectSpies_ValidSpies_StateChangesToSelectingLeader()
        {
            List<IPlayer> players = GeneneratePlayerStubs(8);
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
        public void SelectSpies_ValidSpies_AssignedToResistanceCalledOnNonSpyPlayers()
        {
            List<Mock<IPlayer>> nonSpyMocks = GeneneratePlayerMocks(6);
            IEnumerable<IPlayer> nonSpies = nonSpyMocks.Select(mock => mock.Object);
            IEnumerable<IPlayer> spies = GeneneratePlayerStubs(2);
            Game game = PrepareFreshGame(spies.Concat(nonSpies), null);
            game.SelectSpies(spies);
            nonSpyMocks.ForEach(nonSpyMock => nonSpyMock.Verify(nonSpy => nonSpy.AssignedToResistance(), Times.Once()));
        }

        [TestMethod]
        public void SelectSpies_ValidSpies_AssignedToResistanceNotCalledOnSpyPlayers()
        {
            List<Mock<IPlayer>> spyMocks = GeneneratePlayerMocks(6);
            IEnumerable<IPlayer> spies = spyMocks.Select(mock => mock.Object).ToList();
            IEnumerable<IPlayer> nonSpies = GeneneratePlayerStubs(2);
            Game game = PrepareFreshGame(spies.Concat(nonSpies), null);
            game.SelectSpies(spies);
            spyMocks.ForEach(spyMock => spyMock.Verify(spy => spy.AssignedToResistance(), Times.Never()));
        }

        [TestMethod]
        public void SelectSpies_ValidSpies_AssignedToSpiesNotCalledOnNonSpyPlayers()
        {
            List<Mock<IPlayer>> nonSpyMocks = GeneneratePlayerMocks(6);
            IEnumerable<IPlayer> nonSpies = nonSpyMocks.Select(mock => mock.Object);
            IEnumerable<IPlayer> spies = GeneneratePlayerStubs(2);
            Game game = PrepareFreshGame(spies.Concat(nonSpies), null);
            game.SelectSpies(spies);
            nonSpyMocks.ForEach(nonSpyMock => nonSpyMock.Verify(nonSpy => nonSpy.AssignedToSpies(It.IsAny<IEnumerable<IPlayer>>()), Times.Never()));
        }

        [TestMethod]
        public void SelectSpies_ValidSpies_AssignedToSpiesCalledOnSpyPlayers()
        {
            List<Mock<IPlayer>> spyMocks = GeneneratePlayerMocks(6);
            IEnumerable<IPlayer> spies = spyMocks.Select(mock => mock.Object).ToList();
            IEnumerable<IPlayer> nonSpies = GeneneratePlayerStubs(2);
            Game game = PrepareFreshGame(spies.Concat(nonSpies), null);
            game.SelectSpies(spies);
            spyMocks.ForEach(spyMock => spyMock.Verify(spy => spy.AssignedToSpies(It.IsAny<IEnumerable<IPlayer>>()), Times.Once()));
        }

        [TestMethod]
        public void SelectSpies_ValidSpies_ListPassedToSpyPlayersContainsAllSpies()
        {
            List<Mock<IPlayer>> spyMocks = GeneneratePlayerMocks(6);
            IEnumerable<IPlayer> spies = spyMocks.Select(mock => mock.Object).ToList();
            IEnumerable<IPlayer> nonSpies = GeneneratePlayerStubs(2);
            Game game = PrepareFreshGame(spies.Concat(nonSpies), null);
            // Mocking the AssignedToSpies method to contain an assertion that the parameter passed in it correct:
            spyMocks.ForEach(spyMock => 
                spyMock.Setup(spy => spy.AssignedToSpies(It.IsAny<IEnumerable<IPlayer>>()))
                .Callback((IEnumerable<IPlayer> receivedList) => CollectionAssert.AreEquivalent(spies.ToList(),receivedList.ToList()))
                );
            game.SelectSpies(spies);
        }

        [TestMethod]
        public void SelectLeader_ValidLeader_StateChangesToSelectingMissionOperatives()
        {
            List<IPlayer> players = GeneneratePlayerStubs(7);
            Game game = PrepareGameInSelectLeaderState(players, null);
            game.SelectLeader(players[5]);
            Assert.AreEqual(GameState.SelectingMissionOperatives, game.State);
        }

        [TestMethod]
        public void SelectLeader_ValidLeader_ReturnsTrue()
        {
            List<IPlayer> players = GeneneratePlayerStubs(6);
            Game game = PrepareGameInSelectLeaderState(players, null);
            Assert.AreEqual(GameState.SelectingLeader, game.State);
            Assert.IsTrue(game.SelectLeader(players[5]), null);
        }

        [TestMethod]
        public void SelectLeader_StateIsNotSelectingLeader_ReturnsFalse()
        {
            List<IPlayer> players = GeneneratePlayerStubs(6);
            Game game = PrepareGameInSelectLeaderState(players, null);
            game.SelectLeader(players[5]);
            Assert.AreNotEqual(GameState.SelectingLeader,game.State);
            Assert.IsFalse(game.SelectLeader(players[5]));
        }

        [TestMethod]
        public void SelectLeader_PlayerNotInGame_ReturnsFalse()
        {
            Game game = PrepareGameInSelectLeaderState(GeneneratePlayerStubs(9), null);
            Assert.AreEqual(GameState.SelectingLeader, game.State);
            Assert.IsFalse(game.SelectLeader(GeneneratePlayerStub()));
        }

        [TestMethod]
        public void SelectLeader_PlayerNotInGame_StateIsUnchanged()
        {
            Game game = PrepareGameInSelectLeaderState(GeneneratePlayerStubs(8), null);
            Assert.AreEqual(GameState.SelectingLeader, game.State);
            Assert.IsFalse(game.SelectLeader(GeneneratePlayerStub()));
            Assert.AreEqual(GameState.SelectingLeader, game.State);
        }

        [TestMethod]
        public void SelectLeader_ValidLeader_LeaderIsSet()
        {
            List<IPlayer> players = GeneneratePlayerStubs(7);
            IPlayer expectedLeader = players[3];
            Game game = PrepareGameInSelectLeaderState(players, null);
            game.SelectLeader(expectedLeader);
            Assert.AreEqual(expectedLeader, game.Leader);
        }

        [TestMethod]
        public void SelectLeader_ValidLeader_LeaderChangedEventFired()
        {
            List<IPlayer> players = GeneneratePlayerStubs(10);
            Game game = PrepareGameInSelectLeaderState(players, null);
            int eventCalled = 0;
            game.LeaderChanged += (o, i) => { eventCalled++; };
            game.SelectLeader(players[7]);
            Assert.AreEqual(1, eventCalled, "Leader changed event should be called once when leader is selected.");
        }

        [TestMethod]
        public void SelectLeader_PlayerIdNotInGame_LeaderChangedEventNotFired()
        {
            List<IPlayer> players = GeneneratePlayerStubs(6);
            Game game = PrepareGameInSelectLeaderState(players, null);
            int eventCalled = 0;
            game.LeaderChanged += (o, i) => { eventCalled++; };
            game.SelectLeader(GeneneratePlayerStub());
            Assert.AreEqual(0, eventCalled, "Leader changed event should not be called if the player is not in the game.");
        }

    }
}
