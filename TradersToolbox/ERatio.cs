using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Globalization;
using System.Collections;
using System.Linq;
using System.IO;
using TradersToolbox.Core;
using TradersToolbox.Core.Serializable;

namespace TradersToolbox
{
    class ERatio
    {
        private readonly float[] High;
        private readonly float[] Low;
        private readonly float[] Close;
        private readonly float[] ATR;
        readonly List<byte> Signal = new List<byte>();
        readonly List<byte> RandomSignal = new List<byte>();

        public ERatio(string file_name, BitArray signals, int startDate, int startTime, int stopDate, bool preCalcATR)
        {
            //todo: currency conversion??

            RAWdata sym = RAWdataStorage.GetRawData(file_name, new SymbolId("for ERatio",""));

            int firstIdx = sym.GetCopyInGivenDateTimes(startDate, startTime, stopDate, 240000, out _, out _, out _, out High, out Low, out Close, out _);

            // cut ending without signal
            if (High.Length > signals.Count)  High = High.Take(signals.Count).ToArray();
            if (Low.Length > signals.Count)   Low  = Low.Take(signals.Count).ToArray();
            if (Close.Length > signals.Count) Close = Close.Take(signals.Count).ToArray();

            //calc atr
            ATR = preCalcATR ? GenerateSignalsToTrade.ATR_Func(sym.Highs, sym.Lows, sym.Closes, 10).Skip(firstIdx).Take(Close.Length).ToArray() :
                GenerateSignalsToTrade.ATR_Func(High, Low, Close, 10);

            Random rand = new Random();
            int randomNum = rand.Next(20) + 2;
            int counter = 0; // counter for random signal

            for (int i = 0; i < Close.Length; ++i)
            {
                Signal.Add(i < signals.Count && signals.Get(i) ? (byte)1 : (byte)0);
                ++counter;
                if (counter == randomNum) { RandomSignal.Add(1); counter = 0; }
                else RandomSignal.Add(0);
            }

            //test writer
            //File.WriteAllText("ERatio.txt", string.Join(Environment.NewLine, Signal));
        }

        public void Update(bool isLong, out List<float> ERatios, out List<float> randERatios)
        {
            ERatios = new List<float>();
            randERatios = new List<float>();

            if (Close == null || Close.Length < 1 || ATR == null) return;
            
            List<float> highs = new List<float>();
            List<float> lows = new List<float>();
            List<float> MFE = new List<float>();
            List<float> MAE = new List<float>();
            List<float> rhighs = new List<float>();
            List<float> rlows = new List<float>();
            List<float> rMFE = new List<float>();
            List<float> rMAE = new List<float>();

            for (int n = 1; n <= 30; n++)
            {
            	// Reset MFE and MAE for next n
            	MFE.Clear();
            	MAE.Clear();
            	rMFE.Clear();
            	rMAE.Clear();
                for (int i = 10; i < Close.Length; i++)
                { // 10 is atr lookback
                    if (Signal[i] == 1 && i + n < Close.Length)
                    {
                        // clear highs and lows
                        highs.Clear();
                        lows.Clear();

                        // collect highs and lows for next j bars
                        for (int j = 1; j <= n; j++)
                        {
                            highs.Add(High[i + j]);
                            lows.Add(Low[i + j]);
                        }

                        // find max and min
                        float max = highs.Max();
                        float min = lows.Min();
                       
                       	float mfe, mae;
                       	if (isLong) {
                        	mfe = Math.Max((max - Close[i]) / ATR[i], 0);
                        	mae = Math.Max((Close[i] - min) / ATR[i], 0);

						} else {
							//short
							mfe = Math.Max((Close[i] - min) / ATR[i], 0);
							mae = Math.Max((max - Close[i]) / ATR[i], 0);
						}
						
                        MFE.Add(mfe);
                        MAE.Add(mae);
                    }
                    // -----------------------------
                    //  FOR RANDOM SIGNAL 
                    if (RandomSignal[i] == 1 && i + n < Close.Length)
                    {

                        // clear highs and lows
                        rhighs.Clear();
                        rlows.Clear();

                        // collect highs and lows for next j bars
                        for (int j = 1; j <= n; j++)
                        {
                            rhighs.Add(High[i + j]);
                            rlows.Add(Low[i + j]);
                        }

                        // find max and min
                        float rmax = rhighs.Max();
                        float rmin = rlows.Min();
                       
                       	float rmfe, rmae;
                       	
                       	if (isLong) {
                        	rmfe = Math.Max((rmax - Close[i]) / ATR[i], 0);
                        	rmae = Math.Max((Close[i] - rmin) / ATR[i], 0);
                    	} else {
					
                        	// short
							rmfe = Math.Max((Close[i] - rmin) / ATR[i], 0);
							rmae = Math.Max((rmax - Close[i]) / ATR[i], 0);
						}

                        rMFE.Add(rmfe);
                        rMAE.Add(rmae);
                    }
                }

                // final eRatio and Random Ratio
                if (MFE.Count > 0) ERatios.Add(MFE.Average() / MAE.Average());
                if(rMFE.Count > 0) randERatios.Add(rMFE.Average() / rMAE.Average());
            }
        }
    }
}