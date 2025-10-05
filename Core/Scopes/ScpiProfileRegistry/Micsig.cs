using System;

namespace Oscilloscope_Network_Capture.Core.Scopes
{
    public static partial class ScpiProfileRegistry
    {
        static partial void RegisterMicsigData()
        {
            // ######################################################################
            // SCPI COMMANDS
            // ######################################################################

            AddSeriesProfiles(
                "Micsig",
                new[]
                {
                    "ATO",
                    "ETO",
                    "MDO",
                    "MHO3",
                    "SATO",
                    "STO",
                    "TO"
                },
                p => p
                    .Map(ScopeCommand.Identify, "*IDN?")
                    .Map(ScopeCommand.DrainSystemErrorQueue, "")
                    .Map(ScopeCommand.OperationComplete, "")
                    .Map(ScopeCommand.ClearStatistics, ":MEASURE:STATISTIC:RESET")
                    .Map(ScopeCommand.QueryActiveTrigger, ":TRIGGER:STATUS?") 
                    .Map(ScopeCommand.Stop, ":MENU:STOP")
                    .Map(ScopeCommand.Single, ":MENU:SINGlE")
                    .Map(ScopeCommand.Run, ":MENU:RUN")
                    .Map(ScopeCommand.QueryTriggerMode, ":TRIGGER:TYPE?")
                    .Map(ScopeCommand.QueryTriggerLevel, ":TRIGGER:EDGE:LEVEL?")
                    .Map(ScopeCommand.SetTriggerLevel, ":TRIGGER:EDGE:LEVEL {0}")
                    .Map(ScopeCommand.QueryTimeDiv, ":TIMEBASE:ZOOM:SCALE?")
                    .Map(ScopeCommand.SetTimeDiv, ":TIMEBASE:ZOOM:SCAlE {0}")
                    .Map(ScopeCommand.DumpImage, ":WAV:DATA?")
            );

            // ######################################################################
            // TIME/DIV VALUES
            // ######################################################################

            AddTimeDivTokens(
                "Micsig",
                new[]
                {
                    "ATO",
                    "ETO",
                    "MDO",
                    "MHO3",
                    "SATO",
                    "STO",
                    "TO"
                },
                new[]
                {
                    "2nS", "5nS", "10nS", "20nS", "50nS", "100nS", "200nS", "500nS",
                    "1uS", "2uS", "5uS", "10uS", "20uS", "50uS", "100uS", "200uS", "500uS",
                    "1mS", "2mS", "5mS", "10mS", "20mS", "50mS", "100mS", "200mS", "500mS",
                    "1S", "2S", "5S", "10S", "20S", "50S", "100S", "200S", "500S", "1000S"
                }
            );
        }
    }
}