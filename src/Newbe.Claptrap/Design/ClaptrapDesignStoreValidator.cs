using System;
using System.Collections.Generic;
using System.Linq;

namespace Newbe.Claptrap.Design
{
    public class ClaptrapDesignStoreValidator : IClaptrapDesignStoreValidator
    {
        public (bool isOk, string errorMessage) Validate(IClaptrapDesignStore claptrapDesignStore)
        {
            var errors = claptrapDesignStore.SelectMany(ValidateOne)
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();
            var errorMessage = string.Join(",", errors);

            return (!errors.Any(), errorMessage);

            static IEnumerable<string> ValidateOne(IClaptrapDesign design)
            {
                foreach (var error in ValidateTypes())
                {
                    if (!string.IsNullOrEmpty(error))
                    {
                        yield return $"{design.Identity} {error}";
                    }
                }

                IEnumerable<string> ValidateTypes()
                {
                    foreach (var error in ValidateMasterDesign(design))
                    {
                        yield return error;
                    }

                    yield return ValidateTypeNotNull(design.Identity.TypeCode,
                        nameof(design.Identity.TypeCode));
                    yield return ValidateTypeNotNull(design.Identity,
                        nameof(design.Identity));
                    yield return ValidateTypeNotNull(design.StateDataType,
                        nameof(design.StateDataType));
                    yield return ValidateTypeNotNull(design.EventLoaderFactoryType,
                        nameof(design.EventLoaderFactoryType));
                    yield return ValidateTypeNotNull(design.EventSaverFactoryType,
                        nameof(design.EventSaverFactoryType));
                    yield return ValidateTypeNotNull(design.StateLoaderFactoryType,
                        nameof(design.StateLoaderFactoryType));
                    yield return ValidateTypeNotNull(design.StateSaverFactoryType,
                        nameof(design.StateSaverFactoryType));
                    yield return ValidateTypeNotNull(design.InitialStateDataFactoryType,
                        nameof(design.InitialStateDataFactoryType));
                    yield return ValidateTypeNotNull(design.StateHolderFactoryType,
                        nameof(design.StateHolderFactoryType));
                    yield return ValidateTypeNotNull(design.ClaptrapOptions,
                        nameof(design.ClaptrapOptions));
                    yield return ValidateTypeNotNull(design.ClaptrapOptions.EventLoadingOptions,
                        nameof(design.ClaptrapOptions.EventLoadingOptions));
                    yield return ValidateTypeNotNull(design.ClaptrapOptions.StateRecoveryOptions,
                        nameof(design.ClaptrapOptions.StateRecoveryOptions));
                    yield return ValidateTypeNotNull(design.ClaptrapOptions.StateSavingOptions,
                        nameof(design.ClaptrapOptions.StateSavingOptions));
                    if (design.ClaptrapMasterDesign != null)
                    {
                        yield return ValidateTypeNotNull(design.ClaptrapOptions.MinionOptions!,
                            nameof(design.ClaptrapOptions.MinionOptions));
                    }

                    yield return ValidateTypeNotNull(design.EventHandlerFactoryFactoryType,
                        nameof(design.EventHandlerFactoryFactoryType));
                    yield return ValidateTypeNotNull(design.EventHandlerDesigns, nameof(design.EventHandlerDesigns));
                    yield return ValidateClaptrapComponent<IEventLoader>(design.EventLoaderFactoryType);
                    yield return ValidateClaptrapComponent<IEventSaver>(design.EventSaverFactoryType);
                    yield return ValidateClaptrapComponent<IStateLoader>(design.StateLoaderFactoryType);
                    yield return ValidateClaptrapComponent<IStateSaver>(design.StateSaverFactoryType);
                    yield return ValidateClaptrapComponent<IStateHolder>(design.StateHolderFactoryType);
                    yield return ValidateClaptrapComponent<IEventHandlerFactory>(design.EventHandlerFactoryFactoryType);

                    static string ValidateClaptrapComponent<TComponent>(Type type)
                    {
                        return type.GetInterface(typeof(TComponent).FullName) != null
                            ? $"type {type} is not implement {typeof(TComponent)} ."
                            : string.Empty;
                    }

                    static string ValidateTypeNotNull(object type, string name)
                    {
                        return type == null
                            ? $"{name} is required, please set it correctly"
                            : string.Empty;
                    }

                    static IEnumerable<string> ValidateMasterDesign(IClaptrapDesign design)
                    {
                        if (design.ClaptrapMasterDesign == null)
                        {
                            yield break;
                        }

                        foreach (var (key, _) in design.ClaptrapMasterDesign.EventHandlerDesigns)
                        {
                            if (!design.EventHandlerDesigns.ContainsKey(key))
                            {
                                yield return
                                    $"There is no event handler found for {key} in {design.Identity}. It must be define as this is a minion and the mater will send it to this. If you don`t handle the event, you can define {nameof(EmptyEventHandler)} for this event.";
                            }
                        }
                    }
                }
            }
        }
    }
}