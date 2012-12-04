using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resistance
{
    public interface IPlayer : IOperative, IVoter
    {
        void AssignedToResistance();
        void AssignedToSpies(IEnumerable<IPlayer> spies);

        Task<IEnumerable<IPlayer>> ChooseOperatives(IMission mission);
    }
}
