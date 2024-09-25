using System;
using System.Collections.Generic;
using System.Net;
using Agilent.CommandExpert.ScpiNet.AgE364xD_1_7;

namespace ABT.Test.TestExecutive.SCPI_VISA_Instruments.PowerSupplies.Keysight {

    public class PS_E3634A : Agilent.CommandExpert.ScpiNet.AgE364xD_1_7 {
        public override String MODEL { get { return "E3634A"; } }
        public enum RANGE { P25V, P50V }

        public PS_E3634A(SCPI_VISA_Instrument.Alias id, String description, String address, String className) { base(id, description, address, className); }

        public RANGE RangeGet() { return (RANGE)Enum.Parse(typeof(RANGE), Query(":SOURce:VOLTage:RANGe?")); }

        public void RangeSet(RANGE Range) { Command($":SOURce:VOLTage:RANGe {Range}"); }
    }
}