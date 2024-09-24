using Agilent.CommandExpert.ScpiNet.Ag34980_2_43;
using System;
using System.Diagnostics;

namespace ABT.Test.TestExecutive.SCPI_VISA_Instruments.Multifunction;
public class MF_34980A {
    public void Test() {
        Ag34980 v34980A = new Ag34980("TCPIP0::10.25.32.13::inst0::INSTR");
        v34980A.SCPI.SYSTem.CTYPe.Query(1, out String identity);
        v34980A.SCPI.SYSTem.CDEScription.LONG.Query(1, out String description);
        Debug.Print($"Identity    : '{identity}'.");
        Debug.Print($"Description : '{description}'.");
    }
}
