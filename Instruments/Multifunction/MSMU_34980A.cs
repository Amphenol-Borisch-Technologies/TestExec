using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Agilent.CommandExpert.ScpiNet.Ag34980_2_43;
using static ABT.Test.TestExecutive.Instruments.Instrumentation;

namespace ABT.Test.TestExecutive.Instruments.Multifunction {

    public class MSMU_34980A : Ag34980 {

        public enum ABUS { ABUS1, ABUS2, ABUS3, ABUS4, ALL };
        public enum SLOTS { Slot1 = 1, Slot2 = 2, Slot3 = 3, Slot4 = 4, Slot5 = 5, Slot6 = 6, Slot7 = 7, Slot8 = 8 }
        public enum TEMPERATURE_UNITS { C, F, K }
        public enum RELAY_STATES { opened, CLOSED }

        public MSMU_34980A(String Address) : base(Address) {
            DateTime dt = DateTime.Now;
            Transport.Command.Invoke($":SYSTem:DATE {dt.Year},{dt.Month},{dt.Day}"); 
            Transport.Command.Invoke($":SYSTem:TIME {dt.Hour},{dt.Minute},{dt.Second}");
            UnitsSet(TEMPERATURE_UNITS.F);
        }

        public Boolean InstrumentDMM_Installed() { return Query(":INSTrument:DMM:INSTalled?") == "1"; }
        public STATES InstrumentDMM_Get() { return Query($":INSTrument:DMM?") == "1" ? STATES.ON : STATES.off; }
        public void InstrumentDMM_Set(STATES State) { Transport.Command.Invoke($":INSTrument:DMM {(Int32)State}"); }
        public (Int32 Min, Int32 Max) ModuleChannels(SLOTS Slot) {
            switch (SystemType(Slot)) {
                case "34921A": return (Min: 1, Max: 44);
                case "34939A": return (Min: 1, Max: 68);
                case "34952A": return (Min: 1, Max: 7);
                default      : throw new NotImplementedException($"Module Type '{SystemType(Slot)}' unimplemented.");
            }
        }
        public void RouteCloseExclusive(String Channels) {
            ValidateChannelS(Channels);
            Transport.Command.Invoke($":ROUTe:CLOSe:EXCLusive ({Channels})");
        }
        public void RouteOpenABUS(ABUS ABus) { Transport.Command.Invoke($":ROUTe:OPEN:ABUS {ABus}"); }
        public void RouteOpenAllSlot(SLOTS Slot) { Transport.Command.Invoke($":ROUTe:OPEN:ALL {(Int32)Slot}"); }
        public void RouteOpenAll() { Transport.Command.Invoke($":ROUTe:OPEN:ALL ALL"); }
        public Boolean RouteGet(String Channels, RELAY_STATES State) {
            ValidateChannelS(Channels);
            String s = Query(State is RELAY_STATES.opened ? $":ROUTe:OPEN? ({Channels})" : $":ROUTe:CLOSe? ({Channels})");
            List<String> ls = s.Replace("[", "").Replace("]", "").Replace("0", Boolean.FalseString).Replace("1", Boolean.TrueString).Split(',').ToList();
            List<Boolean> lb = ls.Select(b => Boolean.TryParse(b, out Boolean result) && result).ToList();
            return lb.TrueForAll(b => b == true);
        }
        public void RouteSet(String Channels, RELAY_STATES State) {
            ValidateChannelS(Channels);
            Transport.Command.Invoke(State is RELAY_STATES.opened ? $":ROUTe:OPEN ({Channels})" : $":ROUTe:CLOSe ({Channels})");
        }
        public STATES SystemABusInterlockSimulateGet() { return Query(":SYSTem:ABUS:INTerlock:SIMulate?") == "1" ? STATES.ON : STATES.off; }
        public void SystemABusInterlockSimulateSet(STATES State) { Transport.Command.Invoke($":SYSTem:ABUS:INTerlock:SIMulate {(Int32)State}"); }
        public String SystemDescriptionLong(SLOTS Slot) { return Query($":SYSTem:CDEScription:LONG? {(Int32)Slot}").Replace("\"", ""); }
        public Double SystemModuleTemperature(SLOTS Slot) { return Convert.ToDouble(Query($":SYSTem:MODule:TEMPerature? TRANsducer,{(Int32)Slot}")); }
        public void SystemPreset() { Transport.Command.Invoke(":SYSTem:PRESet"); }

        public String SystemType(SLOTS Slot) { return Query($":SYSTem:CTYPe? {(Int32)Slot}").Replace("\"", "").Split(',')[(Int32)IDN_FIELDS.Model]; }
        public TEMPERATURE_UNITS UnitsGet() { return (TEMPERATURE_UNITS)Enum.Parse(typeof(TEMPERATURE_UNITS), Query($":UNIT:TEMPerature?").Replace("[", "").Replace("]", "")); }
        public void UnitsSet(TEMPERATURE_UNITS Temperature_Units) { Transport.Command.Invoke($":UNIT:TEMPerature {Temperature_Units}"); }

        private String Query(String Q) {
            Transport.Query.Invoke(Q, out String RetVal);
            return RetVal;
        }

        public void ValidateChannelS(String Channels) {
            // TODO: Debug.Print($"ChannelS: '{Channels}'.");
            if (!Regex.IsMatch(Channels, @"^@\d{4}((,|:)\d{4})*$")) {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Invalid syntax for Channels '{Channels}'.");
                sb.AppendLine(" - Must be in form of 1 or more discrete channels and/or ranges preceded by '@'.");
                sb.AppendLine(" - Channel:  '@####':       Discrete channels must be separated by commas; '@1001,1002'.");
                sb.AppendLine(" - Range:    '@####:####':  Channel ranges must be separated by colons; '@1001:1002'.");
                sb.AppendLine(" - Examples: '@1001', '@1001,2001,2005', '@1001,2001:2005' & '@1001,2001:2005,2017,3001:3015,3017' all valid.");
                sb.AppendLine();
                sb.AppendLine("Caveats:");
                sb.AppendLine(" - Whitespace not permitted; '@1001, 1005', '@1001 ,1005' '& '@1001: 1005' all invalid.");
                sb.AppendLine(" - Range cannot include ABus channels, denoted as #9##.  Thus range '@1001:1902' invalid, but discretes '@1001,1902' valid.");
                sb.AppendLine(" - Fist & only first channel begins with '@'.  Thus '1001,2001' & '@1001,@2001' both invalid.");
                throw new ArgumentException(sb.ToString());
                // https://regex101.com/.
            }
            if (Regex.IsMatch(Channels, @":\d{4}:")) throw new ArgumentException($"Invalid syntax for Channels '{Channels}'.  Invalid range ':####:'.");
            Channels = Channels.Replace("@", String.Empty);

            if (Channels.Length == 4) {
                ValidateChannel(Channels);
                return;
            }

            String[] channelsOrRanges = Channels.Split(new Char[] {','}, StringSplitOptions.None);
            foreach (String channelOrRange in channelsOrRanges) {
                if (Regex.IsMatch(channelOrRange, ":")) ValidateRange(channelOrRange);
                else ValidateChannel(channelOrRange);
            }
        }
        public void ValidateChannel(String Channel) {
            // TODO: Debug.Print($"Channel: '{Channel}'.");
            Int32 slotNumber = Int32.Parse(Channel.Substring(0, 2));
            // TODO: Debug.Print($"Slot Number: '{slotNumber}'.");
            if (!Enum.IsDefined(typeof(SLOTS), (SLOTS)slotNumber)) throw new ArgumentException($"Channel '{Channel}' must have valid integer Slot in interval [{(Int32)SLOTS.Slot1}..{(Int32)SLOTS.Slot8}].");
            Int32 channel = Int32.Parse(Channel.Substring(2));
            // TODO: Debug.Print($"Channel: '{channel}'.");
            (Int32 min, Int32 max) = ModuleChannels((SLOTS)slotNumber);
            // TODO: Debug.Print($"ModuleChannels min '{min}' & '{max}'.");
            if (channel < min || max < channel) throw new ArgumentException($"Channel '{Channel}' must have valid integer Channel in interval [{min:D3}..{max:D3}].");
        }
        public void ValidateRange(String Range) {
            // TODO: Debug.Print($"Range: '{Range}'.");
            String[] channels = Range.Split(new Char[] {':'}, StringSplitOptions.None);
            // TODO: for (Int32 i=0; i < channels.Length; i++) Debug.Print($"channels[{i}]='{channels[i]}'.");
            if (channels[0][1].Equals('9') || channels[1][1].Equals('9')) throw new ArgumentException($"Channel Range '{Range}' cannot include ABus Channel #9##.");
            ValidateChannel(channels[0]);
            ValidateChannel(channels[1]);
            if (Convert.ToInt32(channels[0]) >= Convert.ToInt32(channels[1])) throw new ArgumentException($"Channel Range '{Range}' start Channel '{channels[0]}' must be < end Channel '{channels[1]}'.");
        }
    }
}