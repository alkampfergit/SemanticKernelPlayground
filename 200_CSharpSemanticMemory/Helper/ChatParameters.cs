using System;

namespace SemanticMemory.Helper;

public class Parameters
{
    private int _maxResponse;
    private double _temperature;
    private double _topP;

    public int MaxResponse
    {
        get => _maxResponse;
        set => _maxResponse = Math.Clamp(value, 1, 8000);
    }

    public double Temperature
    {
        get => _temperature;
        set => _temperature = Math.Clamp(value, 0, 1);
    }

    public double TopP
    {
        get => _topP;
        set => _topP = Math.Clamp(value, 0, 1);
    }
}
