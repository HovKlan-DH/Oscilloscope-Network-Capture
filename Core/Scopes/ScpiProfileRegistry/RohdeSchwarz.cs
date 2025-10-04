using System;

namespace Oscilloscope_Network_Capture.Core.Scopes
{
    public static partial class ScpiProfileRegistry
    {
        static partial void RegisterRohdeSchwarzData()
        {
            // ######################################################################
            // SCPI COMMANDS
            // ######################################################################

            // MXO 4
            //      MXO44
            // ---
            // https://www.rohde-schwarz.com/webhelp/MXO4_HTML_UserManual_en/Content/6c816e488c7b4546.htm
            // https://www.rohde-schwarz.com/cz/driver-pages/remote-control/instrument-error-checking_231244.html
            // ---
            AddSeriesProfiles(
                "Rohde & Schwarz",
                p => p
                    .Map(ScopeCommand.Identify, "*IDN?")
                    .Map(ScopeCommand.ClearStatistics, "MEASUREMENT:STATISTICS:RESET")
                    .Map(ScopeCommand.QueryActiveTrigger, ":TRIGGER:STATUS?")
                    .Map(ScopeCommand.Stop, ":STOP")
                    .Map(ScopeCommand.Run, ":RUN")
                    .Map(ScopeCommand.Single, ":SINGLE")
                    .Map(ScopeCommand.QueryTriggerMode, "TRIGGER:MODE?") // AUTO,NORMal,FREerun
                    .Map(ScopeCommand.QueryTriggerLevel, "TRIGGER:LEVEL?")
                    .Map(ScopeCommand.SetTriggerLevel, "TRIGGER:LEVEL {0}")
                    .Map(ScopeCommand.QueryTimeDiv, "TIMEBASE:SCALE?")
                    .Map(ScopeCommand.SetTimeDiv, "TIMEBASE:SCALE {0}")
                    .Map(ScopeCommand.DumpImage, "HCOPY:DATA?")
                    .Map(ScopeCommand.PopLastSystemError, "SYSTEM:ERROR?")
                    .Map(ScopeCommand.OperationComplete, "*OPC?"),
                "MXO 4"
            );

            // Rohde & Schwarz
            // ---------------
            // RTA4000 Series
            //      RTA4004
            // ---
            AddSeriesProfiles(
                "Rohde & Schwarz",
                p => p
                    .Map(ScopeCommand.Identify, "*IDN?")
                    .Map(ScopeCommand.ClearStatistics, "MEASUREMENT:STATISTICS:RESET")
                    .Map(ScopeCommand.QueryActiveTrigger, ":ACQUIRE:STATE?")
                    .Map(ScopeCommand.Stop, "STOP")
                    .Map(ScopeCommand.Run, "RUN")
                    .Map(ScopeCommand.Single, "SINGLE")
                    .Map(ScopeCommand.QueryTriggerMode, "TRIGGER:A:MODE?")
                    .Map(ScopeCommand.QueryTriggerLevel, "TRIGGER:A:LEVEL?")
                    .Map(ScopeCommand.SetTriggerLevel, "TRIGGER:A:LEVEL {0}")
                    .Map(ScopeCommand.QueryTimeDiv, "TIMEBASE:SCALE?")
                    .Map(ScopeCommand.SetTimeDiv, "TIMEBASE:SCALE {0}")
                    .Map(ScopeCommand.DumpImage, "HCOPY:DATA?")
                    .Map(ScopeCommand.PopLastSystemError, "SYSTEM:ERROR?")
                    .Map(ScopeCommand.OperationComplete, "*OPC?"),
                "RTA4000"
            );

            // ######################################################################
            // TIME/DIV VALUES
            // ######################################################################
            AddTimeDivTokens(
                "Rohde & Schwarz",
                new[]
                {
                    "2nS", "5nS", "10nS", "20nS", "50nS", "100nS", "200nS", "500nS",
                    "1uS", "2uS", "5uS", "10uS", "20uS", "50uS", "100uS", "200uS", "500uS",
                    "1mS", "2mS", "5mS", "10mS", "20mS", "50mS", "100mS", "200mS", "500mS",
                    "1S", "2S", "5S", "10S", "20S", "50S", "100S", "200S", "500S", "1000S"
                },
                "MXO 4",
                "RTA4000"
            );
        }
    }
}