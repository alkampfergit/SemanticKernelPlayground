using Microsoft.KernelMemory;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SemanticMemory.Extensions
{
    public class CohereReRanker : IReRanker
    {
        public Task<IReadOnlyCollection<Citation>> ReRankAsync(IReadOnlyDictionary<string, IReadOnlyCollection<Citation>> citations)
        {
            throw new NotImplementedException();
        }
    }
}
