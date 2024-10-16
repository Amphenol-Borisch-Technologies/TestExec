using System;
using Agilent.CommandExpert.ScpiNet.AgSCPI99_1_0;

namespace ABT.Test.TestExecutive.Instruments.Generic {
    public class SCPI99 : AgSCPI99, IInstruments {
        public enum IDN_FIELDS { Manufacturer, Model, SerialNumber, FirmwareRevision } // Example: "Keysight Technologies,E36103B,MY61001983,1.0.2-1.02".  
        public const Char IDENTITY_SEPARATOR = ',';

        public String Address { get; }
        public String Detail { get; }

        public void Reinitialize() {
            SCPI.RST.Command();
            SCPI.CLS.Command();
        }

        public SCPI99(String Address, String Detail) : base(Address) {
            this.Address = Address;
            this.Detail = Detail;
        }

        public String Identity(IDN_FIELDS Property) {
            SCPI.IDN.Query(out String Identity);
            return Identity.Split(IDENTITY_SEPARATOR)[(Int32)Property];
        }

        public static String Identity(String Address, IDN_FIELDS Property) {
            new AgSCPI99(Address).SCPI.IDN.Query(out String Identity);
            return Identity.Split(IDENTITY_SEPARATOR)[(Int32)Property];
        }

        public static String Identity(Object Instrument, IDN_FIELDS Property) {
            String Address = ((IInstruments)Instrument).Address;
            return Identity(Address, Property);
        }
    }
}