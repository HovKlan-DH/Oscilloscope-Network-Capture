namespace Oscilloscope_Network_Capture.Core.Scopes
{
    public enum ScopeCommand
    {
        Identify,
        ClearStatistics,
        QueryActiveTrigger,
        Stop,
        Run,
        Single,
        QueryTriggerMode,
        QueryTriggerLevel,
        SetTriggerLevel,
        QueryTimeDiv,
        SetTimeDiv,
        DumpImage,
        PopLastSystemError,
        OperationComplete
    }
}
