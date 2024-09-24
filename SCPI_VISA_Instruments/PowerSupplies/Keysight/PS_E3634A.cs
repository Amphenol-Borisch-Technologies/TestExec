using System;
using System.Collections.Generic;

namespace ABT.Test.TestExecutive.SCPI_VISA_Instruments.PowerSupplies.Keysight
{

    public class PS_E3634A(SCPI_VISA_InstrumentOld.Alias id, String description, String address, String className) : PS_E36XXA(id, description, address, className) {
        public override String MODEL { get { return "E3634A"; } }
        public enum RANGE { P25V, P50V }

        public RANGE RangeGet() { return (RANGE)Enum.Parse(typeof(RANGE), Query(":SOURce:VOLTage:RANGe?")); }

        public void RangeSet(RANGE Range) { Command($":SOURce:VOLTage:RANGe {Range}"); }
    }
}