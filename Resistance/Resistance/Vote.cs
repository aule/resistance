using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Resistance
{
    public class Vote : IVote
    {
        private TaskCompletionSource<bool> _voteCompletionSource;
        private int _votesRemaining;
        private int _yesVotes;
        private int _noVotes;

        public Task<bool> CallVote(IEnumerable<IVoter> voters)
        {
            // Check if a vote is already in process
            if(_voteCompletionSource != null && !_voteCompletionSource.Task.IsCompleted)
            {
                // Create a task in a cancelled state then return it
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
                taskCompletionSource.SetCanceled();
                return taskCompletionSource.Task;
            }
            
            // Ensure the voters are enumerated only once, and convert nulls to empty lists
            List<IVoter> votersList = (voters ?? Enumerable.Empty<IVoter>()).ToList();
            
            // Prepare the initial state for the vote
            _votesRemaining = votersList.Count;
            _yesVotes = _noVotes = 0;
            _voteCompletionSource = new TaskCompletionSource<bool>();
            
            // Call RequestVote on each voter, and pass the results to CastVote
            foreach (IVoter voter in votersList)
            {
                voter.RequestVote().ContinueWith(CastVote);
            }
            
            // Return a task which can be controlled using _voteCompletionSource
            return _voteCompletionSource.Task;
        }

        private void CastVote( Task<bool> vote )
        {
            if (vote.Result)
            {
                Interlocked.Increment(ref _yesVotes);
            } else
            {
                Interlocked.Increment(ref _noVotes);
            }
            if (Interlocked.Decrement(ref _votesRemaining) == 0)
            {
                CountVote();
            }
        } 

        private void CountVote()
        {
            bool result = _yesVotes > _noVotes;
            // Mark the task returned by CallVote as completed, and set the result of the vote
            _voteCompletionSource.TrySetResult(result);
        }
    }
}
