using System;
using Agilent.CommandExpert.ScpiNet.AgE364xD_1_7;

namespace ABT.Test.TestExecutive.Instruments.PowerSupplies.Keysight {

    public class PS_E3649A : AgE364xD {
        public const String MODEL = "E3649A";
        public Instrument.Alias ID;
        public String Description;
        public String ClassName;

        public PS_E3649A(Instrument.Alias id, String description, String address, String className) : base(address) { ID = id; Description = description; ClassName = className; }
    }
}