using Newbe.Claptrap;
using System.Threading.Tasks;
using EventData = Newbe.Claptrap.ScaffoldGeneratorTest.TestEventDataType;
using StateData = Newbe.Claptrap.ScaffoldGeneratorTest.TestStateDataType;
namespace Claptrap._20EventMethods
{
    public class IntReturnMethod : IIntReturnMethod
    {
        public Task<EventMethodResult<EventData, System.Int32>> Invoke(StateData stateData)
        {
            throw new NotImplementedException();
        }
    }
}