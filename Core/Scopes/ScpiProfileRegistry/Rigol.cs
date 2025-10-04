using System;

namespace Oscilloscope_Network_Capture.Core.Scopes
{
    public static partial class ScpiProfileRegistry
    {
        static partial void RegisterRigolData()
        {
            // ######################################################################
            // SCPI COMMANDS
            // ######################################################################

            // https://www.batronix.com/files/Rigol/Oszilloskope/_DS&MSO1000Z/MSO_DS1000Z_ProgrammingGuide_EN.pdf
            // https://www.batronix.com/files/Rigol/Oszilloskope/_DS&MSO2000A/MSO2000A_DS2000A_ProgrammingGuide_EN.pdf
            // https://tw.rigol.com/tw/Images/DHO10004000_ProgrammingGuide_EN_tcm17-5395.pdf
            // ---
            AddSeriesProfiles(
                "Rigol",
                p => p
                    .Map(ScopeCommand.Identify, "*IDN?")
                    .Map(ScopeCommand.ClearStatistics, ":CLEAR") // :MEASURE:STATISTIC:RESET
                    .Map(ScopeCommand.QueryActiveTrigger, ":TRIGGER:STATUS?")
                    .Map(ScopeCommand.Stop, ":STOP")
                    .Map(ScopeCommand.Run, ":RUN")
                    .Map(ScopeCommand.Single, ":SINGLE")
                    .Map(ScopeCommand.QueryTriggerMode, ":TRIGGER:MODE?")
                    .Map(ScopeCommand.QueryTriggerLevel, ":TRIGGER:EDGE:LEVEL?")
                    .Map(ScopeCommand.SetTriggerLevel, ":TRIGGER:EDGE:LEVEL {0}")
                    .Map(ScopeCommand.QueryTimeDiv, ":TIMEBASE:SCALE?")
                    .Map(ScopeCommand.SetTimeDiv, ":TIMEBASE:SCALE {0}")
                    .Map(ScopeCommand.DumpImage, ":DISPLAY:DATA?")
                    .Map(ScopeCommand.PopLastSystemError, ":SYSTEM:ERROR?")
                    .Map(ScopeCommand.OperationComplete, "*OPC?"),
                "DHO1000",
                "DHO4000",
                "DS1000Z",
                "DS2000A",
                "MSO1000Z",
                "MSO2000A"
            );

            // ######################################################################
            // TIME/DIV VALUES
            // ######################################################################

            AddTimeDivTokens(
                "Rigol",
                new[]
                {
                    "2nS", "5nS", "10nS", "20nS", "50nS", "100nS", "200nS", "500nS",
                    "1uS", "2uS", "5uS", "10uS", "20uS", "50uS", "100uS", "200uS", "500uS",
                    "1mS", "2mS", "5mS", "10mS", "20mS", "50mS", "100mS", "200mS", "500mS",
                    "1S", "2S", "5S", "10S", "20S", "50S", "100S", "200S", "500S", "1000S"
                },
                "DHO1000",
                "DHO4000",
                "DS1000Z",
                "DS2000A",
                "MSO1000Z",
                "MSO2000A"
            );
        }
    }
}