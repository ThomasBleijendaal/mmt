using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;

namespace EventCore;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddEventCore()
        {
            var channel = Channel.CreateUnbounded<IEvent>(new() { SingleReader = true });

            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<AggregatingEventProcessor>(sp, channel.Reader));

            services.AddSingleton<ISession, EventCoreSession>();
            services.AddSingleton(sp => ActivatorUtilities.CreateInstance<EventStoreOperations>(sp, channel.Writer));
            services.AddSingleton<EntityCache>();

            return services;
        }

        public IServiceCollection AddInMemoryStorage()
        {
            services.AddSingleton<IEventStore, InMemoryEventStorage>();

            return services;
        }

        public IServiceCollection AddEntity<TEntity>()
            where TEntity : IEntity
        {
            foreach (var @interface in typeof(TEntity).GetInterfaces().Where(x => x.IsGenericType))
            {
                if (@interface.GetGenericTypeDefinition() == typeof(IStartsWith<,>))
                {
                    var proxyType = typeof(StartsWithProxy<,>).MakeGenericType(@interface.GenericTypeArguments);
                    var genericProxyType = typeof(IStartsWithProxy<>).MakeGenericType(@interface.GenericTypeArguments[1]);

                    var createByEventKey = @interface.GenericTypeArguments[0];
                    var key = @interface.GenericTypeArguments.Deconstruct();

                    services.AddKeyedSingleton(typeof(IStartsWithProxy), key, proxyType);
                    services.AddKeyedSingleton(typeof(IStartsWithProxy), createByEventKey, proxyType);
                    services.AddKeyedSingleton(genericProxyType, key, proxyType);
                }
                else if (@interface.GetGenericTypeDefinition() == typeof(IHandles<,>))
                {
                    var proxyType = typeof(HandlesProxy<,>).MakeGenericType(@interface.GenericTypeArguments);
                    var genericProxyType = typeof(IHandlesProxy<>).MakeGenericType(@interface.GenericTypeArguments[1]);

                    var key = @interface.GenericTypeArguments.Deconstruct();

                    services.AddKeyedSingleton(typeof(IHandlesProxy), key, proxyType);
                    services.AddKeyedSingleton(genericProxyType, key, proxyType);
                }
            }

            return services;
        }

        public IServiceCollection AddEventListener<TListener>(ServiceLifetime serviceLifetime = ServiceLifetime.Singleton)
            where TListener : IEventListener
        {
            foreach (var @interface in typeof(TListener).GetInterfaces().Where(x => x.IsGenericType))
            {
                if (@interface.GetGenericTypeDefinition() == typeof(IEventListener<,>))
                {
                    services.Add(new ServiceDescriptor(typeof(TListener), typeof(TListener), serviceLifetime));

                    var proxyType = typeof(EventListenerProxy<,>).MakeGenericType(@interface.GenericTypeArguments);

                    var key = @interface.GenericTypeArguments.Deconstruct();

                    services.Add(new ServiceDescriptor(
                        typeof(IEventListenerProxy),
                        key,
                        (sp, _) => ActivatorUtilities.CreateInstance(sp, proxyType, sp.GetRequiredService<TListener>()),
                        serviceLifetime));
                }
            }

            return services;
        }
    }

    extension(Type[] types)
    {
        private (Type @event, Type) Deconstruct()
        {
            return (types[0], types[1]);
        }
    }
}
