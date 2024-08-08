using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qanaqer.IdGenerator.Postgres;
using Qanaqer.IdGenerator.SqlServer;
using System.Collections.Concurrent;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Qanaqer.IdGenerator.Tests
{
    public class IdGeneratorTests
    {
        private readonly ITestOutputHelper _output;
        public IdGeneratorTests(ITestOutputHelper output) 
        {
            _output = output;
        }
        
        [IdGenSequences(SchemaName = "public")]
        public enum PostgresSequences
        { 
            TestSequence,
        }

        [IdGenSequences(SchemaName = "dbo")]
        public enum MsSqlSequences
        {
            TestSequence,
        }

        [Fact]
        public async Task IdGenerator_MsSql_Succes()
        {
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(c =>
                c.UseSqlServer("Server=localhost;Database={0};user id=sa;password=Password123!;Encrypt=False;"));
            services.AddSingleton<IIdGenerator<MsSqlSequences>, SqlServerIdGenerator<TestDbContext, MsSqlSequences>>();
            services.AddScoped<ISequenceManager, SqlServerSequenceManager<TestDbContext>>();
            services.Configure<IdGeneratorOptions>(o =>
            {
                o.BatchSize = 10000;
            });
            var sp = services.BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();

            var sequenceManager = sp.GetRequiredService<ISequenceManager>();
            await sequenceManager.CreateIfNotExist<MsSqlSequences>();
            var idGenerator = sp.GetRequiredService<IIdGenerator<MsSqlSequences>>();
            var bug = new ConcurrentBag<long>();
            async Task AddId()
            {
                bug.Add(await idGenerator.NextId(MsSqlSequences.TestSequence));
            }
            var tasks = Enumerable.Range(1, 10000).Select(i => AddId());

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            await Task.WhenAll(tasks);
            stopWatch.Stop();
            _output.WriteLine("Elapsed: {0}", stopWatch.ElapsedMilliseconds);

            Enumerable.SequenceEqual(bug.Select(i => (int)i).OrderBy(i => i), Enumerable.Range(1, 10000)).Should().BeTrue();
        }

        [Fact]
        public async Task IdGenerator_Postgres_Succes()
        {
            var services = new ServiceCollection();
            services.AddDbContext<TestDbContext>(c =>
                c.UseNpgsql("Host=localhost;Port=5432;Database=test;Username=postgres;Password=postgres;"));
            services.AddSingleton<IIdGenerator<PostgresSequences>, PgIdGenerator<TestDbContext, PostgresSequences>>();
            services.AddScoped<ISequenceManager, PgSequenceManager<TestDbContext>>();
            services.Configure<IdGeneratorOptions>(o =>
            {
                o.BatchSize = 1000;
            });
            var sp = services.BuildServiceProvider();

            var dbContext = sp.GetRequiredService<TestDbContext>();
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();

            var sequenceManager = sp.GetRequiredService<ISequenceManager>();
            await sequenceManager.CreateIfNotExist<PostgresSequences>();
            var idGenerator = sp.GetRequiredService<IIdGenerator<PostgresSequences>>();
            var bug = new ConcurrentBag<long>();
            async Task AddId() {
                bug.Add(await idGenerator.NextId(PostgresSequences.TestSequence));
            }
            var tasks = Enumerable.Range(1, 10000).Select(i => AddId());

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            await Task.WhenAll(tasks);
            stopWatch.Stop();
            _output.WriteLine("Elapsed: {0}", stopWatch.ElapsedMilliseconds);

            Enumerable.SequenceEqual(bug.Select(i => (int)i).OrderBy(i => i), 
                Enumerable.Range(1, 10000)).Should().BeTrue();
        }
    }
}