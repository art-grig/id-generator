﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Qanaqer.IdGenerator.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Qanaqer.IdGenerator.Abstractions;

namespace Qanaqer.IdGenerator.SqlServer
{
    using DbContext = Microsoft.EntityFrameworkCore.DbContext;
    public class SqlServerIdGenerator<TEnum, TDbContext> : IIdGenerator<TEnum> 
        where TEnum : Enum
        where TDbContext : DbContext
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<TEnum, SemaphoreSlim> _semaphoreMap;
        private readonly Dictionary<TEnum, StrongBox<long>> _idMap;
        private readonly IOptions<IdGeneratorOptions> _idGenOptions;
        private readonly string? _schemaName;

        public SqlServerIdGenerator(IServiceProvider serviceProvider, IOptions<IdGeneratorOptions> idGenOptions)
        {
            _serviceProvider = serviceProvider;
            _idGenOptions = idGenOptions;
            _semaphoreMap = new Dictionary<TEnum, SemaphoreSlim>();
            _idMap = new Dictionary<TEnum, StrongBox<long>>();

            var sequences = Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
            foreach (var seq in sequences)
            {
                _semaphoreMap.Add(seq, new SemaphoreSlim(1, 1));
                _idMap.Add(seq, new StrongBox<long>(0));
            }

            _schemaName = SqlServerSequenceManager<TDbContext>.GetSchemaName<TEnum>();
        }

        private Task<long> FetchNextRange(TEnum sequence, long range)
        {
            return _serviceProvider.ExecuteInNewScope((sp) =>
            {
                var dbContext = sp.GetRequiredService<TDbContext>();
                return dbContext.ExecuteCommandAsync(async (cmd) =>
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "sys.sp_sequence_get_range";
                    cmd.AddParameter("@sequence_name", $"[{_schemaName ?? dbContext.GetDefaultSchemaName()}]." + sequence.ToString());
                    cmd.AddParameter("@range_size", range);

                    // Specify an output parameter to retrieve the first value of the generated range.
                    SqlParameter firstValueInRange = new SqlParameter("@range_first_value", SqlDbType.Variant);
                    firstValueInRange.Direction = ParameterDirection.Output;
                    cmd.Parameters.Add(firstValueInRange);

                    await cmd.ExecuteNonQueryAsync();
                    return (long)firstValueInRange.Value;
                });
            });
        }

        public async Task<long> NextId(TEnum sequence)
        {
            var batchSize = _idGenOptions.Value.BatchSize;
            if (_idMap[sequence].Value % batchSize == 0)
            {
                await _semaphoreMap[sequence].WaitAsync();
                try
                {
                    if (_idMap[sequence].Value % batchSize == 0) {
                        var nextId = await FetchNextRange(sequence, batchSize);
                        Interlocked.Exchange(ref _idMap[sequence].Value, nextId);
                        return nextId;
                    }
                }
                finally
                {
                    _semaphoreMap[sequence].Release();
                }
            }

            return Interlocked.Increment(ref _idMap[sequence].Value); ;
        }
    }
}
