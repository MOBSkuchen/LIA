namespace LIA;

public class CompilationOptions
{
    // Disable Warnings
    public bool DisableWarningMainNotDefined;
    public bool DisableWarningUselessCode;
    public bool DisableWarningUnreachableCode;
    // Trim
    public bool TrimUnreachableCode;
    public bool TrimUselessCode;

    public CompilationOptions()
    {
        SetAllWarnings(false);
        SetAllTrim(false);
    }

    public void SetAllWarnings(bool state)
    {
        DisableWarningMainNotDefined = state;
        DisableWarningUselessCode = state;
        DisableWarningUnreachableCode = state;
    }

    public void SetAllTrim(bool state)
    {
        TrimUnreachableCode = state;
        TrimUselessCode = state;
    }
}