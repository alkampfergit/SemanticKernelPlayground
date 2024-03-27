using Microsoft.KernelMemory;
using Moq;
using SemanticMemory.Extensions;

namespace KernelMemorySamples.Tests
{
    public class UserQuestionPipelineTests
    {
        private const string AnswerHandlerValue = "AnswerHandler";

        [Fact]
        public async Task Null_query_has_no_answer()
        {
            var sut = GenerateSut();

            Mock<IQueryHandler> mockDependency = GenerateQueryExpanderMock("expanded question");
            Mock<IQueryHandler> answerMock = GenerateQueryAnswerMock("ANSWER");
            sut.AddHandler(mockDependency.Object);
            sut.AddHandler(answerMock.Object);

            var userQuestion = new UserQuestion(GenerateOptions(), "");
            await sut.ExecuteQuery(userQuestion);

            //now I need to assert that extended question was added
            Assert.False(userQuestion.Answered);
        }

        [Fact]
        public async Task Verify_basic_pipeline_generation()
        {
            var sut = GenerateSut();

            Mock<IQueryHandler> mockDependency = GenerateQueryExpanderMock("expanded question");
            sut.AddHandler(mockDependency.Object);

            var userQuestion = new UserQuestion(GenerateOptions(), "test question");
            await sut.ExecuteQuery(userQuestion);

            //now I need to assert that extended question was added
            Assert.Single(userQuestion.ExpandedQuestions);
            Assert.Equal("expanded question", userQuestion.ExpandedQuestions.First().Text);
        }

        [Fact]
        public async Task Verify_capability_of_execution_handler_to_answer()
        {
            var sut = GenerateSut();

            Mock<IQueryHandler> mockDependency = GenerateQueryExpanderMock("expanded question");
            Mock<IQueryHandler> answerMock = GenerateQueryAnswerMock("ANSWER");
            sut.AddHandler(mockDependency.Object);
            sut.AddHandler(answerMock.Object);

            var userQuestion = new UserQuestion(GenerateOptions(), "test question");
            await sut.ExecuteQuery(userQuestion);

            //now I need to assert that extended question was added
            Assert.Equal("ANSWER", userQuestion.Answer);
        }

        [Fact]
        public async Task When_an_handler_terminate_subsequent_handlers_are_not_called()
        {
            var sut = GenerateSut();

            Mock<IQueryHandler> mockDependency1 = GenerateQueryExpanderMock("expanded question1");
            Mock<IQueryHandler> mockDependency2 = GenerateQueryExpanderMock("expanded question2");
            Mock<IQueryHandler> answerMock = GenerateQueryAnswerMock("ANSWER");

            Assert.Equal("AnswerHandler", answerMock.Object.Name);

            sut.AddHandler(mockDependency1.Object);
            sut.AddHandler(answerMock.Object);
            sut.AddHandler(mockDependency2.Object);

            var userQuestion = new UserQuestion(GenerateOptions(), "test question");
            await sut.ExecuteQuery(userQuestion);

            //now I need to assert that extended question was added
            Assert.Equal("ANSWER", userQuestion.Answer);
            Assert.Single(userQuestion.ExpandedQuestions);
            Assert.Equal("expanded question1", userQuestion.ExpandedQuestions[0].Text);

            Assert.Equal(AnswerHandlerValue, userQuestion.AnswerHandler);
        }

        [Fact]
        public async Task When_no_handler_answered_the_question_the_answer_is_null()
        {
            var sut = GenerateSut();

            Mock<IQueryHandler> mockDependency1 = GenerateQueryExpanderMock("expanded question1");
            Mock<IQueryHandler> mockDependency2 = GenerateQueryExpanderMock("expanded question2");

            sut.AddHandler(mockDependency1.Object);
            sut.AddHandler(mockDependency2.Object);

            var userQuestion = new UserQuestion(GenerateOptions(), "test question");
            await sut.ExecuteQuery(userQuestion);

            //now I need to assert that extended question was added
            Assert.Null(userQuestion.Answer);
            Assert.False(userQuestion.Answered);
        }

        [Fact]
        public async Task Ability_to_extract_memory_segments_with_handlers()
        {
            var sut = GenerateSut();

            Mock<IQueryHandler> mockDependency1 = GenerateQueryExpanderMock("expanded question1");
            Mock<IQueryHandler> mockDependency2 = GenerateRetrievalMock("pieceoftext", "Document_1", "fileId");

            sut.AddHandler(mockDependency1.Object);
            sut.AddHandler(mockDependency2.Object);

            var userQuestion = new UserQuestion(GenerateOptions(), "test question");
            await sut.ExecuteQuery(userQuestion);

            Assert.Null(userQuestion.Answer);
            //Verify that we have extracted memories
            Assert.Single(userQuestion.Citations);
            Assert.Equal("pieceoftext", userQuestion.Citations[0].Partitions.Single().Text);
            Assert.Equal("Document_1", userQuestion.Citations[0].DocumentId);
            Assert.Equal("fileId", userQuestion.Citations[0].FileId);
        }

        [Fact]
        public async Task Standard_search_regroup_citations()
        {
            var sut = GenerateSut();

            var citations = new List<Citation>();
            citations.Add(new Citation()
            {
                DocumentId = "Document_1",
                FileId = "fileId",
                Link = "lin1",
                Partitions = new List<Citation.Partition>()
                {
                    new Citation.Partition()
                    {
                        Text = "pieceoftextaa"
                    }
                }
            });
            citations.Add(new Citation()
            {
                DocumentId = "Document_2",
                FileId = "fileId",
                Link = "lin2",
                Partitions = new List<Citation.Partition>()
                {
                    new Citation.Partition()
                    {
                        Text = "pieceoftext3"
                    }
                }
            });
            citations.Add(new Citation()
            {
                DocumentId = "Document_1",
                FileId = "fileId",
                Link = "lin1",
                Partitions = new List<Citation.Partition>()
                {
                    new Citation.Partition()
                    {
                        Text = "pieceoftext of the same file"
                    }
                }
            });
           
            Mock<IQueryHandler> citationMock = GenerateCitationsMock(citations);
            Mock<IQueryHandler> answerMock = GenerateQueryAnswerMock("answered");

            sut.AddHandler(citationMock.Object);
            sut.AddHandler(answerMock.Object);

            var userQuestion = new UserQuestion(GenerateOptions(), "test question");
            await sut.ExecuteQuery(userQuestion);

            Assert.True(userQuestion.Answered);
            
            //now we need to verify the citations,
            Assert.Equal(2, userQuestion.Citations.Count);
            var firstCitation1 = userQuestion.Citations.Single(c => c.Link == "lin1");
            var firstCitation2 = userQuestion.Citations.Single(c => c.Link == "lin2");

            Assert.Equal(2, firstCitation1.Partitions.Count);
            Assert.Single(firstCitation2.Partitions);

            Assert.Equal("pieceoftextaa", firstCitation1.Partitions[0].Text);
            Assert.Equal("pieceoftext of the same file", firstCitation1.Partitions[1].Text);
            Assert.Equal("pieceoftext3", firstCitation2.Partitions[0].Text);
        }

        private UserQueryOptions GenerateOptions()
        {
            return new UserQueryOptions("index");
        }

        private static Mock<IQueryHandler> GenerateQueryExpanderMock(params string[] expandedQueryTests)
        {
            //now I need to mock the HandleAsync method modifying the user question adding extra questions
            var mockDependency = new Mock<IQueryHandler>();
            mockDependency.Setup(x => x.HandleAsync(It.IsAny<UserQuestion>(), It.IsAny<CancellationToken>()))
                .Callback<UserQuestion, CancellationToken>((x, _) => x.ExpandedQuestions.AddRange(expandedQueryTests.Select(q => new Question(q))));
            return mockDependency;
        }

        private static Mock<IQueryHandler> GenerateRetrievalMock(string memoryText, string documetnId, string fileId)
        {
            //now I need to mock the HandleAsync method modifying the user question adding extra questions
            var mockDependency = new Mock<IQueryHandler>();
            mockDependency.Setup(x => x.HandleAsync(It.IsAny<UserQuestion>(), It.IsAny<CancellationToken>()))
                .Callback<UserQuestion, CancellationToken>((x, _) =>
                {
                    var citation = new Citation()
                    {
                        DocumentId = documetnId,
                        FileId = fileId,
                    };
                    citation.Partitions = new List<Citation.Partition>()
                    {
                        new Citation.Partition()
                        {
                            Text = memoryText
                        }
                    };

                    x.Citations.Add(citation);
                });

            //setup return value for the Name property
            mockDependency.Setup(x => x.Name).Returns(AnswerHandlerValue);

            return mockDependency;
        }

        private static Mock<IQueryHandler> GenerateQueryAnswerMock(string answer)
        {
            //now I need to mock the HandleAsync method modifying the user question adding extra questions
            var mockDependency = new Mock<IQueryHandler>();
            mockDependency.Setup(x => x.HandleAsync(It.IsAny<UserQuestion>(), It.IsAny<CancellationToken>()))
                .Callback<UserQuestion, CancellationToken>((x, _) => x.Answer = answer);

            //setup return value for the Name property
            mockDependency.Setup(x => x.Name).Returns(AnswerHandlerValue);

            return mockDependency;
        }

        private static Mock<IQueryHandler> GenerateCitationsMock(IEnumerable<Citation> citations)
        {
            //now I need to mock the HandleAsync method modifying the user question adding extra questions
            var mockDependency = new Mock<IQueryHandler>();
            mockDependency.Setup(x => x.HandleAsync(It.IsAny<UserQuestion>(), It.IsAny<CancellationToken>()))
                .Callback<UserQuestion, CancellationToken>((x, _) => x.Citations.AddRange(citations));

            //setup return value for the Name property
            mockDependency.Setup(x => x.Name).Returns(AnswerHandlerValue);

            return mockDependency;
        }

        private static UserQuestionPipeline GenerateSut()
        {
            return new UserQuestionPipeline();
        }
    }
}