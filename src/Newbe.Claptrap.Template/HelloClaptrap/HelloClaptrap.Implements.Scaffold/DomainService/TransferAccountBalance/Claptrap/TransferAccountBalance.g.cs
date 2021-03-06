using System.Threading.Tasks;
using HelloClaptrap.Interfaces.DomainService.TransferAccountBalance;
using Newbe.Claptrap;
using Newbe.Claptrap.Attributes;
using Newbe.Claptrap.Core;
using Newbe.Claptrap.Orleans;
using Orleans;
using StateData = HelloClaptrap.Models.DomainService.TransferAccountBalance.TransferAccountBalanceStateData;
namespace HelloClaptrap.Implements.Scaffold.DomainService.TransferAccountBalance.Claptrap
{
    [ClaptrapComponent("TransferAccountBalance")]
    public partial class TransferAccountBalance : Grain, ITransferAccountBalance
    {
        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            var kind = new ClaptrapKind(ActorType.Claptrap, "TransferAccountBalance");
            var identity = new GrainActorIdentity(kind, this.GetPrimaryKeyString());
            var factory = (IActorFactory)ServiceProvider.GetService(typeof(IActorFactory));
            Actor = factory.Create(identity);
            await Actor.ActivateAsync();
        }
        public override async Task OnDeactivateAsync()
        {
            await base.OnDeactivateAsync();
            await Actor.DeactivateAsync();
        }
        public IActor Actor { get; private set; }
        public StateData ActorState => (StateData)Actor.State.Data;
        public async Task<bool> Transfer(string fromId, string toId, decimal balance)
        {
            var method = (N20EventMethods.Transfer.ITransferMethod)ServiceProvider.GetService(typeof(N20EventMethods.Transfer.ITransferMethod));
            var result = await method.Invoke((StateData)Actor.State.Data, fromId, toId, balance);
            if (result.EventRaising)
            {
                var @event = new DataEvent(Actor.State.Identity, "TransferAccountBalanceFinishedEventData", result.EventData, result.EventUid);
                await Actor.HandleEvent(@event);
            }
            return result.MethodReturn;
        }
    }
}
