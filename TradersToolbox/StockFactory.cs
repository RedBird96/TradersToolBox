using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradersToolbox.Data;

namespace StockPatternSearch
{
    public class DTPoint
    {
        public int date;
        public int time;
        public double price;

        public DTPoint(int _date, int _time, double _price)
        {
            date = _date;
            time = _time;
            price = _price;
        }
    }

    public enum PatternStyle
    {
        DoubleTop = 0,
        DoubleBottom = 1,
        RisingWedge = 2,
        FallingWedge = 3,
        BroadeningTop = 4,
        BroadeningBottom = 5,
        BullPennant = 6,
        BearPennant = 7,
        BullTriangle = 8,
        BearTriangle = 9,
        BullFlag = 10,
        BearFlag = 11,
        HeadAndShoulders = 12,
        InvHeadAndShoulders = 13,
        CupAndHandle = 14,
        TotalPatterns = 15
    };

    public class PatternResult
    {
        public PatternStyle pattern;
        public List<DTPoint> trendLines;

        public PatternResult()
        {
            pattern = PatternStyle.DoubleTop;
            trendLines = new List<DTPoint>();
        }
    }

    public class StockFactory
    {
        public StockFactory()
        {

        }

        public static double SwingHigh(List<TradingData> data, int currentbar, int strength, int length, out int bar)
        {
            double ret = -1;
            bar = -1;

            for (int loop = strength; loop <= length - 1; loop++)
            {
                bool check = true;
                for (int i = 1; i <= strength; i++)
                {
                    if (currentbar - loop - i >= 0 && data[currentbar - loop - i].High <= data[currentbar - loop].High) continue;
                    check = false;
                    break;
                }
                if (check)
                {
                    for (int i = 1; i <= strength; i++)
                    {
                        if (currentbar - loop + i >= 0 && data[currentbar - loop + i].High < data[currentbar - loop].High) continue;
                        check = false;
                        break;
                    }
                }

                if (check)
                {
                    ret = data[currentbar - loop].High;
                    bar = loop;
                    break;
                }
            }

            return ret;
        }
        public static double SwingLow(List<TradingData> data, int currentbar, int strength, int length, out int bar)
        {
            double ret = -1;
            bar = -1;

            for (int loop = strength; loop <= length - 1; loop++)
            {
                bool check = true;
                for (int i = 1; i <= strength; i++)
                {
                    if (currentbar - loop - i >= 0 && data[currentbar - loop - i].Low >= data[currentbar - loop].Low) continue;
                    check = false;
                    break;
                }
                if (check)
                {
                    for (int i = 1; i <= strength; i++)
                    {
                        if (currentbar - loop + i >= 0 && data[currentbar - loop + i].Low > data[currentbar - loop].Low) continue;
                        check = false;
                        break;
                    }
                }

                if (check)
                {
                    ret = data[currentbar - loop].Low;
                    bar = loop;
                    break;
                }
            }

            return ret;
        }
        public static int FindIntersection(double X1, double Y1, double X2, double Y2, double A1, double B1, double A2, double B2)
        {
            int barout;
            double dx, dy, da, db, tt;

            dx = X2 - X1;
            dy = Y2 - Y1;
            da = A2 - A1;
            db = B2 - B1;
            if (Math.Abs(da * dy - db * dx) < 0.001) return 0;
            tt = (da * (Y1 - B1) + db * (A1 - X1)) / (db * dx - da * dy);
            //barout = IntPortion(X1 + tt * dx);
            barout = Convert.ToInt32(X1 + tt * dx);

            return barout;
        }
        public static double iff(bool check, double left, double right)
        {
            return check ? left : right;
        }
        public static int iff(bool check, int left, int right)
        {
            return check ? left : right;
        }
        public static string iffstring(bool check, string left, string right)
        {
            return check ? left : right;
        }
        public static bool ifflogic(bool check, bool left, bool right)
        {
            return check ? left : right;
        }
        public static double MinList(double a, double b)
        {
            return a > b ? b : a;
        }
        public static int MinList(int a, int b)
        {
            return a > b ? b : a;
        }
        public static double MaxList(double a, double b)
        {
            return a > b ? a : b;
        }
        public static int MaxList(int a, int b)
        {
            return a > b ? a : b;
        }
        public static decimal MaxList(decimal a, decimal b)
        {
            return a > b ? a : b;
        }
        public static decimal MinList(decimal a, decimal b)
        {
            return a > b ? b : a;
        }

        public static double Square(double a)
        {
            return a * a;
        }
        public static double PFR_Zinv(double p)
        {
            double denom = 0, numer = 0, ptemp = 0, z = 0;
            double C0 = 2.515517, C1 = 0.802853, C2 = 0.010328;
            double D1 = 1.432788, D2 = 0.189269, D3 = 0.001308;

            ptemp = p;
            if (p > 0.5) ptemp = 1.0 - p;
            z = Math.Sqrt(Math.Abs(-2.0 * Math.Log(MaxList(0.000001, ptemp))));
            numer = C0 + z * (C1 + z * C2);
            denom = 1.0 + z * (D1 + z * (D2 + z * D3));
            z = z - (numer / denom);
            if (p <= 0.5) z = -z;

            return z;
        }

        public static double PFR_MedianArray(List<double> PriceArray, int Length)
        {
            if (Length == 0) return 0;

            PriceArray.Sort();
            if (Length % 2 == 0)
                return (PriceArray[Length / 2 - 1] + PriceArray[Length / 2]) / 2;
            else
                return PriceArray[Length / 2];
        }

        public static double PFR_MedTrueRange(List<TradingData> data, int currentbar, int Length, int EvalBar)
        {
            double atr = 0;
            List<double> tr = new List<double>();

            if (Length > 0 && currentbar >= Length)
            {
                for (int k = 0; k < Length; k++)
                {
                    tr.Add(MaxList(Convert.ToDouble(data[currentbar - (k + 1 + EvalBar)].Close), Convert.ToDouble(data[currentbar - (k + EvalBar)].High)) -
                                MinList(Convert.ToDouble(data[currentbar - (k + 1 + EvalBar)].Close), Convert.ToDouble(data[currentbar - (k + EvalBar)].Low)));
                }
                atr = PFR_MedianArray(tr, Length);
            }
            return atr;
        }

        public static double PFR_ExtremePrice(List<TradingData> data, int currentbar, bool UseLog) //length = 2
        {
            double Tick = 1 / 32;

            double LL = iff(currentbar > 2, MinList(data[currentbar].Low, data[currentbar - 1].Low) - Tick, data[currentbar].Low - Tick);
            double HH = iff(currentbar > 2, MaxList(data[currentbar].High, data[currentbar - 1].High) + Tick, data[currentbar].High + Tick);
            double ratio = MaxList(1.0, iff(HH >= LL && LL > 0, HH / LL, 1.0));
            if (UseLog)
                return Math.Log(Math.Abs(ratio));
            else
                return ratio;
        }

        public static double PFR_BarAnnualization(int BarType, int BarInterval)
        {
            if (BarType == 4)
            {
                return Math.Sqrt(12);
            }
            else if (BarType == 3)
            {
                return Math.Sqrt(52);
            }
            else if (BarType == 2)
            {
                return Math.Sqrt(252);
            }
            else if (BarType == 1)
            {
                return Math.Sqrt(365 * 24 * 60 * 0.690411 * 0.270833 / MaxList(1, BarInterval));
            }
            return Math.Sqrt(252);

        }

        public static double PFR_VariancePS(List<double> prices, int length, int datatype = 2)
        {
            double Divisor = 0, Mean = 0, SumSqr = 0, Len = 0, ret = 0;
            Divisor = iff(datatype == 1, length, length - 1);
            if (Divisor > 0)
            {
                Mean = 0;
                Len = 0;
                for (int i = 0; i < length; i++)
                {
                    Mean = Mean + prices[i];
                    Len = Len + 1;
                }

                Mean = Mean / iff(Len == 0, 1, Len);
                SumSqr = 0;
                for (int i = 0; i < length; i++)
                {
                    SumSqr = SumSqr + Square(prices[i] - Mean);
                }
                ret = SumSqr / Divisor;
            }
            return ret;
        }
        public static double PFR_StandardDev(List<double> prices, int length, int datatype = 2)
        {
            double Value1 = PFR_VariancePS(prices, length, datatype);
            if (Value1 > 0)
                return Math.Sqrt(Value1);
            else
                return 0;
        }
        public static double PFR_PriceAtZ(List<TradingData> data, int currentbar, double ZTarget, double CurrentPrice, int VoltyLength, int BarsToGo, double UpProbability, int Lag = 0)
        {
            double Vlty = 0, VoltySDev = 0;

            int BarType = 2, BarInterval = 0;
            if (data[1].DateInt == data[0].DateInt)
            {
                BarType = 1;
                BarInterval = ((data[1].Time / 100) - (data[0].Time / 100)) * 60 + ((data[1].Time % 100) - (data[0].Time % 100));
            }

            if (currentbar > VoltyLength)
            {
                List<double> prices = new List<double>();
                for (int i = 0; i < VoltyLength; i++)
                {
                    prices.Add(PFR_ExtremePrice(data, currentbar - i, true));
                }
                VoltySDev = PFR_StandardDev(prices, VoltyLength, 2) * PFR_BarAnnualization(BarType, BarInterval);
            }
            if (MinList(CurrentPrice, VoltySDev) <= 0 || BarsToGo <= 0)
            {
                return CurrentPrice;
            }
            else
            {
                Vlty = VoltySDev * Math.Sqrt(BarsToGo / Square(PFR_BarAnnualization(BarType, BarInterval)));
                return CurrentPrice * Math.Exp(Vlty * (ZTarget - PFR_Zinv(1 - UpProbability)));
            }
        }

        public static DateTime IntToDate(int dateInt, int time)
        {
            int month = (dateInt % 10000) / 100;
            int day = dateInt % 100;
            int year = dateInt / 10000;
            DateTime res = new DateTime(year, month, day);
            res = res.AddHours(time / 100);
            res = res.AddMinutes(time % 100);
            return res;
        }

        public static int DateToInt(DateTime date)
        {
            int month = date.Month;
            int day = date.Day;
            int year = date.Year;
            return year * 10000 + month * 100 + day;
        }
        public static String TimeToStr(int time)
        {
            return (time / 100).ToString().PadLeft(2, '0') + ":" + (time % 100).ToString().PadLeft(2, '0');
        }
        public static void PFR_CalcForwardDate(List<TradingData> data, int currentbar, int patternlength, int SignalDate, int SignalTime, out int SignalEndDate, out int SignalEndTime)
        {
            int i;
            SignalEndDate = SignalDate;
            SignalEndTime = SignalTime;

            if (patternlength == 0) return;
            for (i = currentbar; i >= 0; i--)
            {
                if (data[i].DateInt > SignalDate) continue;
                if (data[i].DateInt == SignalDate && data[i].Time > SignalTime) continue;
                break;
            }
            if (i + patternlength < data.Count)
            {
                SignalEndDate = data[i + patternlength].DateInt;
                SignalEndTime = data[i + patternlength].Time;
            }
            else
            {
                SignalEndDate = data[data.Count - 1].DateInt;
                SignalEndTime = data[data.Count - 1].Time;
            }
        }

        public static List<PatternResult> SearchPattern(List<TradingData> data, double RetraceZ = 1, double Retrace_Percent_Override = -1,
            bool Use_Volume = true, int Min_PatternWidthBars = 10, int Max_PatternWidthBars = 1000, double Loosen_Percent = 0, int SwingStrength = 1)
        {
            if (data == null || data.Count < 2)
            {
                return null;
            }

            List<PatternResult> results = new List<PatternResult>();

            double NewSwingPrice = 0, SwingPrice = Convert.ToDouble(data[0].Close);
            int TLDir = 0, PrevTLDir = 0;
            bool SaveSwing = false;
            double pi = 3.14159265358979323846264338327950288;

            List<int> PivotDate = new List<int>();
            List<int> PivotTime = new List<int>();
            List<Double> PivotPrice = new List<double>();
            List<int> PivotBar = new List<int>();
            List<int> PivotDir = new List<int>();

            int BarType = 2, BarInterVal = 0;
            if (data[1].DateInt == data[0].DateInt)
            {
                BarType = 1;
                BarInterVal = ((data[1].Time / 100) - (data[0].Time / 100)) * 60 + ((data[1].Time % 100) - (data[0].Time % 100));
            }
            int npivots = 0;
            double colinearlimit, parallellimit, necklimit, wedgelimit, doubletoplimit, channellimit;
            colinearlimit = Math.Cos(pi / 180 * 4 * (1 + Loosen_Percent * 0.01));
            parallellimit = Math.Cos(pi / 180 * 3 * (1 + Loosen_Percent * 0.01));
            channellimit = Math.Cos(pi / 180 * 0.5 * (1 + Loosen_Percent * 0.01));
            necklimit = Math.Cos(pi / 180 * 1 * (1 + Loosen_Percent * 0.01));
            wedgelimit = Math.Sin(pi / 180 * 3 * (1 + Loosen_Percent * 0.01));
            doubletoplimit = Math.Cos(pi / 180 * 1 * (1 + Loosen_Percent * 0.01));

            for (int currentbar = 1; currentbar < data.Count; currentbar++)
            {
                bool SignalFound = false;
                int RetraceBars = 2;

                double close = data[currentbar].Close;
                double high = data[currentbar].High;
                double low = data[currentbar].Low;

                String SignalDir = "", SignalName = "";
                double atr = PFR_MedTrueRange(data, currentbar, 21, 0);
                double AffineScale = Math.Sqrt(atr);
                double RetracePnts = iff(Retrace_Percent_Override <= 0,
                    PFR_PriceAtZ(data, currentbar, MaxList(0.1, Math.Abs(RetraceZ)), close, 21, RetraceBars, 0.5) - close,
                    Retrace_Percent_Override * 0.01 * close);

                //-------------------------------------------------------------------------------------
                //									Form Pivots
                //-------------------------------------------------------------------------------------
                int shb = 0;
                NewSwingPrice = SwingHigh(data, currentbar, SwingStrength, 2 * SwingStrength, out shb);
                int NewSwingBar = iff(shb != -1, currentbar - shb, -1);
                int NewSwingDate = shb != -1 ? DateToInt(data[currentbar - shb].Date) : -1;
                int NewSwingTime = shb != -1 ? data[currentbar - shb].Time : -1;
                if (NewSwingPrice != -1)
                {
                    if (TLDir <= 0 && NewSwingPrice >= SwingPrice + RetracePnts)
                    {
                        SaveSwing = true;
                        TLDir = 1;
                    }
                    else if (TLDir == 1 && NewSwingPrice >= SwingPrice)
                    {
                        SaveSwing = true;
                    }
                }
                else
                {
                    NewSwingPrice = SwingLow(data, currentbar, SwingStrength, 2 * SwingStrength, out shb);
                    NewSwingBar = iff(shb != -1, currentbar - shb, -1);
                    NewSwingDate = shb != -1 ? DateToInt(data[currentbar - shb].Date) : -1;
                    NewSwingTime = shb != -1 ? data[currentbar - shb].Time : -1;
                    if (NewSwingPrice != -1)
                    {
                        if (TLDir >= 0 && NewSwingPrice <= SwingPrice - RetracePnts)
                        {
                            SaveSwing = true;
                            TLDir = -1;
                        }
                        else if (TLDir == -1 && NewSwingPrice <= SwingPrice)
                        {
                            SaveSwing = true;
                        }
                    }
                }

                //---------------- Save Pivots & Update ZigZag plot
                int SwingBar = 0, SwingDate = DateToInt(data[currentbar].Date), SwingTime = data[currentbar].Time;
                bool updatedNPivots = false;
                if (SaveSwing)
                {
                    // save new swing and reset SaveSwing }	
                    SwingBar = NewSwingBar;
                    SwingDate = NewSwingDate;
                    SwingTime = NewSwingTime;
                    if (TLDir != PrevTLDir)
                    {
                        npivots = npivots + 1;
                        PivotPrice.Add(0);
                        PivotBar.Add(0);
                        PivotDir.Add(0);
                        PivotDate.Add(0);
                        PivotTime.Add(0);
                        updatedNPivots = true;
                        PrevTLDir = TLDir;
                    }
                    PivotPrice[npivots - 1] = NewSwingPrice;
                    PivotBar[npivots - 1] = NewSwingBar;
                    PivotDir[npivots - 1] = TLDir;
                    PivotDate[npivots - 1] = NewSwingDate;
                    PivotTime[npivots - 1] = NewSwingTime;
                    SwingPrice = NewSwingPrice;
                    SaveSwing = false;

                }


                //-------------------------------------------------------------------------------------
                //							Technical Analysis Geometry
                //-------------------------------------------------------------------------------------
                double SignalPrice = 0, SignalStop = 0, SignalTarget = 0, Value1 = 0;
                int SignalEndDate = 0, SignalEndTime = 0;

                int j = 0;
                int WWb = 0, XXb = 0, AAb = 0, BBb = 0, CCb = 0, DDb = 0, EEb = 0, YYb = 0;
                double WWp = 0, XXp = 0, AAp = 0, BBp = 0, CCp = 0, DDp = 0, EEp = 0, YYp = 0;
                int WWd = 0, XXd = 0, AAd = 0, BBd = 0, CCd = 0, DDd = 0, EEd = 0, YYd = 0, SignalDate = 0, SignalTime = 0;
                int WWt = 0, XXt = 0, AAt = 0, BBt = 0, CCt = 0, DDt = 0, EEt = 0, YYt = 0;
                int Wdir = 0, Xdir = 0, Adir = 0, Bdir = 0, Cdir = 0, Ddir = 0, Edir = 0, Ydir = 0;
                bool ACEPivotsABove = false;

                double XWb = 0, XWp = 0, XWWb = 0, XWn = 0, nXWb = 0, nXWp = 0;
                double AXb = 0, AXp = 0, AXXb = 0, AXn = 0, nAXb = 0, nAXp = 0;
                double BAb = 0, BAp = 0, BAAb = 0, BAn = 0, nBAb = 0, nBAp = 0;
                double CBb = 0, CBp = 0, CBn = 0, CBBb = 0, nCBb = 0, nCBp = 0;
                double DCb = 0, DCp = 0, DCCb = 0, DCn = 0, nDCb = 0, nDCp = 0;
                double EDb = 0, EDp = 0, EDDb = 0, EDn = 0, nEDb = 0, nEDp = 0;
                double YEb = 0, YEp = 0, YEEb = 0, YEn = 0, nYEb = 0, nYEp = 0;
                double BXb = 0, BXp = 0, BXXb = 0, BXn = 0, nBXb = 0, nBXp = 0;
                double DBb = 0, DBp = 0, DBBb = 0, DBn = 0, nDBb = 0, nDBp = 0;
                double YDb = 0, YDp = 0, YDDb = 0, YDn = 0, nYDb = 0, nYDp = 0;
                double CAb = 0, CAp = 0, CAAb = 0, CAn = 0, nCAb = 0, nCAp = 0;
                double ECb = 0, ECp = 0, ECCb = 0, ECn = 0, nECb = 0, nECp = 0;
                double EAb = 0, EAp = 0, EAAb = 0, EAn = 0, nEAb = 0, nEAp = 0;
                double EWb = 0, EWp = 0, EWWb = 0, EWn = 0, nEWb = 0, nEWp = 0;
                double YBp = 0, YBb = 0, YBBb = 0, YBn = 0, nYBb = 0, nYBp = 0;
                double XWv = 0, nXWv = 0, AXv = 0, nAXv = 0, BAv = 0, nBAv = 0, CBv = 0, nCBv = 0, DCv = 0, nDCv = 0, EDv = 0, nEDv = 0, YEv = 0, nYEv = 0;
                double XBDc = 0, BDYc = 0, ACEc = 0, CAoEC = 0, BXoDB = 0;
                double ECoYD = 0, DCoYE = 0, BXoCA = 0, DBoEC = 0;
                double ECxYD = 0, EAxYB = 0, BXxCA = 0, AXxCB = 0, BAxDC = 0, DXxCA = 0, nDXb = 0, nDXp = 0;

                double patternlength = 0, patternheight = 0;
                bool similartops = false, M_like = false, rising = false, doubleprices = false, volpattern = false, DoubleTop = false;
                bool similarbots = false, W_like = false, falling = false, DoubleBot = false;
                bool contiguous = false, wedgelike = false, decliningvolume = false, RisingWedge = false, FallingWedge = false;
                bool broadeninglike = false, BroadTop = false, advancingvolume = false, BroadBot = false;
                double CCadj = 0, YYadj = 0, begwidth = 0, endwidth = 0;
                bool flagpole = false, pennantlike = false, BullPennant = false, BearPennant = false;
                double AAadj = 0, DDadj = 0, new_upper_p = 0, new_lower_p = 0;
                int apex_bar = 0;
                bool descending = false, ascending = false, BullTriangle = false, BearTriangle = false;
                bool flaglike = false, BullFlag = false, BearFlag = false;
                double cupthickness = 0, handleheight = 0;
                bool cuplike = false, handlelike = false, CupAndHandle = false;
                bool neckline = false, HS_like = false, HeadAndShoulders = false, InvHeadAndShoulders = false;
                int apex_date = 0, ForwardDate = 0, apex_time = 0, ForwardTime = 0;
                int LastCalcDate = DateToInt(data[currentbar - 1].Date);
                int LastCalcTime = data[currentbar - 1].Time;

                if (updatedNPivots && npivots >= 8)
                {
                    j = npivots - 1;

                    //Bars
                    WWb = PivotBar[j - 7];
                    XXb = PivotBar[j - 6];
                    AAb = PivotBar[j - 5];
                    BBb = PivotBar[j - 4];
                    CCb = PivotBar[j - 3];
                    DDb = PivotBar[j - 2];
                    EEb = PivotBar[j - 1];
                    YYb = PivotBar[j];
                    // prices
                    WWp = PivotPrice[j - 7];
                    XXp = PivotPrice[j - 6];
                    AAp = PivotPrice[j - 5];
                    BBp = PivotPrice[j - 4];
                    CCp = PivotPrice[j - 3];
                    DDp = PivotPrice[j - 2];
                    EEp = PivotPrice[j - 1];
                    YYp = PivotPrice[j];
                    // dates
                    WWd = PivotDate[j - 7];
                    XXd = PivotDate[j - 6];
                    AAd = PivotDate[j - 5];
                    BBd = PivotDate[j - 4];
                    CCd = PivotDate[j - 3];
                    DDd = PivotDate[j - 2];
                    EEd = PivotDate[j - 1];
                    YYd = PivotDate[j];
                    SignalDate = DateToInt(data[currentbar].Date);
                    // times
                    WWt = PivotTime[j - 7];
                    XXt = PivotTime[j - 6];
                    AAt = PivotTime[j - 5];
                    BBt = PivotTime[j - 4];
                    CCt = PivotTime[j - 3];
                    DDt = PivotTime[j - 2];
                    EEt = PivotTime[j - 1];
                    YYt = PivotTime[j];
                    SignalTime = data[currentbar].Time;

                    // directions
                    Wdir = PivotDir[j - 7];
                    Xdir = PivotDir[j - 6];
                    Adir = PivotDir[j - 5];
                    Bdir = PivotDir[j - 4];
                    Cdir = PivotDir[j - 3];
                    Ddir = PivotDir[j - 2];
                    Edir = PivotDir[j - 1];
                    Ydir = PivotDir[j];
                    ACEPivotsABove = Cdir == 1;

                    // ---------------- normalized line vectors
                    // XW

                    XWb = XXb - WWb;
                    XWp = XXp - WWp;
                    XWWb = XWb * AffineScale;
                    XWn = Math.Sqrt(XWWb * XWWb + XWp * XWp);
                    nXWb = XWWb / iff(XWn > 0, XWn, 1);
                    nXWp = XWp / iff(XWn > 0, XWn, 1);

                    // AX
                    AXb = AAb - XXb;
                    AXp = AAp - XXp;
                    AXXb = AXb * AffineScale;
                    AXn = Math.Sqrt(AXXb * AXXb + AXp * AXp);
                    nAXb = AXXb / iff(AXn > 0, AXn, 1);
                    nAXp = AXp / iff(AXn > 0, AXn, 1);

                    // BA
                    BAb = BBb - AAb;
                    BAp = BBp - AAp;
                    BAAb = BAb * AffineScale;
                    BAn = Math.Sqrt(BAAb * BAAb + BAp * BAp);
                    nBAb = BAAb / iff(BAn > 0, BAn, 1);
                    nBAp = BAp / iff(BAn > 0, BAn, 1);

                    // CB
                    CBb = CCb - BBb;
                    CBp = CCp - BBp;
                    CBBb = CBb * AffineScale;
                    CBn = Math.Sqrt(CBBb * CBBb + CBp * CBp);
                    nCBb = CBBb / iff(CBn > 0, CBn, 1);
                    nCBp = CBp / iff(CBn > 0, CBn, 1);

                    // DC
                    DCb = DDb - CCb;
                    DCp = DDp - CCp;
                    DCCb = DCb * AffineScale;
                    DCn = Math.Sqrt(DCCb * DCCb + DCp * DCp);
                    nDCb = DCCb / iff(DCn > 0, DCn, 1);
                    nDCp = DCp / iff(DCn > 0, DCn, 1);

                    // ED
                    EDb = EEb - DDb;
                    EDp = EEp - DDp;
                    EDDb = EDb * AffineScale;
                    EDn = Math.Sqrt(EDDb * EDDb + EDp * EDp);
                    nEDb = EDDb / iff(EDn > 0, EDn, 1);
                    nEDp = EDp / iff(EDn > 0, EDn, 1);

                    // YE		
                    YEb = YYb - EEb;
                    YEp = YYp - EEp;
                    YEEb = YEb * AffineScale;
                    YEn = Math.Sqrt(YEEb * YEEb + YEp * YEp);
                    nYEb = YEEb / iff(YEn > 0, YEn, 1);
                    nYEp = YEp / iff(YEn > 0, YEn, 1);

                    // ---------------- normalized exterior vectors		
                    // BX

                    BXb = BBb - XXb;
                    BXp = BBp - XXp;
                    BXXb = BXb * AffineScale;
                    BXn = Math.Sqrt(BXXb * BXXb + BXp * BXp);
                    nBXb = BXXb / iff(BXn > 0, BXn, 1);
                    nBXp = BXp / iff(BXn > 0, BXn, 1);

                    // DB
                    DBb = DDb - BBb;
                    DBp = DDp - BBp;
                    DBBb = DBb * AffineScale;
                    DBn = Math.Sqrt(DBBb * DBBb + DBp * DBp);
                    nDBb = DBBb / iff(DBn > 0, DBn, 1);
                    nDBp = DBp / iff(DBn > 0, DBn, 1);

                    // YD
                    YDb = YYb - DDb;
                    YDp = YYp - DDp;
                    YDDb = YDb * AffineScale;
                    YDn = Math.Sqrt(YDDb * YDDb + YDp * YDp);
                    nYDb = YDDb / iff(YDn > 0, YDn, 1);
                    nYDp = YDp / iff(YDn > 0, YDn, 1);

                    // CA
                    CAb = CCb - AAb;
                    CAp = CCp - AAp;
                    CAAb = CAb * AffineScale;
                    CAn = Math.Sqrt(CAAb * CAAb + CAp * CAp);
                    nCAb = CAAb / iff(CAn > 0, CAn, 1);
                    nCAp = CAp / iff(CAn > 0, CAn, 1);

                    // EC
                    ECb = EEb - CCb;
                    ECp = EEp - CCp;
                    ECCb = ECb * AffineScale;
                    ECn = Math.Sqrt(ECCb * ECCb + ECp * ECp);
                    nECb = ECCb / iff(ECn > 0, ECn, 1);
                    nECp = ECp / iff(ECn > 0, ECn, 1);

                    // EA
                    EAb = EEb - AAb;
                    EAp = EEp - AAp;
                    EAAb = EAb * AffineScale;
                    EAn = Math.Sqrt(EAAb * EAAb + EAp * EAp);
                    nEAb = EAAb / iff(EAn > 0, EAn, 1);
                    nEAp = EAp / iff(EAn > 0, EAn, 1);

                    // EW
                    EWb = EEb - WWb;
                    EWp = EEp - WWp;
                    EWWb = EWb * AffineScale;
                    EWn = Math.Sqrt(EWWb * EWWb + EWp * EWp);
                    nEWb = EWWb / iff(EWn > 0, EWn, 1);
                    nEWp = EWp / iff(EWn > 0, EWn, 1);
                    // YB	
                    YBb = YYb - BBb;
                    YBp = YYp - BBp;
                    YBBb = YBb * AffineScale;
                    YBn = Math.Sqrt(YBBb * YBBb + YBp * YBp);
                    nYBb = YBBb / iff(YBn > 0, YBn, 1);
                    nYBp = YBp / iff(YBn > 0, YBn, 1);

                    // volume sums on legs
                    XWv = 0;
                    for (j = WWb; j <= XXb; j++)
                    {
                        XWv = XWv + data[j].Volume;
                    }
                    nXWv = iff(XWb > 0, XWv / XWb, XWv);
                    AXv = 0;
                    for (j = XXb; j <= AAb; j++)
                    {
                        AXv = AXv + data[j].Volume;
                    }
                    nAXv = iff(AXb > 0, AXv / AXb, AXv);
                    BAv = 0;
                    for (j = AAb; j <= BBb; j++)
                    {
                        BAv = BAv + data[j].Volume;
                    }
                    nBAv = iff(BAb > 0, BAv / BAb, BAv);
                    CBv = 0;
                    for (j = BBb; j <= CCb; j++)
                    {
                        CBv = CBv + data[j].Volume;
                    }
                    nCBv = iff(CBb > 0, CBv / CBb, CBv);
                    DCv = 0;
                    for (j = CCb; j <= DDb; j++)
                    {
                        DCv = DCv + data[j].Volume;
                    }
                    nDCv = iff(DCb > 0, DCv / DCb, DCv);
                    EDv = 0;
                    for (j = DDb; j <= EEb; j++)
                    {
                        EDv = EDv + data[j].Volume;
                    }
                    nEDv = iff(EDb > 0, EDv / EDb, EDv);
                    YEv = 0;
                    for (j = EEb; j <= YYb; j++)
                    {
                        YEv = YEv + data[j].Volume;
                    }
                    nYEv = iff(YEb > 0, YEv / YEb, YEv);


                    //---------------- Contiguous line dot products
                    XBDc = (nDBb * nBXb + nDBp * nBXp);
                    BDYc = (nDBb * nYDb + nDBp * nYDp);
                    ACEc = (nCAb * nECb + nCAp * nECp);
                    CAoEC = (nCAb * nECb + nCAp * nECp);
                    BXoDB = (nBXb * nDBb + nBXp * nDBp);
                    //---------------- Parallel line dot products
                    ECoYD = (nECb * nYDb + nECp * nYDp);
                    DCoYE = (nDCb * nYEb + nDCp * nYEp);
                    BXoCA = (nBXb * nCAb + nBXp * nCAp);
                    DBoEC = (nDBb * nECb + nDBp * nECp);
                    //---------------- Vector cross products
                    ECxYD = (nECb * nYDp) - (nECp * nYDb);
                    EAxYB = (nEAb * nYBp) - (nEAp * nYBb);
                    BXxCA = (nBXb * nCAp) - (nBXp * nCAb);
                    AXxCB = (nAXb * nCBp) - (nAXp * nCBb);
                    BAxDC = (nBAb * nDCp) - (nBAp * nDCb);
                    DXxCA = (nDXb * nCAp) - (nDXp * nCAb); ///check later


                    //-------------------------------------------------------------------------------------
                    //									Trading Signals
                    //-------------------------------------------------------------------------------------
                    // Double Top/Bottom
                    //-------------------------------------------------------------------------------------	

                    if (ACEPivotsABove)
                    {
                        // Double Top
                        patternlength = Math.Abs(YBb);
                        patternheight = MaxList(CCp - DDp, EEp - YYp);
                        similartops = nECb > doubletoplimit;
                        M_like = nYDb > doubletoplimit;
                        rising = CBp + 1.382 * DCp > 0 && AAp < DDp && WWp < DDp;
                        doubleprices = CCp > DDp && EEp > DDp;
                        volpattern = Use_Volume == false || (Use_Volume && (nCBv > nDCv) && ((CBv + DCv) * (EDb + YEb) > (EDv + YEv) * (CBb + DCb)));
                        DoubleTop = doubleprices && volpattern && similartops && rising && M_like &&
                                          patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;
                        if (DoubleTop)
                        {
                            SignalFound = true;
                            SignalDir = " Short";
                            SignalName = "Double Top";

                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.DoubleTop;
                            foundpattern.trendLines.Add(new DTPoint(BBd, BBt, BBp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            foundpattern.trendLines.Add(new DTPoint(DDd, DDt, DDp));
                            foundpattern.trendLines.Add(new DTPoint(DDd, DDt, DDp));
                            foundpattern.trendLines.Add(new DTPoint(EEd, EEt, EEp));
                            foundpattern.trendLines.Add(new DTPoint(EEd, EEt, EEp));
                            foundpattern.trendLines.Add(new DTPoint(YYd, YYt, YYp));

                            results.Add(foundpattern);
                            // Sell marker
                            SignalPrice = YYp - 0.10 * patternheight;
                            SignalStop = MaxList(CCp, EEp) + patternheight;
                            SignalTarget = MinList(DDp, YYp) - patternheight;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }
                    else
                    {
                        // Double Bottom
                        patternlength = Math.Abs(YBb);
                        patternheight = MaxList(DDp - CCp, YYp - EEp);
                        similarbots = nECb > doubletoplimit;
                        W_like = YDb > doubletoplimit;
                        falling = CBp + 1.382 * DCp < 0 && AAp > DDp && WWp > DDp;
                        volpattern = Use_Volume == false || (Use_Volume && nCBv > nDCv && ((CBv + DCv) * (EDb + YEb) > (EDv + YEv) * (CBb + DCb)));
                        doubleprices = CCp < DDp && EEp < DDp;
                        DoubleBot = doubleprices && volpattern && similarbots && falling && W_like &&
                                          patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;
                        if (DoubleBot)
                        {
                            SignalFound = true;
                            SignalDir = " Long";
                            SignalName = "Double Bottom";

                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.DoubleBottom;
                            foundpattern.trendLines.Add(new DTPoint(BBd, BBt, BBp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            foundpattern.trendLines.Add(new DTPoint(DDd, DDt, DDp));
                            foundpattern.trendLines.Add(new DTPoint(DDd, DDt, DDp));
                            foundpattern.trendLines.Add(new DTPoint(EEd, EEt, EEp));
                            foundpattern.trendLines.Add(new DTPoint(EEd, EEt, EEp));
                            foundpattern.trendLines.Add(new DTPoint(YYd, YYt, YYp));
                            results.Add(foundpattern);

                            SignalPrice = YYp + 0.10 * patternheight;
                            SignalStop = MinList(CCp, EEp) - patternheight;
                            SignalTarget = MaxList(YYp, DDp) + patternheight;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }

                    //-------------------------------------------------------------------------------------
                    // Wedge
                    //-------------------------------------------------------------------------------------
                    if ((ACEPivotsABove))
                    {
                        // Rising Wedge
                        patternlength = MaxList(EAb, YBb);
                        contiguous = ACEc >= colinearlimit && BDYc >= colinearlimit;
                        wedgelike = ECp > 0 && YDp > 0 && EAp > 0 && YBp > 0 && CAp > 0 && DBp > 0 && EAxYB >= wedgelimit;
                        decliningvolume = Use_Volume == false || (Use_Volume && (AXv + BAv) * (CBb + DCb) > (CBv + DCv) * (AXb + BAb) && (CBv + DCv) * (EDb + YEb) > (EDv + YEv) * (CBb + DCb));
                        rising = XXp < BBp;
                        RisingWedge = rising && contiguous && decliningvolume && wedgelike &&
                                          patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;

                        if (RisingWedge)
                        {
                            AAadj = BBp - (BBb - AAb) * (YYp - BBp) / (YYb - BBb);
                            DDadj = DDp - (DDb - AAb) * (YYp - DDp) / (YYb - DDb);
                            YYadj = EEp + (YYb - EEb) * (EEp - AAp) / (EEb - AAb);
                            SignalFound = true;
                            SignalDir = " Short";
                            SignalName = "Rising Wedge";

                            apex_bar = FindIntersection(AAb, AAp, YYb, YYadj, AAb, MinList(AAadj, DDadj), YYb, YYp);
                            PFR_CalcForwardDate(data, currentbar, apex_bar - currentbar, SignalDate, SignalTime, out ForwardDate, out ForwardTime);
                            if (ForwardDate >= LastCalcDate && ForwardTime > LastCalcTime) apex_bar = currentbar;
                            PFR_CalcForwardDate(data, currentbar, MaxList(0, apex_bar - YYb), YYd, YYt, out apex_date, out apex_time);

                            // Upper line	
                            new_upper_p = YYadj + MaxList(0, apex_bar - YYb) * Math.Abs((YYadj - AAp) / (YYb - AAb));
                            new_lower_p = YYp + MaxList(0, apex_bar - YYb) * Math.Abs((YYp - MinList(AAadj, DDadj)) / (YYb - AAb));

                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.RisingWedge;
                            foundpattern.trendLines.Add(new DTPoint(apex_date, apex_time, new_upper_p));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, AAp));
                            foundpattern.trendLines.Add(new DTPoint(apex_date, apex_time, new_lower_p));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, MinList(AAadj, DDadj)));
                            results.Add(foundpattern);

                            // Sell marker
                            begwidth = Math.Abs(AAp - AAadj);
                            endwidth = Math.Abs(YYp - YYadj);
                            SignalPrice = YYp - 0.25 * endwidth;
                            SignalStop = new_upper_p + 0.10 * begwidth;
                            SignalTarget = BBp;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }
                    else
                    {
                        // Falling Wedge
                        patternlength = MaxList(EAb, YBb);
                        contiguous = ACEc >= colinearlimit && BDYc >= colinearlimit;
                        wedgelike = ECp < 0 && YDp < 0 && EAp < 0 && YBp < 0 && DBp < 0 && CAp < 0 && EAxYB <= -wedgelimit;
                        decliningvolume = Use_Volume == false || (Use_Volume && (AXv + BAv) * (CBb + DCb) > (CBv + DCv) * (AXb + BAb) && (CBv + DCv) * (EDb + YEb) > (EDv + YEv) * (CBb + DCb));
                        falling = XXp > BBp;
                        FallingWedge = falling && contiguous && decliningvolume && wedgelike &&
                                          patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;
                        if (FallingWedge)
                        {
                            SignalFound = true;
                            SignalDir = " Long";
                            SignalName = "Falling Wedge";

                            YYadj = EEp + (YYb - EEb) * (EEp - AAp) / (EEb - AAb);
                            AAadj = BBp - (BBb - AAb) * (YYp - BBp) / (YYb - BBb);
                            DDadj = DDp - (DDb - AAb) * (YYp - DDp) / (YYb - DDb);
                            apex_bar = FindIntersection(AAb, AAp, YYb, YYadj, AAb, MaxList(AAadj, DDadj), YYb, YYp);

                            PFR_CalcForwardDate(data, currentbar, apex_bar - currentbar, SignalDate, SignalTime, out ForwardDate, out ForwardTime);
                            if (ForwardDate >= LastCalcDate && ForwardTime > LastCalcTime) apex_bar = currentbar;
                            PFR_CalcForwardDate(data, currentbar, MaxList(0, apex_bar - YYb), YYd, YYt, out apex_date, out apex_time);
                            // Upper line	
                            new_upper_p = YYp - MaxList(0, (apex_bar - YYb)) * Math.Abs((YYp - MaxList(AAadj, DDadj)) / (YYb - AAb));
                            new_lower_p = YYadj - MaxList(0, (apex_bar - YYb)) * Math.Abs((YYadj - AAp) / (YYb - AAb));

                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.FallingWedge;
                            foundpattern.trendLines.Add(new DTPoint(apex_date, apex_time, new_upper_p));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, MaxList(AAadj, DDadj)));
                            foundpattern.trendLines.Add(new DTPoint(apex_date, apex_time, new_lower_p));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, AAp));
                            results.Add(foundpattern);
                            // Buy marker
                            begwidth = Math.Abs(AAp - AAadj);
                            endwidth = Math.Abs(YYp - YYadj);
                            SignalPrice = YYp + 0.25 * endwidth;
                            SignalStop = new_lower_p - 0.10 * begwidth;
                            SignalTarget = BBp;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }

                    //-------------------------------------------------------------------------------------
                    // Broadening Pattern 
                    //-------------------------------------------------------------------------------------	
                    if (ACEPivotsABove)
                    {
                        // Broadening Top
                        patternlength = MaxList(EAb, YBb);
                        contiguous = ACEc >= colinearlimit && BDYc >= colinearlimit;
                        broadeninglike = CCp > AAp && EEp > CCp && DDp < BBp && ECp > 0 && YDp < 0 && EAp > 0 && YBp < 0 && DBp < 0 && CAp > 0 && EAxYB <= -wedgelimit;
                        decliningvolume = Use_Volume == false || (Use_Volume && (AXv + CBv + EDv) * (BAb + DCb + YEb) < (BAv + DCv + YEv) * (AXb + CBb + EDb));
                        rising = XXp < DDp;
                        BroadTop = rising && contiguous && decliningvolume && broadeninglike && patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;

                        if (BroadTop)
                        {
                            SignalFound = true;
                            SignalDir = " Short";
                            SignalName = "Broadening Top";
                            // Upper line		
                            YYadj = EEp + (YYb - EEb) * (EEp - AAp) / (EEb - AAb);
                            //Bottom line
                            AAadj = BBp - (BBb - AAb) * (YYp - BBp) / (YYb - BBb);
                            DDadj = DDp - (DDb - AAb) * (YYp - DDp) / (YYb - DDb);

                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.BroadeningTop;
                            foundpattern.trendLines.Add(new DTPoint(YYd, YYt, YYadj));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, AAp));
                            foundpattern.trendLines.Add(new DTPoint(YYd, YYt, YYp));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, MinList(AAp, MinList(AAadj, DDadj))));
                            results.Add(foundpattern);

                            // Sell marker
                            begwidth = Math.Abs(AAp - AAadj);
                            endwidth = Math.Abs(YYp - YYadj);
                            SignalPrice = YYp - 0.10 * endwidth;
                            SignalStop = YYp + 1.10 * endwidth;
                            SignalTarget = YYp - endwidth;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }
                    else
                    {
                        // Broadening Bottom
                        patternlength = MaxList(EAb, YBb);
                        contiguous = ACEc >= colinearlimit && BDYc >= colinearlimit;
                        broadeninglike = CCp < AAp && EEp < CCp && DDp > BBp && ECp < 0 && YDp > 0 && EAp < 0 && YBp > 0 && DBp > 0 && CAp < 0 && EAxYB >= wedgelimit;
                        advancingvolume = Use_Volume == false || (Use_Volume && (AXv + CBv + EDv) * (BAb + DCb + YEb) > (BAv + DCv + YEv) * (AXb + CBb + EDb));
                        falling = XXp > DDp;
                        BroadBot = falling && contiguous && advancingvolume && broadeninglike && patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;

                        if (BroadBot)
                        {
                            SignalFound = true;
                            SignalDir = " Long";
                            SignalName = "Broadening Bottom";
                            // Lower line
                            YYadj = EEp + (YYb - EEb) * (EEp - AAp) / (EEb - AAb);
                            // Upper line
                            AAadj = BBp - (BBb - AAb) * (YYp - BBp) / (YYb - BBb);
                            DDadj = DDp - (DDb - AAb) * (YYp - DDp) / (YYb - DDb);

                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.BroadeningBottom;
                            foundpattern.trendLines.Add(new DTPoint(YYd, YYt, YYadj));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, AAp));
                            foundpattern.trendLines.Add(new DTPoint(YYd, YYt, YYp));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, MaxList(AAp, MaxList(AAadj, DDadj))));
                            results.Add(foundpattern);

                            // Buy marker
                            begwidth = Math.Abs(AAp - AAadj);
                            endwidth = Math.Abs(YYp - YYadj);
                            SignalPrice = YYp + 0.10 * endwidth;
                            SignalStop = YYp - 1.10 * endwidth;
                            SignalTarget = YYp + endwidth;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }

                    //-------------------------------------------------------------------------------------
                    // Pennant
                    //-------------------------------------------------------------------------------------
                    if (ACEPivotsABove)
                    {
                        CCadj = DDp - ((DDb - CCb) * (YYp - DDp) / (YYb - DDb));
                        YYadj = EEp - ((YYb - EEb) * (CCp - EEp) / (EEb - CCb));
                        begwidth = (CCp - CCadj);
                        endwidth = (YYadj - YYp);
                        flagpole = (CCp - BBp) > 2 * begwidth;
                        pennantlike = ECp < 0 && YDp > 0 && Math.Abs(ECp / YDp) >= 0.1 && Math.Abs(ECp / YDp) <= 10 && ECxYD >= wedgelimit;
                        patternlength = Math.Abs(YBb);
                        volpattern = Use_Volume == false || (Use_Volume && CBv * (DCb + EDb + YEb) > (DCv + EDv + YEv) * CBb);
                        BullPennant = flagpole && pennantlike && volpattern && patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;

                        if (BullPennant)
                        {
                            SignalFound = true;
                            SignalDir = " Long";
                            SignalName = "Bull Pennant";
                            apex_bar = FindIntersection(CCb, CCp, YYb, MaxList(YYadj, YYp), CCb, CCadj, YYb, YYp);
                            PFR_CalcForwardDate(data, currentbar, apex_bar - currentbar, SignalDate, SignalTime, out ForwardDate, out ForwardTime);
                            if (ForwardDate >= LastCalcDate && ForwardTime > LastCalcTime) apex_bar = YYb;
                            PFR_CalcForwardDate(data, currentbar, MaxList(0, apex_bar - YYb), YYd, YYt, out apex_date, out apex_time);

                            new_upper_p = MaxList(YYadj, YYp) - MaxList(0, (apex_bar - YYb)) * Math.Abs((MaxList(YYadj, YYp) - CCp) / (YYb - CCb));
                            new_lower_p = YYp + MaxList(0, (apex_bar - YYb)) * Math.Abs((YYp - CCadj) / (YYb - CCb));

                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.BullPennant;
                            foundpattern.trendLines.Add(new DTPoint(apex_date, apex_time, new_upper_p));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            foundpattern.trendLines.Add(new DTPoint(apex_date, apex_time, new_lower_p));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCadj));
                            foundpattern.trendLines.Add(new DTPoint(BBd, BBt, BBp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            results.Add(foundpattern);

                            // Sell marker
                            SignalPrice = YYadj + 0.10 * begwidth;
                            SignalStop = YYp - begwidth;
                            SignalTarget = YYadj + begwidth;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }
                    else
                    {
                        CCadj = DDp - ((DDb - CCb) * (YYp - DDp) / (YYb - DDb));
                        YYadj = EEp + ((YYb - EEb) * (EEp - CCp) / (EEb - CCb));
                        begwidth = (CCadj - CCp);
                        endwidth = YYp - YYadj;
                        flagpole = (BBp - CCp) > 2 * begwidth;
                        pennantlike = ECp > 0 && YDp < 0 && Math.Abs(ECp / YDp) >= 0.1 && Math.Abs(ECp / YDp) <= 10 && ECxYD <= -wedgelimit;
                        patternlength = YYb - BBb;
                        volpattern = Use_Volume == false || (Use_Volume && CBv * (DCb + EDb + YEb) > (DCv + EDv + YEv) * CBb);
                        BearPennant = flagpole && pennantlike && volpattern && patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;

                        if (BearPennant)
                        {
                            SignalFound = true;
                            SignalDir = " Short";
                            SignalName = "Bear Pennant";
                            apex_bar = FindIntersection(CCb, CCp, YYb, MinList(YYadj, YYp), CCb, CCadj, YYb, YYp);
                            PFR_CalcForwardDate(data, currentbar, apex_bar - currentbar, SignalDate, SignalTime, out ForwardDate, out ForwardTime);
                            if (ForwardDate >= LastCalcDate && ForwardTime > LastCalcTime) apex_bar = YYb;
                            PFR_CalcForwardDate(data, currentbar, MaxList(0, apex_bar - YYb), YYd, YYt, out apex_date, out apex_time);
                            // Upper line	
                            new_upper_p = YYp - MaxList(0, (apex_bar - YYb)) * (CCadj - YYp) / (YYb - CCb);
                            new_lower_p = MinList(YYadj, YYp) + MaxList(0, (apex_bar - YYb)) * (MinList(YYadj, YYp) - CCp) / (YYb - CCb);

                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.BearPennant;
                            foundpattern.trendLines.Add(new DTPoint(apex_date, apex_time, new_upper_p));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCadj));
                            foundpattern.trendLines.Add(new DTPoint(apex_date, apex_time, new_lower_p));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            foundpattern.trendLines.Add(new DTPoint(BBd, BBt, BBp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            results.Add(foundpattern);

                            // Sell marker
                            SignalPrice = YYadj - 0.10 * begwidth;
                            SignalStop = YYp + begwidth;
                            SignalTarget = YYadj - begwidth;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }

                    //-------------------------------------------------------------------------------------
                    // Triangles
                    //-------------------------------------------------------------------------------------
                    if (ACEPivotsABove)
                    {
                        AAadj = BBp - BAb * YBp / YBb;
                        YYadj = EEp + YEb * EAp / EAb;
                        begwidth = Math.Abs(AAp - AAadj);
                        endwidth = Math.Abs(YYp - YYadj);
                        descending = nYBp > necklimit;
                        ascending = nEAp > necklimit;
                        pennantlike = ECp <= 0 && CAp <= 0 && EAp <= 0 && YDp >= 0 && DBp >= 0 && YBp >= 0 && ECxYD >= wedgelimit && EAxYB >= wedgelimit;
                        contiguous = ACEc >= colinearlimit && BDYc >= colinearlimit;
                        patternlength = Math.Abs(YYb - AAb);
                        volpattern = Use_Volume == false || (Use_Volume && (BAv + DCv + YEv) * (CBb + EDb) < (CBv + EDv) * (BAb + DCb + YEb));
                        BullTriangle = descending == false && contiguous && pennantlike && volpattern && patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;

                        if (BullTriangle)
                        {
                            SignalFound = true;
                            SignalDir = " Long";
                            SignalName = iffstring(ascending, "ascending Triangle", "Bullish Triangle");
                            apex_bar = FindIntersection(AAb, AAp, YYb, MaxList(YYadj, YYp), AAb, AAadj, YYb, YYp);
                            PFR_CalcForwardDate(data, currentbar, apex_bar - currentbar, SignalDate, SignalTime, out ForwardDate, out ForwardTime);
                            if (ForwardDate > LastCalcDate && ForwardTime > LastCalcTime) apex_bar = currentbar;
                            PFR_CalcForwardDate(data, currentbar, MaxList(0, apex_bar - YYb), YYd, YYt, out apex_date, out apex_time);
                            // Upper line	
                            new_upper_p = MaxList(YYadj, YYp) - MaxList(0, (apex_bar - YYb)) * Math.Abs((MaxList(YYadj, YYp) - AAp) / (YYb - AAb));
                            new_lower_p = YYp + MaxList(0, (apex_bar - YYb)) * Math.Abs((YYp - AAadj) / (YYb - AAb));

                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.BullTriangle;
                            foundpattern.trendLines.Add(new DTPoint(apex_date, apex_time, new_upper_p));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, AAp));
                            foundpattern.trendLines.Add(new DTPoint(apex_date, apex_time, new_lower_p));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, AAadj));
                            results.Add(foundpattern);

                            // Sell marker
                            SignalPrice = YYadj + 0.10 * begwidth;
                            SignalStop = YYp - begwidth;
                            SignalTarget = YYadj + begwidth;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }
                    else
                    {
                        AAadj = BBp - BAb * YBp / YBb;
                        YYadj = EEp + YEb * EAp / EAb;
                        begwidth = Math.Abs(AAp - AAadj);
                        endwidth = Math.Abs(YYp - YYadj);
                        ascending = nYBp > necklimit;
                        descending = nEAp > necklimit;
                        contiguous = ACEc >= colinearlimit && BDYc >= colinearlimit;
                        pennantlike = ECp >= 0 && CAp >= 0 && EAp >= 0 && YDp <= 0 && DBp <= 0 && YBp <= 0 && ECxYD <= -wedgelimit && EAxYB <= -wedgelimit;
                        patternlength = YYb - AAb;
                        volpattern = Use_Volume == false || (Use_Volume && (BAv + DCv + YEv) * (CBb + EDb) < (CBv + EDv) * (BAb + DCb + YEb));
                        BearTriangle = ascending == false && contiguous && pennantlike && volpattern && patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;

                        if (BearTriangle)
                        {
                            SignalFound = true;
                            SignalDir = " Short";
                            SignalName = iffstring(descending, "descending Triangle", "Bearish Triangle");
                            apex_bar = FindIntersection(AAb, AAp, YYb, MinList(YYadj, YYp), AAb, AAadj, YYb, YYp);
                            PFR_CalcForwardDate(data, currentbar, apex_bar - currentbar, SignalDate, SignalTime, out ForwardDate, out ForwardTime);
                            if (ForwardDate > LastCalcDate && ForwardTime > LastCalcTime) apex_bar = YYb;
                            PFR_CalcForwardDate(data, currentbar, MaxList(0, apex_bar - YYb), YYd, YYt, out apex_date, out apex_time);
                            // Upper line	
                            new_upper_p = YYp - MaxList(0, (apex_bar - YYb)) * (AAadj - YYp) / (YYb - AAb);
                            new_lower_p = YYadj + MaxList(0, (apex_bar - YYb)) * (YYadj - AAp) / (YYb - AAb);

                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.BearTriangle;
                            foundpattern.trendLines.Add(new DTPoint(apex_date, apex_time, new_upper_p));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, AAadj));
                            foundpattern.trendLines.Add(new DTPoint(apex_date, apex_time, new_lower_p));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, AAp));
                            results.Add(foundpattern);
                            // Sell marker
                            SignalPrice = YYadj - 0.10 * begwidth;
                            SignalStop = YYp + begwidth;
                            SignalTarget = YYadj - begwidth;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }

                    //-------------------------------------------------------------------------------------
                    // Flags
                    //-------------------------------------------------------------------------------------
                    if ((ACEPivotsABove))
                    {

                        CCadj = DDp - DCb * YDp / YDb;
                        YYadj = EEp + YEb * ECp / ECb;
                        begwidth = CCp - CCadj;
                        endwidth = YYadj - YYp;
                        flagpole = CBp > 1.618 * MaxList(begwidth, MaxList(endwidth, CCp - YYp));
                        flaglike = ECoYD >= channellimit && ECp < 0 && YDp < 0;
                        patternlength = YBb;
                        volpattern = Use_Volume == false || (Use_Volume && CBv * (DCb + EDb + YEb) > (DCv + EDv + YEv) * CBb);
                        BullFlag = flagpole && flaglike && volpattern && patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;

                        if (BullFlag)
                        {
                            SignalFound = true;
                            SignalDir = " Long";
                            SignalName = "Bull Flag";

                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.BullFlag;
                            foundpattern.trendLines.Add(new DTPoint(YYd, YYt, YYadj));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            foundpattern.trendLines.Add(new DTPoint(YYd, YYt, YYp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, MinList(CCp, CCadj)));
                            foundpattern.trendLines.Add(new DTPoint(BBd, BBt, BBp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            results.Add(foundpattern);

                            // Sell marker
                            SignalPrice = YYadj + 0.10 * endwidth;
                            SignalStop = YYp - 0.10 * endwidth;
                            SignalTarget = YYp + 2.20 * endwidth;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }
                    else
                    {
                        CCadj = DDp - DCb * YDp / YDb;
                        YYadj = EEp + YEb * ECp / ECb;
                        begwidth = CCadj - CCp;
                        endwidth = YYp - YYadj;
                        flagpole = Math.Abs(CBp) > 1.618 * MaxList(begwidth, MaxList(endwidth, YYp - CCp));
                        flaglike = ECoYD >= channellimit && ECp > 0 && YDp > 0;
                        patternlength = YBb;
                        volpattern = Use_Volume == false || (Use_Volume && CBv * (DCb + EDb + YEb) > (DCv + EDv + YEv) * CBb);
                        BearFlag = flagpole && flaglike && volpattern && patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;

                        if (BearFlag)
                        {
                            SignalFound = true;
                            SignalDir = " Short";
                            SignalName = "Bear Flag";

                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.BullFlag;
                            foundpattern.trendLines.Add(new DTPoint(YYd, YYt, YYp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, MaxList(CCp, CCadj)));
                            foundpattern.trendLines.Add(new DTPoint(YYd, YYt, YYadj));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            foundpattern.trendLines.Add(new DTPoint(BBd, BBt, BBp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            results.Add(foundpattern);

                            // Sell marker
                            SignalPrice = YYadj - 0.10 * endwidth;
                            SignalStop = YYp + 0.10 * endwidth;
                            SignalTarget = YYp - 2.20 * endwidth;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }
                    //-------------------------------------------------------------------------------------
                    //  Head && Shoulders 
                    //-------------------------------------------------------------------------------------	
                    if ((ACEPivotsABove))
                    {
                        // Head && Shoulders Top
                        patternlength = YYb - XXb;
                        patternheight = CCp - 0.5 * (DDp + BBp);
                        neckline = nDBp > necklimit;
                        HS_like = WWp < BBp && WWp < DDp && CCp > (AAp + atr) && CCp > (EEp + atr) && EEp > DDp && AAp > DDp;
                        volpattern = Use_Volume == false || (Use_Volume && AXv * BAb > BAv * AXb && CBv * DCb > DCv * CBb && EDv * YEb > YEv * EDb && AXv * (CBb + EDb) > (CBv + EDv) * AXb);
                        HeadAndShoulders = volpattern && neckline && HS_like && patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;

                        if (HeadAndShoulders)
                        {
                            SignalFound = true;
                            SignalDir = " Short";
                            SignalName = "Head && Shoulders Top";
                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.HeadAndShoulders;
                            foundpattern.trendLines.Add(new DTPoint(XXd, XXt, XXp));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, AAp));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, AAp));
                            foundpattern.trendLines.Add(new DTPoint(BBd, BBt, BBp));
                            foundpattern.trendLines.Add(new DTPoint(BBd, BBt, BBp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            foundpattern.trendLines.Add(new DTPoint(DDd, DDt, DDp));
                            foundpattern.trendLines.Add(new DTPoint(DDd, DDt, DDp));
                            foundpattern.trendLines.Add(new DTPoint(EEd, EEt, EEp));
                            foundpattern.trendLines.Add(new DTPoint(EEd, EEt, EEp));
                            foundpattern.trendLines.Add(new DTPoint(YYd, YYt, YYp));
                            results.Add(foundpattern);

                            // Sell marker
                            SignalPrice = YYp - 0.10 * patternheight;
                            SignalStop = YYp + patternheight;
                            SignalTarget = YYp - patternheight;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }
                    else
                    {
                        // Inverse Head && Shoulders
                        patternlength = YYb - XXb;
                        patternheight = 0.5 * (DDp + BBp) - CCp;
                        neckline = nDBp > necklimit;// { BDYc >= colinearlimit &&}
                        HS_like = WWp > BBp && WWp > DDp && CCp < (AAp - atr) && CCp < (EEp - atr) && EEp < DDp && AAp < DDp;
                        volpattern = Use_Volume == false || (Use_Volume && AXv * BAb > BAv * AXb && CBv * DCb > DCv * CBb && EDv * YEb > YEv * EDb && AXv * (CBb + EDb) > (CBv + EDv) * AXb);
                        InvHeadAndShoulders = volpattern && neckline && HS_like && patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;

                        if (InvHeadAndShoulders)
                        {
                            SignalFound = true;
                            SignalDir = " Long";
                            SignalName = "Inverse Head & Shoulders";
                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.InvHeadAndShoulders;
                            foundpattern.trendLines.Add(new DTPoint(XXd, XXt, XXp));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, AAp));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, AAp));
                            foundpattern.trendLines.Add(new DTPoint(BBd, BBt, BBp));
                            foundpattern.trendLines.Add(new DTPoint(BBd, BBt, BBp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            foundpattern.trendLines.Add(new DTPoint(DDd, DDt, DDp));
                            foundpattern.trendLines.Add(new DTPoint(DDd, DDt, DDp));
                            foundpattern.trendLines.Add(new DTPoint(EEd, EEt, EEp));
                            foundpattern.trendLines.Add(new DTPoint(EEd, EEt, EEp));
                            foundpattern.trendLines.Add(new DTPoint(YYd, YYt, YYp));
                            results.Add(foundpattern);

                            // Buy marker
                            SignalPrice = YYp + 0.10 * patternheight;
                            SignalStop = YYp - patternheight;
                            SignalTarget = YYp + patternheight;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }

                    //-------------------------------------------------------------------------------------
                    // Cup && H&&le
                    //-------------------------------------------------------------------------------------
                    if ((ACEPivotsABove))
                    {
                        patternlength = YYb - WWb;
                        patternheight = 0.5 * (WWp + EEp) - BBp;
                        cupthickness = 0.5 * (AAp + CCp) - BBp;
                        handleheight = 0.5 * (WWp + EEp) - YYp;
                        cuplike = XXp < WWp && BBp < XXp && AAp < WWp && DDp > BBp && CCp < WWp && AAp < EEp && CCp < EEp && CCp < WWp && patternheight >= 1.5 * cupthickness && nEWp >= necklimit;
                        handlelike = YYp > BBp && handleheight <= 0.6 * patternheight;
                        volpattern = Use_Volume == false || (Use_Volume && (XWv + AXv + BAv) * (CBb + DCb + EDb) > (CBv + DCv + EDv) * (XWb + AXb + BAb));
                        CupAndHandle = cuplike && handlelike && volpattern && patternlength >= Min_PatternWidthBars && patternlength <= Max_PatternWidthBars;

                        if (CupAndHandle)
                        {
                            SignalFound = true;
                            SignalDir = " Long";
                            SignalName = "Cup && H&&le";
                            PatternResult foundpattern = new PatternResult();
                            foundpattern.pattern = PatternStyle.CupAndHandle;
                            foundpattern.trendLines.Add(new DTPoint(WWd, WWt, WWp));
                            foundpattern.trendLines.Add(new DTPoint(XXd, XXt, XXp));
                            foundpattern.trendLines.Add(new DTPoint(XXd, XXt, XXp));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, AAp));
                            foundpattern.trendLines.Add(new DTPoint(AAd, AAt, AAp));
                            foundpattern.trendLines.Add(new DTPoint(BBd, BBt, BBp));
                            foundpattern.trendLines.Add(new DTPoint(BBd, BBt, BBp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            foundpattern.trendLines.Add(new DTPoint(CCd, CCt, CCp));
                            foundpattern.trendLines.Add(new DTPoint(DDd, DDt, DDp));
                            foundpattern.trendLines.Add(new DTPoint(DDd, DDt, DDp));
                            foundpattern.trendLines.Add(new DTPoint(EEd, EEt, EEp));
                            foundpattern.trendLines.Add(new DTPoint(EEd, EEt, EEp));
                            foundpattern.trendLines.Add(new DTPoint(YYd, YYt, YYp));
                            results.Add(foundpattern);

                            // Buy marker
                            SignalPrice = EEp + 0.10 * patternheight;
                            SignalStop = YYp - patternheight;
                            SignalTarget = EEp + patternheight;
                            PFR_CalcForwardDate(data, currentbar, (int)patternlength, SignalDate, SignalTime, out SignalEndDate, out SignalEndTime);
                        }
                    }
                }

                if (SignalFound)
                {
                    data[currentbar].isPattern = true;
                }
            }
            return results;
        }
    }
}
