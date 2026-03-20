using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;

namespace EventCore;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddEventCore()
        {
            services.AddHostedService<AggregatingEventProcessor>();
            var channel = Channel.CreateUnbounded<IEvent>(new() { SingleReader = true });

            services.AddSingleton(channel.Writer);

            // TODO: make this only for the processor
            services.AddSingleton(channel.Reader);

            services.AddSingleton<ISession, EventCoreSession>();
            services.AddSingleton<EventStoreOperations>();
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
                    services.AddSingleton(new StartsWithEventRegistration(
                        @interface.GenericTypeArguments[0],
                        @interface.GenericTypeArguments[1]));
                }
                else if (@interface.GetGenericTypeDefinition() == typeof(IHandles<,>))
                {
                    services.AddSingleton(new HandlesEventRegistration(
                        @interface.GenericTypeArguments[0],
                        @interface.GenericTypeArguments[1]));
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
                    // TODO: find a reusable IProxy<IEventListener<>> for this?

                    var interfaceType = typeof(IEventListener<,>).MakeGenericType(@interface.GenericTypeArguments);

                    services.Add(new ServiceDescriptor(interfaceType, typeof(TListener), serviceLifetime));

                    var proxyType = typeof(EventListenerProxy<,>).MakeGenericType(@interface.GenericTypeArguments);

                    var key = @interface.GenericTypeArguments.Deconstruct();

                    services.Add(new ServiceDescriptor(typeof(IEventListenerProxy), key, proxyType, serviceLifetime));
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
