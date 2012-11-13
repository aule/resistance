using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Resistance;

namespace ResistanceTests
{
    /// <summary>
    /// Tests for the voting system (implmenting IVote) used to decide on which operatives are sent on missions
    /// </summary>
    [TestClass]
    public class VoteTests
    {
        // Specified the time in milliseconds to wait for the vote counting code to react to the asyncronous voting
        private const int TimeoutForVotesToBeCounted = 100;

        private Vote PrepareFreshVote()
        {
            return new Vote();
        }

        private Vote PrepareCompletedVote()
        {
            Vote vote = new Vote();
            List<Mock<IVoter>> voterMocks = GenenerateVoterMocks(10);
            List<TaskCompletionSource<bool>> votingCompletionSources = SetupRequestVoteMethods(voterMocks);
            Task<bool> votingTask = vote.CallVote(voterMocks.Select(mock => mock.Object));
            CompleteVotesInParallel(votingCompletionSources, true);
            Assert.IsTrue(votingTask.Wait(TimeoutForVotesToBeCounted));
            return vote;
        }

        private Vote PrepareRunningVote()
        {
            Vote vote = new Vote();
            List<Mock<IVoter>> voterMocks = GenenerateVoterMocks(10);
            List<TaskCompletionSource<bool>> votingCompletionSources = SetupRequestVoteMethods(voterMocks);
            Task<bool> votingTask = vote.CallVote(voterMocks.Select(mock => mock.Object));
            Assert.AreEqual(TaskStatus.WaitingForActivation, votingTask.Status);
            return vote;
        }

        private List<Mock<IVoter>> GenenerateVoterMocks(int voterCount)
        {
            return Enumerable.Range(1, voterCount).Select(n => new Mock<IVoter>()).ToList();
        }

        private List<TaskCompletionSource<bool>> SetupRequestVoteMethods(IEnumerable<Mock<IVoter>> mockVoters)
        {
            return mockVoters.Select(mock =>
                                  {
                                      TaskCompletionSource<bool> votingCompletionSource = new TaskCompletionSource<bool>();
                                      mock.Setup(voter => voter.RequestVote()).Returns(votingCompletionSource.Task);
                                      return votingCompletionSource;
                                  }).ToList();
        }

        private void CompleteVotesInParallel(IEnumerable<TaskCompletionSource<bool>> votingCompletionSources, bool result )
        {
            votingCompletionSources.AsParallel().ForAll(votingCompletionSource => votingCompletionSource.SetResult(result));
        }
        
        [TestMethod]
        public void CallVote_FreshVote_ReturnsTaskWaitingForActivation()
        {
            Vote vote = PrepareFreshVote();
            Task<bool> task = vote.CallVote(null);
            Assert.AreEqual(TaskStatus.WaitingForActivation, task.Status);
        }

        [TestMethod]
        public void CallVote_WithVoters_RequestVoteCalledOnAllVoters()
        {
            Vote vote = PrepareFreshVote();
            List<Mock<IVoter>> voterMocks = GenenerateVoterMocks(6);
            IEnumerable<IVoter> voters = voterMocks.Select(mock => mock.Object);
            List<TaskCompletionSource<bool>> votingCompletionSources = SetupRequestVoteMethods(voterMocks);
            vote.CallVote(voters);
            voterMocks.ForEach(voterMock => voterMock.Verify(voter => voter.RequestVote(), Times.Once()));
        }

        [TestMethod]
        public void CallVote_AllVotersReturnTrue_VoteCompletesTrue()
        {
            Vote vote = PrepareFreshVote();
            List<Mock<IVoter>> voterMocks = GenenerateVoterMocks(6);
            IEnumerable<IVoter> voters = voterMocks.Select(mock => mock.Object);
            List<TaskCompletionSource<bool>> votingCompletionSources = SetupRequestVoteMethods(voterMocks);
            Task<bool> votingTask = vote.CallVote(voters);
            CompleteVotesInParallel(votingCompletionSources,true);
            // Allow 100ms for the vote counting code to react to the asyncronous voting, fail if timeout occurs
            Assert.IsTrue(votingTask.Wait(TimeoutForVotesToBeCounted));
            Assert.IsTrue(votingTask.Result);
        }

        [TestMethod]
        public void CallVote_AllVotersReturnFalse_VoteCompletesFalse()
        {
            Vote vote = PrepareFreshVote();
            List<Mock<IVoter>> voterMocks = GenenerateVoterMocks(6);
            IEnumerable<IVoter> voters = voterMocks.Select(mock => mock.Object);
            List<TaskCompletionSource<bool>> votingCompletionSources = SetupRequestVoteMethods(voterMocks);
            Task<bool> votingTask = vote.CallVote(voters);
            CompleteVotesInParallel(votingCompletionSources, false);
            // Allow 100ms for the vote counting code to react to the asyncronous voting, fail if timeout occurs
            Assert.IsTrue(votingTask.Wait(TimeoutForVotesToBeCounted));
            Assert.IsFalse(votingTask.Result);
        }

        [TestMethod]
        public void CallVote_VoteSplitEvenly_VoteCompletesFalse()
        {
            Vote vote = PrepareFreshVote();
            List<Mock<IVoter>> trueVoterMocks = GenenerateVoterMocks(8);
            List<Mock<IVoter>> falseVoterMocks = GenenerateVoterMocks(8);
            IEnumerable<IVoter> voters = trueVoterMocks.Concat(falseVoterMocks).Select(mock => mock.Object);
            List<TaskCompletionSource<bool>> trueVotingCompletionSources = SetupRequestVoteMethods(trueVoterMocks);
            List<TaskCompletionSource<bool>> falseVotingCompletionSources = SetupRequestVoteMethods(falseVoterMocks);
            Task<bool> votingTask = vote.CallVote(voters);
            CompleteVotesInParallel(trueVotingCompletionSources, true);
            CompleteVotesInParallel(falseVotingCompletionSources, false);
            // Allow 100ms for the vote counting code to react to the asyncronous voting, fail if timeout occurs
            Assert.IsTrue(votingTask.Wait(TimeoutForVotesToBeCounted));
            Assert.IsFalse(votingTask.Result);
        }

        [TestMethod]
        public void CallVote_MoreTrueVotesThanFalseVotes_VoteCompletesTrue()
        {
            Vote vote = PrepareFreshVote();
            List<Mock<IVoter>> trueVoterMocks = GenenerateVoterMocks(9);
            List<Mock<IVoter>> falseVoterMocks = GenenerateVoterMocks(8);
            IEnumerable<IVoter> voters = trueVoterMocks.Concat(falseVoterMocks).Select(mock => mock.Object);
            List<TaskCompletionSource<bool>> trueVotingCompletionSources = SetupRequestVoteMethods(trueVoterMocks);
            List<TaskCompletionSource<bool>> falseVotingCompletionSources = SetupRequestVoteMethods(falseVoterMocks);
            Task<bool> votingTask = vote.CallVote(voters);
            CompleteVotesInParallel(trueVotingCompletionSources, true);
            CompleteVotesInParallel(falseVotingCompletionSources, false);
            // Allow 100ms for the vote counting code to react to the asyncronous voting, fail if timeout occurs
            Assert.IsTrue(votingTask.Wait(TimeoutForVotesToBeCounted));
            Assert.IsTrue(votingTask.Result);
        }

        [TestMethod]
        public void CallVote_PreviouslyCompletedVote_ReturnsTaskWaitingForActivation()
        {
            Vote vote = PrepareCompletedVote();
            Task<bool> task = vote.CallVote(null);
            Assert.AreEqual(TaskStatus.WaitingForActivation, task.Status);
        }

        [TestMethod]
        public void CallVote_PreviouslyCompletedVoteThenVoteSplitEvenly_VoteCompletesFalse()
        {
            Vote vote = PrepareCompletedVote();
            List<Mock<IVoter>> trueVoterMocks = GenenerateVoterMocks(8);
            List<Mock<IVoter>> falseVoterMocks = GenenerateVoterMocks(8);
            IEnumerable<IVoter> voters = trueVoterMocks.Concat(falseVoterMocks).Select(mock => mock.Object);
            List<TaskCompletionSource<bool>> trueVotingCompletionSources = SetupRequestVoteMethods(trueVoterMocks);
            List<TaskCompletionSource<bool>> falseVotingCompletionSources = SetupRequestVoteMethods(falseVoterMocks);
            Task<bool> votingTask = vote.CallVote(voters);
            CompleteVotesInParallel(trueVotingCompletionSources, true);
            CompleteVotesInParallel(falseVotingCompletionSources, false);
            // Allow 100ms for the vote counting code to react to the asyncronous voting, fail if timeout occurs
            Assert.IsTrue(votingTask.Wait(TimeoutForVotesToBeCounted));
            Assert.IsFalse(votingTask.Result);
        }

        [TestMethod]
        public void CallVote_StillRunningVote_ReturnsCancelledTask()
        {
            Vote vote = PrepareRunningVote();
            Task<bool> task = vote.CallVote(null);
            Assert.AreEqual(TaskStatus.Canceled, task.Status);
        }

        [TestMethod]
        public void CallVote_StillRunningVote_RequestVoteNotCalledOnAnyVoters()
        {
            Vote vote = PrepareRunningVote();
            List<Mock<IVoter>> voterMocks = GenenerateVoterMocks(6);
            IEnumerable<IVoter> voters = voterMocks.Select(mock => mock.Object);
            List<TaskCompletionSource<bool>> votingCompletionSources = SetupRequestVoteMethods(voterMocks);
            vote.CallVote(voters);
            voterMocks.ForEach(voterMock => voterMock.Verify(voter => voter.RequestVote(), Times.Never()));
        }
    }
}
