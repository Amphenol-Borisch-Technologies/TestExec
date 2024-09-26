using System;
using System.Collections.Generic;
using System.Net;
using Agilent.CommandExpert.ScpiNet.AgE363x_1_7;
using Agilent.CommandExpert.ScpiNet.AgE364xD_1_7;

namespace ABT.Test.TestExecutive.SCPI_VISA_Instruments.PowerSupplies.Keysight {

    public class PS_E3634A : AgE363x {
        public const String MODEL = "E3634A";
        public Alias ID;

        public enum RANGE { P25V, P50V }

        public PS_E3634A(SCPI_VISA_Instrument.Alias id, String description, String address, String className) { base(address); }
        
        public AgE363x E3634A = new AgE363x("%GPIB0::3::INSTR");
    }
}