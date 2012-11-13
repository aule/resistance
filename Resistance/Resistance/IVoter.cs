using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resistance
{
    public interface IVoter
    {
        Task<bool> RequestVote();
    }
}
