using Microsoft.KernelMemory;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticMemory.Extensions
{
    public class UserQuestionPipeline
    {
        private readonly List<IQueryHandler> _queryHandlers = new List<IQueryHandler>();

        public UserQuestionPipeline AddHandler(IQueryHandler queryHandler)
        {
            _queryHandlers.Add(queryHandler);
            return this;
        }

        public Task ExecuteQuery(UserQuestion userQuestion)
        {
            return ExecuteQuery(userQuestion, CancellationToken.None);
        }

        public async Task ExecuteQuery(UserQuestion userQuestion, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userQuestion.Question))
            {
                return;
            }
            foreach (var handler in _queryHandlers)
            {
                await handler.HandleAsync(userQuestion, cancellationToken);
                if (userQuestion.Answered)
                {
                    //We break the pipeline if the question has been answered
                    userQuestion.AnswerHandler = handler.Name;
                    break;
                }
            }

            //if the question was answered we need to group citations
            //TODO: change the internal data instead of using citations?
            if (userQuestion.Answered)
            {
                userQuestion.Citations = userQuestion
                    .Citations
                    .GroupBy(c => c.Link)
                    .Select(g =>
                    {
                        var firstCitation = g.First();
                        //Accumulate all other segments
                        firstCitation.Partitions = g.Select(c => c.Partitions.Single()).ToList();
                        return firstCitation;
                    }).ToList();
            }
        }
    }
}
