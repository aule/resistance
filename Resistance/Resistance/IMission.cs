using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resistance
{
    public interface IMission
    {
        int OperativeCount { get; }
        int SabotageThreshold { get; }

        Task<bool> PerformMission( IEnumerable<IOperative> operatives );
    }
}
