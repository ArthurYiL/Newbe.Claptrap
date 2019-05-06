using System.Threading.Tasks;
using Newbe.Claptrap.Attributes;
using Newbe.Claptrap.Core;
using Newbe.Claptrap.Demo.Interfaces.Domain.Account;
using Newbe.Claptrap.Orleans;
using Orleans;
using StateData = Newbe.Claptrap.Demo.Models.Domain.Account.AccountStateData;
namespace Newbe.Claptrap.Demo.Scaffold.Domain.Account.Claptrap
{
    [ClaptrapComponent("Account")]
    public partial class Account : Grain, IAccount
    {
        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            var kind = new ClaptrapKind(ActorType.Claptrap, "Account");
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
        public async Task TransferIn(decimal amount, string uid)
        {
            var method = (N20EventMethods.TransferIn.ITransferInMethod)ServiceProvider.GetService(typeof(N20EventMethods.TransferIn.ITransferInMethod));
            var result = await method.Invoke((StateData)Actor.State.Data, amount, uid);
            if (result.EventRaising)
            {
                var @event = new DataEvent(Actor.State.Identity, "BalanceChangeEventData", result.EventData, result.EventUid);
                await Actor.HandleEvent(@event);
            }
        }
        public async Task<TransferResult> TransferOut(decimal amount, string uid)
        {
            var method = (N20EventMethods.TransferOut.ITransferOutMethod)ServiceProvider.GetService(typeof(N20EventMethods.TransferOut.ITransferOutMethod));
            var result = await method.Invoke((StateData)Actor.State.Data, amount, uid);
            if (result.EventRaising)
            {
                var @event = new DataEvent(Actor.State.Identity, "BalanceChangeEventData", result.EventData, result.EventUid);
                await Actor.HandleEvent(@event);
            }
            return result.MethodReturn;
        }
        public async Task Lock()
        {
            var method = (N20EventMethods.Lock.ILockMethod)ServiceProvider.GetService(typeof(N20EventMethods.Lock.ILockMethod));
            var result = await method.Invoke((StateData)Actor.State.Data);
            if (result.EventRaising)
            {
                var @event = new DataEvent(Actor.State.Identity, "LockEventData", result.EventData, result.EventUid);
                await Actor.HandleEvent(@event);
            }
        }
    }
}