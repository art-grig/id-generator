using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qanaqer.IdGenerator.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qanaqer.IdGenerator.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddIdGenerator<TEnum, TIdGenerator, TSequenceManager>(this IServiceCollection services, IdGeneratorOptions options)
            where TEnum : Enum
            where TIdGenerator : class, IIdGenerator<TEnum>
            where TSequenceManager : class, ISequenceManager
        {
            services.AddSingleton<IIdGenerator<TEnum>, TIdGenerator>();
            services.AddScoped<ISequenceManager, TSequenceManager>();
            services.Configure<IdGeneratorOptions>(o =>
            {
                o.BatchSize = options.BatchSize;
            });

            return services;
        }
        public static async Task<T> ExecuteInNewScope<T>(this IServiceProvider serviceProvider, Func<IServiceProvider, Task<T>> func)
        {
            using var scope = serviceProvider.CreateScope();
            return await func(scope.ServiceProvider);
        }
    }
}
