using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qanaqer.IdGenerator.Extensions
{
    public static class DbHelperExtensions
    {
        public static void AddParameter(this IDbCommand command, string name, object value)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }

        public static string GetDefaultSchemaName(this DbContext context)
        {
            return context?.Model?.GetDefaultSchema() ?? throw new ArgumentException("Cannot extract defalut schema name from the input");
        }

        public static async Task<T> ExecuteCommandAsync<T>(this DbContext dbContext, Func<DbCommand, Task<T>> commandFunc)
        {
            if (dbContext == null)
            {
                throw new ArgumentNullException(nameof(dbContext));
            }

            var connection = dbContext.Database.GetDbConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using (var cmd = connection.CreateCommand())
            {
                var result = await commandFunc(cmd);
                return result;
            }
        }

        public static async Task ExecuteInTransactionAsync(this DbContext dbContext, Func<Task> operation)
        {
            if (dbContext == null)
                throw new ArgumentNullException(nameof(dbContext));
            if (operation == null)
                throw new ArgumentNullException(nameof(operation));

            using (var transaction = await dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    await operation();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

    }
}
