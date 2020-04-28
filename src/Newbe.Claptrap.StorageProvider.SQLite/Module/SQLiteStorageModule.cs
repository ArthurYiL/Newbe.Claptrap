using Autofac;
using Newbe.Claptrap.Autofac;
using Newbe.Claptrap.EventStore;

namespace Newbe.Claptrap.StorageProvider.SQLite.Module
{
    public class SQLiteStorageModule : StorageSupportModule
    {
        public SQLiteStorageModule() : base(
            EventStoreProvider.SQLite,
            StateStoreProvider.SQLite)
        {
            EventStoreType = typeof(SQLiteEventStore);
            EventStoreFactoryHandlerType = typeof(SQLiteEventStoreFactoryHandler);

            StateStoreType = typeof(SQLiteStateStore);
            StateStoreFactoryHandlerType = typeof(SQLiteStateStoreFactoryHandler);
        }

        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<SQLiteDbFactory>()
                .As<ISQLiteDbFactory>()
                .SingleInstance();

            builder.RegisterType<SQLiteDbManager>()
                .As<ISQLiteDbManager>()
                .SingleInstance();
        }
    }
}