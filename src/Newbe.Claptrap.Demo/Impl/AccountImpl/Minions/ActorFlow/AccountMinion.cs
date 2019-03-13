using System.Threading.Tasks;
using Newbe.Claptrap.Attributes;
using Newbe.Claptrap.Core;
using Newbe.Claptrap.Demo.Impl.AccountImpl.Claptraps;
using Newbe.Claptrap.Demo.Interfaces;
using Orleans;

namespace Newbe.Claptrap.Demo.Impl.AccountImpl.Minions.ActorFlow
{
    [MinionComponent("ActorFlow", "Account")]
    public class AccountMinion
        : Grain, IAccountActorFlowMinion
    {
        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            var actorFactory = (IActorFactory) ServiceProvider.GetService(typeof(IActorFactory));
            var identity =
                new GrainActorIdentity(new MinionKind(ActorType.Minion, "Account", "ActorFlow"),
                    this.GetPrimaryKeyString());
            Actor = actorFactory.Create(identity);
            await Actor.ActivateAsync();
        }

        public IActor Actor { get; private set; }

        public override async Task OnDeactivateAsync()
        {
            await base.OnDeactivateAsync();
            await Actor.DeactivateAsync();
        }

        public Task BalanceChangeNotice(IEvent @event)
        {
            return Actor.HandleEvent(@event);
        }
    }
}