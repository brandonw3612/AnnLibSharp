using System;
using Ann.Enums;

namespace Ann.Utilities;

public class ExceptionHandler
{
    private static ExceptionHandler? _instance;
    public static ExceptionHandler Instance => _instance ??= new();

    private ExceptionHandler()
    {
        // Do nothing.
    }

    public void LogError(
        string message,
        ErrorLevel errorLevel
    )
    {
        if (errorLevel is ErrorLevel.Abort)
        {
            ErrorOccurred?.Invoke(this, new(message));
        }
        else
        {
            WarningThrown?.Invoke(this, new(message));
        }
    }

    public event EventHandler<Primitives.ErrorEventArgs>? ErrorOccurred;
    public event EventHandler<Primitives.ErrorEventArgs>? WarningThrown;
}