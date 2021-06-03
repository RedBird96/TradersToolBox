using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradersToolbox.DataSources
{
    public class StockFloatFileManager
    {
        public Dictionary<string, string> stockArr;

        public string FILENAME = "floats.csv";

        public void LoadFloatFile()
        {
            stockArr = new Dictionary<string, string>();
            string pathName = Path.Combine(Directory.GetCurrentDirectory(), FILENAME);

            if (!File.Exists(pathName))
                return;

            string[] allLines = File.ReadAllLines(pathName);

            foreach(string line in allLines)
            {
                string[] values = line.Split(',');
                int index = 0;
                foreach(string onevalue in values)
                {
                    double canConvertDouble = 0;
                    if (onevalue == "n/a" || Double.TryParse(onevalue, out canConvertDouble))
                        break;
                    index++;
                }

                string data = "";
                for (int i = index ; i < values.Length; i++)
                {
                    data += values[i];
                    data += ",";
                }
                stockArr[values[0]] = data;

            }
        }

        public string GetFloatValue(string symbol)
        {
            string floatVal = "---";
            if (stockArr.Count == 0)
                return floatVal;

            string data = stockArr[symbol];
            string[] datas = data.Split(',');
            if (datas[0] == "n/a" && datas[3] == "n/a" && datas[6] == "n/a" && datas[2] == "n/a" && datas[5] == "n/a" && datas[8] == "n/a")
                return floatVal;

            string strData = "";
            if (datas[0] == "n/a" || datas[2] == "n/a")
            {
                if (datas[3] == "n/a" || datas[5] == "n/a")
                {
                    if (datas[8] == "n/a")
                        return floatVal;
                    strData = datas[6];
                }
                else
                     strData = datas[3];
            }
            else
                strData = datas[0];

            double doublevalue;
            if (Double.TryParse(strData, out doublevalue))
            { 
                floatVal = doublevalue.ToString("N0", CultureInfo.InvariantCulture);
            }
            return floatVal;
        }

        public string GetShortValue(string symbol)
        {
            string shortVal = "---";
            if (stockArr.Count == 0)
                return shortVal;

            string data = stockArr[symbol];
            string[] datas = data.Split(',');
            if (datas[2] == "n/a" && datas[5] == "n/a" && datas[8] == "n/a" && datas[0] == "n/a" && datas[3] == "n/a" && datas[6] == "n/a")
                return shortVal;

            string strData = "";
            if (datas[2] == "n/a" || datas[0] == "n/a")
            {
                if (datas[5] == "n/a" || datas[3] == "n/a")
                {
                    if (datas[6] == "n/a")
                        return shortVal;
                    strData = datas[8];
                }
                else
                    strData = datas[5];
            }
            else
                strData = datas[2];

            double doublevalue;
            if (Double.TryParse(strData, out doublevalue))
            { 
                shortVal = (doublevalue * 100.0).ToString("N2", CultureInfo.InvariantCulture);
            }
            return shortVal;
        }
    }
}
