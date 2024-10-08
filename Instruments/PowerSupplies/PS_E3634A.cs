using System;
using System.Text;
using Agilent.CommandExpert.ScpiNet.AgE363x_1_7;
using static ABT.Test.TestExecutive.Instruments.Instrumentation;

namespace ABT.Test.TestExecutive.Instruments.PowerSupplies  {
    public class PS_E3634A : AgE363x {
        public enum RANGE { P25V, P50V }

        public PS_E3634A(String Address) : base(Address) { }

        public RANGE RangeGet() { return (RANGE)Enum.Parse(typeof(RANGE), Query(":SOURce:VOLTage:RANGe?")); }

        public void RangeSet(RANGE Range) { Transport.Command.Invoke($":SOURce:VOLTage:RANGe {Range}"); }
        
        private String Query(String Q) {
            Transport.Query.Invoke(Q, out String RetVal);
            return RetVal;
        }
    }
}
