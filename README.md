# Qanaqer.IdGenerator

A high-performance, thread-safe ID generator library for .NET that provides sequential ID generation with support for multiple database providers and in-memory generation.

## 🚀 Features

- **High Performance**: Batch-based ID generation with configurable batch sizes for optimal performance
- **Thread-Safe**: Concurrent access support with proper synchronization mechanisms
- **Multiple Providers**: Support for PostgreSQL, SQL Server, and in-memory generation
- **Type-Safe**: Uses strongly-typed enums to define sequence types
- **Entity Framework Integration**: Seamless integration with Entity Framework Core
- **Schema Support**: Configurable database schemas for sequence management
- **Flexible Configuration**: Customizable sequence parameters (increment, start value, batch size)

## 📦 Installation

### Core Package
```bash
dotnet add package Qanaqer.IdGenerator
```

### Database Providers

#### PostgreSQL Provider
```bash
dotnet add package Qanaqer.IdGenerator.Postgres
```

#### SQL Server Provider
```bash
dotnet add package Qanaqer.IdGenerator.SqlServer
```

## 🚀 Quick Start

### 1. Define Your Sequences

First, create an enum to define your sequence types and decorate it with the `IdGenSequences` attribute:

```csharp
using Qanaqer.IdGenerator;

[IdGenSequences(SchemaName = "public")] // PostgreSQL
public enum MySequences
{
    UserIds,
    OrderIds,
    ProductIds
}

[IdGenSequences(SchemaName = "dbo")] // SQL Server
public enum SqlServerSequences
{
    UserIds,
    OrderIds
}
```

### 2. Configure Services

#### PostgreSQL Setup

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Qanaqer.IdGenerator.Postgres.DependencyInjection;

var services = new ServiceCollection();

// Add your DbContext
services.AddDbContext<MyDbContext>(options =>
    options.UseNpgsql("your-connection-string"));

// Add PostgreSQL ID Generator
services.AddPgIdGenerator<MySequences, MyDbContext>(new IdGeneratorOptions
{
    BatchSize = 1000 // Generate IDs in batches of 1000
});
```

#### SQL Server Setup

```csharp
using Qanaqer.IdGenerator.SqlServer.DependencyInjection;

services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer("your-connection-string"));

services.AddSqlServerIdGenerator<SqlServerSequences, MyDbContext>(new IdGeneratorOptions
{
    BatchSize = 500
});
```

#### In-Memory Setup

```csharp
using Qanaqer.IdGenerator;
using Qanaqer.IdGenerator.Extensions;

services.AddIdGenerator<MySequences, InMemoryIdGenerator<MySequences>, InMemorySequenceManager>(
    new IdGeneratorOptions { BatchSize = 1 });
```

### 3. Initialize Database Sequences

```csharp
// Get the sequence manager and create sequences
var sequenceManager = serviceProvider.GetRequiredService<ISequenceManager>();
await sequenceManager.CreateIfNotExist<MySequences>(
    incrementBy: 1, 
    startWith: 1);
```

### 4. Generate IDs

```csharp
using Qanaqer.IdGenerator.Abstractions;

public class UserService
{
    private readonly IIdGenerator<MySequences> _idGenerator;
    
    public UserService(IIdGenerator<MySequences> idGenerator)
    {
        _idGenerator = idGenerator;
    }
    
    public async Task<User> CreateUserAsync(string name)
    {
        var userId = await _idGenerator.NextId(MySequences.UserIds);
        
        return new User
        {
            Id = userId,
            Name = name
        };
    }
}
```

## 🔧 Configuration

### IdGeneratorOptions

```csharp
public class IdGeneratorOptions
{
    /// <summary>
    /// Number of IDs to fetch in each batch from the database.
    /// Higher values improve performance but use more memory.
    /// Default: 1
    /// </summary>
    public long BatchSize { get; set; } = 1;
}
```

### SequenceConfig

```csharp
public class SequenceConfig
{
    /// <summary>
    /// The increment value for the sequence. Default: 1
    /// </summary>
    public int IncrementBy { get; set; } = 1;
    
    /// <summary>
    /// The starting value for the sequence. Default: 1
    /// </summary>
    public int StartWith { get; set; } = 1;
    
    /// <summary>
    /// Custom schema name for the sequence
    /// </summary>
    public string? SchemaName { get; set; }
}
```

### Schema Configuration

Use the `IdGenSequences` attribute to specify the schema:

```csharp
[IdGenSequences(SchemaName = "inventory")]
public enum InventorySequences
{
    ProductIds,
    CategoryIds
}
```

## 📚 Advanced Usage

### Custom Sequence Parameters

```csharp
// Create sequences with custom parameters
await sequenceManager.CreateIfNotExist<MySequences>(
    incrementBy: 10,  // Increment by 10
    startWith: 1000   // Start from 1000
);

// Create specific sequences only
await sequenceManager.CreateIfNotExist<MySequences>(
    incrementBy: 1,
    startWith: 1,
    MySequences.UserIds,
    MySequences.OrderIds
);
```

### Manual Sequence Management

```csharp
// Create sequences with custom names and schema
await sequenceManager.CreateIfNotExist(
    incrementBy: 1,
    startWith: 1,
    schemaName: "custom_schema",
    "sequence_name_1",
    "sequence_name_2"
);

// Drop sequences
await sequenceManager.DropIfExist<MySequences>();
await sequenceManager.DropIfExist("custom_schema", "sequence_name");
```

### High-Concurrency Scenarios

```csharp
// Example: Generate 10,000 IDs concurrently
var tasks = Enumerable.Range(1, 10000)
    .Select(async _ => await idGenerator.NextId(MySequences.UserIds));

var ids = await Task.WhenAll(tasks);

// All IDs will be unique and sequential
Console.WriteLine($"Generated {ids.Length} unique IDs");
Console.WriteLine($"Range: {ids.Min()} - {ids.Max()}");
```

## 🏗️ Architecture

### Components

- **IIdGenerator<TEnum>**: Core interface for ID generation
- **ISequenceManager**: Interface for managing database sequences
- **IdGeneratorOptions**: Configuration options
- **Provider-Specific Implementations**:
  - `PgIdGenerator`: PostgreSQL implementation
  - `SqlServerIdGenerator`: SQL Server implementation
  - `InMemoryIdGenerator`: In-memory implementation

### How It Works

1. **Batch Processing**: IDs are fetched from the database in configurable batches
2. **Local Caching**: Fetched IDs are cached locally for immediate distribution
3. **Thread Safety**: Concurrent access is handled with semaphores and atomic operations
4. **Automatic Refill**: When local cache runs low, new batches are fetched automatically

## ⚡ Performance Considerations

### Batch Size Optimization

- **Small Applications**: Use smaller batch sizes (10-100) to reduce memory usage
- **High-Throughput Applications**: Use larger batch sizes (1000-10000) for better performance
- **Memory vs Performance**: Larger batches use more memory but reduce database round trips

```csharp
// For high-throughput scenarios
services.AddPgIdGenerator<MySequences, MyDbContext>(new IdGeneratorOptions
{
    BatchSize = 5000 // Fetch 5000 IDs at once
});
```

### Database Considerations

- Sequences are created with appropriate increment values
- Database connections are managed efficiently through dependency injection
- Transactions are used to ensure consistency

## 🧪 Testing

The library includes comprehensive tests demonstrating usage patterns:

```csharp
[Fact]
public async Task IdGenerator_Postgres_Success()
{
    // Setup services and database
    var services = new ServiceCollection();
    services.AddDbContext<TestDbContext>(c =>
        c.UseNpgsql("connection-string"));
    services.AddPgIdGenerator<PostgresSequences, TestDbContext>(
        new IdGeneratorOptions { BatchSize = 1000 });
    
    var serviceProvider = services.BuildServiceProvider();
    
    // Initialize database and sequences
    var dbContext = serviceProvider.GetRequiredService<TestDbContext>();
    await dbContext.Database.MigrateAsync();
    
    var sequenceManager = serviceProvider.GetRequiredService<ISequenceManager>();
    await sequenceManager.CreateIfNotExist<PostgresSequences>();
    
    // Generate IDs concurrently
    var idGenerator = serviceProvider.GetRequiredService<IIdGenerator<PostgresSequences>>();
    var tasks = Enumerable.Range(1, 10000)
        .Select(async _ => await idGenerator.NextId(PostgresSequences.TestSequence));
    
    var ids = await Task.WhenAll(tasks);
    
    // Verify all IDs are unique and sequential
    var sortedIds = ids.OrderBy(x => x).ToArray();
    Assert.Equal(Enumerable.Range(1, 10000), sortedIds);
}
```

## 📋 API Reference

### IIdGenerator<TEnum>

```csharp
public interface IIdGenerator<TEnum> where TEnum : Enum
{
    /// <summary>
    /// Generates the next unique ID for the specified sequence
    /// </summary>
    /// <param name="sequence">The sequence type to generate ID for</param>
    /// <returns>A unique sequential ID</returns>
    Task<long> NextId(TEnum sequence);
}
```

### ISequenceManager

```csharp
public interface ISequenceManager
{
    /// <summary>
    /// Creates sequences for all values in the enum if they don't exist
    /// </summary>
    Task CreateIfNotExist<TEnum>(int incrementBy = 1, int startWith = 1) where TEnum : Enum;
    
    /// <summary>
    /// Creates specific sequences if they don't exist
    /// </summary>
    Task CreateIfNotExist<TEnum>(int incrementBy = 1, int startWith = 1, params TEnum[] sequence) where TEnum : Enum;
    
    /// <summary>
    /// Creates sequences with custom names
    /// </summary>
    Task CreateIfNotExist(int incrementBy = 1, int startWith = 1, string? schemaName = null, params string[] sequenceNames);
    
    /// <summary>
    /// Drops all sequences for the enum if they exist
    /// </summary>
    Task DropIfExist<TEnum>() where TEnum : Enum;
    
    /// <summary>
    /// Drops specific sequences if they exist
    /// </summary>
    Task DropIfExist(string? schemaName = null, params string[] sequenceNames);
}
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🔗 Related Packages

- [Microsoft.EntityFrameworkCore](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/)
- [Npgsql.EntityFrameworkCore.PostgreSQL](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/)
- [Microsoft.EntityFrameworkCore.SqlServer](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer/)
