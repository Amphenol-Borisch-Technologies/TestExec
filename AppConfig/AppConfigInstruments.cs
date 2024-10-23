using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABT.Test.Exec.AppConfig {
    public static class Instruments {
        public static Dictionary<String, Object> Get() {
            Dictionary<String, Object> Instruments = GetInstrumentsPortable();
            Dictionary<String, String> InstrumentsStationary = GetInstrumentsStationary();
            IEnumerable<XElement> IS = XElement.Load(TestExec.ConfigurationTestExec).Elements("InstrumentsStationary").Elements("InstrumentStationary");
            XElement I;
            // Now add InstrumentsStationary listed in app.config, but must first read their Address, Detail & ClassName from TestExec.ConfigurationTestExec.
            foreach (KeyValuePair<String, String> kvp in InstrumentsStationary) {
                I = IS.FirstOrDefault(x => x.Element("ID").Value == kvp.Key);
                if (I == null) throw new ArgumentException($"Instrument with ID '{kvp.Key}' not present in file '{TestExec.ConfigurationTestExec}'.");
                Instruments.Add(kvp.Key, Activator.CreateInstance(Type.GetType(I.Element("ClassName").Value), new Object[] { I.Element("Address").Value, I.Element("Detail").Value }));
            }
            return Instruments;
        }

        private static Dictionary<String, String> GetInstrumentsStationary() {
            InstrumentsStationaryRequiredSection ISRSs = (InstrumentsStationaryRequiredSection)ConfigurationManager.GetSection(InstrumentsStationaryRequiredSection.ClassName);
            InstrumentsStationaryRequired ISRs = ISRSs.InstrumentsStationaryRequired;
            Dictionary<String, String> InstrumentsStationary = new Dictionary<String, String>();
            foreach (InstrumentStationaryRequired ISR in ISRs) try {
                InstrumentsStationary.Add(ISR.ID, ISR.ClassName);
            } catch (Exception e) {
                StringBuilder sb = new StringBuilder().AppendLine();
                sb.AppendLine($"App.config issue with InstrumentStationaryRequired:");
                sb.AppendLine($"   ID              : {ISR.ID}");
                sb.AppendLine($"   ClassName       : {ISR.ClassName}{Environment.NewLine}");
                sb.AppendLine($"Exception Message(s):");
                sb.AppendLine($"{e}{Environment.NewLine}");
                throw new ArgumentException(sb.ToString());
            }
            return InstrumentsStationary;
        }

        private static Dictionary<String, Object> GetInstrumentsPortable() {
            InstrumentsPortableRequiredSection IPRSs = (InstrumentsPortableRequiredSection)ConfigurationManager.GetSection(InstrumentsPortableRequiredSection.ClassName);
            InstrumentsPortableRequired IPRs = IPRSs.InstrumentsPortableRequired;
            Dictionary<String, Object> Instruments = new Dictionary<String, Object>();
            foreach (InstrumentPortableRequired IPR in IPRs) try {
                Instruments.Add(IPR.ID, Activator.CreateInstance(Type.GetType(IPR.ClassName), new Object[] { IPR.Address, IPR.Detail }));
            } catch (Exception e) {
                StringBuilder sb = new StringBuilder().AppendLine();
                sb.AppendLine($"App.config issue with InstrumentPortableRequired:");
                sb.AppendLine($"   ID              : {IPR.ID}");
                sb.AppendLine($"   Detail          : {IPR.Detail}");
                sb.AppendLine($"   Address         : {IPR.Address}");
                sb.AppendLine($"   ClassName       : {IPR.ClassName}{Environment.NewLine}");
                sb.AppendLine($"Exception Message(s):");
                sb.AppendLine($"{e}{Environment.NewLine}");
                throw new ArgumentException(sb.ToString());
            }
            return Instruments;
        }
    }
}
