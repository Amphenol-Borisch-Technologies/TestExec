﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using Agilent.CommandExpert.ScpiNet.AgSCPI99_1_0;
using TestLibrary.AppConfig;
// All Agilent.CommandExpert.ScpiNet drivers are created by adding new SCPI VISA Instruments in Keysight's Command Expert app software.
//  - Command Expert literally downloads & installs Agilent.CommandExpert.ScpiNet drivers when new SVIs are added.
//  - The Agilent.CommandExpert.ScpiNet dirvers are installed into folder C:\ProgramData\Keysight\Command Expert\ScpiNetDrivers.
// https://www.keysight.com/us/en/lib/software-detail/computer-software/command-expert-downloads-2151326.html
//
// Recommend using Command Expert to generate SCPI commands, which are directly exportable as .Net statements.
//
namespace TestLibrary.SCPI_VISA_Instruments {
    // NOTE: https://forums.ni.com/t5/Instrument-Control-GPIB-Serial/IVI-Drivers-Pros-and-Cons/td-p/4165671.
    public enum SCPI_VISA_CATEGORIES {
        CounterTimer,           // CT
        ElectronicLoad,         // EL
        LogicAnalyzer,          // LA
        MultiMeter,             // MM
        OscilloScope,           // OS
        PowerSupply,            // PS
        ProgrammableInstrument, // PI (For SCPI99 compliant VISA instruments without model specific SCPI drivers.)
        WaveformGenerator       // WG
    }

    public enum SCPI_VISA_IDs {
        // NOTE: Not all SCPI_VISA_IDs are necessarily present/installed; actual configuration defined in file App.config.
        //  - SCPI_VISA_IDs has *vastly* more capacity than needed, but doing so costs little.
        CT1, CT2, CT3, CT4, CT5, CT6, CT7, CT8, CT9,    // Counter Timers 1 - 9.
        EL1, EL2, EL3, EL4, EL5, EL6, EL7, EL8, EL9,    // Electronic Loads 1 - 9.
        LA1, LA2, LA3, LA4, LA5, LA6, LA7, LA8, LA9,    // Logic Analyzers 1 - 9.
        MM1, MM2, MM3, MM4, MM5, MM6, MM7, MM8, MM9,    // Multi-Meters 1 - 9.
        OS1, OS2, OS3, OS4, OS5, OS6, OS7, OS8, OS9,    // OscilloScopes 1 - 9.
        PI1, PI2, PI3, PI4, PI5, PI6, PI7, PI8, PI9,    // Programmable Instruments 1 - 9.  For SCPI99 compliant VISA instruments without model specific SCPI drivers.
        PS1, PS2, PS3, PS4, PS5, PS6, PS7, PS8, PS9,    // Power Supplies 1 - 9.
        WG1, WG2, WG3, WG4, WG5, WG6, WG7, WG8, WG9     // Waveform Generators 1 - 9.
    }

    public enum SCPI_IDENTITY { Manufacturer, Model, SerialNumber, FirmwareRevision }
    // Example: "Keysight Technologies,E36103B,MY61001983,1.0.2-1.02".
    
    public static class SCPI_VISA {
        // NOTE: Unlike all other classes in namespace TestLibrary.SCPI_VISA_Instruments, SCPI_VISA utilizes only VISA addresses, not Instrument objects contained in their SCPI_VISA_Instrument objects.
        //  - Thus SCPI_VISA has to inefficiently create temporary AgSCPI99 objects for each method, disposed immediately after use.
        public static String CHANNEL_1 = "(@1)";
        public static String CHANNEL_2 = "(@2)";
        public static String CHANNEL_1_2 = "(@1:2)";
        public static String SELF_TEST_ERROR_MESSAGE = $"SCPI VISA Instrument Address '{0}' failed SelfTest.";
        public const Char IDENTITY_SEPARATOR = ',';
        public const Int32 WIDTH = -16;

        public static void Reset(SCPI_VISA_Instrument SVI) { Reset(SVI.Address); }
        public static void Reset(String address) { new AgSCPI99(address).SCPI.RST.Command(); }

        public static void ResetAll(Dictionary<SCPI_VISA_IDs, SCPI_VISA_Instrument> SVIs) { foreach (KeyValuePair<SCPI_VISA_IDs, SCPI_VISA_Instrument> kvp in SVIs) Reset(kvp.Value); }
        public static void ResetAll(List<String> addresses) { foreach (String address in addresses) Reset(address); }

        public static void Clear(SCPI_VISA_Instrument SVI) { Clear(SVI.Address); }
        public static void Clear(String address) { new AgSCPI99(address).SCPI.CLS.Command(); }
        
        public static void ClearAll(Dictionary<SCPI_VISA_IDs, SCPI_VISA_Instrument> SVIs) { foreach (KeyValuePair<SCPI_VISA_IDs, SCPI_VISA_Instrument> kvp in SVIs) Clear(kvp.Value); }
        public static void ClearAll(List<String> addresses) { foreach (String address in addresses) Clear(address); }

        public static void SelfTest(SCPI_VISA_Instrument SVI) { try { SelfTest(SVI.Address); } catch (InvalidOperationException) { throw new InvalidOperationException(GetErrorMessage(SVI, String.Format(SELF_TEST_ERROR_MESSAGE, SVI.Address))); } }
        public static void SelfTest(String address) {
            Clear(address);
            new AgSCPI99(address).SCPI.TST.Query(out Int32 selfTestResult);
            if (selfTestResult != 0) throw new InvalidOperationException(String.Format(SELF_TEST_ERROR_MESSAGE, address));
        }

        public static void SelfTestAll(Dictionary<SCPI_VISA_IDs, SCPI_VISA_Instrument> SVIs) { foreach (KeyValuePair<SCPI_VISA_IDs, SCPI_VISA_Instrument> kvp in SVIs) SelfTest(kvp.Value); }
        public static void SelfTestAll(List<String> addresses) { foreach (String address in addresses) SelfTest(address); }

        public static void Initialize(SCPI_VISA_Instrument SVI) {
            Reset(SVI); // Reset SVI to default power-on states.
            SelfTest(SVI);
        }
        public static void Initialize(String address) {
            Reset(address);
            SelfTest(address);
        }

        public static void InitializeAll(Dictionary<SCPI_VISA_IDs, SCPI_VISA_Instrument> SVIs) { foreach (KeyValuePair<SCPI_VISA_IDs, SCPI_VISA_Instrument> kvp in SVIs) Initialize(kvp.Value); }
        public static void InitializeAll(List<String> addresses) { foreach (String address in addresses) Initialize(address); }

        public static Int32 QuestionCondition(SCPI_VISA_Instrument SVI) { return QuestionCondition(SVI.Address); }
        public static Int32 QuestionCondition(String address) {
            new AgSCPI99(address).SCPI.STATus.QUEStionable.CONDition.Query(out Int32 ConditionRegister);
            return ConditionRegister;
        }

        public static String GetIdentity(SCPI_VISA_Instrument SVI) { return GetIdentity(SVI.Address); }
        public static String GetIdentity(String address) {
            new AgSCPI99(address).SCPI.IDN.Query(out String Identity);
            return Identity;
        }

        public static String GetIdentity(SCPI_VISA_Instrument SVI, SCPI_IDENTITY property) { return GetIdentity(SVI.Address, property); }
        public static String GetIdentity(String address, SCPI_IDENTITY property) { return GetIdentity(address).Split(SCPI_VISA.IDENTITY_SEPARATOR)[(Int32)property]; }

        public static void Command(String command, SCPI_VISA_Instrument SVI) { Command(command, SVI.Address); }
        public static void Command(String command, String address) { new AgSCPI99(address).Transport.Command.Invoke(command); }

        public static String Query(String query, SCPI_VISA_Instrument SVI) { return Query(query, SVI.Address); }
        public static String Query(String query, String address) {
            new AgSCPI99(address).Transport.Query.Invoke(query, out String ReturnString);
            return ReturnString;
        }

        public static String GetSCPI_VISA_IntrumentInfo(SCPI_VISA_Instrument SVI, String optionalHeader = "") {
            String SCPI_VISA_Message = (optionalHeader == "") ? "" : optionalHeader += Environment.NewLine;
            foreach (PropertyInfo pi in SVI.GetType().GetProperties()) SCPI_VISA_Message += $"{pi.Name,WIDTH}: '{pi.GetValue(SVI)}'{Environment.NewLine}";
            return SCPI_VISA_Message;
        }

        internal static String GetErrorMessage(SCPI_VISA_Instrument SVI) { return GetSCPI_VISA_IntrumentInfo(SVI, "SCPI-VISA SCPI_VISA_Instrument failed:"); }

        internal static String GetErrorMessage(SCPI_VISA_Instrument SVI, String errorMessage) { return $"{GetErrorMessage(SVI)}{"Error Message",WIDTH}: '{errorMessage}'.{Environment.NewLine}"; }

        internal static String GetErrorMessage(SCPI_VISA_Instrument SVI, String errorMessage, Int32 errorNumber) { return $"{GetErrorMessage(SVI, errorMessage)}{"Error Number",WIDTH}: '{errorNumber}'.{Environment.NewLine}"; }
    }
}