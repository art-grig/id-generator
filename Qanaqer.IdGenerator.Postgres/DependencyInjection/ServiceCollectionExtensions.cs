using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qanaqer.IdGenerator.Extensions;

namespace Qanaqer.IdGenerator.Postgres.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPgIdGenerator<TEnum, TDbContext>(this IServiceCollection services, IdGeneratorOptions options)
            where TEnum : Enum
            where TDbContext : DbContext
        {
            return services.AddIdGenerator<TEnum,
                PgIdGenerator<TEnum, TDbContext>,
                PgSequenceManager<TDbContext>>(options);
        }
    }
}
