using System;
using System.Windows.Forms;
using Agilent.CommandExpert.ScpiNet.Ag34401_11;
using Agilent.CommandExpert.ScpiNet.Ag34401_11.SCPI;

namespace ABT.Test.TestExecutive.Instruments.MultiMeters {
    public class MM_34401A : Ag34401 {

        public enum MMD { MIN, MAX, DEF }
        public enum TERMINAL { Front, Rear };
        public enum PROPERTY { AmperageAC, AmperageDC, Continuity, Frequency, Fresistance, Period, Resistance, VoltageAC, VoltageDC, VoltageDiodic }

        public MM_34401A(String Address) : base(Address) { }

        public void DelayAutoSet(Boolean state) { SCPI.TRIGger.DELay.AUTO.Command(state); }

        public Boolean DelayAutoIs() {
            SCPI.TRIGger.DELay.AUTO.Query(out Boolean state);
            return state;
        }

        public void DelaySet(MMD mmd) { SCPI.TRIGger.DELay.Command(Enum.GetName(typeof(MMD), mmd)); }

        public void DelaySet(Double Seconds) { SCPI.TRIGger.DELay.Command(Seconds); }

        public Double DelayGet() {
            SCPI.TRIGger.DELay.Query($"{MMD.MIN}", out Double seconds);
            return seconds;
        }

        public Double Get(PROPERTY property) {
            // SCPI FORMAT:DATA(ASCii/REAL) command unavailable on KS 34461A.
            switch (property) {
                case PROPERTY.AmperageAC:
                    SCPI.MEASure.CURRent.AC.Query($"{MMD.DEF}", $"{MMD.DEF}", out Double acCurrent);
                    return acCurrent;
                case PROPERTY.AmperageDC:
                    SCPI.MEASure.CURRent.DC.Query($"{MMD.DEF}", $"{MMD.DEF}", out Double dcCurrent);
                    return dcCurrent;
                case PROPERTY.Continuity:
                    SCPI.MEASure.CONTinuity.Query(out Double continuity);
                    return continuity;
                case PROPERTY.Frequency:
                    SCPI.MEASure.FREQuency.Query($"{MMD.DEF}", $"{MMD.DEF}", out Double frequency);
                    return frequency;
                case PROPERTY.Fresistance:
                    SCPI.MEASure.FRESistance.Query($"{MMD.DEF}", $"{MMD.DEF}", out Double fresistance);
                    return fresistance;
                case PROPERTY.Period:
                    SCPI.MEASure.PERiod.Query($"{MMD.DEF}", $"{MMD.DEF}", out Double period);
                    return period;
                case PROPERTY.Resistance:
                    SCPI.MEASure.RESistance.Query($"{MMD.DEF}", $"{MMD.DEF}", out Double resistance);
                    return resistance;
                case PROPERTY.VoltageAC:
                    SCPI.MEASure.VOLTage.AC.Query($"{MMD.DEF}", $"{MMD.DEF}", out Double acVoltage);
                    return acVoltage;
                case PROPERTY.VoltageDC:
                    SCPI.MEASure.VOLTage.DC.Query($"{MMD.DEF}", $"{MMD.DEF}", out Double dcVoltage);
                    return dcVoltage;
                case PROPERTY.VoltageDiodic:
                    SCPI.MEASure.DIODe.Query(out Double diodeVoltage);
                    return diodeVoltage;
                default:
                    throw new NotImplementedException(TestExec.NotImplementedMessageEnum(typeof(PROPERTY)));
            }
        }

        public void TerminalsSetRear() {
            if (TerminalsGet() == TERMINAL.Front) _ = MessageBox.Show("Please depress Keysight 34401A Front/Rear button.", "Paused, click OK to continue.", MessageBoxButtons.OK, MessageBoxIcon.Information);
            SCPI.TRIGger.DELay.Command(Enum.GetName(typeof(MMD), $"{MMD.DEF}"));
            SCPI.TRIGger.DELay.AUTO.Command(true);
        }

        public TERMINAL TerminalsGet() {
            SCPI.ROUTe.TERMinals.Query(out String terminals);
            return String.Equals(terminals, "REAR") ? TERMINAL.Rear : TERMINAL.Front;
        }

        private String Query(String Q) {
            Transport.Query.Invoke(Q, out String RetVal);
            return RetVal;
        }
    }
}
