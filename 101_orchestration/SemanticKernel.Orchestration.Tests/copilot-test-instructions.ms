If you need to mock a Kernel object you will use this synytax that create a builder and then add it the standard helper of the KernelStore

var builder = Kernel.CreateBuilder();
var mocks = builder.Services.AddMockedLLM("gpt4o");

var kernelStore = new Orchestrators.KernelStore();
kernelStore.AddKernel("gpt4o", builder, ModelInformation.GPT4O);
kernelStore.AddKernel("gpt4mini", builder, ModelInformation.GPT4O);

Then if you need to mock a chat response you will use instruction

mocks.ChatCompletionMock.SetMockResponse("Hello, I'm here to help!");