using System;
using System.Threading.Tasks;

namespace Qanaqer.IdGenerator
{
    public interface IIdGenerator<TEnum> where TEnum : Enum
    {
        Task<long> NextId(TEnum sequence);
    }
}
