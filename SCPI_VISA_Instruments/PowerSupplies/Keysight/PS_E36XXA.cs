using System;
using System.Collections.Generic;

namespace ABT.Test.TestExecutive.SCPI_VISA_Instruments.PowerSupplies.Keysight {

    public abstract class PS_E36XXA(SCPI_VISA_InstrumentOld.Alias id, String description, String address, String className) : SCPI_VISA_InstrumentOld(id, description, address, className) {
        public override String MODEL { get { return "E36XXA"; } }

        public virtual void Set(Single Volts, Single Amps, Single OVP, STATES State) {
            SCPI.Clear();
            SCPI.AppendLine($":OUTPut:STATe 0");
            SCPI.AppendLine($":SOURce:VOLTage:PROTection:LEVel {OVP.ToString()}");
            SCPI.AppendLine($":SOURce:VOLTage:LEVel:IMMediate:AMPLitude {Volts.ToString()}");
            SCPI.AppendLine($":SOURce:CURRent:LEVel:IMMediate:AMPLitude {Amps.ToString()}");
            SCPI.AppendLine($":OUTPut:STATe {((Int32)State).ToString()}");
            Command(ref SCPI);
        }

        public virtual STATES StateGet() { return Query(":OUTPut:STATe?") == "0" ? STATES.off : STATES.ON; }

        public virtual void StateSet(STATES State) { Command($":OUTPut:STATe {(Int32)State}"); }
    }
}