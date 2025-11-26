namespace Oscilloscope_Network_Capture.Core.Scopes
{
    public enum ScopeCommand
    {
        Identify,
        DrainSystemErrorQueue,
        OperationComplete,
        ClearStatistics,
        QueryActiveTrigger,
        Stop,
        Single,
        Run,
        QueryTriggerMode,
        QueryTriggerLevel,
        SetTriggerLevel,
        QueryTimeDiv,
        SetTimeDiv,
        QueryVoltsDiv,
        SetVoltsDiv,
        DumpImage
    }
}
