using System;
using System.Threading.Tasks;
using Newbe.Claptrap.Attributes;
using Newbe.Claptrap.Core;
using Newbe.Claptrap.Demo.Models.EventData;

namespace Newbe.Claptrap.Demo.Impl.AccountImpl.Minions.Database
{
    public class LockEventDataEventHandler :
        MinionEventHandlerBase<NoneStateData, LockEventData>
    {
        public override Task HandleEventCore(NoneStateData state, LockEventData @event)
        {
            Console.WriteLine(@event);
            return Task.CompletedTask;
        }
    }
}