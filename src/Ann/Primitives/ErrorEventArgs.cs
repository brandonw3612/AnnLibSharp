using System;

namespace Ann.Primitives;

public class ErrorEventArgs : EventArgs
{
    public string Message { get; }
    
    public ErrorEventArgs(string message)
    {
        Message = message;
    }
}