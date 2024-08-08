using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Qanaqer.IdGenerator
{
    public class InMemoryIdGenerator<TEnum> : IIdGenerator<TEnum> where TEnum : Enum
    {
        private readonly Dictionary<TEnum, StrongBox<long>> _idMap;

        public InMemoryIdGenerator()
        {
            _idMap = new Dictionary<TEnum, StrongBox<long>>();

            var seqs = Enum.GetValues(typeof(TEnum)).Cast<TEnum>();

            foreach (var seq in seqs)
            {
                _idMap.Add(seq, new StrongBox<long>(0));
            }
        }

        public Task<long> NextId(TEnum sequence)
        {
            return Task.FromResult(Interlocked.Increment(ref _idMap[sequence].Value));
        }
    }
}
