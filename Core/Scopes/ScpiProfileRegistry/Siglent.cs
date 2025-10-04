using System;

namespace Oscilloscope_Network_Capture.Core.Scopes
{
    public static partial class ScpiProfileRegistry
    {
        static partial void RegisterSiglentData()
        {
            // ######################################################################
            // SCPI COMMANDS
            // ######################################################################

            // SDS1000X-E Series
            //      SDS1104X-E
            // ---------------
            // Documented from:
            //      https://siglentna.com/wp-content/uploads/dlm_uploads/2017/10/ProgrammingGuide_forSDS-1-1.pdf
            // Backup:
            //      https://www.siglenteu.com/resources/documents/digital-oscilloscopes/
            //      https://www.siglenteu.com/download/9103/?tmstv=1759564212
            // ---
            AddSeriesProfiles(
                "Siglent",
                new[]
                {
                    "SDS1000DL+",
                    "SDS1000CML+",
                    "SDS1000CNL+",
                    "SDS1000X",
                    "SDS1000X+",
                    "SDS1000X-E",
                    "SDS2000X"
                },
                p => p
                    .Map(ScopeCommand.Identify, "*IDN?")
                    .Map(ScopeCommand.PopLastSystemError, ":SYST:ERR?")
                    .Map(ScopeCommand.OperationComplete, "*OPC?")
                    .Map(ScopeCommand.ClearStatistics, "*CLS")
                    .Map(ScopeCommand.QueryActiveTrigger, "TRIG_MODE?")
                    .Map(ScopeCommand.Stop, "STOP")
                    .Map(ScopeCommand.Single, "TRIG_MODE SINGLE")
                    .Map(ScopeCommand.Run, "TRIG_MODE AUTO")
                    .Map(ScopeCommand.QueryTriggerMode, "TRIG_SELECT?")
                    .Map(ScopeCommand.QueryTriggerLevel, "TRIG_LEVEL?")
                    .Map(ScopeCommand.SetTriggerLevel, "TRIG_LEVEL {0}")
                    .Map(ScopeCommand.QueryTimeDiv, "TIME_DIV?")
                    .Map(ScopeCommand.SetTimeDiv, "TIME_DIV {0}")
                    .Map(ScopeCommand.DumpImage, "SCREEN_DUMP")
            );

            // SDS3000X HD Series
            //      SDS3104X HD
            // --------------------
            // https://www.siglenteu.com/resources/documents/digital-oscilloscopes/
            // https://www.siglenteu.com/wp-content/uploads/dlm_uploads/2024/02/SDS3000X-HD_ProgrammingGuide_EN11F.pdf
            // ---
            AddSeriesProfiles(
                "Siglent",
                new[]
                {
                    "SDS800X HD",
                    "SDS1000X HD",
                    "SDS2000X HD",
                    "SDS2000X Plus",
                    "SDS3000X HD",
                    "SDS5000X",
                    "SDS6000 Pro",
                    "SDS6000A",
                    "SDS6000L",
                    "SDS7000A",
                    "SHS800X",
                    "SHS1000X"
                },
                p => p
                    .Map(ScopeCommand.Identify, "*IDN?")
                    .Map(ScopeCommand.PopLastSystemError, ":SYST:ERR?")
                    .Map(ScopeCommand.OperationComplete, "*OPC?")
                    .Map(ScopeCommand.ClearStatistics, "*CLS")
                    .Map(ScopeCommand.QueryActiveTrigger, "TRIG_MODE?")
                    .Map(ScopeCommand.Stop, "STOP")
                    .Map(ScopeCommand.Single, "TRIG_MODE SINGLE")
                    .Map(ScopeCommand.Run, "TRIG_MODE AUTO")
                    .Map(ScopeCommand.QueryTriggerMode, "TRIG_SELECT?")
                    .Map(ScopeCommand.QueryTriggerLevel, ":TRIGGER:EDGE:LEVEL?")
                    .Map(ScopeCommand.SetTriggerLevel, ":TRIGGER:EDGE:LEVEL {0}")
                    .Map(ScopeCommand.QueryTimeDiv, "TIME_DIV?")
                    .Map(ScopeCommand.SetTimeDiv, "TIME_DIV {0}")
                    .Map(ScopeCommand.DumpImage, ":PRINT? PNG")
            );

            // ######################################################################
            // TIME/DIV VALUES
            // ######################################################################

            AddTimeDivTokens(
                "Siglent",
                new[]
                {
                    "SDS800X HD",
                    "SDS1000CML+",
                    "SDS1000CNL+",
                    "SDS1000DL+",
                    "SDS1000X",
                    "SDS1000X+",
                    "SDS1000X-E",
                    "SDS1000X HD",
                    "SDS2000X",
                    "SDS2000X HD",
                    "SDS2000X Plus",
                    "SDS3000X HD",
                    "SDS5000X",
                    "SDS6000 Pro",
                    "SDS6000A",
                    "SDS6000L",
                    "SDS7000A",
                    "SHS800X",
                    "SHS1000X"
                },
                new[]
                {
                    "1nS", "2nS", "5nS", "10nS", "20nS", "50nS", "100nS", "200nS", "500nS",
                    "1uS", "2uS", "5uS", "10uS", "20uS", "50uS", "100uS", "200uS", "500uS",
                    "1mS", "2mS", "5mS", "10mS", "20mS", "50mS", "100mS", "200mS", "500mS",
                    "1S", "2S", "5S", "10S", "20S", "50S"
                }
            );
        }
    }
}