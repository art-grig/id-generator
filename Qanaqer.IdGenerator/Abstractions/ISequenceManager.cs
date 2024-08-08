using System;
using System.Threading.Tasks;

namespace Qanaqer.IdGenerator.Abstractions
{
    public interface ISequenceManager
    {

        Task CreateIfNotExist<TEnum>(int incrementBy = 1, int startWith = 1) where TEnum : Enum;
        Task CreateIfNotExist<TEnum>(int incrementBy = 1, int startWith = 1, params TEnum[] sequence) where TEnum : Enum;
        Task CreateIfNotExist(int incrementBy = 1, int startWith = 1, string? schemaName = null, params string[] sequenceNames);
        Task DropIfExist<TEnum>() where TEnum : Enum;
        Task DropIfExist(string? schemaName = null, params string[] sequenceNames);
    }
}
