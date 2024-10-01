using System;
using Agilent.CommandExpert.ScpiNet.AgE363x_1_7;

namespace ABT.Test.TestExecutive.Instruments.PowerSupplies.Keysight {

    public class PS_E3634A : AgE363x {
        public const String MODEL = "E3634A";
        public Instrument.Alias ID;
        public String Description;
        public String ClassName;

        public enum RANGE { P25V, P50V }

        public PS_E3634A(Instrument.Alias id, String description, String address, String className) : base(address) { ID = id; Description = description; ClassName = className; }
    }
}