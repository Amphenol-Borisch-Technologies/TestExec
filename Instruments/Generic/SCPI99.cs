using System;
using Agilent.CommandExpert.ScpiNet.AgSCPI99_1_0;

namespace ABT.Test.TestExecutive.Instruments.Generic {
    public class SCPI99 : AgSCPI99 {
        public String Address;
        public String Detail;

        public SCPI99(String Address, String Detail) : base(Address) {
            this.Address = Address;
            this.Detail = Detail;
        }
    }
}