using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resistance
{
    public interface IVote
    {
        Task<bool> CallVote(IEnumerable<IVoter> voters );
    }
}
