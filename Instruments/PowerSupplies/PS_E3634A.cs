using System;
using Agilent.CommandExpert.ScpiNet.AgE363x_1_7;
using static ABT.Test.TestExecutive.Instruments.Instrumentation;

namespace ABT.Test.TestExecutive.Instruments.PowerSupplies  {
    public class PS_E3634A : AgE363x {
        public enum RANGE { P25V, P50V }
        public enum MMD { MINimum, MAXimum, DEFault }

        public PS_E3634A(String Address) : base(Address) { }

        public RANGE RangeGet() { 
            SCPI.SOURce.VOLTage.RANGe.Query(out String range);
            return (RANGE)Enum.Parse(typeof(RANGE), range);
        }

        public void RangeSet(RANGE Range) { SCPI.SOURce.VOLTage.RANGe.Command($"{Range}"); }

        public void Set(Single Volts, Single Amps, Single OVP, STATES State) {
            SCPI.OUTPut.STATe.Command(false);
            SCPI.SOURce.VOLTage.PROTection.CLEar.Command();
            SCPI.SOURce.VOLTage.PROTection.LEVel.Command($"{MMD.MAXimum}");
            SCPI.SOURce.VOLTage.LEVel.IMMediate.AMPLitude.Command($"{Volts}");
            SCPI.SOURce.CURRent.LEVel.IMMediate.AMPLitude.Command($"{Amps}");
            SCPI.SOURce.VOLTage.PROTection.LEVel.Command($"{OVP}");
            SCPI.OUTPut.STATe.Command(State == STATES.ON);
        }

        public STATES StateGet() {
            SCPI.OUTPut.STATe.Query(out Boolean state);
            return state ? STATES.ON : STATES.off;
        }

        public void StateSet(STATES State) { SCPI.OUTPut.STATe.Command(State == STATES.ON); }
    }
}
