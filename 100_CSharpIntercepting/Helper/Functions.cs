using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LogIntercepting.Helper;

public class OpenAiFunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("parameters")]
    public OpenAiFunctionParameters Parameters { get; set; }
}

public class OpenAiFunctionParameters
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, OpenAiFunctionPropertyBase> Properties { get; set; }

    [JsonPropertyName("required")]
    public string[] Required { get; set; }
}

[JsonDerivedType(typeof(OpenAiFunctionArrayProperty))]
[JsonDerivedType(typeof(OpenAiFunctionObjectProperty))]
[JsonDerivedType(typeof(OpenAiFunctionProperty))]
public class OpenAiFunctionPropertyBase
{
    [JsonPropertyName("type")]
    public string Type { get; set; }
}

public class OpenAiFunctionArrayProperty : OpenAiFunctionPropertyBase
{
    public OpenAiFunctionArrayProperty()
    {
        Type = "array";
    }

    [JsonPropertyName("items")]
    public OpenAiFunctionPropertyBase Items { get; set; }
}

public class OpenAiFunctionObjectProperty : OpenAiFunctionPropertyBase
{
    public OpenAiFunctionObjectProperty()
    {
        Type = "object";
    }

    [JsonPropertyName("properties")]
    public Dictionary<string, OpenAiFunctionPropertyBase> Properties { get; set; }

    [JsonPropertyName("required")]
    public string[] Required { get; set; }
}

public class OpenAiFunctionProperty : OpenAiFunctionPropertyBase
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("description")]
    public string Description { get; set; }
}