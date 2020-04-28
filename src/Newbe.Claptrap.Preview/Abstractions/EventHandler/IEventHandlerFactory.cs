using Newbe.Claptrap.Preview.Context;
using Newbe.Claptrap.Preview.Core;

namespace Newbe.Claptrap.Preview.EventHandler
{
    public interface IEventHandlerFactory
    {
        /// <summary>
        /// create event handler from event context
        /// </summary>
        /// <param name="eventContext"></param>
        /// <exception cref="EventHandlerNotFoundException">thrown if there is no handler found</exception>
        /// <returns></returns>
        IEventHandler Create(IEventContext eventContext);
    }
}