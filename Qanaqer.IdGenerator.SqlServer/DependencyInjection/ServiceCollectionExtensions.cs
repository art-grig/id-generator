using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qanaqer.IdGenerator.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qanaqer.IdGenerator.SqlServer.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSqlServerIdGenerator<TEnum, TDbContext>(this IServiceCollection services, IdGeneratorOptions options)
            where TEnum : Enum
            where TDbContext : DbContext
        {
            return services.AddIdGenerator<TEnum,
                SqlServerIdGenerator<TEnum, TDbContext>,
                SqlServerSequenceManager<TDbContext>>(options);
        }
    }
}
