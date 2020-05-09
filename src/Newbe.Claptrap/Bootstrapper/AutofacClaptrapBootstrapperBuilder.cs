using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Autofac;
using Microsoft.Extensions.Logging;
using Newbe.Claptrap.Design;
using Newbe.Claptrap.Modules;
using Newbe.Claptrap.Options;
using Newtonsoft.Json;
using static Newbe.Claptrap.LK.L0001AutofacClaptrapBootstrapperBuilder;
using Module = Autofac.Module;

namespace Newbe.Claptrap.Bootstrapper
{
    public class AutofacClaptrapBootstrapperBuilder : IClaptrapBootstrapperBuilder
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ContainerBuilder _applicationBuilder;
        private readonly ILogger<AutofacClaptrapBootstrapperBuilder> _logger;
        private readonly Lazy<IL> _l;

        public AutofacClaptrapBootstrapperBuilder(
            ILoggerFactory loggerFactory,
            ContainerBuilder applicationBuilder)
        {
            _loggerFactory = loggerFactory;
            _applicationBuilder = applicationBuilder;
            LoggerFactoryHolder.Instance = loggerFactory;
            _logger = loggerFactory.CreateLogger<AutofacClaptrapBootstrapperBuilder>();
            _l = new Lazy<IL>(CreateL);
            Options = new ClaptrapBootstrapperBuilderOptions
            {
                DesignAssemblies = Enumerable.Empty<Assembly>(),
                CultureInfo = CultureInfo.CurrentCulture,
                ClaptrapDesignStoreConfigurators = new List<IClaptrapDesignStoreConfigurator>
                {
                    new GlobalClaptrapDesignStoreConfigurator(new GlobalClaptrapDesign
                    {
                        ClaptrapOptions = new ClaptrapOptions
                        {
                            StateSavingOptions = new StateSavingOptions
                            {
                                SavingWindowTime = TimeSpan.FromSeconds(10),
                                SaveWhenDeactivateAsync = true,
                                SavingWindowVersionLimit = 1000,
                            },
                            MinionOptions = new MinionOptions
                            {
                                ActivateMinionsAtStart = false
                            },
                            EventLoadingOptions = new EventLoadingOptions
                            {
                                LoadingCountInOneBatch = 1000
                            },
                            StateRecoveryOptions = new StateRecoveryOptions
                            {
                                StateRecoveryStrategy = StateRecoveryStrategy.FromStore
                            }
                        },
                        InitialStateDataFactoryType = typeof(DefaultInitialStateDataFactory),
                        StateHolderFactoryType = typeof(NoChangeStateHolderFactory),
                        EventHandlerFactoryFactoryType = typeof(EventHandlerFactoryFactory),
                    })
                },
                ClaptrapDesignStoreProviders = new List<IClaptrapDesignStoreProvider>(),
                ClaptrapModuleProviders = new List<IClaptrapModuleProvider>()
            };
        }

        private IL CreateL()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new LocalizationModule(Options.CultureInfo));
            var container = builder.Build();
            var l = container.Resolve<IL>();
            return l;
        }

        public ClaptrapBootstrapperBuilderOptions Options { get; }

        public IClaptrapBootstrapper Build()
        {
            try
            {
                return BuildCore();
            }
            catch (Exception e)
            {
                _logger.LogError(e, _l.Value[L001BuildException]);
                throw;
            }

            IClaptrapBootstrapper BuildCore()
            {
                var builder = new ContainerBuilder();
                builder.RegisterModule<ClaptrapDesignScanningModule>();
                builder.RegisterModule(new LoggingModule(_loggerFactory));
                builder.RegisterModule(new LocalizationModule(Options.CultureInfo));
                builder.RegisterInstance(Options);
                var container = builder.Build();
                IClaptrapDesignStore? claptrapDesignStore = null;
                using (var scope = container.BeginLifetimeScope())
                {
                    var factory = scope.Resolve<IClaptrapDesignStoreFactory>();
                    var validator = scope.Resolve<IClaptrapDesignStoreValidator>();
                    claptrapDesignStore =
                        CreateClaptrapDesignStore(
                            factory,
                            validator,
                            Options.DesignAssemblies ?? throw new ArgumentNullException());
                }

                IClaptrapApplicationModule[]? applicationModules = null;
                using (var scope = container.BeginLifetimeScope(innerBuilder =>
                {
                    var providerTypes = Options.ModuleAssemblies.SelectMany(x => x.GetTypes())
                        .Where(x => x.IsClass && !x.IsAbstract)
                        .Where(x => x.GetInterface(typeof(IClaptrapApplicationModuleProvider).FullName) != null)
                        .ToArray();
                    _logger.LogDebug("Found type {providerTypes} as {name}",
                        providerTypes,
                        nameof(IClaptrapApplicationModuleProvider));
                    innerBuilder.RegisterTypes(providerTypes)
                        .As<IClaptrapApplicationModuleProvider>()
                        .InstancePerLifetimeScope();
                    innerBuilder.RegisterInstance(claptrapDesignStore);
                }))
                {
                    var moduleProviders =
                        scope.Resolve<IEnumerable<IClaptrapApplicationModuleProvider>>();
                    applicationModules = moduleProviders
                        .SelectMany(x =>
                        {
                            var ms = x.GetClaptrapApplicationModules().ToArray();
                            _logger.LogDebug("Found {count} claptrap application modules from {type} : {modules}",
                                ms.Length,
                                x,
                                ms.Select(a => a.Name));
                            return ms;
                        })
                        .ToArray();

                    _logger.LogInformation(
                        "Scanned {assemblies}, and found {count} claptrap application modules : {modules}",
                        Options.ModuleAssemblies,
                        applicationModules.Length,
                        applicationModules.Select(x => x.Name));
                }

                var autofacModules = applicationModules
                    .OfType<Module>()
                    .ToArray();

                _logger.LogInformation(
                    "Filtered and found {count} Autofac modules : {modules}",
                    autofacModules.Length,
                    autofacModules);

                // TODO move
                var providers = Options.ModuleAssemblies
                    .SelectMany(x => x.GetTypes())
                    .Where(x => x.IsClass && !x.IsAbstract)
                    .Where(x => x.GetInterface(typeof(IClaptrapModuleProvider).FullName) != null)
                    .ToArray();
                _logger.LogInformation(
                    "Scanned {assemblies}, and found {count} claptrap modules providers : {modules}",
                    Options.ModuleAssemblies,
                    providers.Length,
                    providers);

                _applicationBuilder.RegisterTypes(providers)
                    .As<IClaptrapModuleProvider>();

                var claptrapBootstrapper =
                    new AutofacClaptrapBootstrapper(_applicationBuilder,
                        autofacModules,
                        claptrapDesignStore);

                return claptrapBootstrapper;
            }
        }

        private IClaptrapDesignStore CreateClaptrapDesignStore(
            IClaptrapDesignStoreFactory factory,
            IClaptrapDesignStoreValidator validator,
            IEnumerable<Assembly> assemblies)
        {
            foreach (var provider in Options.ClaptrapDesignStoreProviders)
            {
                _logger.LogDebug(_l.Value[L002AddProviderAsClaptrapDesignProvider], provider);
                factory.AddProvider(provider);
            }

            var assemblyArray = assemblies as Assembly[] ?? assemblies.ToArray();
            _logger.LogDebug(_l.Value[L003StartToScan],
                assemblyArray.Length,
                assemblyArray.Select(x => x.FullName));
            _logger.LogDebug(_l.Value[L004StartToCreateClaptrapDesign]);
            var claptrapDesignStore = factory.Create(assemblyArray);

            _logger.LogInformation(_l.Value[L005ClaptrapStoreCreated]);
            _logger.LogDebug(_l.Value[L006ShowAllDesign],
                JsonConvert.SerializeObject(claptrapDesignStore.ToArray()));

            foreach (var configurator in Options.ClaptrapDesignStoreConfigurators)
            {
                _logger.LogDebug(_l.Value[L007StartToConfigureDesignStore], configurator);
                configurator.Configure(claptrapDesignStore);
            }

            _logger.LogInformation(_l.Value[L008CountDesigns],
                claptrapDesignStore.Count());
            _logger.LogDebug(_l.Value[L009ShowDesignsAfterConfiguration],
                JsonConvert.SerializeObject(claptrapDesignStore.ToArray()));

            _logger.LogDebug(_l.Value[L010StartToValidateDesigns]);
            var (isOk, errorMessage) = validator.Validate(claptrapDesignStore);
            if (!isOk)
            {
                throw new ClaptrapDesignStoreValidationFailException(errorMessage);
            }

            _logger.LogInformation(_l.Value[L011DesignValidationSuccess]);
            return claptrapDesignStore;
        }
    }
}