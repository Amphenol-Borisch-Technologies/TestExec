using System;
using Agilent.CommandExpert.ScpiNet.AgE364xD_1_7;
using static ABT.Test.TestExecutive.Instruments.Instrumentation;

namespace ABT.Test.TestExecutive.Instruments.PowerSupplies {
    public class SCPI_PS_E3649A : AgE364xD {

        public SCPI_PS_E3649A(String Address) : base(Address) { }

        public enum OUTPUTS { OUTput1, OUTput2 };
        public enum MMD { MINimum, MAXimum, DEFault }

        public OUTPUTS Selected() {
            SCPI.INSTrument.SELect.Query(out String select);
            return select == "OUTP1" ? OUTPUTS.OUTput1 : OUTPUTS.OUTput2;
        }

        public void Select(OUTPUTS Output) { SCPI.INSTrument.SELect.Command($"{Output}"); }

        public void Set(OUTPUTS Output, Single Volts, Single Amps, Single OVP, STATES State) {
            Select(Output);
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
