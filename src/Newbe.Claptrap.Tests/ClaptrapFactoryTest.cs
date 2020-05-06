using System.Collections.Immutable;
using Autofac;
using Autofac.Extras.Moq;
using FluentAssertions;
using Newbe.Claptrap.Preview;
using Newbe.Claptrap.Preview.Abstractions.Core;
using Newbe.Claptrap.Preview.Abstractions.Design;
using Newbe.Claptrap.Preview.Abstractions.Options;
using Newbe.Claptrap.Preview.Impl;
using Newbe.Claptrap.Preview.Impl.Design;
using Newbe.Claptrap.Preview.Impl.MemoryStore;
using Xunit;
using Xunit.Abstractions;

namespace Newbe.Claptrap.Tests
{
    public class ClaptrapFactoryTest
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ClaptrapFactoryTest(
            ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void CreateMaster()
        {
            var actorIdentity = TestClaptrapIdentity.Instance;
            var claptrapDesign = new ClaptrapDesign
            {
                ClaptrapOptions = new ClaptrapOptions
                {
                    MinionOptions = new MinionOptions(),
                    EventLoadingOptions = new EventLoadingOptions(),
                    StateRecoveryOptions = new StateRecoveryOptions(),
                    StateSavingOptions = new StateSavingOptions()
                },
                Identity = actorIdentity,
                EventHandlerDesigns = ImmutableDictionary<string, IClaptrapEventHandlerDesign>.Empty,
                StateHolderFactoryType = typeof(DeepClonerStateHolderFactory),
                StateDataType = typeof(TestStateData),
                EventLoaderFactoryType = typeof(MemoryEventStoreFactory),
                EventSaverFactoryType = typeof(MemoryEventStoreFactory),
                StateLoaderFactoryType = typeof(MemoryStateStoreFactory),
                StateSaverFactoryType = typeof(MemoryStateStoreFactory),
                EventHandlerFactoryFactoryType = typeof(EventHandlerFactoryFactory),
                InitialStateDataFactoryType = typeof(DefaultInitialStateDataFactory)
            };
            var claptrapDesignStore = new ClaptrapDesignStore();
            claptrapDesignStore.AddOrReplace(claptrapDesign);
            using var mocker = AutoMockHelper.Create(_testOutputHelper, builderAction: builder =>
            {
                builder.RegisterInstance(claptrapDesignStore)
                    .AsImplementedInterfaces()
                    .SingleInstance();
            });

            var actorFactory = mocker.Create<ClaptrapFactory>();
            var actor = actorFactory.Create(actorIdentity);
            actor.Should().NotBeNull();
        }

        [Fact]
        public void CreateMinion()
        {
            var actorIdentity = TestClaptrapIdentity.Instance;
            var claptrapDesignStore = new ClaptrapDesignStore();
            var masterDesign = new ClaptrapDesign
            {
                ClaptrapOptions = new ClaptrapOptions
                {
                    MinionOptions = new MinionOptions(),
                    EventLoadingOptions = new EventLoadingOptions(),
                    StateRecoveryOptions = new StateRecoveryOptions(),
                    StateSavingOptions = new StateSavingOptions()
                },
                Identity = actorIdentity,
                EventHandlerDesigns = ImmutableDictionary<string, IClaptrapEventHandlerDesign>.Empty,
                StateHolderFactoryType = typeof(DeepClonerStateHolderFactory),
                StateDataType = typeof(TestStateData),
                EventLoaderFactoryType = typeof(MemoryEventStoreFactory),
                EventSaverFactoryType = typeof(MemoryEventStoreFactory),
                StateLoaderFactoryType = typeof(MemoryStateStoreFactory),
                StateSaverFactoryType = typeof(MemoryStateStoreFactory),
                EventHandlerFactoryFactoryType = typeof(EventHandlerFactoryFactory),
                InitialStateDataFactoryType = typeof(DefaultInitialStateDataFactory)
            };
            claptrapDesignStore.AddOrReplace(masterDesign);
            var minionDesign = new ClaptrapDesign
            {
                ClaptrapOptions = new ClaptrapOptions
                {
                    MinionOptions = new MinionOptions(),
                    EventLoadingOptions = new EventLoadingOptions(),
                    StateRecoveryOptions = new StateRecoveryOptions(),
                    StateSavingOptions = new StateSavingOptions()
                },
                Identity = actorIdentity,
                EventHandlerDesigns = ImmutableDictionary<string, IClaptrapEventHandlerDesign>.Empty,
                StateHolderFactoryType = typeof(DeepClonerStateHolderFactory),
                StateDataType = typeof(TestStateData),
                EventLoaderFactoryType = typeof(MemoryEventStoreFactory),
                EventSaverFactoryType = typeof(MemoryEventStoreFactory),
                StateLoaderFactoryType = typeof(MemoryStateStoreFactory),
                StateSaverFactoryType = typeof(MemoryStateStoreFactory),
                EventHandlerFactoryFactoryType = typeof(EventHandlerFactoryFactory),
                InitialStateDataFactoryType = typeof(DefaultInitialStateDataFactory),
                ClaptrapMasterDesign = masterDesign
            };
            claptrapDesignStore.AddOrReplace(minionDesign);
            using var mocker = AutoMockHelper.Create(_testOutputHelper, builderAction: builder =>
            {
                builder.RegisterInstance(claptrapDesignStore)
                    .AsImplementedInterfaces()
                    .SingleInstance();
            });

            var actorFactory = mocker.Create<ClaptrapFactory>();
            var actor = actorFactory.Create(actorIdentity);
            actor.Should().NotBeNull();
        }


        private class TestStateData : IStateData
        {
        }
    }
}