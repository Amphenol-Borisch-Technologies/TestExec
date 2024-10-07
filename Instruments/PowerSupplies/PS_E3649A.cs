using System;
using System.Text;
using Agilent.CommandExpert.ScpiNet.AgE364xD_1_7;
using static ABT.Test.TestExecutive.Instruments.Instrumentation;

namespace ABT.Test.TestExecutive.Instruments.PowerSupplies {
    public class PS_E3649A : AgE364xD {
        public readonly String ID;
        public readonly String Detail;
        public readonly String Address;

        public PS_E3649A(String ID, String Detail, String Address) : base(Address) {
            this.ID = ID;
            this.Detail = Detail;
            this.Address = Address;
        }

        public enum OUTPUTS { OUTPut1, OUTPut2 };

        public OUTPUTS InstrumentGet() { return Query(":INSTrument:SELect?") == "OUTP1" ? OUTPUTS.OUTPut1 : OUTPUTS.OUTPut2; }

        public void InstrumentSet(OUTPUTS Output) { Transport.Command.Invoke($":INSTrument:SELect {Enum.GetName(typeof(OUTPUTS), Output)}"); }

        public void Set(OUTPUTS Output, Single Volts, Single Amps, Single OVP, STATES State) {
            InstrumentSet(Output);
            Set(Volts, Amps, OVP, State);
        }

        private String Query(String Q) {
            Transport.Query.Invoke(Q, out String RetVal);
            return RetVal;
        }

        public void Set(Single Volts, Single Amps, Single OVP, STATES State) {
            StringBuilder scpi = new StringBuilder();
            scpi.AppendLine($":OUTPut:STATe 0");
            scpi.AppendLine($":SOURce:VOLTage:PROTection:LEVel {OVP}");
            scpi.AppendLine($":SOURce:VOLTage:LEVel:IMMediate:AMPLitude {Volts}");
            scpi.AppendLine($":SOURce:CURRent:LEVel:IMMediate:AMPLitude {Amps}");
            scpi.AppendLine($":OUTPut:STATe {((Int32)State)}");
            Transport.Command.Invoke(scpi.ToString());
        }

        public STATES StateGet() { return Query(":OUTPut:STATe?") == "0" ? STATES.off : STATES.ON; }

        public void StateSet(STATES State) { Transport.Command.Invoke($":OUTPut:STATe {(Int32)State}"); }

    }
}
