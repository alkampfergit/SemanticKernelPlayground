using Microsoft.ML.Tokenizers;

namespace SemanticKernel.Orchestration.Orchestrators;

public class ModelInformation
{
    private readonly string _value;
    private readonly TiktokenTokenizer _tokenizer;

    private ModelInformation(string value)
    {
        _value = value;
        _tokenizer = TiktokenTokenizer.CreateForModel(value);
    }

    public TiktokenTokenizer Tokenizer => _tokenizer;

    public override string ToString() => _value;

    // Chat models
    public static readonly ModelInformation GPT4O = new("gpt-4o");
    public static readonly ModelInformation O1 = new("o1");
    public static readonly ModelInformation GPT4 = new("gpt-4");
    public static readonly ModelInformation GPT35Turbo = new("gpt-3.5-turbo");
    public static readonly ModelInformation GPT35Turbo16k = new("gpt-3.5-turbo-16k");
    public static readonly ModelInformation GPT35 = new("gpt-35");
    public static readonly ModelInformation GPT35TurboAzure = new("gpt-35-turbo");
    public static readonly ModelInformation GPT35Turbo16kAzure = new("gpt-35-turbo-16k");

    // Text models
    public static readonly ModelInformation TextDavinci003 = new("text-davinci-003");
    public static readonly ModelInformation TextDavinci002 = new("text-davinci-002");
    public static readonly ModelInformation TextDavinci001 = new("text-davinci-001");
    public static readonly ModelInformation TextCurie001 = new("text-curie-001");
    public static readonly ModelInformation TextBabbage001 = new("text-babbage-001");
    public static readonly ModelInformation TextAda001 = new("text-ada-001");
    public static readonly ModelInformation Davinci = new("davinci");
    public static readonly ModelInformation Curie = new("curie");
    public static readonly ModelInformation Babbage = new("babbage");
    public static readonly ModelInformation Ada = new("ada");

    // Code models
    public static readonly ModelInformation CodeDavinci002 = new("code-davinci-002");
    public static readonly ModelInformation CodeDavinci001 = new("code-davinci-001");
    public static readonly ModelInformation CodeCushman002 = new("code-cushman-002");
    public static readonly ModelInformation CodeCushman001 = new("code-cushman-001");
    public static readonly ModelInformation DavinciCodex = new("davinci-codex");
    public static readonly ModelInformation CushmanCodex = new("cushman-codex");

    // Edit models
    public static readonly ModelInformation TextDavinciEdit001 = new("text-davinci-edit-001");
    public static readonly ModelInformation CodeDavinciEdit001 = new("code-davinci-edit-001");

    // Embedding models
    public static readonly ModelInformation TextEmbeddingAda002 = new("text-embedding-ada-002");
    public static readonly ModelInformation TextEmbedding3Small = new("text-embedding-3-small");
    public static readonly ModelInformation TextEmbedding3Large = new("text-embedding-3-large");

    // Old embedding models
    public static readonly ModelInformation TextSimilarityDavinci001 = new("text-similarity-davinci-001");
    public static readonly ModelInformation TextSimilarityCurie001 = new("text-similarity-curie-001");
    public static readonly ModelInformation TextSimilarityBabbage001 = new("text-similarity-babbage-001");
    public static readonly ModelInformation TextSimilarityAda001 = new("text-similarity-ada-001");
    public static readonly ModelInformation TextSearchDavinciDoc001 = new("text-search-davinci-doc-001");
    public static readonly ModelInformation TextSearchCurieDoc001 = new("text-search-curie-doc-001");
    public static readonly ModelInformation TextSearchBabbageDoc001 = new("text-search-babbage-doc-001");
    public static readonly ModelInformation TextSearchAdaDoc001 = new("text-search-ada-doc-001");
    public static readonly ModelInformation CodeSearchBabbageCode001 = new("code-search-babbage-code-001");
    public static readonly ModelInformation CodeSearchAdaCode001 = new("code-search-ada-code-001");

    // Open source models
    public static readonly ModelInformation GPT2 = new("gpt2");
}
