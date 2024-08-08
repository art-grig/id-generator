using Qanaqer.IdGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qanaqer.IdGenerator.Extensions;
using Qanaqer.IdGenerator.Abstractions;

namespace Qanaqer.IdGenerator.Postgres
{
    using DbContext = Microsoft.EntityFrameworkCore.DbContext;
    internal class PgSequenceManager<TDbContext> : ISequenceManager
        where TDbContext : DbContext
    {
        private readonly DbContext _dbContext;

        public PgSequenceManager(TDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public static string? GetSchemaName<TEnum>()
        {
            var enumType = typeof(TEnum);
            var attributeType = typeof(IdGenSequencesAttribute);
            var attributes = enumType.GetCustomAttributes(attributeType, true);

            if (attributes.Length == 0)
                throw new ArgumentException($"{enumType.Name} does not have attribute {attributeType.Name}");

            return (attributes!.First() as IdGenSequencesAttribute)!.SchemaName;
        }

        public Task CreateIfNotExist<TEnum>(int incrementBy = 1, int startWith = 1) where TEnum : Enum
        {
            return CreateIfNotExist(incrementBy, startWith, GetSchemaName<TEnum>(), Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Select(s => s.ToString()).ToArray());
        }

        public Task CreateIfNotExist<TEnum>(int incrementBy = 1, int startWith = 1, params TEnum[] sequences) where TEnum : Enum
        {
            return CreateIfNotExist(incrementBy, startWith, GetSchemaName<TEnum>(), sequences.Select(s => s.ToString()).ToArray());
        }

        public async Task CreateIfNotExist(int incrementBy = 1, int startWith = 1, string? schemaName = null, params string[] sequenceNames)
        {
            schemaName = schemaName ?? _dbContext.GetDefaultSchemaName();
            foreach (var sequenceName in sequenceNames.Distinct())
            {
                await _dbContext.ExecuteCommandAsync((cmd) =>
                {
                    cmd.CommandText = $@"
                    DO $$
                    BEGIN
                        IF NOT EXISTS (
                            SELECT 1
                            FROM pg_class c
                            JOIN pg_namespace n ON n.oid = c.relnamespace
                            WHERE c.relkind = 'S' -- 'S' stands for sequence
                            AND c.relname = '{sequenceName}'
                            AND n.nspname = '{schemaName}'
                        ) THEN
                            CREATE SEQUENCE ""{schemaName}"".""{sequenceName}""
                            INCREMENT {incrementBy}
                            START {startWith};
                        END IF;
                    END
                    $$;";

                    return cmd.ExecuteNonQueryAsync();
                });
            }
        }


        public Task DropIfExist<TEnum>() where TEnum : Enum
        {
            return DropIfExist(GetSchemaName<TEnum>(), Enum.GetValues(typeof(TEnum)).Cast<TEnum>().Select(s => s.ToString()).ToArray());
        }

        public Task DropIfExist(string? schemaName = null, params string[] sequenceNames)
        {
            schemaName = schemaName ?? _dbContext.GetDefaultSchemaName();
            return _dbContext.ExecuteInTransactionAsync(async () =>
            {
                foreach (var sequenceName in sequenceNames.Distinct())
                {
                    await _dbContext.ExecuteCommandAsync((cmd) =>
                    {
                        cmd.CommandText = $@"DROP SEQUENCE IF EXISTS [{schemaName}].{sequenceName}"; //TODO: perhaps need use CACHE option

                        return cmd.ExecuteNonQueryAsync();
                    });
                }
            });
        }
    }
}
