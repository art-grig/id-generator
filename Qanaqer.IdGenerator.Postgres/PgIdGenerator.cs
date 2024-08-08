using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Qanaqer.IdGenerator;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Qanaqer.IdGenerator.Extensions;
using System.Runtime.CompilerServices;
using System.Diagnostics;

namespace Qanaqer.IdGenerator.Postgres
{
    using DbContext = Microsoft.EntityFrameworkCore.DbContext;
    public class PgIdGenerator<TDbContext, TEnum> : IIdGenerator<TEnum> where TEnum : Enum
        where TDbContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<TEnum, ConcurrentQueue<long>> _idQuquesMap;
        private readonly IOptions<IdGeneratorOptions> _idGenOptions;
        private readonly string? _schemaName;
        private readonly Dictionary<TEnum, SemaphoreSlim> _semaphoreMap;

        public PgIdGenerator(IServiceProvider serviceProvider,
            IOptions<IdGeneratorOptions> idGenOptions)
        {
            _serviceProvider = serviceProvider;
            _idGenOptions = idGenOptions;
            _idQuquesMap = new ConcurrentDictionary<TEnum, ConcurrentQueue<long>>();
            _semaphoreMap = new Dictionary<TEnum, SemaphoreSlim>();

            var sequences = Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
            foreach (var seq in sequences)
            {
                _idQuquesMap.TryAdd(seq, new ConcurrentQueue<long>());
                _semaphoreMap.TryAdd(seq, new SemaphoreSlim(1, 1));
            }

            _schemaName = PgSequenceManager<TDbContext>.GetSchemaName<TEnum>();
        }

        private async Task<T> WrapFuncInScope<T>(Func<IServiceProvider, Task<T>> func)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                return await func(scope.ServiceProvider);
            }
        }

        private Task<long> FetchNextIdFromDb(TEnum sequence, long range)
        {
            return WrapFuncInScope((sp) =>
            {
                var dbContext = sp.GetRequiredService<TDbContext>();
                return dbContext.ExecuteCommandAsync(async (cmd) =>
                {
                    cmd.CommandType = CommandType.Text;
                    var sequenceName = $@"{_schemaName ?? dbContext.GetDefaultSchemaName()}.""{sequence.ToString()}""";
                    cmd.CommandText = $"SELECT nextval('{sequenceName}') FROM generate_series(1, {range})";

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        _idQuquesMap[sequence].Enqueue(reader.GetInt64(0));
                    }

                    _idQuquesMap[sequence].TryDequeue(out var nextId);
                    return nextId;
                });
            });
        }

        public async Task<long> NextId(TEnum sequence)
        {
            var batchSize = _idGenOptions.Value.BatchSize <= 0 ? 1 : _idGenOptions.Value.BatchSize;

            if (_idQuquesMap[sequence].TryDequeue(out var nextId))
            {
                return nextId;
            }
            else
            {
                await _semaphoreMap[sequence].WaitAsync();
                try
                {
                    if (_idQuquesMap[sequence].TryDequeue(out var newFetchedId))
                    {
                        return newFetchedId;
                    }

                    var fetchedNextId = await FetchNextIdFromDb(sequence, batchSize);
                    return fetchedNextId;
                }
                finally
                {
                    _semaphoreMap[sequence].Release();
                }
            }
        }
    }
}
