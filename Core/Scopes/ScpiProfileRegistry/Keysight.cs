using System;

namespace Oscilloscope_Network_Capture.Core.Scopes
{
    public static partial class ScpiProfileRegistry
    {
        static partial void RegisterKeysightData()
        {
            // ######################################################################
            // SCPI COMMANDS
            // ######################################################################

            // InfiniiVision 2000 X-Series
            //      AGILENT TECHNOLOGIES DSO-X 2004A
            // ---
            // https://www.keysight.com/us/en/assets/9018-06893/programming-guides/9018-06893.pdf
            // ---
            AddSeriesProfiles(
                "Keysight",
                p => p
                    .Map(ScopeCommand.Identify, "*IDN?")
                    .Map(ScopeCommand.ClearStatistics, "*CLS")
                    .Map(ScopeCommand.QueryActiveTrigger, ":TRIGGER:STATUS?")
                    .Map(ScopeCommand.Stop, ":STOP")
                    .Map(ScopeCommand.Run, ":RUN")
                    .Map(ScopeCommand.Single, ":SINGLE")
                    .Map(ScopeCommand.QueryTriggerMode, ":TRIGGER:MODE?") // EDGE,GLIT,PATT,TV,DEL,EBUR,OR,RUNT,SHOL,TRAN,SBUS1,USB
                    .Map(ScopeCommand.QueryTriggerLevel, ":TRIGGER:LEVEL?")
                    .Map(ScopeCommand.SetTriggerLevel, ":TRIGGER:LEVEL {0}")
                    .Map(ScopeCommand.QueryTimeDiv, ":TIMEBASE:SCALE?")
                    .Map(ScopeCommand.SetTimeDiv, ":TIMEBASE:SCALE {0}")
                    .Map(ScopeCommand.DumpImage, ":DISPLAY:DATA?")
                    .Map(ScopeCommand.PopLastSystemError, ":SYSTEM:ERROR?")
                    .Map(ScopeCommand.OperationComplete, "*OPC?"),
            "InfiniiVision 2000 X"
            );

            // Keysight/Agilent
            // ----------------
            // InfiniiVision 6000 X
            //      AGILENT TECHNOLOGIES DSO6104L
            // ---
            // https://www.keysight.com/hk/en/assets/9018-07124/programming-guides/9018-07124.pdf
            // ---
            AddSeriesProfiles(
                "Keysight",
                p => p
                    .Map(ScopeCommand.Identify, "*IDN?")
                    .Map(ScopeCommand.ClearStatistics, "*CLS")
                    .Map(ScopeCommand.QueryActiveTrigger, ":TRIGGER:SWEEP?")
                    .Map(ScopeCommand.Stop, ":STOP")
                    .Map(ScopeCommand.Run, ":RUN")
                    .Map(ScopeCommand.Single, ":SINGLE")
                    .Map(ScopeCommand.QueryTriggerMode, ":TRIGGER:MODE?")
                    .Map(ScopeCommand.QueryTriggerLevel, ":TRIGGER:EDGE:LEVEL?")
                    .Map(ScopeCommand.SetTriggerLevel, ":TRIGGER:EDGE:LEVEL {0}")
                    .Map(ScopeCommand.QueryTimeDiv, ":TIMEBASE:SCALE?")
                    .Map(ScopeCommand.SetTimeDiv, ":TIMEBASE:SCALE {0}")
                    .Map(ScopeCommand.DumpImage, ":DISPLAY:DATA? PNG")
                    .Map(ScopeCommand.PopLastSystemError, ":SYSTEM:ERROR?")
                    .Map(ScopeCommand.OperationComplete, "*OPC?"),
            "InfiniiVision 6000 X"
            );

            // ######################################################################
            // TIME/DIV VALUES
            // ######################################################################

            AddTimeDivTokens(
                "Keysight",
                new[]
                {
                    "5ns","10ns","20ns","50ns","100ns","200ns","500ns",
                    "1us","2us","5us","10us","20us","50us","100us","200us","500us",
                    "1ms","2ms","5ms","10ms","20ms","50ms","100ms","200ms","500ms",
                    "1s","2s","5s","10s","20s","50s"
                },
                "InfiniiVision 2000 X",
                "InfiniiVision 6000 X"
            );
        }
    }
}