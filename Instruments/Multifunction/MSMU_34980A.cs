using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Agilent.CommandExpert.ScpiNet.Ag34980_2_43;

namespace ABT.Test.TestExecutive.Instruments.Multifunction {

    public class MSMU_34980A : SCPI_VISA_InstrumentOld {
        public override String MODEL { get { return "34980A"; } }
        public enum ABUS { ABUS1, ABUS2, ABUS3, ABUS4, ALL };
        public enum SLOTS { Slot1 = 1, Slot2 = 2, Slot3 = 3, Slot4 = 4, Slot5 = 5, Slot6 = 6, Slot7 = 7, Slot8 = 8 }
        public enum TEMPERATURE_UNITS { C, F, K }

        public MSMU_34980A(Instrument.Alias id, String description, String address, String className) : base(address) {
            SystemDateSet(new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day));
            SystemTimeSet(new TimeOnly(DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second));
            UnitsSet(TEMPERATURE_UNITS.F);
        }

        public List<Int32> DiagnosticRelayCycles(String Channels) {
            ValidateChannelS(Channels);
            String s = Query($":DIAGnostic:RELay:CYCLes? ({Channels})");
            List<String> ls = [.. s.Replace("[", "").Replace("]", "").Split(",")];
            List<Int32> li = ls.Select(i => Int32.Parse(i)).ToList();
            return li;
        }
        public Boolean InstrumentDMM_Installed() { return Query(":INSTrument:DMM:INSTalled?") == "1"; }
        public STATES InstrumentDMM_Get() { return Query($":INSTrument:DMM?") == "1" ? STATES.ON : STATES.off; }
        public void InstrumentDMM_Set(STATES State) { Command($":INSTrument:DMM {(Int32)State}"); }
        public (Int32 Min, Int32 Max) ModuleChannels(SLOTS Slot) {
            return SystemType(Slot) switch {
                "34921A" => (Min: 1, Max: 44),
                "34939A" => (Min: 1, Max: 68),
                "34952A" => (Min: 1, Max: 7),
                _ => throw new NotImplementedException($"Module Type '{SystemType(Slot)}' unimplemented."),
            };
        }
        public void RouteCloseExclusive(String Channels) {
            ValidateChannelS(Channels);
            Command($":ROUTe:CLOSe:EXCLusive ({Channels})");
        }
        public void RouteOpenABUS(ABUS ABus) { Command($":ROUTe:OPEN:ABUS {ABus}"); }
        public void RouteOpenAllSlot(SLOTS Slot) { Command($":ROUTe:OPEN:ALL {(Int32)Slot}"); }
        public void RouteOpenAll() { Command($":ROUTe:OPEN:ALL ALL"); }
        public Boolean RouteGet(String Channels, RELAY_STATES State) {
            ValidateChannelS(Channels);
            String s = Query(State is RELAY_STATES.opened ? $":ROUTe:OPEN? ({Channels})" : $":ROUTe:CLOSe? ({Channels})");
            List<String> ls = [.. s.Replace("[", "").Replace("]", "").Replace("0", Boolean.FalseString).Replace("1", Boolean.TrueString).Split(",")];
            List<Boolean> lb = ls.Select(b => Boolean.TryParse(b, out Boolean result) && result).ToList();
            return lb.TrueForAll(b => b == true);
        }
        public void RouteSet(String Channels, RELAY_STATES State) {
            ValidateChannelS(Channels);
            Command(State is RELAY_STATES.opened ? $":ROUTe:OPEN ({Channels})" : $":ROUTe:CLOSe ({Channels})");
        }
        public STATES SystemABusInterlockSimulateGet() { return Query(":SYSTem:ABUS:INTerlock:SIMulate?") == "1" ? STATES.ON : STATES.off; }
        public void SystemABusInterlockSimulateSet(STATES State) { Command($":SYSTem:ABUS:INTerlock:SIMulate {(Int32)State}"); }
        public DateOnly SystemDateGet() {
            Int32[] date = Query($":SYSTem:DATE?").Split(",").Select(t => (Int32)Convert.ToDouble(t)).ToArray();
            return new DateOnly(year: date[0], month: date[1], day: date[2]);
        }
        public void SystemDateSet(DateOnly Date) { Command($":SYSTem:DATE {Date.Year},{Date.Month},{Date.Day}"); }
        public String SystemDescriptionLong(SLOTS Slot) { return Query($":SYSTem:CDEScription:LONG? {(Int32)Slot}").Replace("\"", ""); }
        public Double SystemModuleTemperature(SLOTS Slot) { return Convert.ToDouble(Query($":SYSTem:MODule:TEMPerature? TRANsducer,{(Int32)Slot}")); }
        public void SystemPreset() { Command(":SYSTem:PRESet"); }
        public TimeOnly SystemTimeGet() {
            Int32[] time = Query($":SYSTem:TIME?").Split(",").Select(t => (Int32)Convert.ToDouble(t)).ToArray();
            return new TimeOnly(hour: time[0], minute: time[1], second: time[2]);
        }
        public void SystemTimeSet(TimeOnly Time) { Command($":SYSTem:TIME {Time.Hour},{Time.Minute},{Time.Second}"); }
        public String SystemType(SLOTS Slot) { return Query($":SYSTem:CTYPe? {(Int32)Slot}").Replace("\"", "").Split(",")[(Int32)IDN_FIELDS.Model]; }
        public TEMPERATURE_UNITS UnitsGet() { return (TEMPERATURE_UNITS)Enum.Parse(typeof(TEMPERATURE_UNITS), Query($":UNIT:TEMPerature?").Replace("[", "").Replace("]", "")); }
        public void UnitsSet(TEMPERATURE_UNITS Temperature_Units) { Command($":UNIT:TEMPerature {Temperature_Units}"); }

        private void ValidateChannelS(String Channels) {
            // TODO: Debug.Print($"ChannelS: '{Channels}'.");
            if (!Regex.IsMatch(Channels, @"^@\d{4}((,|:)\d{4})*$")) {
                StringBuilder sb = new();
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

            String[] channelsOrRanges = Channels.Split(",", StringSplitOptions.None);
            foreach (String channelOrRange in channelsOrRanges) {
                if (Regex.IsMatch(channelOrRange, ":")) ValidateRange(channelOrRange);
                else ValidateChannel(channelOrRange);
            }
        }
        private void ValidateChannel(String Channel) {
            // TODO: Debug.Print($"Channel: '{Channel}'.");
            Int32 slotNumber = Int32.Parse(Channel[..1]);
            // TODO: Debug.Print($"Slot Number: '{slotNumber}'.");
            if (!Enum.IsDefined(typeof(SLOTS), (SLOTS)slotNumber)) throw new ArgumentException($"Channel '{Channel}' must have valid integer Slot in interval [{(Int32)SLOTS.Slot1}..{(Int32)SLOTS.Slot8}].");
            Int32 channel = Int32.Parse(Channel[2..]);
            // TODO: Debug.Print($"Channel: '{channel}'.");
            (Int32 min, Int32 max) = ModuleChannels((SLOTS)slotNumber);
            // TODO: Debug.Print($"ModuleChannels min '{min}' & '{max}'.");
            if (channel < min || max < channel) throw new ArgumentException($"Channel '{Channel}' must have valid integer Channel in interval [{min:D3}..{max:D3}].");
        }
        private void ValidateRange(String Range) {
            // TODO: Debug.Print($"Range: '{Range}'.");
            String[] channels = Range.Split(":", StringSplitOptions.None);
            // TODO: for (Int32 i=0; i < channels.Length; i++) Debug.Print($"channels[{i}]='{channels[i]}'.");
            if (channels[0][1].Equals('9') || channels[1][1].Equals('9')) throw new ArgumentException($"Channel Range '{Range}' cannot include ABus Channel #9##.");
            ValidateChannel(channels[0]);
            ValidateChannel(channels[1]);
            if (Convert.ToInt32(channels[0]) >= Convert.ToInt32(channels[1])) throw new ArgumentException($"Channel Range '{Range}' start Channel '{channels[0]}' must be < end Channel '{channels[1]}'.");
        }
    }
}