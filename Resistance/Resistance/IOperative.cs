using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resistance
{
    public interface IOperative
    {
        Task<bool> PerformMission(IMission mission);
    }
}
