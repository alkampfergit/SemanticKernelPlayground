namespace LogIntercepting.Helper;

public static class JsonHelper
{
    public static string Beautify(string json)
    {
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<object>(json);
        return System.Text.Json.JsonSerializer.Serialize(deserialized, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
    }
}