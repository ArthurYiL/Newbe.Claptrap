using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extras.Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Xunit;
using Moq;
using Newbe.Claptrap.Context;
using Newbe.Claptrap.Core;
using Newbe.Claptrap.EventHandler;
using Newbe.Claptrap.EventStore;
using Newbe.Claptrap.StateStore;
using Xunit;
using Xunit.Abstractions;

namespace Newbe.Claptrap.Tests
{
    public class ReactiveActorTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ReactiveActorTest(
            ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task EmptyEvents()
        {
            using var mocker = AutoMock.GetStrict();
            mocker.VerifyAll = true;

            var state = new AccountState();
            mocker.Mock<IStateStore>()
                .Setup(x => x.GetStateSnapshot())
                .ReturnsAsync(state);

            mocker.Mock<IEventStore>()
                .Setup(x => x.GetEvents(It.IsAny<ulong>(), It.IsAny<ulong>()))
                .ReturnsAsync(Enumerable.Empty<IEvent>());

            IActor actor = mocker.Create<Actor>();
            await actor.ActivateAsync();
        }

        [Fact]
        public async Task SomeEvents()
        {
            using var mocker = AutoMock.GetStrict(builder =>
            {
                var loggerFactory = new LoggerFactory(new[] {new XunitLoggerProvider(_testOutputHelper)});
                builder.RegisterInstance(loggerFactory)
                    .AsImplementedInterfaces();
                builder.RegisterGeneric(typeof(Logger<>))
                    .As(typeof(ILogger<>))
                    .SingleInstance();
            });
            mocker.VerifyAll = true;

            var state = new AccountState();
            mocker.Mock<IStateStore>()
                .Setup(x => x.GetStateSnapshot())
                .ReturnsAsync(state);

            mocker.Mock<IEventStore>()
                .SetupSequence(x => x.GetEvents(It.IsAny<ulong>(), It.IsAny<ulong>()))
                .ReturnsAsync(AllEvents())
                .ReturnsAsync(Enumerable.Empty<IEvent>());

            mocker.Mock<IEventHandlerFactory>()
                .Setup(x => x.Create(It.IsAny<IEventContext>()))
                .Returns(new Handler());

            IActor actor = mocker.Create<Actor>();
            await actor.ActivateAsync();

            state.Version.Should().Be(3);

            static IEnumerable<IEvent> AllEvents()
            {
                yield return new AccountEvent
                {
                    Version = 1,
                };
                yield return new AccountEvent
                {
                    Version = 2,
                };
                yield return new AccountEvent
                {
                    Version = 3,
                };
            }
        }

        [Fact]
        public async Task ThrowException()
        {
            using var mocker = AutoMock.GetStrict(builder =>
            {
                var loggerFactory = new LoggerFactory(new[] {new XunitLoggerProvider(_testOutputHelper)});
                builder.RegisterInstance(loggerFactory)
                    .AsImplementedInterfaces();
                builder.RegisterGeneric(typeof(Logger<>))
                    .As(typeof(ILogger<>))
                    .SingleInstance();
            });
            mocker.VerifyAll = true;

            var state = new AccountState
            {
                Identity = _testActorIdentity
            };
            mocker.Mock<IStateStore>()
                .Setup(x => x.GetStateSnapshot())
                .ReturnsAsync(state);

            mocker.Mock<IEventStore>()
                .SetupSequence(x => x.GetEvents(It.IsAny<ulong>(), It.IsAny<ulong>()))
                .ReturnsAsync(AllEvents())
                .ReturnsAsync(Enumerable.Empty<IEvent>());

            mocker.Mock<IEventHandlerFactory>()
                .SetupSequence(x => x.Create(It.IsAny<IEventContext>()))
                .Returns(new Handler())
                .Returns(new ExceptionHandler())
                .Returns(new Handler())
                ;

            IActor actor = mocker.Create<Actor>();

            await Assert.ThrowsAsync<ActivateFailException>(async () => await actor.ActivateAsync());

            state.Version.Should().Be(1);

            static IEnumerable<IEvent> AllEvents()
            {
                yield return new AccountEvent
                {
                    Version = 1,
                };
                yield return new AccountEvent
                {
                    Version = 2,
                };
                yield return new AccountEvent
                {
                    Version = 3,
                };
            }
        }

        [Fact]
        public async Task HandleEvent()
        {
            using var mocker = AutoMock.GetStrict(builder =>
            {
                var loggerFactory = new LoggerFactory(new[] {new XunitLoggerProvider(_testOutputHelper)});
                builder.RegisterInstance(loggerFactory)
                    .AsImplementedInterfaces();
                builder.RegisterGeneric(typeof(Logger<>))
                    .As(typeof(ILogger<>))
                    .SingleInstance();
            });
            mocker.VerifyAll = true;

            var state = new AccountState();
            mocker.Mock<IStateStore>()
                .Setup(x => x.GetStateSnapshot())
                .ReturnsAsync(state);

            mocker.Mock<IEventStore>()
                .SetupSequence(x => x.GetEvents(It.IsAny<ulong>(), It.IsAny<ulong>()))
                .ReturnsAsync(Enumerable.Empty<IEvent>());

            mocker.Mock<IEventStore>()
                .Setup(x => x.SaveEvent(It.IsAny<IEvent>()))
                .ReturnsAsync(EventSavingResult.Success);

            mocker.Mock<IStateHolder>()
                .Setup(x => x.DeepCopy(It.IsAny<IState>()))
                .Returns(state);

            mocker.Mock<IEventHandlerFactory>()
                .SetupSequence(x => x.Create(It.IsAny<IEventContext>()))
                .Returns(new Handler())
                ;

            IActor actor = mocker.Create<Actor>();
            await actor.ActivateAsync();

            await actor.HandleEvent(new AccountEvent());
            state.Version.Should().Be(1);
        }

        public class AccountState : IState
        {
            public IActorIdentity Identity { get; set; }
            public IStateData Data { get; set; }
            public ulong Version { get; set; }

            public void IncreaseVersion()
            {
                Version += 1;
            }
        }

        public class AccountEvent : IEvent
        {
            public IActorIdentity ActorIdentity { get; set; }
            public ulong Version { get; set; }
            public IEventUid Uid { get; set; }
            public string EventTypeCode { get; set; }
            public IEventData Data { get; set; }
        }

        public class Handler : IEventHandler
        {
            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public Task<IState> HandleEvent(IEventContext eventContext)
            {
                return Task.FromResult(eventContext.State);
            }
        }

        public class ExceptionHandler : IEventHandler
        {
            public ValueTask DisposeAsync()
            {
                return new ValueTask();
            }

            public Task<IState> HandleEvent(IEventContext eventContext)
            {
                throw new Exception();
            }
        }

        private readonly IActorIdentity _testActorIdentity = new ActorIdentity("123", "testActor");

        public class ActorIdentity : IActorIdentity
        {
            public ActorIdentity(string id, string typeCode)
            {
                Id = id;
                TypeCode = typeCode;
            }

            public string Id { get; }
            public string TypeCode { get; }

            public bool Equals(IActorIdentity other)
            {
                return Id == other.Id && TypeCode == other.TypeCode;
            }

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ActorIdentity) obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Id, TypeCode);
            }
        }
    }
}