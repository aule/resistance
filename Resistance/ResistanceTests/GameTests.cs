using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        // Specifies the time in milliseconds to wait for asyncronous code to complete
        private const int TimeoutForAsyncCode = 100;

        private Game PrepareFreshGame(IEnumerable<IPlayer> players, IEnumerable<IMission> missions, IVote votingSystem = null)
        {
            return new Game(players, missions, votingSystem);
        }

        private Game PrepareGameInSelectLeaderState(IEnumerable<IPlayer> players, IEnumerable<IMission> missions, IEnumerable<IPlayer> spies = null, IVote votingSystem = null)
        {
            Game game = PrepareFreshGame(players, missions, votingSystem);
            game.SelectSpies(spies);
            Assert.AreEqual(GameState.SelectingLeader, game.State);
            return game;
        }

        private Game PrepareGameInWaitingForMissionState(IEnumerable<IPlayer> players, IEnumerable<IMission> missions, IEnumerable<IPlayer> spies = null, IPlayer leader = null, IVote votingSystem = null)
        {
            Game game = PrepareGameInSelectLeaderState(players, missions, spies, votingSystem);
            game.SelectLeader(leader ?? game.Players.First());
            Assert.AreEqual(GameState.WaitingForMission, game.State);
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

        private Action<IEnumerable<IPlayer>> SetupOperativesChoiceMethod(Mock<IPlayer> leaderMock)
        {
            // Use a TaskCompletionSource to mock the Task representing the player choosing
            TaskCompletionSource<IEnumerable<IPlayer>> choiceCompletionSource = new TaskCompletionSource<IEnumerable<IPlayer>>();
            leaderMock.Setup(player => player.ChooseOperatives(It.IsAny<IMission>()))
                      .Returns(choiceCompletionSource.Task);
            return choiceCompletionSource.SetResult;
        }

        private Action<bool> SetupVoteResultMethod(Mock<IVote> voteMock)
        {
            // Use a TaskCompletionSource to mock the Task representing the vote taking place
            TaskCompletionSource<bool> voteCompletionSource = new TaskCompletionSource<bool>();
            voteMock.Setup(vote => vote.CallVote(It.IsAny<IEnumerable<IPlayer>>()))
                      .Returns(voteCompletionSource.Task);
            return voteCompletionSource.SetResult;
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
            CollectionAssert.AreEqual(players, game.Players.ToList());
        }

        [TestMethod]
        public void Missions_NewGame_MatchesMissionsInConstructor()
        {
            List<IMission> missions = GenenerateMissionStubs(5);
            Game game = PrepareFreshGame(null, missions);
            CollectionAssert.AreEqual(missions, game.Missions.ToList());
        }

        [TestMethod]
        public void GameState_NewGame_SetToNotReady()
        {
            Game game = PrepareFreshGame(null, null);
            Assert.AreEqual(GameState.NotReady, game.State);
        }

        [TestMethod]
        public void RejectedTeamsThisRound_NewGame_SetToZero()
        {
            Game game = PrepareFreshGame(null, null);
            Assert.AreEqual(0, game.RejectedTeamsThisRound);
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
        public void SelectLeader_ValidLeader_StateChangesToWaitingForMission()
        {
            List<IPlayer> players = GeneneratePlayerStubs(7);
            Game game = PrepareGameInSelectLeaderState(players, null);
            game.SelectLeader(players[5]);
            Assert.AreEqual(GameState.WaitingForMission, game.State);
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

        [TestMethod]
        public void StartMission_StateIsNotWaitingForMission_ReturnsFalse()
        {
            List<IPlayer> players = GeneneratePlayerStubs(6);
            List<IMission> missions = GenenerateMissionStubs(5);
            Game game = PrepareGameInSelectLeaderState(players, missions);
            Assert.IsFalse(game.StartMission(missions.First()));
        }

        [TestMethod]
        public void StartMission_MissionNotInGame_ReturnsFalse()
        {
            List<IPlayer> players = GeneneratePlayerStubs(6);
            List<IMission> missions = GenenerateMissionStubs(5);
            Game game = PrepareGameInWaitingForMissionState(players, missions);
            IMission missionNotInGame = GenenerateMissionStub();
            Assert.IsFalse(game.StartMission(missionNotInGame));
        }

        [TestMethod]
        public void StartMission_MissionNotInGame_StateIsUnchanged()
        {
            List<IPlayer> players = GeneneratePlayerStubs(6);
            List<IMission> missions = GenenerateMissionStubs(5);
            Game game = PrepareGameInWaitingForMissionState(players, missions);
            IMission missionNotInGame = GenenerateMissionStub();
            game.StartMission(missionNotInGame);
            Assert.AreEqual(GameState.WaitingForMission,game.State);
        }

        [TestMethod]
        public void StartMission_ValidMission_ReturnsTrue()
        {
            List<IPlayer> players = GeneneratePlayerStubs(6);
            List<IMission> missions = GenenerateMissionStubs(5);
            Game game = PrepareGameInWaitingForMissionState(players, missions);
            Assert.IsTrue(game.StartMission(missions.First()));
        }

        [TestMethod]
        public void StartMission_ValidMission_CurrentMissionIsSet()
        {
            List<IPlayer> players = GeneneratePlayerStubs(6);
            List<IMission> missions = GenenerateMissionStubs(5);
            IMission nextMission = missions.Last();
            Game game = PrepareGameInWaitingForMissionState(players, missions);
            game.StartMission(nextMission);
            Assert.AreEqual(nextMission, game.CurrentMission);
        }

        [TestMethod]
        public void StartMission_ValidMission_StateChangesToSelectingMissionOperatives()
        {
            List<IPlayer> players = GeneneratePlayerStubs(6);
            List<IMission> missions = GenenerateMissionStubs(5);
            Game game = PrepareGameInWaitingForMissionState(players, missions);
            game.StartMission(missions.First());
            Assert.AreEqual(GameState.SelectingMissionOperatives, game.State);
        }

        [TestMethod]
        public void StartMission_ValidMission_MissionRemovedFromList()
        {
            List<IPlayer> players = GeneneratePlayerStubs(6);
            List<IMission> missions = GenenerateMissionStubs(5);
            IMission nextMission = missions.Last();
            Game game = PrepareGameInWaitingForMissionState(players, missions);
            game.StartMission(nextMission);
            CollectionAssert.DoesNotContain(game.Missions.ToList(),nextMission);
        }

        [TestMethod]
        public void StartMission_ValidMission_ChooseOperativesCalledOnLeader()
        {
            Mock<IPlayer> leaderMock = new Mock<IPlayer>();
            IPlayer leader = leaderMock.Object;
            List<IPlayer> players = GeneneratePlayerStubs(6);
            players.Add(leader);
            List<IMission> missions = GenenerateMissionStubs(5);
            IMission nextMission = missions.Last();
            Game game = PrepareGameInWaitingForMissionState(players, missions, leader: leader);
            game.StartMission(nextMission);
            leaderMock.Verify(player => player.ChooseOperatives(nextMission), Times.Once());
        }

        [TestMethod]
        public void StartMission_PlayerChoosesOperatives_OperativesChosenEventFired()
        {
            Mock<IPlayer> leaderMock = new Mock<IPlayer>();
            Action<IEnumerable<IPlayer>> chooseOperatives = SetupOperativesChoiceMethod(leaderMock);

            IPlayer leader = leaderMock.Object;
            List<IPlayer> players = GeneneratePlayerStubs(6);
            players.Add(leader);
            List<IMission> missions = GenenerateMissionStubs(5);
            IMission nextMission = missions.Last();
            Game game = PrepareGameInWaitingForMissionState(players, missions, leader: leader);

            // Subscribe to the event to observe it, using an AutoResetEvent to wait for async code to run
            int eventCalled = 0;
            AutoResetEvent eventTriggeredNotifier = new AutoResetEvent(false);
            game.OperativesChosen += (o, i) =>
            {
                eventCalled++;
                eventTriggeredNotifier.Set();
            };

            game.StartMission(nextMission);
            chooseOperatives(Enumerable.Empty<IPlayer>());
            Assert.IsTrue(eventTriggeredNotifier.WaitOne(TimeoutForAsyncCode));
            Assert.AreEqual(1, eventCalled);
        }

        [TestMethod]
        public void StartMission_PlayerChoosesOperatives_StateChangesToVoting()
        {
            Mock<IPlayer> leaderMock = new Mock<IPlayer>();
            Action<IEnumerable<IPlayer>> chooseOperatives = SetupOperativesChoiceMethod(leaderMock);

            IPlayer leader = leaderMock.Object;
            List<IPlayer> players = GeneneratePlayerStubs(6);
            players.Add(leader);
            List<IMission> missions = GenenerateMissionStubs(5);
            IMission nextMission = missions.Last();
            Game game = PrepareGameInWaitingForMissionState(players, missions, leader: leader);

            // Subscribe to the event using an AutoResetEvent to allow the test to wait until the event is called
            AutoResetEvent eventTriggeredNotifier = new AutoResetEvent(false);
            game.OperativesChosen += (o, i) => eventTriggeredNotifier.Set();

            game.StartMission(nextMission);
            chooseOperatives(Enumerable.Empty<IPlayer>());
            Assert.IsTrue(eventTriggeredNotifier.WaitOne(TimeoutForAsyncCode));
            Assert.AreEqual(GameState.Voting, game.State);
        }

        [TestMethod]
        public void StartMission_PlayerChoosesOperatives_VoteCalledWithAllPlayers()
        {
            Mock<IPlayer> leaderMock = new Mock<IPlayer>();
            Mock<IVote> voteMock = new Mock<IVote>();
            Action<IEnumerable<IPlayer>> chooseOperatives = SetupOperativesChoiceMethod(leaderMock);

            IPlayer leader = leaderMock.Object;
            List<IPlayer> players = GeneneratePlayerStubs(6);
            players.Add(leader);
            List<IMission> missions = GenenerateMissionStubs(5);
            IMission nextMission = missions.Last();
            Game game = PrepareGameInWaitingForMissionState(players, missions, leader: leader, votingSystem: voteMock.Object);

            // Subscribe to the event using an AutoResetEvent to allow the test to wait until the event is called
            AutoResetEvent eventTriggeredNotifier = new AutoResetEvent(false);
            game.OperativesChosen += (o, i) => eventTriggeredNotifier.Set();

            game.StartMission(nextMission);
            chooseOperatives(Enumerable.Empty<IPlayer>());
            Assert.IsTrue(eventTriggeredNotifier.WaitOne(TimeoutForAsyncCode));
            voteMock.Verify(vote => vote.CallVote(players), Times.Once());
        }

        [TestMethod]
        public void StartMission_VoteRejected_TeamRejectedFired()
        {
            Mock<IPlayer> leaderMock = new Mock<IPlayer>();
            Mock<IVote> voteMock = new Mock<IVote>();
            Action<IEnumerable<IPlayer>> chooseOperatives = SetupOperativesChoiceMethod(leaderMock);
            Action<bool> completeVote = SetupVoteResultMethod(voteMock);

            IPlayer leader = leaderMock.Object;
            List<IPlayer> players = GeneneratePlayerStubs(6);
            players.Add(leader);
            List<IMission> missions = GenenerateMissionStubs(5);
            IMission nextMission = missions.Last();
            Game game = PrepareGameInWaitingForMissionState(players, missions, leader: leader, votingSystem: voteMock.Object);

            // Subscribe to the event using an AutoResetEvent to allow the test to wait until the event is called
            int eventCalled = 0;
            AutoResetEvent eventTriggeredNotifier = new AutoResetEvent(false);
            game.TeamRejected += (o, i) =>
            {
                eventCalled++;
                eventTriggeredNotifier.Set();
            };

            game.StartMission(nextMission);
            chooseOperatives(Enumerable.Empty<IPlayer>());
            completeVote(false);
            Assert.IsTrue(eventTriggeredNotifier.WaitOne(TimeoutForAsyncCode));
            Assert.AreEqual(1, eventCalled);
        }

        [TestMethod]
        public void StartMission_VotePassed_MissionStartingFired()
        {
            Mock<IPlayer> leaderMock = new Mock<IPlayer>();
            Mock<IVote> voteMock = new Mock<IVote>();
            Action<IEnumerable<IPlayer>> chooseOperatives = SetupOperativesChoiceMethod(leaderMock);
            Action<bool> completeVote = SetupVoteResultMethod(voteMock);

            IPlayer leader = leaderMock.Object;
            List<IPlayer> players = GeneneratePlayerStubs(6);
            players.Add(leader);
            List<IMission> missions = GenenerateMissionStubs(5);
            IMission nextMission = missions.Last();
            Game game = PrepareGameInWaitingForMissionState(players, missions, leader: leader, votingSystem: voteMock.Object);

            // Subscribe to the event using an AutoResetEvent to allow the test to wait until the event is called
            int eventCalled = 0;
            AutoResetEvent eventTriggeredNotifier = new AutoResetEvent(false);
            game.MissionStarting += (o, i) =>
            {
                eventCalled++;
                eventTriggeredNotifier.Set();
            };

            game.StartMission(nextMission);
            chooseOperatives(Enumerable.Empty<IPlayer>());
            completeVote(true);
            Assert.IsTrue(eventTriggeredNotifier.WaitOne(TimeoutForAsyncCode));
            Assert.AreEqual(1, eventCalled);
        }

        [TestMethod]
        public void StartMission_VoteRejected_StateChangesToSelectingLeader()
        {
            Mock<IPlayer> leaderMock = new Mock<IPlayer>();
            Mock<IVote> voteMock = new Mock<IVote>();
            Action<IEnumerable<IPlayer>> chooseOperatives = SetupOperativesChoiceMethod(leaderMock);
            Action<bool> completeVote = SetupVoteResultMethod(voteMock);

            IPlayer leader = leaderMock.Object;
            List<IPlayer> players = GeneneratePlayerStubs(6);
            players.Add(leader);
            List<IMission> missions = GenenerateMissionStubs(5);
            IMission nextMission = missions.Last();
            Game game = PrepareGameInWaitingForMissionState(players, missions, leader: leader, votingSystem: voteMock.Object);

            // Subscribe to the event using an AutoResetEvent to allow the test to wait until the event is called
            AutoResetEvent eventTriggeredNotifier = new AutoResetEvent(false);
            game.TeamRejected += (o, i) => eventTriggeredNotifier.Set();

            game.StartMission(nextMission);
            chooseOperatives(Enumerable.Empty<IPlayer>());
            completeVote(false);
            Assert.IsTrue(eventTriggeredNotifier.WaitOne(TimeoutForAsyncCode));
            Assert.AreEqual(GameState.SelectingLeader, game.State);
        }

        [TestMethod]
        public void StartMission_VoteRejected_MissionAddedBackToStartOfList()
        {
            Mock<IPlayer> leaderMock = new Mock<IPlayer>();
            Mock<IVote> voteMock = new Mock<IVote>();
            Action<IEnumerable<IPlayer>> chooseOperatives = SetupOperativesChoiceMethod(leaderMock);
            Action<bool> completeVote = SetupVoteResultMethod(voteMock);

            IPlayer leader = leaderMock.Object;
            List<IPlayer> players = GeneneratePlayerStubs(6);
            players.Add(leader);
            List<IMission> missions = GenenerateMissionStubs(5);
            IMission nextMission = missions.Last();
            Game game = PrepareGameInWaitingForMissionState(players, missions, leader: leader, votingSystem: voteMock.Object);

            // Subscribe to the event using an AutoResetEvent to allow the test to wait until the event is called
            AutoResetEvent eventTriggeredNotifier = new AutoResetEvent(false);
            game.TeamRejected += (o, i) => eventTriggeredNotifier.Set();

            game.StartMission(nextMission);
            chooseOperatives(Enumerable.Empty<IPlayer>());
            completeVote(false);
            Assert.IsTrue(eventTriggeredNotifier.WaitOne(TimeoutForAsyncCode));
            Assert.AreEqual(nextMission,game.Missions.First());
        }

        [TestMethod]
        public void RejectedTeamsThisRound_VoteRejected_IncreasesByOne()
        {
            Mock<IPlayer> leaderMock = new Mock<IPlayer>();
            Mock<IVote> voteMock = new Mock<IVote>();
            Action<IEnumerable<IPlayer>> chooseOperatives = SetupOperativesChoiceMethod(leaderMock);
            Action<bool> completeVote = SetupVoteResultMethod(voteMock);

            IPlayer leader = leaderMock.Object;
            List<IPlayer> players = GeneneratePlayerStubs(6);
            players.Add(leader);
            List<IMission> missions = GenenerateMissionStubs(5);
            IMission nextMission = missions.Last();
            Game game = PrepareGameInWaitingForMissionState(players, missions, leader: leader, votingSystem: voteMock.Object);

            // Subscribe to the event using an AutoResetEvent to allow the test to wait until the event is called
            AutoResetEvent eventTriggeredNotifier = new AutoResetEvent(false);
            game.TeamRejected += (o, i) => eventTriggeredNotifier.Set();

            game.StartMission(nextMission);
            chooseOperatives(Enumerable.Empty<IPlayer>());
            completeVote(false);
            Assert.IsTrue(eventTriggeredNotifier.WaitOne(TimeoutForAsyncCode));
            Assert.AreEqual(1, game.RejectedTeamsThisRound);
        }

        [TestMethod]
        public void RejectedTeamsThisRound_VotePassed_ResetToZero()
        {
            Mock<IPlayer> leaderMock = new Mock<IPlayer>();
            Mock<IVote> voteMock = new Mock<IVote>();
            Action<IEnumerable<IPlayer>> chooseOperatives = SetupOperativesChoiceMethod(leaderMock);
            Action<bool> completeVote = SetupVoteResultMethod(voteMock);

            IPlayer leader = leaderMock.Object;
            List<IPlayer> players = GeneneratePlayerStubs(6);
            players.Add(leader);
            List<IMission> missions = GenenerateMissionStubs(5);
            IMission nextMission = missions.Last();
            Game game = PrepareGameInWaitingForMissionState(players, missions, leader: leader, votingSystem: voteMock.Object);

            // Subscribe to the event using an AutoResetEvent to allow the test to wait until async voting code completes
            AutoResetEvent eventTriggeredNotifier = new AutoResetEvent(false);
            game.TeamRejected += (o, i) => eventTriggeredNotifier.Set();
            game.MissionStarting += (o, i) => eventTriggeredNotifier.Set();

            // Fail a vote first so the value is set to one
            game.StartMission(nextMission);
            chooseOperatives(Enumerable.Empty<IPlayer>());
            completeVote(false);
            eventTriggeredNotifier.WaitOne(TimeoutForAsyncCode);

            // Set up the leader and mocks again
            chooseOperatives = SetupOperativesChoiceMethod(leaderMock);
            completeVote = SetupVoteResultMethod(voteMock);
            Assert.IsTrue(game.SelectLeader(leaderMock.Object));

            // Run another vote and pass it
            game.StartMission(nextMission);
            chooseOperatives(Enumerable.Empty<IPlayer>());
            completeVote(true);

            Assert.IsTrue(eventTriggeredNotifier.WaitOne(TimeoutForAsyncCode));
            Assert.AreEqual(0, game.RejectedTeamsThisRound);
        }

    }
}
