using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TradersToolbox.Core.Serializable
{
    //todo: unit test to load/save/copy configuration

    [DataContract]
    class Configuration
    {
        [DataMember]
        public string SymbolsXML = string.Empty;        
        [DataMember]
        public string SimConfigXML = string.Empty;


        [DataMember]
        public decimal MCDD_Iterations = 1000;
        [DataMember]
        public decimal MCDD_AcctSize = 500;
        [DataMember]
        public decimal VT_Iterations = 1000;
        [DataMember]
        public decimal VT_Trades = 1000;
        [DataMember]
        public decimal VT_Variations = 0.04M;
        [DataMember]
        public decimal VT_Ruin = 10000;
        [DataMember]
        public decimal MCE_ToPick = 20;
        [DataMember]
        public decimal MCE_Iterations = 1000;
        [DataMember]
        public decimal MCE_Lower = 5;
        [DataMember]
        public decimal MCE_Upper = 95;

        [DataMember]
        public decimal NoiseTestOpen = 40;
        [DataMember]
        public decimal NoiseTestHigh = 40;
        [DataMember]
        public decimal NoiseTestLow = 40;
        [DataMember]
        public decimal NoiseTestClose = 40;
        [DataMember]
        public decimal NoiseTestMax = 40;
        [DataMember]
        public decimal NoiseTestCount = 100;

        [DataMember]
        public string VsOther1Symbol = string.Empty;
        [DataMember]
        public string VsOther2Symbol = string.Empty;
        [DataMember]
        public string VsOther3Symbol = string.Empty;

        [DataMember]
        public bool TTmaxPNLEnabled = false;
        [DataMember]
        public decimal TTmaxPNLstart = 0;
        [DataMember]
        public decimal TTmaxPNLend = 0;
        [DataMember]
        public decimal TTmaxPNLstep = 100;
        [DataMember]
        public bool TTminPNLenabled = false;
        [DataMember]
        public decimal TTminPNLstart = 0;
        [DataMember]
        public decimal TTminPNLend = 0;
        [DataMember]
        public decimal TTminPNLstep = 100;
        [DataMember]
        public bool TTmaxTradesEnabled = false;
        [DataMember]
        public decimal TTmaxTradesStart = 0;
        [DataMember]
        public decimal TTmaxTradesEnd = 0;
        [DataMember]
        public decimal TTmaxTradesStep = 1;
        [DataMember]
        public bool TTstartTimeEnabled = false;
        [DataMember]
        public DateTime TTstartTimeStart = new DateTime();
        [DataMember]
        public DateTime TTstartTimeEnd = new DateTime();
        [DataMember]
        public DateTime TTstartTimeStep = new DateTime(1,1,1,1,0,0);    //1 hour
        [DataMember]
        public bool TTendTimeEnabled = false;
        [DataMember]
        public DateTime TTendTimeStart = new DateTime();
        [DataMember]
        public DateTime TTendTimeEnd = new DateTime();
        [DataMember]
        public DateTime TTendTimeStep = new DateTime(1, 1, 1, 1, 0, 0);    //1 hour
        [DataMember]
        public bool TTaddCode = false;
        
        [DataMember]
        public DateTime IntraEndOfDay = new DateTime(1, 1, 1, 17, 0, 0);    //17:00
        [DataMember]
        public string IntraSymbol = string.Empty;
        [DataMember]
        public bool IntraExcludeExits = false;

        [DataMember]
        public System.Collections.Specialized.StringCollection FavoriteSignals = null;
        [DataMember]
        public System.Collections.Specialized.StringCollection CustomStrategies = null;

        [DataMember]
        public string SignalsXML = null;

        public void CopyFromAppSettings()
        {
            foreach (var f in GetType().GetFields())
            {
                if (f.Name == nameof(SimConfigXML))
                    f.SetValue(this, TradersToolbox.Properties.Settings.Default.SimConfigs?.Count > 0 ? TradersToolbox.Properties.Settings.Default.SimConfigs[0] : string.Empty);
                else
                    f.SetValue(this, TradersToolbox.Properties.Settings.Default[f.Name]);
            }
        }

        public void CopyToAppSettings()
        {
            //ensure symbols are updated first
            TradersToolbox.Properties.Settings.Default.SymbolsXML = SymbolsXML;

            foreach (var f in GetType().GetFields())
            {
                if(f.Name == nameof(SimConfigXML))
                {
                    if (f.GetValue(this) is string s && !string.IsNullOrEmpty(s))
                    {
                        TradersToolbox.Properties.Settings.Default.SimConfigs = new System.Collections.Specialized.StringCollection() { s };
                    }
                    else
                        TradersToolbox.Properties.Settings.Default.SimConfigs = null;
                }
                else if (f.Name != nameof(SymbolsXML))  //skip symbols
                    TradersToolbox.Properties.Settings.Default[f.Name] = f.GetValue(this);
            }
        }

        public static Configuration Load(string filename)
        {
            try
            {
                string xml = System.IO.File.ReadAllText(filename);
                return (Configuration)Serializer.Deserialize(xml, typeof(Configuration));
            }
            catch(Exception ex)
            {
                Logger.Current.Warn("{0}.{1}(): Can't load configuration! {2}", nameof(Configuration), nameof(Load), ex.Message);
                return null;
            }
        }

        public bool Save(string filename)
        {
            try
            {
                string xml = Serializer.Serialize(this);
                if (string.IsNullOrEmpty(xml) == false)
                    System.IO.File.WriteAllText(filename, xml);
                else
                    throw new Exception("XML is empty");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Current.Warn("{0}.{1}(): Can't save configuration! {2}", nameof(Configuration), nameof(Save), ex.Message);
                return false;
            }
        }
    }
}
