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
            if(_voteCompletionSource != null && !_voteCompletionSource.Task.IsCompleted)
            {
                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
                taskCompletionSource.SetCanceled();
                return taskCompletionSource.Task;
            }
            List<IVoter> votersList = (voters ?? Enumerable.Empty<IVoter>()).ToList();
            _votesRemaining = votersList.Count;
            _yesVotes = _noVotes = 0;
            _voteCompletionSource = new TaskCompletionSource<bool>();
            foreach (IVoter voter in votersList)
            {
                voter.RequestVote().ContinueWith(CastVote);
            }
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
            _voteCompletionSource.TrySetResult(result);
        }
    }
}
