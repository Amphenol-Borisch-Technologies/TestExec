using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using ABT.Test.TestExecutive.AppConfig;
using Agilent.CommandExpert.ScpiNet.Ag34401_11.SCPI.FETCh;
using Agilent.CommandExpert.ScpiNet.AgSCPI99_1_0;

namespace ABT.Test.TestExecutive.Instruments {
    public enum STATES { off = 0, ON = 1 } // NOTE: To Command an instrument off or ON, and Query it's STATE, again off or ON.
    public enum SENSE_MODE { EXTernal, INTernal }
    // Consistent convention for lower-cased inactive states off/low/zero as 1st states in enums, UPPER-CASED active ON/HIGH/ONE as 2nd states.

    public interface IInstruments {
        String Address { get; } // NOTE: Store in instrument objects for easy error reporting of addresses.  Not easily gotten otherwise.
        String Detail { get; }  // NOTE: Store in instrument objects for easy error reporting of detailed descriptions, similar but more useful than SCPI's *IDN query.
        void Reinitialize();    // NOTE: After each test run, reinitialize instrument.  Typically performs SCPI's *RST & *CLS commands.
    }

    public static class Instruments {
        public static Dictionary<String, Object> Get() {
            Dictionary<String, Object> Instruments = new Dictionary<String, Object>();
            foreach (XElement xe in XElement.Load(TestExec.GlobalConfigurationFile).Elements("InstrumentsSystem").Elements("Instrument")) {
                String ID = xe.Attribute("ID").Value;
                String Detail = xe.Attribute("Detail").Value;
                String Address = xe.Attribute("Address").Value;
                String ClassName = xe.Attribute("ClassName").Value;
                Instruments.Add(ID, Activator.CreateInstance(Type.GetType(ClassName), new Object[] { ID, Address, Detail }));
            }
            return Instruments;
        }

        public static (String Detail, String Address, String ClassName) Get(String ID) {
            XElement Instrument = XElement.Load(TestExec.GlobalConfigurationFile).Element("Instruments").Elements("Instrument").FirstOrDefault(x => x.Element("ID").Value == ID);
            if (Instrument != null) return (Instrument.Element("Detail").Value, Instrument.Element("Address").Value, Instrument.Element("ClassName").Value);
            throw new ArgumentException($"Instrument with ID '{ID}' not present in file '{TestExec.GlobalConfigurationFile}'.");
        }

        public static void Reinitialize(Dictionary<String, Object> Instruments) {
            foreach (KeyValuePair<String, Object> kvp in Instruments) ((IInstruments)kvp.Value).Reinitialize();
        }
    }

    public class InstrumentStationary {
        public static Dictionary<String, Object> Get() {
            InstrumentsStationaryRequiredSection ISRS = (InstrumentsStationaryRequiredSection)ConfigurationManager.GetSection(InstrumentsStationaryRequiredSection.ClassName);
            InstrumentsStationaryRequired ISRs = ISRS.InstrumentsStationaryRequired;
            Dictionary<String, Object> dictionary = new Dictionary<String, Object>();
            foreach (InstrumentStationaryRequired ISR in ISRs) try {
                    (String Detail, String Address) = GetInstrumentStationary(ISR.ID);
                    dictionary.Add(ISR.Alias, Activator.CreateInstance(Type.GetType(ISR.ClassName), new Object[] { Address, Detail }));
                } catch (Exception e) {
                    StringBuilder sb = new StringBuilder().AppendLine();
                    sb.AppendLine($"App.config issue with InstrumentStationaryRequired:");
                    sb.AppendLine($"   ID              : {ISR.ID}");
                    sb.AppendLine($"   Alias           : {ISR.Alias}");
                    sb.AppendLine($"   ClassName       : {ISR.ClassName}{Environment.NewLine}");
                    sb.AppendLine($"Exception Message(s):");
                    sb.AppendLine($"{e}{Environment.NewLine}");
                    throw new ArgumentException(sb.ToString());
                }
            return dictionary;
        }

        private static (String Detail, String Address) GetInstrumentStationary(String ID) {
            (String Detail, String Address) DA = (String.Empty, String.Empty);
            try {


            Dictionary<String, Object> Instruments = new Dictionary<String, Object>();
            foreach (XElement xe in XElement.Load(TestExec.GlobalConfigurationFile).Elements("InstrumentsSystem").Elements("Instrument")) {
                String ID = xe.Attribute("ID").Value;
                String Detail = xe.Attribute("Detail").Value;
                String Address = xe.Attribute("Address").Value;
                String ClassName = xe.Attribute("ClassName").Value;
                Instruments.Add(ID, Activator.CreateInstance(Type.GetType(ClassName), new Object[] { ID, Address, Detail }));
            }


        public static (String Detail, String Address, String ClassName) Get(String ID) {
            XElement Instrument = XElement.Load(TestExec.GlobalConfigurationFile).Element("Instruments").Elements("Instrument").FirstOrDefault(x => x.Element("ID").Value == ID);
            if (Instrument != null) return (Instrument.Element("Detail").Value, Instrument.Element("Address").Value, Instrument.Element("ClassName").Value);
            throw new ArgumentException($"Instrument with ID '{ID}' not present in file '{TestExec.GlobalConfigurationFile}'.");
        }




            } catch (Exception e) {
                StringBuilder sb = new StringBuilder().AppendLine();
                sb.AppendLine($"App.config issue with InstrumentStationary:");
                sb.AppendLine($"   ID              : {ID}");
                sb.AppendLine($"   Detail          : {DA.Detail}");
                sb.AppendLine($"   Address         : {DA.Address}{Environment.NewLine}");
                sb.AppendLine($"Exception Message(s):");
                sb.AppendLine($"{e}{Environment.NewLine}");
                throw new ArgumentException(sb.ToString());
            }
            return (String.Empty, String.Empty);
        }
    }

    public class InstrumentPortable {
        public static Dictionary<String, Object> Get() {
            InstrumentsPortableRequiredSection IPRS = (InstrumentsPortableRequiredSection)ConfigurationManager.GetSection(InstrumentsPortableRequiredSection.ClassName);
            InstrumentsPortableRequired IPRs = IPRS.InstrumentsPortableRequired;
            Dictionary<String, Object> dictionary = new Dictionary<String, Object>();
            foreach (InstrumentPortableRequired IPR in IPRs) try {
                    dictionary.Add(IPR.Alias, Activator.CreateInstance(Type.GetType(IPR.ClassName), new Object[] { IPR.Address, IPR.Detail }));
                } catch (Exception e) {
                    StringBuilder sb = new StringBuilder().AppendLine();
                    sb.AppendLine($"App.config issue with InstrumentPortableRequired:");
                    sb.AppendLine($"   Alias           : {IPR.Alias}");
                    sb.AppendLine($"   Detail          : {IPR.Detail}");
                    sb.AppendLine($"   Address         : {IPR.Address}");
                    sb.AppendLine($"   ClassName       : {IPR.ClassName}{Environment.NewLine}");
                    sb.AppendLine($"Exception Message(s):");
                    sb.AppendLine($"{e}{Environment.NewLine}");
                    throw new ArgumentException(sb.ToString());
                }
            return dictionary;
        }
    }

}
