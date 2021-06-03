using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using TradersToolbox.Core;
using TradersToolbox.Core.Serializable;

namespace TradersToolbox
{
    static class Codegen
    {
        public static List<string> EL(List<IReadonlySimStrategy> activeResults, out List<KeyValuePair<string, string>> extraEvCode)
        {
            StringBuilder str = new StringBuilder();
            extraEvCode = new List<KeyValuePair<string, string>>();

            foreach (var res in activeResults)
            {
                List<string> strategyRules = new List<string>();
                List<SymbolId> strategyRulesSymbols = new List<SymbolId>();
                int atLeastEntry = 0;
                int atLeastExit = -99999;
                int extraCodeIndex = 50;
                StringBuilder rulesString = new StringBuilder();
                StringBuilder extraCodeEntry = new StringBuilder();
                {
                    if (res.ReadonlyEntrySignals == null)
                        rulesString.Append(res.Name);
                    else
                    {
                        int strCount = 0;
                        for (int i = 0; i < res.ReadonlyEntrySignals.Count; i++)
                        {
                            if (res.ReadonlyEntrySignals[i] is SignalStrategy)
                            {
                                strategyRules.Add(res.ReadonlyEntrySignals[i].Key);
                                strategyRulesSymbols.Add((res.ReadonlyEntrySignals[i] as SignalStrategy).strategy_symbolId);
                                ++strCount;
                            }
                            else if (res.ReadonlyEntrySignals[i] is SignalEnsemble)
                            {
                                atLeastEntry = Math.Max(atLeastEntry, (res.ReadonlyEntrySignals[i] as SignalEnsemble).atLeast);
                                foreach (var kv in (res.ReadonlyEntrySignals[i] as SignalEnsemble).strategies_names_and_symbols)
                                {
                                    strategyRules.Add(kv.Key);
                                    strategyRulesSymbols.Add(kv.Value);
                                }
                            }
                            else
                            {
                                string extra = res.ReadonlyEntrySignals[i].GetExtraCode(Signal.CodeType.EL, "Value", ref extraCodeIndex);
                                if (!string.IsNullOrEmpty(extra))
                                    extraCodeEntry.Append(extra);

                                rulesString.AppendFormat(" and {0}", string.IsNullOrEmpty(extra) ? res.ReadonlyEntrySignals[i].CodeEL : res.ReadonlyEntrySignals[i].GetCode(Signal.CodeType.EL));
                            }
                        }
                        atLeastEntry += strCount;
                        if (rulesString.Length > 0)
                            rulesString.Remove(0, 5);
                        else
                            rulesString.Append("close <> 0");

                        var dt = Security.ToDateTime(res.SignalStartDate, res.SignalStartTime);
                        int year = dt.Year - 1900;
                        rulesString.Insert(0, string.Format("(Date[0]>{0:D2}{1:D2}{2:D2} or (Date[0]={0:D2}{1:D2}{2:D2} and Time[0]>={3})) and{4}",
                            year, dt.Month, dt.Day, res.SignalStartTime / 100, Environment.NewLine));
                    }
                }
                StringBuilder rulesExitString = new StringBuilder();
                StringBuilder extraCodeExit = new StringBuilder();
                {
                    if (res.ReadonlyExitSignals != null)
                        foreach (Signal s in res.ReadonlyExitSignals)
                            if ((s is SignalStrategy) == false && (s is SignalEnsemble) == false)
                            {
                                string extra = s.GetExtraCode(Signal.CodeType.EL, "Value", ref extraCodeIndex);
                                if (!string.IsNullOrEmpty(extra))
                                    extraCodeExit.Append(extra);

                                rulesExitString.AppendFormat("{0} and ", string.IsNullOrEmpty(extra) ? s.CodeEL : s.GetCode(Signal.CodeType.EL));
                            }
                    if (rulesExitString.Length > 0)
                        rulesExitString.Length -= 5;
                }

                if (strategyRules.Count > 0)   // have strategy signals
                    str.AppendLine(@"{
    Build Alpha Ensemble Strategy

    For interval inputs:
    1 - N (minutes)
    ADE.Daily - for Daily timeframe
    ADE.Weekly - for Weekly timeframe
    ADE.Monthly - for Monthly timeframe
}");

                str.AppendFormat(@"{{
    Strategy Details:
    Symbol: <b>{0}</b>
    Market 2: <b>{15}</b>
    Market 3: <b>{16}</b>
    Market 4: <b>{17}</b>
    Start Date: <b>{1}</b>
    Stop Date: <b>{2}</b>
    Out of Sample: <b>{3} %</b>
    Fitness Function: <b>{4}</b>
    Profit Target On: <b>{5}</b>
    Profit Multiple: <b>{6}</b>
    Stop Loss On: <b>{7}</b>
    Stop Loss Multiple: <b>{8}</b>
    Highest High On: <b>{9}</b>
    Highest High Lookback: <b>{10}</b>
    Lowest Low On: <b>{11}</b>
    Lowest Low Lookback: <b>{12}</b>
    Max Time: <b>{13}</b>
    Profitable Closes: <b>{14}</b>
}}
", res.Symbol, res.SignalStartDate, res.StopDate, res.OutOfSample,
res.Fitness.ToString(),
res.PT_ON > 0 ? "Yes" : "No", res.PT_mult, res.SL_ON > 0 ? "Yes" : "No", res.SL_mult,
res.HH_ON > 0 ? "Yes" : "No", res.HHlook, res.LL_ON > 0 ? "Yes" : "No", res.LLlook,
res.MaxTime == 0 ? 9999 : res.MaxTime, res.ProfX == 0 ? 9999 : res.ProfX,
res.GetMarketSymbolId(2).IsEmpty() ? "---" : res.GetMarketSymbolId(2).ToString(),
res.GetMarketSymbolId(3).IsEmpty() ? "---" : res.GetMarketSymbolId(3).ToString(),
res.GetMarketSymbolId(4).IsEmpty() ? "---" : res.GetMarketSymbolId(4).ToString());

                string open = res.EntryOnClose > 0 ? "<b>this bar close</b>" : "<b>next bar market</b>";
                string close = res.ExitOnClose > 0 ? "<b>this bar close</b>" : "<b>next bar market</b>";

                bool intraday = Properties.Settings.Default.TTaddCode && res.ReadonlyIntradayTestResults?.Count == 5;
                bool isRebalance = (res.TradeMode == TradeMode.LongRebalance || res.TradeMode == TradeMode.ShortRebalance)/* && res.ChildStrategiesCount > 0*/;

                if (isRebalance)
                {
                    int symbCount = 1;
                    if (res.ReadonlyChildren?.Count() > 0) symbCount = res.ReadonlyChildren.Count();
                    if (res.ReadonlyParent != null) res.ReadonlyParent.ReadonlyChildren.Count();

                    str.AppendLine(@"
// Rotation
defineDLLFunc: ""GlobalVariable.dll"", float, ""GV_SetFloat"", int, float;
defineDLLFunc: ""GlobalVariable.dll"", float, ""GV_GetFloat"", int;");

                    str.AppendFormat(@"
// Rotation
inputs:
	top_or_bottom ( <b>{0}</b> ),
	max_positions ( <b>{1}</b> ),
	port ( <b>{2}</b> )
	;
	
// Rotation
var: 
	first_of_mon ( 0 ),
	mon_perf ( 0 ),
	myvalue ( 0 ),
	threshold ( 0 ),
	ok_to_trade ( 0 )
	;
	
// Rotation
arrays:
	rotValues[](0)
	;
array_setmaxindex(rotValues,<b>{3}</b>);

// Rotation
", res.RebalanceSymbolsCount > 0 ? "true" : "false", res.RebalanceSymbolsCount >= 0 ? res.RebalanceSymbolsCount : -res.RebalanceSymbolsCount,
"1", symbCount);

                    for (int i = 0; i < symbCount; ++i)
                        str.AppendLine($"rotValues[{i}] = GV_GetFloat({i + 1});");

                    str.AppendFormat(@"
value99 = array_sort(rotValues,0,<b>{0}</b>,top_or_bottom);
	
myvalue = GV_GetFloat(port);
threshold = rotValues[max_positions-1];
ok_to_trade = iff(top_or_bottom,iff(myValue <= threshold,1,0),iff(myValue >= threshold,1,0));
", symbCount);
                }

                str.Append(@"
inputs:");

                for (int i = 0; i < strategyRules.Count; ++i)
                    str.AppendFormat(@"
    <b>Strat{0}</b>.Name(""<b>MP{0}</b>""),
    <b>Strat{0}</b>.Symbol(""<b>{1}{2}</b>""), // Set Symbol & Interval to match chart running strategy
    <b>Strat{0}</b>.Interval(ADE.Daily),", i + 1, Utils.SymbolsManager.GetSymbolType(strategyRulesSymbols[i]) == SymbolType.Futures ? "@" : "", strategyRulesSymbols[i]);

                str.AppendFormat(@"
    CombineSignals(0),  // 0 = all, +1 = long, -1 = short
    Required4Entry(<b>{11}</b>),
    Required4Exit(<b>{12}</b>),  // if signal count falls below this number it will exit
    PT_ON(<b>{0}</b>),
    SL_ON(<b>{1}</b>),
    TL_ON(<b>{13}</b>),
    HH_ON(<b>{2}</b>),
    LL_ON(<b>{3}</b>),
    Max_Time(<b>{4}</b>), 
    Profitable_Closes(<b>{5}</b>),
    ReadFiles(false), // set to true to read from file, false if all info is on open charts
    atr_length(<b>{6}</b>),
    hh_length(<b>{7}</b>),
    ll_length(<b>{8}</b>),
    pt_mult(<b>{9}</b>),
    sl_mult(<b>{10}</b>),
    tl_mult(<b>{14}</b>),
    delay(<b>{15}</b>)
    ;
", res.PT_ON, res.SL_ON, res.HH_ON, res.LL_ON, res.MaxTime == 0 ? 9999 : res.MaxTime, res.ProfX == 0 ? 9999 : res.ProfX, res.ATR_len, res.HHlook, res.LLlook,
res.PT_mult.ToString("F2", CultureInfo.InvariantCulture), res.SL_mult.ToString("F2", CultureInfo.InvariantCulture), atLeastEntry, atLeastExit,
res.TL_ON, res.TL_mult.ToString("F2", CultureInfo.InvariantCulture), res.DelayedEntry);

                if (strategyRules.Count > 0)
                {
                    str.Append(@"
vars:
    interval(ADE.BarInterval),   // Must be set to correct bar type
    token(GetSymbolName),");
                    for (int i = 1; i <= strategyRules.Count; ++i)
                        str.AppendFormat(@"
    intrabarpersist <b>iMap{0}</b>(MapSN.New),  // used to pass data to ADE", i);
                    str.AppendFormat(@"
    int sigCount(0),
    int n(0)
    ;

array:
    signals[<b>{0}</b>](0)
    ;
", strategyRules.Count);
                }

                str.Append(@"
vars:
    isIBOG(GetAppInfo(aiIntraBarOrder) = 1),
    intrabarpersist mp(0),
    intrabarpersist mpPrev(0),
    intrabarpersist isClose(true),
    intrabarpersist isOpen(false),
    atr(0),
    hh(0),
    ll(0),
    prof_x(0),
    PT(0),
    SL(0),
    TL(0),
    PS(0)
    ;
");

                if (intraday) str.AppendFormat(@"
// INTRADAY
vars:
    intrabarpersist trades_today(0),
    intrabarpersist pnl_today(0),
    start_time(<b>{0}</b>),
    end_time(<b>{1}</b>),
    session_start(<b>{2:D4}</b>),
    max_trades(<b>{3}</b>),
    start_pnl(0),
    max_pnl(<b>{4}</b>),
    min_pnl(<b>{5}</b>)
    ;
", res.ReadonlyIntradayTestResults[2] / 100, res.ReadonlyIntradayTestResults[3] / 100, res.SessionEndTime,
res.ReadonlyIntradayTestResults[4], res.ReadonlyIntradayTestResults[0], res.ReadonlyIntradayTestResults[1]);

                if (strategyRules.Count > 0)
                {
                    str.Append(@"
once begin
    // load data
    if ReadFiles then begin");
                    for (int i = 1; i <= strategyRules.Count; ++i)
                        str.AppendFormat(@"
        <b>value{0}</b> = ADE.OpenMap(<b>Strat{0}</b>.Name, <b>Strat{0}</b>.Symbol, <b>Strat{0}</b>.Interval);", i);
                    str.Append(@"
    end;
end;
");
                }

                str.Append(@"
isOpen = (isClose = true);
isClose = (BarStatus(1) <> 1);

if isClose then begin
    hh = Highest(High, hh_length);
    ll = Lowest(Low, ll_length);
    atr = AvgTrueRange(atr_length);");

                if (strategyRules.Count > 0)
                {
                    str.AppendLine();
                    for (int i = 1; i <= strategyRules.Count; ++i)
                        str.AppendFormat(@"
    <b>value{0}</b> = ADE.GetBarInfo(<b>Strat{0}</b>.Name, <b>Strat{0}</b>.Symbol, <b>Strat{0}</b>.Interval, ADE.BarID, <b>iMap{0}</b>);", i);
                    str.AppendLine();
                    for (int i = 0; i < strategyRules.Count; ++i)
                        str.AppendFormat(@"
    signals[<b>{0}</b>] = MapSN.Get(<b>iMap{1}</b>, ""MP"");", i, i + 1);
                    str.AppendFormat(@"

    if CombineSignals = 0 then begin
        sigCount = 0;
        for n = 0 to <b>{0}</b>-1 begin
            sigCount += absvalue(signals[n]);
        end;
    end;

    if CombineSignals = +1 then begin
        sigCount = 0;
        for n = 0 to <b>{0}</b>-1 begin
            if signals[n] > 0 then
                sigCount += signals[n];
        end;
    end;
    
    if CombineSignals = -1 then begin
        sigCount = 0;
        for n = 0 to <b>{0}</b>-1 begin
            if signals[n] < 0 then
                sigCount += absvalue(signals[n]);
        end;
    end;", strategyRules.Count);
                }

                string modeStr = (res.PosSizeMode == PositionSizingMode.Fixed) ? "Fixed" : (res.PosSizeMode == PositionSizingMode.Volatility ? "ATR" : "Default");
                string posSstr = "1";
                if (res.SymbolType == SymbolType.FOREX)
                    posSstr = string.Format(@"fx_position_size(""{0}"",""{1}"",True,{2},atr,SL_ON * {3:F2})", res.Currency, modeStr, res.AccountValue, res.SL_mult);
                else
                {
                    string symt = (res.SymbolType == SymbolType.Futures) ? "Fut" : "Other";
                    posSstr = string.Format(@"position_size(""{0}"", ""{1}"", {2}, {3}, atr, SL_ON * {4:F2})",
                        symt, modeStr, res.AccountValue, Utils.SymbolsManager.GetSymbolMargin(res.SymbolId), res.SL_mult);
                }

                str.AppendFormat(@"
end;

PS = {0};

mpPrev = mp;
mp = MarketPosition;
", posSstr);

                if (intraday)
                {
                    str.AppendLine(@"
// INTRADAY
PNL_TODAY = NETPROFIT + openpositionprofit - START_PNL;");
                }

                // signals extra code
                if (extraCodeExit.Length > 0)
                    str.AppendLine($"{Environment.NewLine}<b>{extraCodeExit}</b>");

                str.AppendFormat(@"
// ------------------------------------------
//  Exits
//
if mp = <b>{3}</b> then begin

    TL = Round2Fraction(Close[0] <b>{5}</b> atr[0] * tl_mult);
    If BarsSinceEntry > 0 and TL <b>{1}</b> TL[1] then TL = TL[1];

    if BarsSinceEntry = <b>{0}</b> or mp <> mpPrev then begin
        //PT = Round2Fraction(Close[0] <b>{4}</b> atr[0] * pt_mult);
        //SL = Round2Fraction(Close[0] <b>{5}</b> atr[0] * sl_mult);
        //TL = Round2Fraction(Close[0] <b>{5}</b> atr[0] * tl_mult);
        prof_x = 0;", res.EntryOnClose == 0 ? "0" : "1", res.IsLong ? "<" : ">", null, res.IsLong ? "+1" : "-1", res.IsLong ? "+" : "-", res.IsLong ? "-" : "+");

                if (intraday)
                {
                    str.AppendFormat(@"

        // INTRADAY
        TRADES_TODAY += 1;");
                }

                str.AppendLine(@"
    end;");

                if (strategyRules.Count > 0)
                {
                    str.AppendFormat(@"
    if sigCount <= Required4Exit then
        {0} (""EnsembleX"") next bar market;
", res.IsLong ? "<b>Sell</b>" : "<b>BuyToCover</b>");
                }

                str.AppendFormat(@"
    // Profitable closes
    if isClose and Close <b>{6}</b> EntryPrice then
        prof_x += 1;
    if BarsSinceEntry < Max_Time{3} and Profitable_Closes > 0 and prof_x >= Profitable_Closes then begin
        {0} (""ProfX"") all contracts {1};
        PT = Round2Fraction(Close[0] <b>{12}</b> atr[0] * pt_mult);
        SL = Round2Fraction(Close[0] <b>{13}</b> atr[0] * sl_mult);
        TL = Round2Fraction(Close[0] <b>{13}</b> atr[0] * tl_mult);
    end;
    // Max bars in trade
    if BarsSinceEntry >= Max_Time{3} then begin
        {0} (""TimeX"") all contracts {1};
        PT = Round2Fraction(Close[0] <b>{12}</b> atr[0] * pt_mult);
        SL = Round2Fraction(Close[0] <b>{13}</b> atr[0] * sl_mult);
        TL = Round2Fraction(Close[0] <b>{13}</b> atr[0] * tl_mult);
        prof_x = 0;
    end;

    // Highest high
    if HH_ON = 1 then
        if isIBOG then
            {0} (""HHx "") all contracts {2} hh[1] <b>{8}</b>
        else
            {0} (""HHx"") all contracts {2} hh <b>{8}</b>;

    // Lowest Low
    if LL_ON = 1 then
        if isIBOG then
            {0} (""LLx "") all contracts {2} ll[1] <b>{9}</b>
        else
            {0} (""LLx"") all contracts {2} ll <b>{9}</b>;

    // Profit Target
    if PT_ON = 1 then
        {0} (""PTx"") all contracts {2} PT limit;
    if PT_ON = 2 then
        setprofittarget(pt_mult);

    // Stop Loss
    if SL_ON = 1 then
        {0} (""SLx"") all contracts {2} SL stop;
    if SL_ON = 2 then
        setstoploss(sl_mult);

    // Trail Loss
    If TL_ON = 1 then
        {0} (""TLx"") all contracts {2} TL stop;
    If TL_ON = 2 then
        setdollartrailing(tl_mult);

    // Force End of Day
    <b>{4}</b> if Time >= <b>{5:D4}</b> then {0} (""EODx"") all contracts {1};

    // Signal Exit
    <b>{10}</b>If BarsSinceEntry < Max_Time{3} and <b>{11}</b> then begin
    <b>{10}</b>    {0} (""SigX"") all contracts {1};
    <b>{10}</b>    PT = Round2Fraction(Close[0] <b>{12}</b> atr[0] * pt_mult);
    <b>{10}</b>    SL = Round2Fraction(Close[0] <b>{13}</b> atr[0] * sl_mult);
    <b>{10}</b>    TL = Round2Fraction(Close[0] <b>{13}</b> atr[0] * tl_mult);
    <b>{10}</b>    prof_x = 0;
    <b>{10}</b>end;
", res.IsLong ? "<b>Sell</b>" : "<b>BuyToCover</b>", close, "next bar", res.EntryOnClose == 0 ? "<b>-1</b>" : "",
res.ForceExitOnSessionEnd ? "" : "// ", res.SessionEndTime, res.IsLong ? ">=" : "<=", res.IsLong ? "-1" : "+1",
res.IsLong ? "limit" : "stop", res.IsLong ? "stop" : "limit",
res.ExitMode > 0 && res.ReadonlyExitSignals?.Count > 0 ? "" : "//", rulesExitString.ToString(),
res.IsLong ? "+" : "-", res.IsLong ? "-" : "+");

                if (isRebalance)
                {
                    string rebCondition = "";
                    if (res.RebalancePeriod == 1) rebCondition = " and (month(date) = 3 or month(date) = 6 or month(date) = 9 or month(date) = 12)";
                    else if (res.RebalancePeriod == 2) rebCondition = " and month(date) = 12";

                    str.AppendFormat(@"
    // Rotation Exit
    if (TDLM = 0 and barssinceentry > 0{2}) then begin
    	{0} (""RotX"") all contracts {1};
    end;
", res.IsLong ? "<b>Sell</b>" : "<b>BuyToCover</b>", close, rebCondition);
                }

                str.AppendFormat(@"
end else
    if mp = <b>{0}</b> then begin
end;
", res.IsLong ? "-1" : "+1");

                // signals extra code
                if (extraCodeEntry.Length > 0)
                    str.AppendLine($"{Environment.NewLine}<b>{extraCodeEntry}</b>");

                str.Append(@"
// -----------------------------------------
//  entry
//
if isClose then begin");

                if (intraday) str.Append(@"
    // INTRADAY
    If time >= session_start and time[1] < session_start then begin
        trades_today = 0;
        pnl_today = 0;
        start_pnl = netprofit;	
    end;
");
                str.AppendFormat(@"
    condition1 = <b>{0}{1}</b>;
end;
", rulesString.ToString(), intraday ? " and time >= start_time and time <= end_time and trades_today < max_trades and pnl_today > min_pnl and pnl_today < max_pnl" : "");

                str.AppendFormat(@"
if isClose and condition1[delay]{10} then begin
    {0} (""Entry"") PS contract {1};

    if mp = 0 and isIBOG = false then begin

        PT = Round2Fraction(Close[0] <b>{8}</b> atr[0] * pt_mult);
        SL = Round2Fraction(Close[0] <b>{9}</b> atr[0] * sl_mult);
        TL = Round2Fraction(Close[0] <b>{9}</b> atr[0] * tl_mult);

        // Profit target
        if PT_ON = 1 then
            {4} (""PTx1"") all contracts next bar PT limit;
        if PT_ON = 2 then
            setprofittarget(pt_mult);

        // Stop loss
        if SL_ON = 1 then
            {4} (""SLx1"") all contracts next bar SL stop;
        if SL_ON = 2 then
            setstoploss(sl_mult);

        // Trail Loss
        If TL_ON = 1 then
            {4} (""TLx1"") all contracts next bar TL stop;
        If TL_ON = 2 then
            setdollartrailing(tl_mult);

        // Highest high
        if HH_ON = 1 then
            {4} (""HHx1"") all contracts next bar hh <b>{6}</b>;

        // Lowest Low
        if LL_ON = 1 then
            {4} (""LLx1"") all contracts next bar ll <b>{7}</b>;
    end;
end;

", res.IsLong ? "<b>Buy</b>" : "<b>SellShort</b>", open, null, null, res.IsLong ? "<b>Sell</b>" : "<b>BuyToCover</b>", null,
res.IsLong ? "limit" : "stop", res.IsLong ? "stop" : "limit", res.IsLong ? "+" : "-", res.IsLong ? "-" : "+",
(strategyRules.Count > 0) ? " and sigCount >= Required4Entry" : "");

                {
                    var evSigs = res.ReadonlyEntrySignals?.Where(x => x is SignalNewsEvents);
                    if (evSigs != null)
                        foreach (var sig in evSigs)
                        {
                            var extraEVcode = GetEventsExtraCode(new List<Signal>() { sig }, Security.ToDateTime(res.SignalStartDate, res.SignalStartTime), Security.ToDateTime(res.StopDate, 235959), "EL");
                            if (string.IsNullOrEmpty(extraEVcode) == false)
                            {
                                extraEvCode.Add(new KeyValuePair<string, string>(/*(extraEvCode.Count + 1).ToString()*/sig.Text, extraEVcode));
                            }
                        }
                    evSigs = res.ReadonlyExitSignals?.Where(x => x is SignalNewsEvents);
                    if (evSigs != null)
                        foreach (var sig in evSigs)
                        {
                            var extraEVcode = GetEventsExtraCode(new List<Signal>() { sig }, Security.ToDateTime(res.SignalStartDate, res.SignalStartTime), Security.ToDateTime(res.StopDate, 235959), "EL");
                            if (string.IsNullOrEmpty(extraEVcode) == false && extraEvCode.All(x => x.Value != extraEVcode))
                            {
                                extraEvCode.Add(new KeyValuePair<string, string>(/*(extraEvCode.Count + 1).ToString()*/sig.Text, extraEVcode));
                            }
                        }
                }
            }

            if (extraEvCode.Count == 0) extraEvCode = null;

            return new List<string>() { str.ToString() };
        }


        public static List<string> NT(List<IReadonlySimStrategy> activeResults, out List<KeyValuePair<string, string>> extraEvCode)
        {
            StringBuilder str = new StringBuilder();
            extraEvCode = new List<KeyValuePair<string, string>>();


            str.AppendLine(@"#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.Strategies;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NinjaTrader.NinjaScript.DrawingTools;
using System.Windows.Media;
using NinjaTrader.NinjaScript.AddOns;

#endregion
");

            foreach (var res in activeResults)
            {
                int atLeastEntry = 0;
                int atLeastExit = 0;
                StringBuilder rulesString = new StringBuilder();
                List<string> triggersIndex = new List<string>();
                List<string> triggersRule1 = new List<string>();
                List<string> triggersExitIndex = new List<string>();
                List<string> triggersExitRule1 = new List<string>();
                int triggersCount = 0;
                {
                    if (res.ReadonlyEntrySignals == null)
                        rulesString.Append(res.Name);
                    else
                    {
                        int strCount = 0;
                        for (int i = 0; i < res.ReadonlyEntrySignals.Count; ++i)
                        {
                            if (res.ReadonlyEntrySignals[i] is SignalStrategy)
                            {
                                ++strCount;
                            }
                            else if (res.ReadonlyEntrySignals[i] is SignalEnsemble)
                            {
                                atLeastEntry = Math.Max(atLeastEntry, (res.ReadonlyEntrySignals[i] as SignalEnsemble).atLeast);
                            }
                            else
                            {
                                if (res.ReadonlyEntrySignals[i] is SignalTrigger sigTrig && sigTrig.Children?.Count >= 2)
                                {
                                    sigTrig.triggerIndex = triggersCount++;         // specify index before getting code
                                    triggersIndex.Add(sigTrig.triggerIndex.ToString());
                                    triggersRule1.Add(sigTrig.Children[0].CodeNT);
                                }

                                rulesString.AppendFormat(" && {0}", res.ReadonlyEntrySignals[i].CodeNT);
                            }
                        }
                        atLeastEntry += strCount;
                        if (rulesString.Length > 0)
                            rulesString.Remove(0, 4);
                        else
                            rulesString.Append("Close[0] != 0");

                        rulesString.Insert(0, string.Format("(ToDay(Time[0])>{0} || (ToDay(Time[0])=={0} && ToTime(Time[0])>={1})) &&{2}",
                            res.SignalStartDate, res.SignalStartTime, Environment.NewLine));
                    }
                }
                StringBuilder rulesExitString = new StringBuilder();
                {
                    for (int i = 0; res.ReadonlyExitSignals != null && i < res.ReadonlyExitSignals.Count; i++)
                    {
                        if (res.ReadonlyExitSignals[i] is SignalStrategy || res.ReadonlyExitSignals[i] is SignalEnsemble)
                            ++atLeastExit;
                        else
                        {
                            if (res.ReadonlyExitSignals[i] is SignalTrigger sigTrig && sigTrig.Children?.Count >= 2)
                            {
                                sigTrig.triggerIndex = triggersCount++;         // specify index before getting code
                                triggersExitIndex.Add(sigTrig.triggerIndex.ToString());
                                triggersExitRule1.Add(sigTrig.Children[0].CodeNT);
                            }

                            rulesExitString.AppendFormat("{0} && ", res.ReadonlyExitSignals[i].CodeNT);
                        }
                    }
                    if (rulesExitString.Length > 0)
                        rulesExitString.Length -= 4;
                }

                bool intraday = Properties.Settings.Default.TTaddCode && res.ReadonlyIntradayTestResults?.Count == 5;
                bool isRebalance = (res.TradeMode == TradeMode.LongRebalance || res.TradeMode == TradeMode.ShortRebalance)/* && res.ChildStrategiesCount > 0*/;

                List<SymbolId> rebalanceSymbols = null;
                if (isRebalance)
                {
                    if (res.ReadonlyChildren?.Count() > 0)
                        rebalanceSymbols = res.ReadonlyChildren.Select(x => x.SymbolId).ToList();
                    if (res.ReadonlyParent != null)
                        rebalanceSymbols = res.ReadonlyParent.ReadonlyChildren.Select(x => x.SymbolId).ToList();
                }

                str.AppendFormat(@"/*
    Strategy Details:
    Symbol: <b>{0}</b>
    Market 2: <b>{15}</b>
    Market 3: <b>{16}</b>
    Market 4: <b>{17}</b>
    Start Date: <b>{1}</b>
    Stop Date: <b>{2}</b>
    Out of Sample %: <b>{3} %</b>
    Fitness Function: <b>{4}</b>
    Profit Target On: <b>{5}</b>
    Profit Multiple: <b>{6}</b>
    Stop Loss On: <b>{7}</b>
    Stop Loss Multiple: <b>{8}</b>
    Highest High On: <b>{9}</b>
    Highest High Lookback: <b>{10}</b>
    Lowest Low On: <b>{11}</b>
    Lowest Low Lookback: <b>{12}</b>
    Max Time: <b>{13}</b>
    Profitable Closes: <b>{14}</b>
*/
", res.Symbol, res.SignalStartDate, res.StopDate, res.OutOfSample,
res.Fitness.ToString(),
res.PT_ON > 0 ? "Yes" : "No", res.PT_mult, res.SL_ON > 0 ? "Yes" : "No", res.SL_mult,
res.HH_ON > 0 ? "Yes" : "No", res.HHlook, res.LL_ON > 0 ? "Yes" : "No", res.LLlook,
res.MaxTime == 0 ? 9999 : res.MaxTime, res.ProfX == 0 ? 9999 : res.ProfX,
res.GetMarketSymbolId(2).IsEmpty() ? "---" : res.GetMarketSymbolId(2).ToString(),
res.GetMarketSymbolId(3).IsEmpty() ? "---" : res.GetMarketSymbolId(3).ToString(),
res.GetMarketSymbolId(4).IsEmpty() ? "---" : res.GetMarketSymbolId(4).ToString());

                string mrkt = "Other";
                if (res.SymbolType == SymbolType.FOREX) mrkt = "FX";
                else if (res.SymbolType == SymbolType.Futures) mrkt = "Fut";
                string kind = "Default";
                if (res.PosSizeMode == PositionSizingMode.Fixed) kind = "Fixed";
                else if (res.PosSizeMode == PositionSizingMode.Volatility) kind = "ATR";

                str.AppendFormat(@"
namespace NinjaTrader.NinjaScript.Strategies
{{
    public class <b>{13}</b> : Strategy
    {{
        #region Constants

        private const string StrategyName = ""StrategyY"";
        private const string StrategyVersion = """";

        private const string StrategyDescription = ""Please add strategy description here."";
        private const int FirstValidValue = 256;
        private const string ProfxExitName = ""ProfX"";
        private const string TimexExitName = ""TimeX"";
        private const string MarketTypeFx = ""FX"";
        private const string MarketTypeFutures = ""Fut"";
        private const string MarketTypeOther = ""Other"";
        private const string KindFixed = ""Fixed"";
        private const string KindAtr = ""ATR"";
        private const string KindDefault = ""Default"";
        private const string EnterLongName = ""Open Long"";
        private const string EnterShortName = ""Open Short"";

        private const string StaticATRStopName = ""ATR Stop"";
        private const string StaticHhLlStopName = ""HHLL Stop"";
        private const string TrailATRStopName = ""ATR Trail Stop"";
        private const string DollarStopName = ""Dollar Stop"";
        private const string DollarTrailName = ""Dollar Trail"";

        private const string StaticProfitTargetName = ""Dollar Target"";
        private const string AtrProfitTargetName = ""ATR Profit Target"";
        private const string HhLlProfitTargetName = ""HHLL Profit Target"";

        #endregion

        #region Variables

        private MyMarketPositionTypes _currentPositionType;

        public enum MyMarketPositionTypes
        {{
            Long,
            Short,
            Both,
            None
        }}

        private string _stopLossName;
        private string _profitTargetName;
        private string _entryName;

        private int _currentPosType;
        private int _prevPosType;
        private int _tradesToday;
        private int _profitCloseExitCounter;

        private double _prevAvgPrice;
        private double _startOfDayCumPnl;
        private double _pnlToday;
        private double _profitTarget;
        private double _stop;
        private double _profit;

        private bool _entryThisBar;
        private bool _exitInitiated;
        private bool _entrySignalFinal;
        private bool _isMaster;

        private string _symbol = <b>""{12}""</b>;
        private int _atrPeriod = <b>{0}</b>;
        private int _sessoinTime = <b>{1:D4}00</b>;
        private int _startTime = <b>{2}</b>;
        private int _endTime   = <b>{3}</b>;
        private int _maxTrades = <b>{4}</b>;
        private double _maxPnl = <b>{5}</b>;
        private double _minPnl = <b>{6}</b>;
        private string _mrkt   = <b>""{7}""</b>;
        private string _kind   = <b>""{8}""</b>;
        private string _target = <b>""{9}""</b>;
        private bool _round    = true;
        private float _acct    = <b>{10}</b>;
        private float _margin  = <b>{11}</b>;
        private int _posSize   = 1;
        private bool _allowReEntryInBar = false;
", res.ATR_len, res.SessionEndTime, intraday ? res.ReadonlyIntradayTestResults[2] : 0, intraday ? res.ReadonlyIntradayTestResults[3] : 235959,
intraday ? res.ReadonlyIntradayTestResults[4] : 999999, intraday ? res.ReadonlyIntradayTestResults[0] : 999999, intraday ? res.ReadonlyIntradayTestResults[1] : -999999,
mrkt, kind, res.Currency, res.AccountValue,
res.SymbolType == SymbolType.Futures || res.SymbolType == SymbolType.Custom ? Utils.SymbolsManager.GetSymbolMargin(res.SymbolId) : 0,
res.ChildStrategiesCount > 0 ? res.ReadonlyChildren.First().Symbol : res.Symbol,
atLeastEntry > 0 ? "Ensemble" : "SlaveY");

                if (atLeastEntry > 0)
                    str.Append(@"
        //Ensemble
        private string mEntryNameLong   = ""Long"";
        private string mEntryNameShort = ""Short"";
        private int mProfitableCount = 0;
        private Series<int> mCntSignals;
        private bool entry_signal = false;
        private bool exited = false;
");
                else str.Append(@"
        private double _dollarTrail;
        private double _tempTrail;
");

                if (isRebalance)
                    str.AppendFormat(@"
        private double[] Metrics;
        private bool ok_to_trade = false;
        private int bskt_sz = <b>{0}</b>;
        private int rotation_signal = <b>{1}</b>; // 0 OFF, 1 Monthly, 2 Quarterly, 3 Annually
        private bool rotation_exit = false;

", res.RebalanceSymbolsCount, res.RebalancePeriod + 1);

                for (int i = 0; i < triggersIndex.Count; ++i)
                    str.AppendFormat(@"<b>
        private Series<bool> trigger{0};</b>", triggersIndex[i]);
                for (int i = 0; i < triggersExitIndex.Count; ++i)
                    str.AppendFormat(@"<b>
        private Series<bool> trigger{0};</b>", triggersExitIndex[i]);

                str.Append(@"
        #endregion

        #region Parameters

        [Display(Name = ""Entry Direction"", GroupName = ""NinjaScriptParameters"", Order = 0)]
        public MyMarketPositionTypes StrategyMarketPositionType { get; set; }
");
                if (atLeastEntry > 0)
                    str.Append(@"
        [Display(Name = ""Get slaves of ID"", GroupName = ""Ensemble"", Order = 0)]
        public string SlavesID { get; set; }
");
                else
                    str.Append(@"
        [Display(Name = ""Get slaves of ID"", GroupName = ""Master-Slave Settings"", Order = 0)]
        public string SlaveIDs { get; set; }
");

                str.AppendLine(@"
        [Display(Name = ""Request External Data"", GroupName = ""My Parameters"", Order = 0)]
        public bool RequestExternalData { get; set; }

        [Display(Name = ""Symbol_1"", GroupName = ""My Parameters"", Order = 0)]
        public string SymbolOne { get; set; }

        [Display(Name = ""Symbol_2"", GroupName = ""My Parameters"", Order = 0)]
        public string SymbolTwo { get; set; }

        [Display(Name = ""Symbol_3"", GroupName = ""My Parameters"", Order = 0)]
        public string SymbolThree { get; set; }

        [Display(Name = ""Symbol_4"", GroupName = ""My Parameters"", Order = 0)]
        public string SymbolFour { get; set; }");

                if (isRebalance && rebalanceSymbols != null)
                    for (int i = 0; i < rebalanceSymbols.Count - 1; ++i)
                        str.AppendFormat(@"
        [Display(Name = ""Symbol_{0}"", GroupName = ""My Parameters"", Order = 0)]
        public string Symbol{0} {{ get; set; }}
", i + 5);

                str.AppendLine(@"
        [Display(Name = ""Symbol Period Type"", GroupName = ""My Parameters"", Order = 0)]
        public BarsPeriodType SymbolPeriodType { get; set; }

        [Display(Name = ""Symbol Period Length"", GroupName = ""My Parameters"", Order = 0)]
        public int SymbolPeriodLength { get; set; }

        [Display(Name = ""Delay Entry Bars"", GroupName = ""My Parameters"", Order = 1)]
        public int DelayByBars { get; set; }


        #region Profit Targets

        [Display(Name = ""Dollar Target"", GroupName = ""Profit Target Parameters"", Order = 0)]
        public bool DollarTargetIsOn { get; set; }

        [Display(Name = ""Dollar Target Value"", GroupName = ""Profit Target Parameters"", Order = 1)]
        public double ProfitTargetDollars { get; set; }

        [Display(Name = ""ATR Profit Target"", GroupName = ""Profit Target Parameters"", Order = 2)]
        public bool AtrProfitTargetIsOn { get; set; }

        [Display(Name = ""ATR Profit Target Coef"", GroupName = ""Profit Target Parameters"", Order = 3)]
        [Range(0, int.MaxValue)]
        public double ProfitTargetCoef { get; set; }

        [Display(Name = ""LLHH Profit Target"", GroupName = ""Profit Target Parameters"", Order = 4)]
        public bool LlHhProfitTargetIsOn { get; set; }

        [Display(Name = ""LLHH Profit Period"", GroupName = ""Profit Target Parameters"", Order = 5)]
        public int LlHhProfitPeriod { get; set; }

        #endregion

        #region StopLoss

        [Display(Name = ""Dollar Stop"", GroupName = ""Stop Loss Parameters"", Order = 0)]
        public bool DollarStop { get; set; }

        [Display(Name = ""Dollar Stop Value"", GroupName = ""Stop Loss Parameters"", Order = 1)]
        public double DollarStopValue { get; set; }

        [Display(Name = ""Dollar Stop Trail"", GroupName = ""Stop Loss Parameters"", Order = 2)]
        public bool DollarStopTrail { get; set; }

        [Display(Name = ""Dollar Stop Trail Value"", GroupName = ""Stop Loss Parameters"", Order = 3)]
        public double DollarStopTrailValue { get; set; }

        [Display(Name = ""ATR Stop Loss"", GroupName = ""Stop Loss Parameters"", Order = 4)]
        public bool StaticAtrStopLossIsOn { get; set; }

        [Display(Name = ""ATR Stop Loss Coef"", GroupName = ""Stop Loss Parameters"", Order = 5)]
        [Range(0,int.MaxValue)]
        public double StopLossCoef { get; set; }

        [Display(Name = ""ATR Trailing Stop Loss"", GroupName = ""Stop Loss Parameters"", Order = 6)]
        public bool TrailAtrStopLossIsOn { get; set; }

        [Display(Name = ""ATR Trailing Stop Loss Coef"", GroupName = ""Stop Loss Parameters"", Order = 7)]
        [Range(0, int.MaxValue)]
        public double TrailAtrStopLossCoef { get; set; }

        [Display(Name = ""LLHH Stop Loss"", GroupName = ""Stop Loss Parameters"", Order = 8)]
        public bool HhLlStopLossIsOn { get; set; }

        [Display(Name = ""LLHH Stop Period"", GroupName = ""Stop Loss Parameters"", Order = 9)]
        public int HhLlStopPeriod { get; set; }

        #endregion

        #region Exits

        [NinjaScriptProperty]
        [Display(Name = ""Profit Close Exit"", GroupName = ""Exit Parameters"", Order = 0)]
        public bool ProfitExit { get; set; }

        [NinjaScriptProperty]
        [Display(Name = ""Profitable Closes"", GroupName = ""Exit Parameters"", Order = 1)]
        public int ProfitableCloses { get; set; }

        [NinjaScriptProperty]
        [Display(Name = ""Time Exit"", GroupName = ""Exit Parameters"", Order = 2)]
        public bool TimeExit { get; set; }

        [NinjaScriptProperty]
        [Display(Name = ""Max Time"", GroupName = ""Exit Parameters"", Order = 3)]
        public int Maxtime { get; set; }

        #endregion");

                if (atLeastEntry > 0)
                    str.Append(@"
        #region Ensemble
        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name=""Signals For Entry"", Description=""Min Number of Slave Signals for Position Entry"", Order=1, GroupName=""Ensemble"")]
        public int SignalsForEntry
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name=""Signals For Exit"", Description=""Min Number of Slave Signals for Position Exit"", Order=2, GroupName=""Ensemble"")]
        public int SignalsForExit
        { get; set; }

        [NinjaScriptProperty]
        [Range(0, int.MaxValue)]
        [Display(Name=""Bar Delay For Entry"", Description=""Number of Bars Delay for Position Entry"", Order=3, GroupName=""Ensemble"")]
        public int BarOffsetEntry
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name=""Use Long Signals"", Description=""Use Long Signals"", Order=4, GroupName=""Ensemble"")]
        public bool UseLongSignals
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name=""Use Short Signals"", Description=""Use Short Signals"", Order=5, GroupName=""Ensemble"")]
        public bool UseShortSignals
        { get; set; }

        [NinjaScriptProperty]
        [Display(Name=""Use Signals for Exit"", Description=""Use Signals for Exit"", Order=6, GroupName=""Ensemble"")]
        public bool UseSignalsForExit
        { get; set; }

        #endregion
");

                str.AppendLine(@"
        #endregion");

                SymbolId convSt = new SymbolId("EURUSD", "");
                bool fxNotDef = false;
                if (res.SymbolType == SymbolType.FOREX)
                {
                    double unused = 0;
                    if (res.PosSizeMode == PositionSizingMode.Fixed)
                    {
                        if (Utils.SymbolsManager.GetSymbolType(res.SymbolId) == SymbolType.FOREX)
                        {
                            if (res.Symbol.Substring(0, 3) != res.Currency)
                            {
                                fxNotDef = true;
                                convSt = new SymbolId(res.Symbol.Substring(0, 3) + res.Currency, "");
                                if (!File.Exists(Utils.SymbolsManager.GetSymbolFileNameMult(convSt, out unused)))
                                {
                                    convSt = new SymbolId(res.Currency + res.Symbol.Substring(0, 3), "");
                                    if (!File.Exists(Utils.SymbolsManager.GetSymbolFileNameMult(convSt, out unused)))
                                        convSt = new SymbolId("EURUSD", "");
                                }
                            }
                        }
                        else if ("USD" != res.Currency)
                        {
                            fxNotDef = true;
                            convSt = new SymbolId("USD" + res.Currency, "");
                            if (!File.Exists(Utils.SymbolsManager.GetSymbolFileNameMult(convSt, out unused)))
                            {
                                convSt = new SymbolId(res.Currency + "USD", "");
                                if (!File.Exists(Utils.SymbolsManager.GetSymbolFileNameMult(convSt, out unused)))
                                    convSt = new SymbolId("EURUSD", "");
                            }
                        }
                    }
                    else if (res.PosSizeMode == PositionSizingMode.Volatility)
                    {
                        if (Utils.SymbolsManager.GetSymbolType(res.SymbolId) == SymbolType.FOREX)
                        {
                            if (res.Symbol.Substring(3) != res.Currency)
                            {
                                fxNotDef = true;
                                convSt = new SymbolId(res.Currency + res.Symbol.Substring(3), "");
                                if (!File.Exists(Utils.SymbolsManager.GetSymbolFileNameMult(convSt, out unused)))
                                {
                                    convSt = new SymbolId(res.Symbol.Substring(3) + res.Currency, "");
                                    if (!File.Exists(Utils.SymbolsManager.GetSymbolFileNameMult(convSt, out unused)))
                                        convSt = new SymbolId("EURUSD", "");
                                }
                            }
                        }
                        else if ("USD" != res.Currency)
                        {
                            fxNotDef = true;
                            convSt = new SymbolId(res.Currency + "USD", "");
                            if (!File.Exists(Utils.SymbolsManager.GetSymbolFileNameMult(convSt, out unused)))
                            {
                                convSt = new SymbolId("USD" + res.Currency, "");
                                if (!File.Exists(Utils.SymbolsManager.GetSymbolFileNameMult(convSt, out unused)))
                                    convSt = new SymbolId("EURUSD", "");
                            }
                        }
                    }
                }

                string trigStr = "";
                for (int i = 0; i < triggersIndex.Count; ++i)
                    trigStr += string.Format(@"
                trigger{0} = new Series<bool>(this);", triggersIndex[i]);
                for (int i = 0; i < triggersExitIndex.Count; ++i)
                    trigStr += string.Format(@"
                trigger{0} = new Series<bool>(this);", triggersExitIndex[i]);

                str.AppendFormat(@"
        protected override void OnStateChange()
        {{
            if (State == State.SetDefaults)
            {{
                InitializeSettings();
                SetDefaultParameters();
                {0}
            }}
            else if (State == State.Configure)
            {{
                if (RequestExternalData)
                    AddExternalData();

                {3}Metrics = new double[<b>{4}</b>];
            }}
            else if (State == State.DataLoaded)
            {{
                {1}<b>{2}</b>
            }}
        }}
", atLeastEntry > 0 ? @"
                // Ensemble
                SignalsForEntry   = " + atLeastEntry + @";
                SignalsForExit    = " + atLeastExit + @";
                BarOffsetEntry    = 0;
                UseLongSignals    = true;
                UseShortSignals   = false;
                UseSignalsForExit = false;
" : "", atLeastEntry > 0 ? @"
                //Ensemble
                mCntSignals = new Series<int>(this);" : "", trigStr,
                isRebalance ? "" : "//", rebalanceSymbols?.Count ?? 0);

                if (triggersIndex.Count > 0 || triggersExitIndex.Count > 0)
                    str.AppendLine(@"
        int triggerFunction(Series<bool> test, int N)
        {
	        int trigger = 0;
	        for (int i = 0; i < N; i++)
	        {
		        if (test[i])
			        trigger += 1;
	        }
	        return trigger;
        }");

                StringBuilder rebSymbols = new StringBuilder();
                if (isRebalance && rebalanceSymbols != null)
                {
                    var sidBase = res.ChildStrategiesCount > 0 ? res.ReadonlyChildren.First().SymbolId : res.SymbolId;
                    int i = 5;
                    foreach (var symId in rebalanceSymbols)
                    {
                        if (symId == sidBase) continue;
                        rebSymbols.AppendLine($"            Symbol{i++} = \"{symId}\";");
                    }
                }

                str.AppendFormat(@"
        private void InitializeSettings()
        {{
            ClearOutputWindow();
            Description                  = StrategyDescription;
            Name                         = StrategyName + StrategyVersion;
            Calculate                    = Calculate.OnBarClose;
            EntriesPerDirection          = 100;
            EntryHandling                = EntryHandling.AllEntries;
            IsExitOnSessionCloseStrategy = <b>{0}</b>;
            ExitOnSessionCloseSeconds    = 30;
            IsFillLimitOnTouch           = false;
            MaximumBarsLookBack          = MaximumBarsLookBack.TwoHundredFiftySix;
            OrderFillResolution          = OrderFillResolution.Standard;
            Slippage                     = <b>{1}</b>;
            StartBehavior                = StartBehavior.WaitUntilFlat;
            TimeInForce                  = TimeInForce.Gtc;
            TraceOrders                  = false;
            RealtimeErrorHandling        = RealtimeErrorHandling.StopCancelClose;
            StopTargetHandling           = StopTargetHandling.PerEntryExecution;
            BarsRequiredToTrade          = 255;
            IsInstantiatedOnEachOptimizationIteration = true; 
        }}

        private void SetDefaultParameters()
        {{
            TimeExit = true;
            ProfitExit = false;
            DollarTargetIsOn = <b>{5}</b>;
            AtrProfitTargetIsOn = <b>{4}</b>;
            StaticAtrStopLossIsOn = <b>{6}</b>;
            TrailAtrStopLossIsOn = <b>{8}</b>;
            DollarStopTrail = <b>{9}</b>;
            DollarStop = <b>{7}</b>;
            HhLlStopLossIsOn = <b>{2}</b>;
            LlHhProfitTargetIsOn = <b>{3}</b>;
            SymbolOne = ""^VIX"";
            SymbolTwo = <b>""{12}""</b>;
            SymbolThree = <b>""{13}""</b>;
            SymbolFour = <b>""{14}""</b>;
<b>{22}</b>
            SymbolPeriodType = BarsPeriodType.Day;
            SymbolPeriodLength = 1;
            StrategyMarketPositionType = MyMarketPositionTypes.<b>{15}</b>;
            ProfitableCloses = <b>{10}</b>;
            Maxtime = <b>{11}</b>;
            LlHhProfitPeriod = <b>{17}</b>;
            HhLlStopPeriod = <b>{16}</b>;
            DelayByBars = <b>{18}</b>;
            StopLossCoef = <b>{19}</b>;
            ProfitTargetCoef = <b>{20}</b>;
            TrailAtrStopLossCoef = <b>{21}</b>;
            ProfitTargetDollars = <b>{20}</b>;
            DollarStopValue = <b>{19}</b>;
            DollarStopTrailValue = <b>{21}</b>;
        }}
", res.ForceExitOnSessionEnd ? "true" : "false", res.Slippage, res.HH_ON > 0 ? "true" : "false", res.LL_ON > 0 ? "true" : "false",
res.PT_ON == 1 ? "true" : "false", res.PT_ON == 2 ? "true" : "false", res.SL_ON == 1 ? "true" : "false", res.SL_ON == 2 ? "true" : "false",
res.TL_ON == 1 ? "true" : "false", res.TL_ON == 2 ? "true" : "false",
res.ProfX == 0 ? 9999 : res.ProfX, res.MaxTime == 0 ? 9999 : res.MaxTime,
res.GetMarketSymbolId(2).IsEmpty() ? "None" : res.GetMarketSymbolId(2).Name,
res.GetMarketSymbolId(3).IsEmpty() ? "None" : res.GetMarketSymbolId(3).Name,
convSt, res.IsLong ? "Long" : "Short",
res.HHlook, res.LLlook, res.DelayedEntry, res.SL_mult, res.PT_mult, res.TL_mult, rebSymbols.ToString());

                StringBuilder rebAddDataSeries = new StringBuilder();
                if (isRebalance && rebalanceSymbols != null)
                {
                    for (int z = 0; z < rebalanceSymbols.Count - 1; ++z)
                        rebAddDataSeries.AppendLine($"            AddDataSeries(Symbol{z + 5}, SymbolPeriodType, SymbolPeriodLength);");
                }

                str.AppendFormat(@"
        private void AddExternalData()
        {{
            {6}AddDataSeries(SymbolOne, SymbolPeriodType, SymbolPeriodLength);
            {0}AddDataSeries(SymbolTwo, SymbolPeriodType, SymbolPeriodLength);
            {1}AddDataSeries(SymbolThree, SymbolPeriodType, SymbolPeriodLength);
            {2}AddDataSeries(SymbolFour, SymbolPeriodType, SymbolPeriodLength);
{5}        }}

        private bool _enterLong;
        private bool _enterShort;
        private bool _exitLong;
        private bool _exitShort;
        {3}

        {4}protected override void OnPositionUpdate(Position position, double averagePrice, int quantity, MarketPosition marketPosition)
        {4}{{
        {4}    TfSyncMasterSlave.WriteToSlaveFile(this, position, SlaveIDs);	
        {4}}}
", (!res.GetMarketSymbolId(2).IsEmpty() || fxNotDef || isRebalance) ? "" : "<b>//</b>",
   (!res.GetMarketSymbolId(3).IsEmpty() || fxNotDef || isRebalance) ? "" : "<b>//</b>",
(fxNotDef || isRebalance) ? "" : "<b>//</b>",
atLeastEntry > 0 ? "private bool _ensembleExit;" : "",
atLeastEntry > 0 ? "//" : "",
rebAddDataSeries.ToString(),
isRebalance ? "" : "//");

                str.AppendLine(@"
        protected override void OnBarUpdate()
        {
            if (CurrentBar < FirstValidValue)
                return;

            if (BarsInProgress != 0)               
                return;
");

                if (isRebalance)
                {
                    string per = res.RebalancePeriod == 0 ? "Monthly" : (res.RebalancePeriod == 1 ? "Quarterly" : "Annually");
                    string met = "pf";
                    switch (res.RebalanceMethod)
                    {
                        case RebalanceMethod.ProfitFactor: met = "pf"; break;
                        case RebalanceMethod.WinningPercentage: met = "wp"; break;
                        case RebalanceMethod.AverageReturn: met = "avg"; break;
                        case RebalanceMethod.Volatility: met = "vol"; break;
                        case RebalanceMethod.Sharpe: met = "shrp"; break;
                        case RebalanceMethod.RangeLocation: met = "rng"; break;
                        case RebalanceMethod.RateOfChange: met = "roc"; break;
                        case RebalanceMethod.Momentum: met = "momo"; break;
                        case RebalanceMethod.FipScore: met = "fip"; break;
                    }

                    str.AppendFormat(@"
            double thresh = RotationSend(<b>""{0}"", ""{1}""</b>)[0];
            Metrics[0] = thresh;

            for (int i = BarsArray.Length - <b>{2}</b>; i < BarsArray.Length; i++)
                Metrics[i - 4] = RotationSend(BarsArray[i], <b>""{0}"", ""{1}""</b>)[0];

            if (bskt_sz > 0)
                Metrics = Metrics.OrderByDescending(c => c).ToArray();
            else
                Metrics = Metrics.OrderBy(c => c).ToArray();

            ok_to_trade = false;
            if (bskt_sz > 0 && thresh >= Metrics[bskt_sz - 1])
                ok_to_trade = true;
            else if (bskt_sz < 0 && thresh <= Metrics[Math.Abs(bskt_sz) - 1])
                ok_to_trade = true;

            Print(DateTime.Now.ToString() + ""|"" + Name + ""|Time "" + Time[0] + "", DataSeries"" + BarsArray.Length);
", per, met, rebalanceSymbols.Count - 1);
                }

                str.AppendFormat(@"
            SetGlobalPositionSize();
            CalculateDailyTradesAndPnl();
            SetEntrySignals();
            SetExitSignals();
            {0}SetRotationExitSignal();
", res.TradeMode == TradeMode.Long || res.TradeMode == TradeMode.Short ? "//" : "");

                if (atLeastEntry > 0)
                    str.Append(@"
            // Ensemble
            // use aggregated slave signal for trading

            if (UseLongSignals || UseShortSignals)
            {
                int cntLongs      = 0;
                int cntShorts     = 0;
                int cntFlats      = 0;
                int ageSlaveEntry = 0;
                
                // get slave signals
                TfSyncMasterSlave.ReadFromSlaveFile(
                    this,
                    SlavesID,
                    out cntLongs,
                    out cntShorts,
                    out cntFlats,
                    out ageSlaveEntry
                );
                
                // analyze slave signals
                mCntSignals[0] = 0;
                if (UseLongSignals)
                    mCntSignals[0] += cntLongs;
                if (UseShortSignals)
                    mCntSignals[0] += cntShorts;
                
                // exit based on slave signals
                if ((Position.MarketPosition == MarketPosition.Long) && 
                    UseSignalsForExit && (mCntSignals[0] < SignalsForExit))
                {
                    Print(DateTime.Now.ToString()+""|""+ Name + ""|Time "" + Time[0] + "", exit long, "" + mCntSignals[0] + "" <= "" + SignalsForExit);
                    ExitLong(mEntryNameLong,""Ensemble Exit"");
                    _ensembleExit = true;
                }
                if ((Position.MarketPosition == MarketPosition.Short) && 
                    UseSignalsForExit && (mCntSignals[0] < SignalsForExit))
                {
                    Print(DateTime.Now.ToString()+""|""+ Name + ""|Time "" + Time[0] + "", exit short, "" + mCntSignals[0] + "" <= "" + SignalsForExit);
                    ExitShort(mEntryNameShort,""Ensemble Exit"");
                    _ensembleExit = true;
                }
                entry_signal = false;
                // enter based on slave signals
                if ((StrategyMarketPositionType == MyMarketPositionTypes.Long) 
                    && (mCntSignals[BarOffsetEntry] >= SignalsForEntry) 
                    && (ageSlaveEntry > 0))
                {
                    //Print(DateTime.Now.ToString()+""|""+ Name + ""|Time "" + Time[0] + "", enter long, "" + mCntSignals[BarOffsetEntry] + "" >= "" + SignalsForEntry);
                    entry_signal = true;
                }
                if ((StrategyMarketPositionType == MyMarketPositionTypes.Short) 
                    && (mCntSignals[BarOffsetEntry] >= SignalsForEntry) 
                    && (ageSlaveEntry > 0))
                {
                    //Print(DateTime.Now.ToString()+""|""+ Name + ""|Time "" + Time[0] + "", enter short, "" + mCntSignals[BarOffsetEntry] + "" >= "" + SignalsForEntry);
                    entry_signal = true;
                }
            }
            // End Ensemble
           
            Print(DateTime.Now.ToString()+""|""+ Name + ""|Time "" + Time[0] + "", enter long, "" + mCntSignals[BarOffsetEntry] + "" >= "" + _enterLong + "" "" + entry_signal);
");

                str.AppendFormat(@"
            if (PositionIsActive(Position))
            {{
                _delayLong = 0;
                _delayShort = 0;

                if (ProfitExit && ProfitableCloseExitIsActive())
                {{
                    _profitCloseExitCounter =
                        IsLongOrShortInProfit() ? _profitCloseExitCounter + 1 : _profitCloseExitCounter;

                    if (_profitCloseExitCounter >= ProfitableCloses)
                        ExitMarket(ProfxExitName);
                }}
                
                if (IsLong(Position) && _exitLong)
                    ExitMarket(""Signal Exit"");
                
                if (IsShort(Position) && _exitShort)
                    ExitMarket(""Signal Exit"");
                
                {1}if (rotation_signal > 0 && rotation_exit)
                {1}    ExitMarket(""Rotation Exit"");

                {0}// Ensemble
                {0}if (_ensembleExit)
                {0}    ExitMarket(""Ensemble Exit"");

                if (TimeExit && TimeToClosePosition())
                    ExitMarket(TimexExitName);

                CalculateStopTargets(false);
                CalculateProfitTargets(false);
                SetStopOrders();
                SetProfitOrders();
            }}
            else
            {{
                EntryModule();
            }}
        }}
", atLeastEntry > 0 ? "" : "//",
res.TradeMode == TradeMode.Long || res.TradeMode == TradeMode.Short ? "//" : "");

                str.AppendLine(@"
        private int _delayLong;
        private int _delayShort;

        private void EntryModule()
        {
            if (!DelayedEntryIsOn())
                EntryNoDelay();

            if (DelayedEntryIsOn())
            {
                if (_delayLong > 0)
                {
                    _delayLong++;
                    _delayShort = 0;
                }

                if (_delayShort > 0)
                {
                    _delayShort++;
                    _delayLong = 0; 
                }

                if (_enterLong  && _delayLong == 0 && TradeLongOrBoth() && _delayShort ==0)
                {
                    _delayLong++;
                }

                if (_enterShort && _delayShort == 0 && TradeShortOrBoth() && _delayLong ==0)
                {
                    _delayShort++;
                }

                if (_delayLong > DelayByBars)
                {
                    EnterLong(_posSize, EnterLongName);
                }

                if (_delayShort > DelayByBars)
                {
                    EnterShort(_posSize, EnterShortName);
                }
            }
        }

        private bool TradeLongOrBoth()
        {
            return StrategyMarketPositionType == MyMarketPositionTypes.Both ||
                   StrategyMarketPositionType == MyMarketPositionTypes.Long;
        }

        private bool TradeShortOrBoth()
        {
            return StrategyMarketPositionType == MyMarketPositionTypes.Both ||
                   StrategyMarketPositionType == MyMarketPositionTypes.Short;
        }

        private void EntryNoDelay()
        {
            if (StrategyMarketPositionType == MyMarketPositionTypes.Both)
            {
                if (_enterLong)
                    EnterLong(_posSize, EnterLongName);

                if (_enterShort)
                    EnterShort(_posSize, EnterShortName);
            }

            if (StrategyMarketPositionType == MyMarketPositionTypes.Long)
            {
                if (_enterLong)
                    EnterLong(_posSize, EnterLongName);
            }

            if (StrategyMarketPositionType == MyMarketPositionTypes.Short)
            {
                if (_enterShort)
                    EnterShort(_posSize, EnterShortName);
            }
        }

        private bool DelayedEntryIsOn()
        {
            return DelayByBars > 0;
        }

        private bool EntryTimeIsOk()
        {
            return ToTime(Time[0]) >= _startTime && ToTime(Time[0]) <= _endTime;
        }

        private bool EntryPnlIsOk()
        {
            return _pnlToday > _minPnl && _pnlToday < _maxPnl;
        }

        private bool EntryTradesTodayIsOk()
        {
            return _tradesToday < _maxTrades;
        }");

                if (res.TradeMode == TradeMode.LongRebalance || res.TradeMode == TradeMode.ShortRebalance)
                    str.AppendLine(@"
        private void SetRotationExitSignal()
        {
            if (rotation_signal == 1)
                rotation_exit = TDLM()[0] == 0;

            if (rotation_signal == 2)
                rotation_exit = (Time[0].Month == 3 || Time[0].Month == 6 || Time[0].Month == 9 || Time[0].Month == 12) && TDLM()[0] == 0;

            if (rotation_signal == 3)
                rotation_exit = Time[0].Month == 12 && TDLM()[0] == 0;
        }");

                trigStr = "";
                for (int i = 0; i < triggersExitIndex.Count; ++i)
                    trigStr += string.Format(@"            trigger{0}[0] = {1};
", triggersExitIndex[i], triggersRule1[i]);

                str.AppendFormat(@"
        private void SetExitSignals()
        {{
<b>{0}</b>            _exitLong = <b>{1}</b>;
            _exitShort = <b>{1}</b>;
        }}
", trigStr, res.ExitMode > 0 && res.ReadonlyExitSignals?.Count > 0 ? rulesExitString.ToString() : "false");

                trigStr = "";
                for (int i = 0; i < triggersIndex.Count; ++i)
                    trigStr += string.Format(@"            trigger{0}[0] = {1};
", triggersIndex[i], triggersRule1[i]);

                str.AppendFormat(@"
        private void SetEntrySignals()
        {{
<b>{2}</b>            _enterLong = <b>{0}{1}</b>{3};
            _enterLong &= ExtraEntryConditions();

            _enterShort = <b>{0}{1}</b>{3};
            _enterShort &= ExtraEntryConditions();
        }}", atLeastEntry > 0 ? "entry_signal && " : "", rulesString.ToString(), trigStr, isRebalance ? " && ok_to_trade" : "");

                str.AppendLine(@"
        private bool ExtraEntryConditions()
        {
            return EntryTimeIsOk() && EntryPnlIsOk() && EntryTradesTodayIsOk();
        }

        private bool TimeToClosePosition()
        {
            var entryName = GetEntryName();
            if (entryName == """") return false;
            return Maxtime > 0 && BarsSinceEntryExecution(BarsInProgress, entryName, 0) >= Maxtime - 1;
        }

        private bool ProfitableCloseExitIsActive()
        {
            var entryName = GetEntryName();
            if (entryName == """") return false;
            return ProfitableCloses > 0 && BarsSinceEntryExecution(BarsInProgress, entryName, 0) > 0;
        }

        private string GetEntryName()
        {
            if (_currentPositionType == MyMarketPositionTypes.Long)
            {
                return EnterLongName;
            }
            if (_currentPositionType == MyMarketPositionTypes.Short)
            {
                return EnterShortName;
            }

            return """";
        }

        private bool IsLongOrShortInProfit()
        {
            return IsLong(Position) && LongPositionInProfit() || IsShort(Position) && ShortPositionInProfit();
        }

        private bool LongPositionInProfit()
        {
            return Close[0] >= Position.AveragePrice;
        }

        private bool ShortPositionInProfit()
        {
            return Close[0] <= Position.AveragePrice;
        }

        private void CalculateDailyTradesAndPnl()
        {
            _currentPosType = Position.MarketPosition == MarketPosition.Long ? 1 : 0;

            if (IsNewTrade())
                _tradesToday += 1;

            _pnlToday = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - _startOfDayCumPnl;
            _prevPosType = _currentPosType;
            _prevAvgPrice = Position.AveragePrice;

            if (IsNewDay())
            {
                _tradesToday = 0;
                _pnlToday = 0;
                _startOfDayCumPnl = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
            }
        }

        public bool PositionIsActive(Position position)
        {
            return position.MarketPosition != MarketPosition.Flat;
        }

        public bool IsLong(Position position)
        {
            return position.MarketPosition == MarketPosition.Long;
        }

        public bool IsShort(Position position)
        {
            return position.MarketPosition == MarketPosition.Short;
        }

        private bool IsNewTrade()
        {
            return _currentPosType == 1 && _prevPosType != 1 ||
                   (_currentPosType == 1 && _prevPosType == 1 &&
                    Position.AveragePrice != _prevAvgPrice);
        }

        private bool IsNewDay()
        {
            return Time[0].Day != Time[1].Day && IsFirstTickOfBar;
        }

        private void SetGlobalPositionSize()
        {
            _posSize = 0;

            double atr = AFATR(_atrPeriod)[0];
            double risk = _acct * .01;
            double conversion = 0;
            double type = StaticAtrStopLossIsOn ? 0.00 : 1;

            if (_mrkt == MarketTypeFx)
                SetFxPositionSize(risk, atr, type, conversion);
            
            else if (_mrkt == MarketTypeFutures)
                SetFuturesPositionSize(risk, atr, type);
           
            else if (_mrkt == MarketTypeOther)
                SetOtherPositionSize(risk, atr, type);
        }

        private void SetFxPositionSize(double risk, double atr, double type, double conversion)
        {
            var symbStartThree = _symbol.Substring(0, 3);
            var symbEndThree = _symbol.Substring(3, 3);
            var marketStartThree = SymbolFour.Substring(0, 3);
            var marketEndThree = SymbolFour.Substring(3, 3);

            if (_kind == KindFixed) // to convert need XXXUSD
            {
                if (symbStartThree == marketEndThree)
                    conversion = 1 / Closes[4][0];

                else if (symbStartThree == marketStartThree && marketEndThree == _target)
                    conversion = Closes[4][0];

                else if (symbStartThree == marketStartThree && marketEndThree == _target)
                    conversion = 1;

                _posSize = (int)(_acct / conversion);
            }
            else if (_kind == KindAtr) // to convert need USDYYY
            {
                if (marketEndThree == _target)
                    conversion = 1;

                else if (marketStartThree == _target)
                    conversion = Closes[4][0];

                else if (symbEndThree == marketStartThree && marketEndThree == _target)
                    conversion = 1 / Closes[4][0];

                _posSize = (int)(risk * conversion / (atr * type));
            }
            else if (_kind == KindDefault)
            {
                _posSize = 100000;
            }
        }

        private void SetFuturesPositionSize(double risk, double atr, double type)
        {
            if (_kind == KindFixed)
                _posSize = (int)(_acct / _margin);

            else if (_kind == KindAtr)
                _posSize = (int)(risk / (atr * type * Instrument.MasterInstrument.PointValue));

            else if (_kind == KindDefault)
                _posSize = 1;
        }

        private void SetOtherPositionSize(double risk, double atr, double type)
        {
            if (_kind == KindFixed)
                _posSize = (int)(_acct / Close[0]);

            else if (_kind == KindAtr)
                _posSize = (int)(risk / (atr * type));

            else if (_kind == KindDefault)
                _posSize = 100;
        }

        protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity,
            MarketPosition marketPosition, string orderId, DateTime time)
            {
                if (OrderFilled(execution.Order))
                {
                    if (IsEntryOrder(execution.Order))
                    {
                        _delayLong = 0;
                        _delayShort = 0;

                        SetCurrentPosType(execution.Order);
                        CalculateStopTargets(true);
                        CalculateProfitTargets(true);
                        SetStopOrders();
                        SetProfitOrders();
                    
                }
                    else
                    {
                        _profitCloseExitCounter = 0;
                        SetCurrentPosType(execution.Order);

                        if (ImmediateReEntry(execution.Order))
                        {
                            EntryModule();
                        }
                    }
                }
            }");

                str.AppendFormat(@"
        private bool ImmediateReEntry(Order order)
        {{
            return order.Name == ProfxExitName || order.Name == TimexExitName || order.Name == ""Signal Exit""{0};
        }}
", atLeastEntry > 0 ? " || order.Name == \"Ensemble Exit\"" : "");

                str.Append(@"
        private void SetCurrentPosType(Order order)
        {
            if (order.Name == EnterLongName)
                _currentPositionType = MyMarketPositionTypes.Long;

            else if (order.Name == EnterShortName)
                _currentPositionType = MyMarketPositionTypes.Short;

            else
            {
                _currentPositionType = MyMarketPositionTypes.None;
            }
        }

        double _staticProfit = 0;
        double _atrProfit = 0;
        double _hHlLTarget = 0;

        private void CalculateProfitTargets(bool isAtEntryFill)
        {
            
            if (_currentPositionType == MyMarketPositionTypes.Long)
            {
                if (DollarTargetIsOn && isAtEntryFill)
                {
                    _staticProfit = Position.AveragePrice + (ProfitTargetDollars / (Instrument.MasterInstrument.PointValue * Position.Quantity));
                    //_staticProfit = Close[0] + (ProfitTargetDollars / (Instrument.MasterInstrument.PointValue * Position.Quantity));
                    
                }
                if (AtrProfitTargetIsOn && isAtEntryFill)
                {
                    _atrProfit = Close[0] + AFATR(_atrPeriod)[0] * ProfitTargetCoef;

                }
                if (LlHhProfitTargetIsOn)
                {
                    _hHlLTarget = MAX(High, LlHhProfitPeriod)[0];
                    
                }

                _profit = MinOfThree(_staticProfit, _atrProfit, _hHlLTarget);
                SetProfitTargetName(_staticProfit, _atrProfit, _hHlLTarget);
            }

            else if (_currentPositionType == MyMarketPositionTypes.Short)
            {
                if (DollarTargetIsOn && isAtEntryFill)
                {
                    _staticProfit = Position.AveragePrice - (ProfitTargetDollars / (Instrument.MasterInstrument.PointValue * Position.Quantity));
                    //_staticProfit = Close[0] - (ProfitTargetDollars / (Instrument.MasterInstrument.PointValue * Position.Quantity));
                }
                if (AtrProfitTargetIsOn && isAtEntryFill)
                {
                    _atrProfit = Close[0] - AFATR(_atrPeriod)[0] * ProfitTargetCoef;
                    
                }
                if (LlHhProfitTargetIsOn)
                {
                    _hHlLTarget = MIN(Low, LlHhProfitPeriod)[0];
                }

                _profit = MaxOfThree(_staticProfit, _atrProfit, _hHlLTarget);
                SetProfitTargetName(_staticProfit, _atrProfit, _hHlLTarget);
            }
        }

        double _stopAtr = 0;
        double _stopHhLl = 0;
        double _stopTrailAtr = 0;
        private double _dollarStop;
");

                if (atLeastEntry > 0)
                    str.AppendLine("        private double _dollarTrail;");

                str.Append(@"
        private void CalculateStopTargets(bool isAtEntryFill)
        {
            if (!AnyStopIsOn())
                return;

            if (_currentPositionType == MyMarketPositionTypes.Long)
            {
                if (StaticAtrStopLossIsOn && isAtEntryFill)
                {
                    _stopAtr = Close[0] - AFATR(_atrPeriod)[0] * StopLossCoef; 
                }
                if (DollarStop && isAtEntryFill)
                {
                    _dollarStop = Position.AveragePrice - (DollarStopValue / (Instrument.MasterInstrument.PointValue * Position.Quantity));
                    //_dollarStop = Close[0] - (DollarStopValue / (Instrument.MasterInstrument.PointValue * Position.Quantity));
                }
                if (HhLlStopLossIsOn)
                {
                    _stopHhLl = MIN(Low, HhLlStopPeriod)[0];
                }
                if (TrailAtrStopLossIsOn)
                {
                    _stopTrailAtr = Close[0] - AFATR(_atrPeriod)[0] * TrailAtrStopLossCoef;

                    if (!isAtEntryFill)
                        _stopTrailAtr = Math.Max(_stop, _stopTrailAtr);
                }
                if (DollarStopTrail)
                {
                    _tempTrail = High[0] - (DollarStopTrailValue / (Instrument.MasterInstrument.PointValue * Position.Quantity));

                    if (BarsSinceEntryExecution() == 0)
                    {
                        _tempTrail = 0;
                        _dollarTrail = Position.AveragePrice - (DollarStopTrailValue / (Instrument.MasterInstrument.PointValue * Position.Quantity));
                    }

                    if (_tempTrail > _dollarTrail)
                        _dollarTrail = _tempTrail;

                    if (!isAtEntryFill)
                    {
                        var newValue = Close[0] - (DollarStopTrailValue / (Instrument.MasterInstrument.PointValue * Position.Quantity));
                        _dollarTrail = Math.Max(_stop, newValue);
                    }
                }

                _stop = MaxOfAll(_stopAtr, _stopHhLl, _stopTrailAtr, _dollarStop, _dollarTrail);
                SetStopLossName(_stopAtr, _stopHhLl, _stopTrailAtr, _dollarStop, _dollarTrail);
            }

            else if (_currentPositionType == MyMarketPositionTypes.Short)
            {
                if (StaticAtrStopLossIsOn && isAtEntryFill)
                {
                    _stopAtr = Close[0] + AFATR(_atrPeriod)[0] * StopLossCoef;
                }
                if (DollarStop && isAtEntryFill)
                {
                    _dollarStop = Position.AveragePrice + (DollarStopValue / (Instrument.MasterInstrument.PointValue * Position.Quantity));
                    //_dollarStop = Close[0] + (DollarStopValue / (Instrument.MasterInstrument.PointValue * Position.Quantity));
                }
                if (HhLlStopLossIsOn)
                {
                    _stopHhLl = MAX(High, HhLlStopPeriod)[0];
                }
                if (TrailAtrStopLossIsOn)
                {
                    _stopTrailAtr = Close[0] + AFATR(_atrPeriod)[0] * TrailAtrStopLossCoef;

                    if (!isAtEntryFill)
                        _stopTrailAtr = Math.Min(_stop, _stopTrailAtr);
                }
                if (DollarStopTrail)
                {
                    _tempTrail = Low[0] + (DollarStopTrailValue / (Instrument.MasterInstrument.PointValue * Position.Quantity));
                    
                    if (BarsSinceEntryExecution() == 0)
                    {
                        _tempTrail = 0;
                        _dollarTrail = Position.AveragePrice + (DollarStopTrailValue / (Instrument.MasterInstrument.PointValue * Position.Quantity));
                    }
                    
                    if (_tempTrail < _dollarTrail)
                        _dollarTrail = _tempTrail;

                    if (!isAtEntryFill)
                    {
                        var newValue = Close[0] + (DollarStopTrailValue / (Instrument.MasterInstrument.PointValue * Position.Quantity));
                        _dollarTrail = Math.Min(_stop, newValue);
                    }
                }

                _stop = MinOfAll(_stopAtr, _stopHhLl, _stopTrailAtr, _dollarStop, _dollarTrail);
                SetStopLossName(_stopAtr, _stopHhLl, _stopTrailAtr, _dollarStop, _dollarTrail);
            }
        }

        private void SetStopLossName(double stopAtr, double stopHhLl, double stopTrailAtr, double dollarStop, double dollarTrail)
        {
            if (_stop == stopAtr)
                _stopLossName = StaticATRStopName;

            if (_stop == stopHhLl)
                _stopLossName = StaticHhLlStopName;

            if (_stop == stopTrailAtr)
                _stopLossName = TrailATRStopName;

            if (_stop == dollarStop)
                _stopLossName = DollarStopName;

            if (_stop == dollarTrail)
                _stopLossName = DollarTrailName;
        }

        private void SetProfitTargetName(double staticProfit, double atrProfit, double hHlLTarget)
        {
            if (_profit == staticProfit)
                _profitTargetName = StaticProfitTargetName;

            if (_profit == atrProfit)
                _profitTargetName = AtrProfitTargetName;

            if (_profit == hHlLTarget)
                _profitTargetName = HhLlProfitTargetName;
        }

        private double MaxOfThree(double valOne, double valTwo, double valThree)
        {
            return Math.Max(valOne, Math.Max(valTwo, valThree));
        }

        private double MaxOfAll(params double[] vals)
        {

            var values = new List<double>();

            foreach (var number in vals)
            {
                values.Add(number);
            }

            if (values.Count == 0)
                return 0;

            var maxVal = values.Max();
            return maxVal;
        }

        private double MinOfThree(double valOne, double valTwo, double valThree)
        {
            var values = new List<double>()
            {   
                valOne, valTwo, valThree
            };

            var nonZeroValues = values.Where(v => v != 0);

            if (nonZeroValues.Count() == 0)
                return 0;

            var minNonZero = nonZeroValues.Min();
            return minNonZero;
        }

        private double MinOfAll(params double[] vals)
        {
            var values = new List<double>();

            foreach (var number in vals)
            {
                values.Add(number);
            }

            var nonZeroValues = values.Where(v => v != 0);

            if (nonZeroValues.Count() == 0)
                return 0;

            var minNonZero = nonZeroValues.Min();
            return minNonZero;
        }

        private void SetStopOrders()
        {
            if (!AnyStopIsOn() || _stop == 0 || _entryName == """")
                return;

            if (IsLong(Position) && _stop < GetCurrentBid() - Slippage)
                ExitLongStopMarket(_stop, _stopLossName, EnterLongName);

            if (IsShort(Position) && _stop > GetCurrentAsk() + Slippage)
                ExitShortStopMarket(_stop, _stopLossName, EnterShortName);
        }

        private void SetProfitOrders()
        {
            if (!AnyProfitIsOn() || _profit == 0 || _entryName == """")
                return;

            if (IsLong(Position) && _profit > GetCurrentBid() + Slippage)
                ExitLongLimit(_profit, _profitTargetName, EnterLongName);

            if (IsShort(Position) && _profit < GetCurrentAsk() - Slippage)
                ExitShortLimit(_profit, _profitTargetName, EnterShortName);
        }

        private bool AnyStopIsOn()
        {
            return StaticAtrStopLossIsOn || HhLlStopLossIsOn || TrailAtrStopLossIsOn || DollarStop ||
                   DollarStopTrail;
        }

        private bool AnyProfitIsOn()
        {
            return DollarTargetIsOn || AtrProfitTargetIsOn || LlHhProfitTargetIsOn;
        }

        private bool IsEntryOrder(Order order)
        {
            if (order.Name == EnterLongName || order.Name == EnterShortName)
                return true;

            return false;
        }

        public bool OrderFilled(Order order)
        {
            return order.OrderState == OrderState.Filled;
        }

        private void ExitMarket(string exitName)
        {
            if (IsLong(Position))
                ExitLong(exitName, EnterLongName);

            else if (IsShort(Position))
                ExitShort(exitName, EnterShortName);
        }
    }
}");

                {
                    var evSigs = res.ReadonlyEntrySignals?.Where(x => x is SignalNewsEvents);
                    if (evSigs != null)
                        foreach (var sig in evSigs)
                        {
                            var extraEVcode = GetEventsExtraCode(new List<Signal>() { sig }, Security.ToDateTime(res.SignalStartDate, res.SignalStartTime), Security.ToDateTime(res.StopDate, 235959), "NT");
                            if (string.IsNullOrEmpty(extraEVcode) == false)
                            {
                                extraEvCode.Add(new KeyValuePair<string, string>(/*(extraEvCode.Count + 1).ToString()*/sig.Text, extraEVcode));
                            }
                        }
                    evSigs = res.ReadonlyExitSignals?.Where(x => x is SignalNewsEvents);
                    if (evSigs != null)
                        foreach (var sig in evSigs)
                        {
                            var extraEVcode = GetEventsExtraCode(new List<Signal>() { sig }, Security.ToDateTime(res.SignalStartDate, res.SignalStartTime), Security.ToDateTime(res.StopDate, 235959), "NT");
                            if (string.IsNullOrEmpty(extraEVcode) == false && extraEvCode.All(x => x.Value != extraEVcode))
                            {
                                extraEvCode.Add(new KeyValuePair<string, string>(/*(extraEvCode.Count + 1).ToString()*/sig.Text, extraEVcode));
                            }
                        }
                }
            }

            if (extraEvCode.Count == 0) extraEvCode = null;

            return new List<string>() { str.ToString() };
        }

        public static List<string> MT4(List<IReadonlySimStrategy> activeResults, out List<KeyValuePair<string, string>> extraEvCode)
        {
            //List<IReadonlySimStrategy> readyResults = new List<IReadonlySimStrategy>();
            StringBuilder inputStr = new StringBuilder();

            //foreach (var res1 in activeResults)
            //    if (activeResults.First().Symbol == res1.Symbol && res1.ReadonlyEntrySignals.Count > 0)  //same symbol only
            //        readyResults.Add(res1);
            //if (readyResults.Count == 0)
            //{
            //    ShowMessage("There are no strategies with supported signals");
            //    return;
            //}

            //foreach (SimResults res in readyResults)
            //{
            IReadonlySimStrategy res = activeResults[0];
            bool intraday = Properties.Settings.Default.TTaddCode && res.ReadonlyIntradayTestResults?.Count == 5;

            int atLeastEntry = 0;
            int strCount = 0;

            if (!res.IsLong)
                inputStr.Append("LONG_ENTRY_END LONG_EXIT_END ");

            if (res.ReadonlyEntrySignals != null)
            {
                inputStr.Append($"(TimeToStr(Time[1],TIME_DATE) > \\\"{res.SignalStartDate / 10000}.{(res.SignalStartDate / 100) % 100:D2}.{res.SignalStartDate % 100:D2}\\\")".Replace(" ", $"{(char)5}"));
                //inputStr.Append(string.Format("(_Date_1(0)>{0} || (_Date_1(0)=={0} && _Time_1(0)>={1})", res.SignalStartDate, res.SignalStartTime).Replace(" ", $"{(char)5}"));
                inputStr.Append(" ");

                foreach (Signal r in res.ReadonlyEntrySignals)
                {
                    if (r is SignalStrategy)
                    {
                        ++strCount;
                    }
                    else if (r is SignalEnsemble)
                    {
                        atLeastEntry = Math.Max(atLeastEntry, (r as SignalEnsemble).atLeast);
                    }
                    else if (r is SignalTrigger)   //custom indicator
                    {
                        inputStr.AppendFormat("{0} ", r.CodeMT4);
                    }
                    else if (!string.IsNullOrEmpty(r.Key))
                    {
                        string code = r.CodeMT4.Replace(" ", $"{(char)5}").Replace("\"", "\\\"").Replace("*", "*+");
                        if (r.Key == code)
                            inputStr.AppendFormat("{0}{1} ", r.Key, r.MarketNumber > 1 ? $"#{r.MarketNumber}" : "");
                        else
                            inputStr.AppendFormat("{0} ", code);
                    }
                }
            }
            atLeastEntry += strCount;

            if (atLeastEntry > 0) inputStr.Append("MasterEnter ");
            if (res.IsLong)
                inputStr.Append("LONG_ENTRY_END");
            else inputStr.Append("SHORT_ENTRY_END");

            int atLeastExit = 0;
            strCount = 0;
            if (res.ExitMode > 0 && res.ReadonlyExitSignals != null)
            {
                foreach (Signal r in res.ReadonlyExitSignals)
                {
                    if (r is SignalStrategy)
                    {
                        ++strCount;
                    }
                    else if (r is SignalEnsemble)
                    {
                        atLeastExit = Math.Max(atLeastExit, (r as SignalEnsemble).atLeast);
                    }
                    else if (r is SignalTrigger)   //custom indicator
                    {
                        inputStr.AppendFormat(" {0}", r.CodeMT4);
                    }
                    else if (!string.IsNullOrEmpty(r.Key))
                    {
                        string code = r.CodeMT4.Replace(" ", $"{(char)5}").Replace("\"", "\\\"").Replace("*", "*+");
                        if (r.Key == code)
                            inputStr.AppendFormat(" {0}{1}", r.Key, r.MarketNumber > 1 ? $"#{r.MarketNumber}" : "");
                        else
                            inputStr.AppendFormat(" {0}", code);
                    }
                }
            }
            atLeastExit += strCount;

            if (atLeastExit > 0) inputStr.Append(" MasterExit");
            if (res.IsLong)
                inputStr.Append(" LONG_EXIT_END SHORT_ENTRY_END SHORT_EXIT_END");
            else inputStr.Append(" SHORT_EXIT_END");

            inputStr.AppendFormat(CultureInfo.InvariantCulture,
                " {0:F3} {1:F3} {2} {3:F3} {4:F3} {5:F3} {6:F3} {7:F3} {8:F3} {9:F3} {10:F3} {11:F3} {12} {13} {14} {15} {16} {17:F3} {18:F3} {19:F3} {20} {21} {22} {23} {24}",
                res.Net_PnL, res.Drawdown, res.TradesCount, res.WinPercentage, res.Mean_w_trades, res.Mean_l_trades, res.Std_w_trades, res.Std_l_trades,
                res.ProfitFactor, res.RatioWL, res.CPCRatio, res.RobustIndex, res.PT_ON, res.SL_ON, res.TL_ON, res.HH_ON > 0 ? res.HHlook : 0, res.LL_ON > 0 ? res.LLlook : 0,
                res.PT_mult, res.SL_mult, res.TL_mult, res.MaxTime == 0 ? 9999 : res.MaxTime, res.ProfX == 0 ? 9999 : res.ProfX, res.IsLong ? 1 : 0, res.DelayedEntry, res.DelayedEntry);

            /*1 - default
            2 - Fixed FX
            3 - ATR FX
            4 - Fixed Other
            5 - ATR Other
            6 - Fixed Futures
            7 - ATR Futures*/
            int posMet = 1;
            switch (res.SymbolType)
            {
                case SymbolType.Futures:
                    posMet = (res.PosSizeMode == PositionSizingMode.Fixed ? 6 : (res.PosSizeMode == PositionSizingMode.Volatility ? 7 : 1)); break;
                case SymbolType.FOREX:
                    posMet = (res.PosSizeMode == PositionSizingMode.Fixed ? 2 : (res.PosSizeMode == PositionSizingMode.Volatility ? 3 : 1)); break;
                case SymbolType.ETF:
                case SymbolType.Crypto:
                case SymbolType.Custom:
                    posMet = (res.PosSizeMode == PositionSizingMode.Fixed ? 4 : (res.PosSizeMode == PositionSizingMode.Volatility ? 5 : 1)); break;
                default:
                    posMet = 1; break;
            }

            inputStr.AppendFormat(CultureInfo.InvariantCulture,
                " {0} {1} {2} {3} {4}", posMet, res.AccountValue, "0.01", res.ATR_len, Utils.SymbolsManager.GetSymbolMargin(res.SymbolId));

            if (!res.GetMarketSymbolId(2).IsEmpty())
                inputStr.AppendFormat(CultureInfo.InvariantCulture, " {0} {1}", res.GetMarketSymbolId(2).Name,
                    Utils.SymbolsManager.GetSymbolType(res.GetMarketSymbolId(2)) == SymbolType.Custom ? "0" : "1440");

            if (!res.GetMarketSymbolId(3).IsEmpty())
                inputStr.AppendFormat(CultureInfo.InvariantCulture, " {0} {1}", res.GetMarketSymbolId(3).Name,
                    Utils.SymbolsManager.GetSymbolType(res.GetMarketSymbolId(3)) == SymbolType.Custom ? "0" : "1440");

            inputStr.Append($" {res.RebalancePeriod}");

            int rebMet = 0;
            switch (res.RebalanceMethod)
            {
                //Profit Factor
                case RebalanceMethod.ProfitFactor:      rebMet = 1; break;
                //Winning Percentage
                case RebalanceMethod.WinningPercentage: rebMet = 2; break;
                //Average Return
                case RebalanceMethod.AverageReturn:     rebMet = 3; break;
                //Volatility
                case RebalanceMethod.Volatility:        rebMet = 8; break;
                //Sharpe
                case RebalanceMethod.Sharpe:            rebMet = 6; break;
                //Range Location
                case RebalanceMethod.RangeLocation:     rebMet = 9; break;
                //Rate of Change
                case RebalanceMethod.RateOfChange:      rebMet = 4; break;
                //Momentum
                case RebalanceMethod.Momentum:          rebMet = 5; break;
                //Fip Score
                case RebalanceMethod.FipScore:          rebMet = 7; break;
            }

            inputStr.Append($" {rebMet}");
            inputStr.Append($" {res.RebalanceSymbolsCount}");

            bool isRebalance = (res.TradeMode == TradeMode.LongRebalance || res.TradeMode == TradeMode.ShortRebalance)/* && res.ChildStrategiesCount > 0*/;
            if (isRebalance)
            {
                List<SymbolId> rebalanceSymbols = null;
                if (res.ReadonlyChildren?.Count() > 0)
                    rebalanceSymbols = res.ReadonlyChildren.Select(x => x.SymbolId).ToList();
                if (res.ReadonlyParent != null)
                    rebalanceSymbols = res.ReadonlyParent.ReadonlyChildren.Select(x => x.SymbolId).ToList();

                foreach (var symId in rebalanceSymbols)
                    inputStr.Append($" {symId}");
            }

            inputStr.AppendFormat(CultureInfo.InvariantCulture, " {0:D4} {1:D4} {2:D4} {3} {4} {5}\n",
                intraday ? res.ReadonlyIntradayTestResults[2] / 100 : 0,
                intraday ? res.ReadonlyIntradayTestResults[3] / 100 : 2359,
                intraday ? res.SessionEndTime : 1700,
                intraday ? res.ReadonlyIntradayTestResults[0] : 9999999,
                intraday ? res.ReadonlyIntradayTestResults[1] : -9999999,
                intraday ? res.ReadonlyIntradayTestResults[4] : 999999);
            //}

            //get result from c++
            IntPtr pp = IntPtr.Zero;
            CppSimulatorInterface.ConvertToMT4(inputStr.ToString(), ref pp);
            string resStr = Marshal.PtrToStringAnsi(pp);
            Marshal.FreeCoTaskMem(pp);

            //replace $$NumberOfPositionsToEnter$$, $$NumberOfPositionsToExit$$
            resStr = resStr.Replace("$$NumberOfPositionsToEnter$$", atLeastEntry.ToString()).Replace("$$NumberOfPositionsToExit$$", atLeastExit.ToString());

            //replace marker2/3 vars
            //resStr = resStr.Replace("/*MARKET_INIT*/", String.Format("string market2symbol = \"{0}\";\nstring market3symbol = \"{1}\";", res.market2, res.market3));

            //replace entryOnClose and exitOnClose
            int ien = resStr.IndexOf("E N T R Y");
            int iex = resStr.IndexOf("E X I T S");

            resStr = resStr.Substring(0, ien) +
                resStr.Substring(ien, iex - ien).Replace("_NEXT_BAR_OPEN", res.EntryOnClose > 0 ? "<b>_THIS_BAR_CLOSE</b>" : "<b>_NEXT_BAR_OPEN</b>") +
                resStr.Substring(iex).Replace("_NEXT_BAR_OPEN", res.ExitOnClose > 0 ? "<b>_THIS_BAR_CLOSE</b>" : "<b>_NEXT_BAR_OPEN</b>");

            if (!string.IsNullOrEmpty(resStr))
            {
                extraEvCode = new List<KeyValuePair<string, string>>();

                var evSigs = res.ReadonlyEntrySignals?.Where(x => x is SignalNewsEvents);
                if (evSigs != null)
                    foreach (var sig in evSigs)
                    {
                        var extraEVcode = GetEventsExtraCode(new List<Signal>() { sig }, Security.ToDateTime(res.SignalStartDate, res.SignalStartTime), Security.ToDateTime(res.StopDate, 235959), "MT4");
                        if (string.IsNullOrEmpty(extraEVcode) == false)
                        {
                            extraEvCode.Add(new KeyValuePair<string, string>(/*(extraEvCode.Count + 1).ToString()*/sig.Text, extraEVcode));
                        }
                    }
                evSigs = res.ReadonlyExitSignals?.Where(x => x is SignalNewsEvents);
                if (evSigs != null)
                    foreach (var sig in evSigs)
                    {
                        var extraEVcode = GetEventsExtraCode(new List<Signal>() { sig }, Security.ToDateTime(res.SignalStartDate, res.SignalStartTime), Security.ToDateTime(res.StopDate, 235959), "MT4");
                        if (string.IsNullOrEmpty(extraEVcode) == false && extraEvCode.All(x => x.Value != extraEVcode))
                        {
                            extraEvCode.Add(new KeyValuePair<string, string>(/*(extraEvCode.Count + 1).ToString()*/sig.Text, extraEVcode));
                        }
                    }

                if (extraEvCode.Count == 0) extraEvCode = null;

                return new List<string>() { resStr };
            }
            else
            {
                extraEvCode = null;
                return null;
            }
        }

        public static List<string> Python(List<IReadonlySimStrategy> activeResults)
        {
            List<IReadonlySimStrategy> readyResults = new List<IReadonlySimStrategy>();
            string resStr;

            Dictionary<string, int> Qid = new Dictionary<string, int>()
            {
                { "SPY", 8554  },
                { "GDX", 32133 },
                { "USO", 28320 },
                { "TLT", 23921 },
                { "JNK", 35175 },
                { "HYG", 33655 },
                { "GLD", 26807 },
                { "SLV", 28368 },
                { "DIA", 2174  },
                { "QQQ", 19920 },
                { "IWM", 21519 },
                { "XLF", 19656 },
                { "XME", 32274 },
                { "XLU", 19660 },
                { "XLI", 19657 },
                { "XHB", 28074 },
                { "IBB", 22445 },
                { "XLY", 19662 },
                { "XLP", 19659 },
                { "XLE", 19655 },
                { "XLV", 19661 },
                { "XLK", 19658 },
                { "EEM", 24705 },
                { "EWA", 14516 },
                { "EWU", 0     },
                { "EWQ", 0     },
                { "EWG", 14518 },
                { "EWJ", 14520 },
                { "FXI", 26703 },
                { "EWC", 14517 }
            };

            foreach (var res in activeResults)
                if (Qid.ContainsKey(res.Symbol) && (res.GetMarketSymbolId(2).IsEmpty() || Qid.ContainsKey(res.GetMarketSymbolId(2).Name))
                    && (res.GetMarketSymbolId(3).IsEmpty() || Qid.ContainsKey(res.GetMarketSymbolId(3).Name)))
                {
                    if (res.ReadonlyEntrySignals.Count > 0)
                        readyResults.Add(res);
                }
            if (readyResults.Count == 0)
            {
                //ShowMessage("There are no strategies with supported ETF symbols or signals");
                return null;
            }

            // beggining of output file
            resStr = @"import pandas as pd
from numpy import array
import talib
import datetime
import pytz

from quantopian.pipeline.factors import RSI
from zipline.utils import tradingcalendar as calendar
from zipline.utils.tradingcalendar import get_early_closes

PRINT_BARS = not True
PRINT_TRADE_TIMES = True
PRINT_ORDERS = True

def initialize(context):
    """"""
    Called once at the start of the algorithm.
    """"""
    # Create list of ""triplets""
    # Each triplet group must be it's own list with the security to be traded as the first security of the list
    context.security_group = list()
";
            // construct triplets
            Random rnd = new Random(DateTime.Now.Millisecond);
            int m2, m3;
            foreach (IReadonlySimStrategy res in readyResults)
            {
                m2 = m3 = 0;
                do m2 = rnd.Next(30000); while (m2 == Qid[res.Symbol]);
                do m3 = rnd.Next(30000); while (m3 == Qid[res.Symbol] || m3 == m2);
                resStr += string.Format("    context.security_group.append([sid(<b>{0}</b>), sid(<b>{1}</b>), sid(<b>{2}</b>)])" + Environment.NewLine, Qid[res.Symbol],
                    string.IsNullOrEmpty(res.GetMarketSymbolId(2).Name) ? m2 /*Qid[res.symbol]*/ : Qid[res.GetMarketSymbolId(2).Name],
                    string.IsNullOrEmpty(res.GetMarketSymbolId(3).Name) ? m3 /*Qid[res.symbol]*/ : Qid[res.GetMarketSymbolId(3).Name]);
            }

            resStr += @"
    # Create list of only the securities that are traded
    context.trade_securities = list()
    context.intraday_bars = dict()
    context.last = dict()
    context.stocks = list()

    for group in context.security_group:
        # Verify the group has exactly 3 listed securities
        if len(group) != 3: raise ValueError('The security group to trade {} must have 3 securities.'.format(group[0].symbol))
        
        # Add first security only to the traded list (if not already added)
        if group[0] not in context.trade_securities: context.trade_securities.append(group[0])
        
        # Loop through all stocks in triplet group
        for stock in group:
            # Add stock to context.stocks (if not already added) and create dictionary objects to hold intraday bars/last prices
            if stock not in context.stocks:
                context.stocks.append(stock) 
                context.intraday_bars[stock] = [[],[],[],[]] # empty list of lists (for ohlc prices)
                context.last[stock] = None                   # set last price to None

    # Define time zone for algorithm
    context.time_zone = 'US/Eastern' # algorithm time zone
    
    # Define the benchmark (used to get early close dates for reference).
    context.spy = sid(8554) # SPY
    start_date = context.spy.security_start_date
    end_date   = context.spy.security_end_date
    
    # Get the dates when the market closes early:
    context.early_closes = get_early_closes(start_date,end_date).date
";

            resStr += @"
    # Create variables required for each triplet
    context.PT = dict()
    context.SL = dict()
    context.Highest = dict()
    context.Lowest = dict()
    context.bars_since_entry = dict()
    context.prof_x = dict()
    context.entry_price = dict()
    
    context.o1 = dict()
    context.o2 = dict()
    context.o3 = dict()
    context.o4 = dict()
    context.o5 = dict()
    context.o6 = dict()
    context.ord1 = dict()
    context.ord2 = dict()
    context.max_daily_entries = dict()
    context.daily_entries = dict()
    context.max_pnl = dict()
    context.min_pnl = dict()
    context.closed_pnl = dict()
    context.trading_periods = dict()
    context.status = dict()
    context.positions_closed = dict()
    context.exit_at_close = dict()
    context.time_to_close = dict()
    context.reset_filter = dict()
    context.reset_minutes = dict()
    
    context.long_position = dict()
    context.execution_market_close = dict()
    context.execution_market_open = dict()
    context.long_on = dict()
    context.PT_ON = dict()
    context.SL_ON = dict()
    context.HH = dict()
    context.LL = dict()
    context.profitable_closes = dict()
    context.max_time = dict()
    context.position_amount = dict()

    i = 0
    for group in context.security_group:
        context.Highest[i] = 100000.0
        context.Lowest[i] = 0.0
        context.bars_since_entry[i] = 0
        context.prof_x[i] = 0    
        context.entry_price[i] = None
        
        context.o1[i] = 0
        context.o2[i] = 0
        context.o3[i] = 0
        context.o4[i] = 0
        context.o5[i] = 0
        context.o6[i] = 0
        context.ord1[i] = None
        context.ord2[i] = None
        context.daily_entries[i] = 0
        context.closed_pnl[i] = 0 # keep track of individual system profits
        context.status[i] = 'NOT TRADING' # status of the algo: 'NOT TRADING' or 'TRADING'
        context.positions_closed[i] = False # keep track of when pnl exits are triggered
";

            // CUSTOMIZE THE INPUT SETTINGS FOR EACH TRADED SECURITY HERE
            bool firstInput = true;
            int index = 0;
            foreach (IReadonlySimStrategy res in readyResults)
            {
                if (firstInput)
                {
                    resStr += @"
        if i == " + index + ":" + Environment.NewLine;
                    firstInput = false;
                }
                else
                    resStr += @"
        elif i == " + index + ":" + Environment.NewLine;

                resStr += "            context.execution_market_close[i] = <b>" + (res.EntryOnClose == 1 ? "True" : "False") + "</b>" + Environment.NewLine;
                resStr += "            context.execution_market_open[i] = <b>" + (res.EntryOnClose == 0 ? "True" : "False") + "</b>" + Environment.NewLine;
                resStr += "            context.long_on[i] = <b>" + (res.IsLong ? "1" : "0") + "</b> # 1 = long, 0 = short" + Environment.NewLine;
                resStr += "            context.PT_ON[i] = <b>" + res.PT_ON + "</b>" + Environment.NewLine;
                resStr += "            context.SL_ON[i] = <b>" + res.SL_ON + "</b>" + Environment.NewLine;
                resStr += "            context.HH[i] = <b>" + res.HH_ON + "</b>" + Environment.NewLine;
                resStr += "            context.LL[i] = <b>" + res.LL_ON + "</b>" + Environment.NewLine;
                resStr += "            context.profitable_closes[i] = <b>" + (res.ProfX == 0 ? 9999 : res.ProfX) + "</b>" + Environment.NewLine;
                resStr += "            context.max_time[i] = <b>" + (res.MaxTime == 0 ? 9999 : res.MaxTime) + "</b>" + Environment.NewLine;
                resStr += "            context.position_amount[i] = <b>" + res.OutOfSample + "</b>" + Environment.NewLine;
                // intraday filter params
                bool intraday = Properties.Settings.Default.TTaddCode && res.ReadonlyIntradayTestResults?.Count == 5;
                if (intraday)
                {
                    resStr += "            context.max_daily_entries[i] = <b>" + res.ReadonlyIntradayTestResults[4] + "</b>" + Environment.NewLine;
                    resStr += "            context.max_pnl[i] = <b>" + res.ReadonlyIntradayTestResults[0] + "</b>" + Environment.NewLine;
                    resStr += "            context.min_pnl[i] = <b>" + res.ReadonlyIntradayTestResults[1] + "</b>" + Environment.NewLine;
                    resStr += String.Format("            context.trading_periods[i] = <b>['{0:D2}:{1:D2}-{2:D2}:{3:D2}']</b> # format: 'HH:MM-HH:MM'{4}",
                        res.ReadonlyIntradayTestResults[2] / 10000, (res.ReadonlyIntradayTestResults[2] % 10000) / 100, res.ReadonlyIntradayTestResults[3] / 10000, (res.ReadonlyIntradayTestResults[3] % 10000) / 100, Environment.NewLine);
                    resStr += "            context.reset_filter[i] = True # Turns on/off resetting the filter variables" + Environment.NewLine;
                }
                else
                {
                    resStr += "            context.max_daily_entries[i] = 999999" + Environment.NewLine;
                    resStr += "            context.max_pnl[i] = 9999999" + Environment.NewLine;
                    resStr += "            context.min_pnl[i] = -9999999" + Environment.NewLine;
                    resStr += "            context.trading_periods[i] = ['00:00-23:59'] # format: 'HH:MM-HH:MM'" + Environment.NewLine;
                    resStr += "            context.reset_filter[i] = False # Turns on/off resetting the filter variables" + Environment.NewLine;
                }
                resStr += "            context.reset_minutes[i] = 1 # number of minutes after market open to call reset_trade_filters()" + Environment.NewLine;
                resStr += "            context.exit_at_close[i] = <b>" + (res.ForceExitOnSessionEnd ? "True" : "False") + "</b> # Turns on/off auto exit x minutes before market close" + Environment.NewLine;
                resStr += "            context.time_to_close[i] = 1 # minutes prior to market close to exit all positions" + Environment.NewLine;
                index++;
            }

            resStr += @"
        else:
            raise ValueError('No input variables were defined for triplet, index # {}'.format(i))
            
        if context.long_on[i]: # long only strategy
            context.PT[i] = 100000.0
            context.SL[i] = 0.0
        else: # short only strategy
            context.PT[i] = 0.0
            context.SL[i] = 100000.0
        
        # Update index i
        i += 1
        ";

            int intraday_bar_length = 5;
            //todo: autocheck timeframe Nov 2020
            /*if (readyResults.Count > 0 && symbolsDescription.ContainsKey(readyResults[0].SymbolId))
            {
                SymbolResData sym = symbolsDescription[readyResults[0].SymbolId];
                int[] Dates = sym.data.Dates;
                int[] Times = sym.data.Times;
                if (Times.Length > 1 && Dates[0] == Dates[1])
                    intraday_bar_length = (Times[1] - Times[0]) / 100;
            }*/

            resStr += @"
    # Rebalance intraday. 
    context.minute_counter = 0
    context.intraday_bar_length = <b>" + intraday_bar_length + @"</b> # number of minutes for the end of day function to run
    
    # Run my_schedule_task and update bars based on configured inputs above
    for i in range(1, 390):
        if i % context.intraday_bar_length == 0: # bar close
            schedule_function(get_intraday_bar, date_rules.every_day(), time_rules.market_open(minutes=i)) # update bars
            if True in context.execution_market_close.values(): # check for True in dictionary
                schedule_function(my_schedule_task, date_rules.every_day(), time_rules.market_open(minutes=i))
            
        if i % context.intraday_bar_length == 1: # bar open
            if True in context.execution_market_open.values(): # check for True in dictionary
                schedule_function(my_schedule_task, date_rules.every_day(), time_rules.market_open(minutes=i))

   ";

            resStr += @"
def before_trading_start(context, data):
    """"""
    Called every day before market open.
    """"""
    i = 0
    for group in context.security_group:
        context.ord1[i] = None
        context.ord2[i] = None
        reset_trade_filters(context, i)
        i += 1
    context.minute_counter = 0
    context.date = get_datetime(context.time_zone)#.strftime(""%H%M""))


def reset_trade_filters(context, index):
    """"""
    Called every day at preset time.
    """"""
    i = 0
    for group in context.security_group:
        if i == index:
            log.info('Reseting all trade filters for triplet {}'.format(i))
            context.daily_entries[i] = 0
            context.closed_pnl[i] = 0
            context.positions_closed[i] = False
            return
        else: i += 1


def get_intraday_bar(context, data):
    """"""
    Function calculates historical ohlcv bars for a custom intraday period.
    """"""
    # Loop through all assets and print the intraday bars
    for stock in context.stocks:

        # Get enough data to form the past 30 intraday bars
        bar_count = context.intraday_bar_length * 30

        # Get bars for stock
        df = data.history(stock, ['open', 'high', 'low', 'close', 'volume'], bar_count, '1m')
        # Resample dataframe for desired intraday bar
        resample_period = str(context.intraday_bar_length) + 'T'
        result = df.resample(resample_period, base = 1).first()
        result['open'] = df['open'].resample(resample_period, base = 1).first()
        result['high'] = df['high'].resample(resample_period, base = 1).max()
        result['low'] = df['low'].resample(resample_period, base = 1).min()
        result['close'] = df['close'].resample(resample_period, base = 1).last()
        result['volume'] = df['volume'].resample(resample_period, base = 1).sum()
        # Remove nan values
        result = result.dropna()

        if PRINT_BARS: log.info('{} {} minute bar: open={}, high={}, low={}, close={}, volume={}'.format(stock.symbol, context.intraday_bar_length, result['open'][-1], result['high'][-1], result['low'][-1], result['close'][-1], result['volume'][-1]))

        # Save bars for stock to context dictionary
            context.intraday_bars[stock] = [result['open'], result['high'], result['low'], result['close']]

    ";

            resStr += String.Format(@"
def my_schedule_task(context,data):
    """"""
    Execute orders according to our schedule_function() timing.
    """"""
    # Check if bar open or close
    if context.minute_counter % context.intraday_bar_length == 1: market_close = False # bar open
    else: market_close = True
        
    # Loop through security groups
    i = 0
    for stocks in context.security_group:
        
        # Do not continue if all positions have been closed for triplet
        if context.positions_closed[i]: return 
        
        # Do not run if not during trading hours
        if not time_to_trade(context.trading_periods[i], context.time_zone): return # Trading is not allowed
        
        # Verify daily entry limit hasn't been reached
        if context.daily_entries[i] >= context.max_daily_entries[i]: continue # go to next group of stocks

        # Check if schedule task should be run for stock
        if market_close:
            # Check if schedule function for stock should be run on bar close
            if not context.execution_market_close[i]: continue # go to next group of stocks
        else: # market open
            # Check if schedule function for stock should be run on bar open
            if not context.execution_market_open[i]: continue # go to next group of stocks
        
        # Try to get prices for all stocks
        try:
            # Get prices for first stock of triplet
            P = context.last[stocks[0]]
            stock0_prices = context.intraday_bars[stocks[0]]
            O = array(stock0_prices[0])
            H = array(stock0_prices[1])
            L = array(stock0_prices[2])
            C = array(stock0_prices[3])
            V = data.history(stocks[0], ""volume"", bar_count=20, frequency=tf)

            CAvg = C[-3:].mean()
            highest = H[-5:].max()
            lowest = L[-5:].min()
            atr = talib.ATR(H,L,C,{0})[-1]
    
            # Get prices for second stock of triplet
            P2 = context.last[stocks[1]]
            stock1_prices = context.intraday_bars[stocks[1]]
            O2 = array(stock1_prices[0])
            H2 = array(stock1_prices[1])
            L2 = array(stock1_prices[2])
            C2 = array(stock1_prices[3])
            V2 = data.history(stocks[1], ""volume"", bar_count=20, frequency=tf)
                                             
            # Get prices for third stock of triplet
            P3 = context.last[stocks[2]]
            stock2_prices = context.intraday_bars[stocks[2]]
            O3 = array(stock2_prices[0])
            H3 = array(stock2_prices[1])
            L3 = array(stock2_prices[2])
            C3 = array(stock2_prices[3])
            V3 = data.history(stocks[2], ""volume"", bar_count=20, frequency=tf)
        except: continue # go to next group of stocks
", readyResults[0].ATR_len);

            //cacl max signals count
            int maxSignalsCount = 1;
            foreach (IReadonlySimStrategy res in readyResults)
                maxSignalsCount = Math.Max(res.ReadonlyEntrySignals.Count, maxSignalsCount);

            // CUSTOMIZE THE ENTRY SIGNALS FOR EACH TRADED SECURITY HERE
            firstInput = true;
            index = 0;
            foreach (IReadonlySimStrategy res in readyResults)
            {
                List<string> rules = new List<string>();
                if (res.ReadonlyEntrySignals == null)
                    rules.Add(res.Name);
                else
                {
                    foreach (Signal s in res.ReadonlyEntrySignals)
                    {
                        if (s is SignalStrategy || s is SignalEnsemble)
                            rules.Add(s.Key);
                        else
                            rules.Add(s.CodePython);
                    }
                }
                for (int i = rules.Count; i < maxSignalsCount; i++)
                    rules.Add("C[-1] != 0");

                if (firstInput)
                {
                    resStr += @"
        if i == " + index + ":" + Environment.NewLine;
                    firstInput = false;
                }
                else
                    resStr += @"
        elif i == " + index + ":" + Environment.NewLine;

                for (int i = 0; i < rules.Count; i++)
                    resStr += "            <b>signal" + (i + 1).ToString() + " = " + rules[i] + "</b>" + (i < rules.Count - 1 ? Environment.NewLine : "");
                index += 3;
            }

            resStr += @"
        else:
            raise ValueError('No signals were defined for triplet, index # {}'.format(i))

        Condition1 = signal1";

            for (int i = 2; i <= maxSignalsCount; i++)
                resStr += " and signal" + i;

            resStr += @"
        
        if context.HH[i]: context.o3[i] = 1  
        if context.LL[i]: context.o4[i] = 1  
        if context.PT_ON[i]: context.o5[i] = 1  
        if context.SL_ON[i]: context.o6[i] = 1
         
        checkZeroOrders = context.portfolio.positions[stocks[0]].amount==0
        
        if checkZeroOrders and Condition1:
            if context.long_on[i]: # long only strategy
                if context.PT_ON[i]: context.PT[i] = C[-1] + atr * 2.00
                if context.SL_ON[i]: context.SL[i] = C[-1] - atr * 2.00
                order(stocks[0], context.position_amount[i])
                if PRINT_ORDERS: log.info(""Bought to open {} at price {}"".format(stocks[0].symbol,P))
                context.daily_entries[i] += 1
                
            else: # short only strategy
                if context.PT_ON[i]: context.PT[i] = C[-1] - atr * 2.00
                if context.SL_ON[i]: context.SL[i] = C[-1] + atr * 2.00
                order(stocks[0], -context.position_amount[i])
                if PRINT_ORDERS: log.info(""Sold to open {} at price {}"".format(stocks[0].symbol, P))
                context.daily_entries[i] += 1

            context.entry_price[i] = P
            context.bars_since_entry[i] = 0
            context.prof_x[i] = 0

        if context.HH[i]: context.Highest[i] = highest[-1]
        if context.LL[i]: context.Lowest[i] = lowest[-1]

        if not checkZeroOrders: # have an open position
            co1 = 0
            co2 = 0
            context.bars_since_entry[i] += 1
            if context.bars_since_entry[i] > 0:
                if context.long_on[i] and P >= context.entry_price[i]:
                    context.prof_x[i] += 1
                if not context.long_on[i] and P <= context.entry_price[i]:
                    context.prof_x[i] += 1

            if context.prof_x[i] >= context.profitable_closes[i]: co1 = 1
            if context.bars_since_entry[i] >= context.max_time[i]: co2 = 1

            if co1 or co2:
                # Cancel open orders for triplet
                if context.ord1[i]:
                    cancel_order(context.ord1[i])
                    context.ord1[i] = None
                if context.ord2[i]:
                    cancel_order(context.ord2[i])
                    context.ord2[i] = None

                # Close current position, if one
            if context.entry_price[i] and context.long_on[i]:
                    order(stocks[0], -context.position_amount[i])
                    if PRINT_ORDERS: log.info(""Sold to close {} at price {}"".format(stocks[0].symbol, P))
                    context.closed_pnl[i] += ((P - context.entry_price[i]) * context.position_amount[i])

                elif context.entry_price[i]:
                    order(stocks[0], context.position_amount[i])
                    if PRINT_ORDERS: log.info(""Bought to close {} at price {}"".format(stocks[0].symbol, P))
                    context.closed_pnl[i] += ((context.entry_price[i] - P) * context.position_amount[i])

                context.bars_since_entry[i] = 0
                context.prof_x[i] = 0

        # Update index i
        i += 1
";

            resStr += @"
def handle_data(context,data):
    """"""
    Called every minute.
    """"""
    context.minute_counter += 1 # increment minute counter
    
    # Get the current exchange time, in local timezone: 
    dt = pd.Timestamp(get_datetime()).tz_convert(context.time_zone) # pandas.tslib.Timestamp type

    # Get last prices for all stocks
    for stock in context.stocks:
        context.last[stock] = data.current(stock, 'price')
    
    # Get tradeable securities only
    stocks = context.trade_securities
    Last = data.history(stocks, ""price"", bar_count=20, frequency='1m')
    
    i = 0
    for group in context.security_group:
        
        stock = group[0] # tradeable security of group
        P = Last[stock]

        # Check if the trade filter should be reset for the triplet
        if context.reset_filter[i] and context.reset_minutes[i] == context.minute_counter: reset_trade_filters(context, i)
         
        # Check if it is time to close the triplet positions for the end of the day
        if before_close(context, dt, context.time_to_close[i]):
            if not context.positions_closed[i]:
                log.info('Time to close all positions for {}, triplet {} for the end of the day'.format(stock.symbol, i))
                close_triplet_positions(context, i)
            return
            
        # Do not continue if all positions have been closed for triplet
        if context.positions_closed[i]: return 
        
        # Check whether trading is allowed or not
        prev_status = context.status[i]
        if time_to_trade(context.trading_periods[i], context.time_zone): # Trading is allowed
            context.status[i] = 'TRADING'
            if PRINT_TRADE_TIMES and prev_status != context.status[i]: log.info('Trading has started for {}, triplet {}.'.format(stock.symbol, i))
        else: # Trading is not allowed
            if context.status[i] == 'TRADING':
                if PRINT_TRADE_TIMES: log.info('Trading has stopped for {}, triplet {}.'.format(stock.symbol, i))
                close_triplet_positions(context, i)
            context.status[i] = 'NOT TRADING'

        # Get current pnl and exit all positions if limits are reached
        open_position_pnl = 0
        if context.entry_price[i]:
            if context.long_on[i]: open_position_pnl = ((P[-1]-context.entry_price[i])*context.position_amount[i])
            else:
                open_position_pnl = ((context.entry_price[i]-P[-1])*context.position_amount[i])
        current_pnl = open_position_pnl + context.closed_pnl[i]       
        
        if current_pnl >= context.max_pnl[i] or current_pnl <= context.min_pnl[i]:
            close_triplet_positions(context, i)
        
        # Check for exit signals
        if context.portfolio.positions[stock].amount > 0: # open long position 
            if context.ord1[i] is None and context.ord2[i] is None:
                if context.long_on[i]: # long only strategy
                    co3 = context.o3[i] and P[-1] > context.Highest[i]
                    co4 = context.o4[i] and P[-1] < context.Lowest[i]
                    co5 = context.o5[i] and P[-1] > context.PT[i]
                    co6 = context.o6[i] and P[-1] < context.SL[i]
                    LimitPr = round(min(context.Highest[i], context.PT[i]),2)
                    StopPr = round(max(context.Lowest[i], context.SL[i]),2)
                    
                    #log.info(""Checking {0} at limit price {1} and Stop price {2}"".format(stock,LimitPr,StopPr)) 
                    
                    if co3 or co5:
                        context.ord1[i] = order_target(stock,0,style=LimitOrder(LimitPr))
                        if PRINT_ORDERS: log.info(""Sold to close {} at limit price {}"".format(stock.symbol,LimitPr))
                        context.closed_pnl[i] += ((P[-1]-context.entry_price[i])*context.position_amount[i])    
                    elif co4 or co6:
                        context.ord2[i] = order_target(stock,0,style=StopOrder(StopPr))
                        if PRINT_ORDERS: log.info(""Sold to close {} at stop price {}"".format(stock.symbol,StopPr))
                        context.closed_pnl[i] += ((P[-1]-context.entry_price[i])*context.position_amount[i])
                           
                else: # short only strategy
                    log.info(""ERROR: INVALID LONG POSITION FOR SHORT {} TRIPLET"".format(stock.symbol))
      
        elif context.portfolio.positions[stock].amount < 0: # open short position
            if context.ord1[i] is None and context.ord2[i] is None:
                if not context.long_on[i]: # short only strategy
                    co3 = context.o3[i] and P[-1] > context.Highest[i]
                    co4 = context.o4[i] and P[-1] < context.Lowest[i]
                    co5 = context.o5[i] and P[-1] < context.PT[i]
                    co6 = context.o6[i] and P[-1] > context.SL[i]   
                    LimitPr = round(min(context.Lowest[stock], context.PT[stock]),2)
                    StopPr = round(max(context.Highest[stock], context.SL[stock]),2)
                    
                    #log.info(""Checking {0} at limit price {1} and Stop price {2}"".format(stock,LimitPr,StopPr))
                    
                    if co3 or co5:
                        context.ord1[i] = order_target(stock,0,style=LimitOrder(LimitPr))
                        if PRINT_ORDERS: log.info(""Bought to close {} at limit price {}"".format(stock.symbol,LimitPr))
                        context.closed_pnl[i] += ((context.entry_price[i]-P[-1])*context.position_amount[i])
                    elif co4 or co6:
                        context.ord2[i] = order_target(stock,0,style=StopOrder(StopPr))
                        if PRINT_ORDERS: log.info(""Bought to close {} at stop price {}"".format(stock.symbol,StopPr))
                        context.closed_pnl[i] += ((context.entry_price[i]-P[-1])*context.position_amount[i])
 
                else: # long only strategy
                    log.info(""ERROR: INVALID SHORT POSITION FOR LONG {} TRIPLET"".format(stock.symbol))

        else: # no open position
            #log.info(""Resetting parameters {0}"".format(stock))
            context.ord1[i] = None
            context.ord2[i] = None
            context.entry_price[i] = None
            context.Highest[i] = 100000.0
            context.Lowest[i] = 0.0
            if context.long_on[i]: # long only strategy
                context.PT[i] = 100000.0
                context.SL[i] = 0.0
            else: # short only strategy
                context.PT[i] = 0.0
                context.SL[i] = 100000.0
            
        # Update index i
        i += 1
        
def Cube_HLC(H,L,C):
    return (H * L * C) ** (1. / 3.)


def time_to_trade(periods, time_zone):
    """"""
    Check if the current time is inside trading intervals specified by the periods parameter.

    :param periods: periods
    :type periods: list of strings in 'HH:MM-HH:MM' format
    :param time_zone: Time zone
    :type time_zone: string

    returns: True if current time is with a defined period, otherwise False
    """"""
    # Convert current time to HHMM int
    now = int(get_datetime(time_zone).strftime(""%H%M""))

    for period in periods:
        # Convert ""HH:MM-HH:MM"" to two integers HHMM and HHMM
        splitted = period.split('-')
        start = int(''.join(splitted[0].split(':')))
        end = int(''.join(splitted[1].split(':')))

        if start <= now < end: return True
    return False


def close_triplet_positions(context, index):
    """"""
    Cancel any open orders and close any positions for a given triplet.
    """"""
    i = 0
    for group in context.security_group:
        if i == index:
            stock = group[0]
            P = context.last[stock]
            log.info('Cancelling open orders and closing any positions for triplet {}'.format(i))
            # Cancel open orders for triplet
            if context.ord1[i]:
                cancel_order(context.ord1[i])
                context.ord1[i] = None
            if context.ord2[i]:
                cancel_order(context.ord2[i])
                context.ord2[i] = None
            
            # Close current position, if one
            if context.entry_price[i] and context.long_on[i]:
                order(stock, -context.position_amount[i])
                if PRINT_ORDERS: log.info(""Sold to close {} at price {}"".format(stock.symbol,P))
                context.closed_pnl[i] += ((P-context.entry_price[i])*context.position_amount[i])
                    
            elif context.entry_price[i]:
                order(stock, context.position_amount[i])
                if PRINT_ORDERS: log.info(""Bought to close {} at price {}"".format(stock.symbol,P))
                context.closed_pnl[i] += ((context.entry_price[i]-P)*context.position_amount[i])
            
            context.bars_since_entry[i] = 0
            context.prof_x[i] = 0

            # Set positions_closed variable to True
            context.positions_closed[i] = True
            return
            
        else: i += 1
            
            
def before_close(context, dt, minutes=0, hours=0):
    '''
    Determine if it is a variable number of hours/minutes before the market close.
    dt = pandas.tslib.Timestamp
    
    Trading calendar source code
    https://github.com/quantopian/zipline/blob/master/zipline/utils/calendars/trading_calendar.py
    '''
    tz = pytz.timezone(context.time_zone)
    
    date = get_datetime().date()
    # set the closing hour
    if date in context.early_closes: close_hr = 13 # early closing time (EST)
    else: close_hr = 16 # normal closing time (EST)
        
    close_dt = tz.localize(datetime.datetime(date.year, date.month, date.day, close_hr, 0, 0)) # datetime.datetime with tz
    close_dt = pd.Timestamp(close_dt) # convert datetime.datetime with tz to pandas.tslib.Timestamp

    delta_t = datetime.timedelta(minutes=60*hours + minutes)
    
    return dt > close_dt - delta_t
";

            // append additional functions
            string add = Environment.NewLine;
            foreach (KeyValuePair<string, string> p in SignalsData.getSignalNamesPythonAddition)
            {
                if (resStr.Contains(p.Key))
                    add += p.Value + Environment.NewLine + Environment.NewLine;
            }
            resStr += add;

            return new List<string>() { resStr };
        }

        public static List<string> PRT(List<IReadonlySimStrategy> activeResults, out List<KeyValuePair<string, string>> extraEvCode)
        {
            StringBuilder str = new StringBuilder();
            extraEvCode = new List<KeyValuePair<string, string>>();

            // filter startegies with Vix, sprd1, sprd2 signals

            foreach (var res in activeResults)
            {
                List<string> rules = new List<string>();
                List<string> rulesExtraCode = new List<string>();
                List<string> exitRules = new List<string>();
                List<string> exitRulesExtraCode = new List<string>();
                List<string> triggersIndex = new List<string>();
                List<int> triggersLookb = new List<int>();
                List<string> triggersRule1 = new List<string>();
                List<string> exitTriggersIndex = new List<string>();
                List<int> exitTriggersLookb = new List<int>();
                List<string> exitTriggersRule1 = new List<string>();
                List<int> intradayRulesIndexes = new List<int>();
                {
                    if (res.ReadonlyEntrySignals == null)
                    {
                        rules.Add(res.Name);
                        rulesExtraCode.Add(string.Empty);
                    }
                    else
                    {
                        rules.Add(string.Format("Date[0]>{0} or (Date[0]={0} and Time[0]>={1})", res.SignalStartDate, res.SignalStartTime));
                        rulesExtraCode.Add(string.Empty);
                        try
                        {
                            char pref = 'A';
                            foreach (Signal s in res.ReadonlyEntrySignals)
                            {
                                if (s is SignalStrategy || s is SignalEnsemble)
                                {
                                    rules.Add(s.Key);
                                    rulesExtraCode.Add(string.Empty);
                                }
                                else
                                {
                                    int index = 1;
                                    string extraCode = s.GetExtraCode(Signal.CodeType.PRT, (pref++).ToString(), ref index);
                                    rulesExtraCode.Add(extraCode);

                                    if (s is SignalTrigger sigTrig && sigTrig.Children?.Count >= 2)
                                    {
                                        sigTrig.triggerIndex = triggersIndex.Count;         // specify index before getting code
                                        triggersIndex.Add(sigTrig.triggerIndex.ToString());
                                        triggersLookb.Add(decimal.ToInt32(sigTrig.Args.FirstOrDefault(x => x.Key == SignalParametric.rule1_Base_Offset_key)?.BaseValue ?? 0));
                                        triggersRule1.Add(sigTrig.Children[0].GetCode(Signal.CodeType.PRT));
                                    }

                                    rules.Add(string.IsNullOrEmpty(extraCode) ? s.CodePRT : s.GetCode(Signal.CodeType.PRT));
                                }
                            }
                        }
                        catch (KeyNotFoundException)    //no base or custom signal found
                        {
                            //MessageBox.Show(ex.Message);
                            continue;
                        }
                    }
                }
                if (res.ReadonlyExitSignals != null)
                {
                    char pref = 'A';
                    foreach (Signal s in res.ReadonlyExitSignals)
                    {
                        if (s is SignalStrategy || s is SignalEnsemble)
                        {
                            exitRules.Add(s.Key);
                            exitRulesExtraCode.Add(string.Empty);
                        }
                        else
                        {
                            int index = 1;
                            string extraCode = s.GetExtraCode(Signal.CodeType.PRT, $"Ex{pref++}", ref index);
                            exitRulesExtraCode.Add(extraCode);

                            if (s is SignalTrigger sigTrig && sigTrig.Children?.Count >= 2)
                            {
                                sigTrig.triggerIndex = triggersIndex.Count + exitTriggersIndex.Count;         // specify index before getting code
                                exitTriggersIndex.Add(sigTrig.triggerIndex.ToString());
                                exitTriggersLookb.Add(decimal.ToInt32(sigTrig.Args.FirstOrDefault(x => x.Key == SignalParametric.rule1_Base_Offset_key)?.BaseValue ?? 0));
                                exitTriggersRule1.Add(sigTrig.Children[0].GetCode(Signal.CodeType.PRT));
                            }

                            exitRules.Add(string.IsNullOrEmpty(extraCode) ? s.CodePRT : s.GetCode(Signal.CodeType.PRT));
                        }
                    }
                }

                str.AppendFormat(@"//-----------------------------------------
//  Strategy Details:
//  Symbol: <b>{0}</b>
//  Market 2: <b>{15}</b>
//  Market 3: <b>{16}</b>
//  Market 4: <b>{17}</b>
//  Start Date: <b>{1}</b>
//  Stop Date: <b>{2}</b>
//  Out of Sample: <b>{3} %</b>
//  Fitness Function: <b>{4}</b>
//  Profit Target On: <b>{5}</b>
//  Profit Multiple: <b>{6}</b>
//  Stop Loss On: <b>{7}</b>
//  Stop Loss Multiple: <b>{8}</b>
//  Highest High On: <b>{9}</b>
//  Highest High Lookback: <b>{10}</b>
//  Lowest Low On: <b>{11}</b>
//  Lowest Low Lookback: <b>{12}</b>
//  Max Time: <b>{13}</b>
//  Profitable Closes: <b>{14}</b>
//-----------------------------------------
", res.Symbol, res.SignalStartDate, res.StopDate, res.OutOfSample,
res.Fitness.ToString(),
res.PT_ON > 0 ? "Yes" : "No", res.PT_mult, res.SL_ON > 0 ? "Yes" : "No", res.SL_mult,
res.HH_ON > 0 ? "Yes" : "No", res.HHlook, res.LL_ON > 0 ? "Yes" : "No", res.LLlook,
res.MaxTime == 0 ? 9999 : res.MaxTime, res.ProfX == 0 ? 9999 : res.ProfX,
res.GetMarketSymbolId(2).IsEmpty() ? "---" : res.GetMarketSymbolId(2).ToString(),
res.GetMarketSymbolId(3).IsEmpty() ? "---" : res.GetMarketSymbolId(3).ToString(),
res.GetMarketSymbolId(4).IsEmpty() ? "---" : res.GetMarketSymbolId(4).ToString());

                bool intraday = Properties.Settings.Default.TTaddCode && res.ReadonlyIntradayTestResults?.Count == 5;

                str.AppendFormat(@"
DEFPARAM CumulateOrders = False
//-----------------------------------------
//  declarations
//
ONCE PTON = <b>{0}</b>
ONCE SLON = <b>{1}</b>
ONCE TLON = <b>{8}</b>
ONCE PT = <b>{2:F2}</b>
ONCE SL = <b>{3:F2}</b>
ONCE TL = <b>{9:F2}</b>
ONCE HHON = <b>{4}</b>
ONCE LLON = <b>{5}</b>
ONCE Maxtime = <b>{6}</b>
ONCE Profitablecloses = <b>{7}</b>
ONCE Profx = 0
ONCE delay = <b>{10}</b>
", res.PT_ON, res.SL_ON, res.PT_mult, res.SL_mult, res.HH_ON, res.LL_ON, res.MaxTime == 0 ? 9999 : res.MaxTime, res.ProfX == 0 ? 9999 : res.ProfX,
res.TL_ON, res.TL_mult, res.DelayedEntry);

                str.AppendLine(@"
// -----------------------------------------
//  Position Sizing
//");

                double mult;
                {
                    mult = Utils.SymbolsManager.GetSymbolMult(res.SymbolId);
                    if (mult == 0) mult = 1;
                }
                float LastConversion;
                {
                    int[] DD = new int[1];
                    float[] Val = new float[1];
                    DateTime dt = DateTime.Now;
                    DD[0] = dt.Year * 10000 + dt.Month * 100 + dt.Day;
                    Val[0] = 1;
                    GenerateSignalsToTrade.CalcPositionSizes(res.PosSizeMode, res.SymbolId, mult, 1, res.AccountValue, res.Currency, DD, Val, Val, out LastConversion, Utils.SymbolsManager);
                }

                if (res.PosSizeMode == PositionSizingMode.Fixed)
                {
                    if (res.SymbolType == SymbolType.FOREX)
                    {
                        str.AppendFormat(@"
once account = <b>{0}</b>
once conversion = <b>{1}</b>
lotsize = (account / conversion) / 1000
", res.AccountValue, LastConversion);
                    }
                    else if (res.SymbolType == SymbolType.Futures)
                    {
                        str.AppendFormat(@"
ONCE account = <b>{0}</b>
ONCE margin  = <b>{1}</b>
lotsize = account / margin
", res.AccountValue, Utils.SymbolsManager.GetSymbolMargin(res.SymbolId));
                    }
                    else
                    {
                        str.AppendFormat(@"
once account = <b>{0}</b>
lotsize = account / close[0]
", res.AccountValue);
                    }
                }
                else if (res.PosSizeMode == PositionSizingMode.Volatility)
                {
                    if (res.SymbolType == SymbolType.FOREX)
                    {
                        str.AppendFormat(@"
once account = <b>{0}</b>
once risk    = .01
once conversion = <b>{1}</b>
atr = averageTrueRange[<b>{2}</b>](Close)
lotsize = ((account * risk) * (conversion / atr)) / 1000
", res.AccountValue, LastConversion, res.ATR_len);
                    }
                    else if (res.SymbolType == SymbolType.Futures)
                    {
                        str.AppendFormat(@"
once account = <b>{0}</b>
once bpv     = <b>{1}</b>
once type = SL * SLON
atr = averageTrueRange[<b>{2}</b>](Close)
if type > 0 then
type = type
else
type = 1
endif
lotsize = (account * .01) / (atr * type * bpv)
", res.AccountValue, mult, res.ATR_len);
                    }
                    else
                    {
                        str.AppendFormat(@"
once account = <b>{0}</b>
once type = SL * SLON
atr = averageTrueRange[<b>{1}</b>](Close)
if type > 0 then
type = type
else
type = 1
endif
lotsize = account / (atr * type)
", res.AccountValue, res.ATR_len);
                    }
                }

                if (intraday) str.AppendFormat(@"
//-----------------------------------------
//  Intraday Checks
//
once tt = 0
once startpnl = strategyprofit
if tradeindex = barindex and <b>{0}</b> then
tt = tt + 1
endif
if time >= <b>{1:D4}00</b> and time[1] < <b>{1:D4}00</b> then
tt = 0
pnltoday = 0
startpnl = strategyprofit
endif
pnltoday = strategyprofit - startpnl
", res.IsLong ? "longonmarket" : "shortonmarket", res.SessionEndTime);

                str.Append(@"
//-----------------------------------------
//  Entry
//
<b>");

                for (int i = 0; i < rules.Count; i++)
                    if (intradayRulesIndexes.Contains(i))
                    {
                        str.AppendLine("timeframe(daily,updateonclose)");
                        str.Append(rulesExtraCode[i]);
                        //str.AppendLine($"Indicator{i + 1} = {rules[i]}");
                        str.AppendLine("timeframe(default,updateonclose)");
                    }
                    else
                    {
                        str.Append(rulesExtraCode[i]);
                        //str.AppendLine($"Indicator{i + 1} = {rules[i]}");
                    }

                for (int i = 0; i < triggersIndex.Count; ++i)
                {
                    str.AppendFormat(@"trigger{0} = 0
count{0} = 0
for count{0} = 0 to {1} do
if ({2})[count{0}] then
trigger{0} = trigger{0} + 1
endif
next

", triggersIndex[i], triggersLookb[i], triggersRule1[i]);
                }

                for (int i = 0; i < rules.Count; i++)
                    str.AppendLine($"Indicator{i + 1} = {rules[i]}");

                str.Append("IndicatorALL = Indicator1");
                for (int i = 2; i <= rules.Count; i++)
                    str.Append(" and Indicator" + i);

                str.AppendFormat(@"{0}</b>
IF indicatorALL[delay] then
", !intraday ? "" : string.Format(" and time >= {0:D6} and time <= {1:D6} and tt <= {2} and pnltoday <= {3} and pnltoday >= {4}",
res.ReadonlyIntradayTestResults[2], res.ReadonlyIntradayTestResults[3], res.ReadonlyIntradayTestResults[4], res.ReadonlyIntradayTestResults[0], res.ReadonlyIntradayTestResults[1]));

                if (res.IsLong)
                {
                    str.AppendFormat(@"BUY <b>{0}</b> SHARES AT MARKET
if (tradeprice <> tradeprice(1) and barindex = tradeindex) or (not longonmarket) then
", res.PosSizeMode == PositionSizingMode.Default ? "1" : "lotsize");
                }
                else
                {
                    str.AppendFormat(@"SELLSHORT <b>{0}</b> SHARES AT MARKET
if (tradeprice <> tradeprice(1) and barindex = tradeindex) or (not shortonmarket) then
", res.PosSizeMode == PositionSizingMode.Default ? "1" : "lotsize");
                }

                if (res.IsLong)
                {
                    str.AppendFormat(@"
IF PTON = 1 THEN
TP = Close[0] + AverageTrueRange[<b>{0}</b>](CLOSE) * PT
SELL AT TP LIMIT
ENDIF
IF PTON = 2 THEN
SET TARGET PROFIT (PT / (pointvalue * <b>{1}</b>))
ENDIF
IF SLON = 1 THEN
LS = Close[0] - AverageTrueRange[<b>{0}</b>](CLOSE) * SL
SELL AT LS STOP
ENDIF
IF SLON = 2 THEN
SET STOP LOSS (SL / (pointvalue * <b>{1}</b>))
ENDIF
Profx = 0
endif
ENDIF

// ------------------------------------------
//  exits
//

IF TLON = 1 then
LT = Close[0] - AverageTrueRange[<b>{0}</b>](CLOSE) * TL
if LONGONMARKET and LT < LT[1] then
LT = LT[1]
ENDIF
SELL AT LT STOP
ENDIF

IF TLON = 2 then
SET STOP pTRAILING (TL / (pointvalue * round(<b>{1}</b> - .5)))
ENDIF

IF LONGONMARKET and close[0] >= TRADEPRICE then
profx = profx + 1
ENDIF

IF profx >= profitablecloses THEN
SELL AT MARKET
ENDIF

IF longonmarket and barindex - tradeindex >= maxtime - 1 THEN
SELL AT MARKET
ENDIF
IF HHON = 1 THEN
SELL AT highest[5](high)LIMIT
ENDIF
IF LLON = 1 THEN
SELL AT lowest[5](low)STOP
ENDIF
", res.ATR_len, res.PosSizeMode == PositionSizingMode.Default ? "1" : "lotsize");
                    str.AppendFormat(@"
// Force Exit
<b>{1}</b>if time >= <b>{0:D4}00</b> and time[1] < <b>{0:D4}00</b> then
<b>{1}</b>sell at market
<b>{1}</b>endif
", res.SessionEndTime, res.ForceExitOnSessionEnd ? "" : "//");
                }
                else
                {
                    str.AppendFormat(@"
IF PTON = 1 THEN
TP = Close[0] - AverageTrueRange[<b>{0}</b>](CLOSE) * PT
EXITSHORT AT TP LIMIT
ENDIF
IF PTON = 2 THEN
SET TARGET PROFIT (PT / (pointvalue * <b>{1}</b>))
ENDIF
IF SLON = 1 THEN
LS = Close[0] + AverageTrueRange[<b>{0}</b>](CLOSE) * SL
EXITSHORT AT LS STOP
ENDIF
IF SLON = 2 THEN
SET STOP LOSS (SL / (pointvalue * <b>{1}</b>))
EXITSHORT AT LS STOP
ENDIF
Profx = 0
endif
ENDIF

// ------------------------------------------
//  exits
//

IF TLON = 1 then
LT = Close[0] + AverageTrueRange[<b>{0}</b>](CLOSE) * TL
if SHORTONMARKET and LT > LT[1] then
LT = LT[1]
ENDIF
EXITSHORT AT LT STOP
ENDIF

IF TLON = 2 then
SET STOP pTRAILING (TL / (pointvalue * round(<b>{1}</b> - .5)))
ENDIF

IF SHORTONMARKET and close[0] <= TRADEPRICE then
profx = profx + 1
ENDIF

IF profx >= profitablecloses THEN
EXITSHORT AT MARKET
ENDIF

IF SHORTONMARKET and barindex - tradeindex >= maxtime - 1 THEN
EXITSHORT AT MARKET
ENDIF
IF HHON = 1 THEN
EXITSHORT AT highest[5](high) STOP
ENDIF
IF LLON = 1 THEN
EXITSHORT AT lowest[5](low) LIMIT
ENDIF
", res.ATR_len, res.PosSizeMode == PositionSizingMode.Default ? "1" : "lotsize");
                    str.AppendFormat(@"
// Force Exit
<b>{1}</b>if time >= <b>{0:D4}00</b> and time[1] < <b>{0:D4}00</b> then
<b>{1}</b>exitshort at market
<b>{1}</b>endif
", res.SessionEndTime, res.ForceExitOnSessionEnd ? "" : "//");
                }

                str.Append("<b>");
                for (int i = 0; i < exitRules.Count; i++)
                    str.Append(exitRulesExtraCode[i]);

                for (int i = 0; i < exitTriggersIndex.Count; ++i)
                {
                    str.AppendFormat(@"trigger{0} = 0
count{0} = 0
for count{0} = 0 to {1} do
if ({2})[count{0}] then
trigger{0} = trigger{0} + 1
endif
next

", exitTriggersIndex[i], exitTriggersLookb[i], exitTriggersRule1[i]);
                }

                for (int i = 0; i < exitRules.Count; i++)
                    str.AppendLine($"IndicatorExit{i + 1} = {exitRules[i]}");

                str.AppendFormat("{0}IndicatorExitALL = IndicatorExit1", exitRules.Count > 0 ? "" : "//");
                for (int i = 2; i <= exitRules.Count; i++)
                    str.Append(" and IndicatorExit" + i);
                str.Append("</b>");

                str.AppendFormat(@"
// Signal Exit
<b>{0}</b>if <b>{1}</b> then
<b>{0}</b><b>{2}</b> at market
<b>{0}</b>endif
", res.ExitMode > 0 && res.ReadonlyExitSignals?.Count > 0 ? "" : "//", "IndicatorExitALL[0]", res.IsLong ? "sell" : "EXITSHORT");

                {
                    var evSigs = res.ReadonlyEntrySignals?.Where(x => x is SignalNewsEvents);
                    if (evSigs != null)
                        foreach (var sig in evSigs)
                        {
                            var extraEVcode = GetEventsExtraCode(new List<Signal>() { sig }, Security.ToDateTime(res.SignalStartDate, res.SignalStartTime), Security.ToDateTime(res.StopDate, 235959), "PRT");
                            if (string.IsNullOrEmpty(extraEVcode) == false)
                            {
                                extraEvCode.Add(new KeyValuePair<string, string>(/*(extraEvCode.Count + 1).ToString()*/sig.Text, extraEVcode));
                            }
                        }
                    evSigs = res.ReadonlyExitSignals?.Where(x => x is SignalNewsEvents);
                    if (evSigs != null)
                        foreach (var sig in evSigs)
                        {
                            var extraEVcode = GetEventsExtraCode(new List<Signal>() { sig }, Security.ToDateTime(res.SignalStartDate, res.SignalStartTime), Security.ToDateTime(res.StopDate, 235959), "PRT");
                            if (string.IsNullOrEmpty(extraEVcode) == false && extraEvCode.All(x => x.Value != extraEVcode))
                            {
                                extraEvCode.Add(new KeyValuePair<string, string>(/*(extraEvCode.Count + 1).ToString()*/sig.Text, extraEVcode));
                            }
                        }
                }
            }

            if (str.Length == 0)
            {
                extraEvCode = null;
                return null;
                //ShowMessage("There are no strategies with supported signals");
            }
            else
            {
                if (extraEvCode.Count == 0) extraEvCode = null;

                return new List<string>() { str.ToString() };
            }
        }


        private static string GetEventsExtraCode(IEnumerable<Signal> eventSignals, DateTime start, DateTime stop, string type)
        {
            if (eventSignals == null || eventSignals.Count() == 0 || stop < start) return null;

            HashSet<DateTime> newsDates = new HashSet<DateTime>();

            foreach (var sig in eventSignals)
            {
                if (sig is SignalNewsEvents sigEv)
                {
                    var dts = sigEv.GetEventDateTimes();
                    foreach (var dt in dts)
                    {
                        var dtoffset = dt.AddDays(sigEv.DaysOffset);
                        if (dtoffset >= start && dtoffset <= stop && newsDates.Contains(dtoffset) == false)
                            newsDates.Add(dtoffset);
                    }
                }
            }

            if (newsDates.Count == 0) return null;

            // generate code
            StringBuilder output = new StringBuilder();

            switch (type)
            {
                default:
                case "EL":
                    output.AppendLine("value1 = 0;");
                    output.AppendLine("if");
                    foreach (var dt in newsDates)
                        output.AppendLine($"    date = {dt.Year - 1900}{dt.Month:D2}{dt.Day:D2} or");
                    output.Length -= 4;
                    output.AppendLine();
                    output.AppendLine("then value1 = 1;");
                    output.AppendLine("NewsEventN = value1;");
                    break;
                case "NT":
                    output.AppendLine("if (CurrentBar < 10)");
                    output.AppendLine("\treturn;");
                    output.Append("if (");
                    foreach (var dt in newsDates)
                        output.AppendLine($"\tToDay(Time[0]) == {dt.Year}{dt.Month:D2}{dt.Day:D2} ||");
                    output.Length -= 4;
                    output.AppendLine(")");
                    output.AppendLine("{ Value[0] = 1; }");
                    break;
                case "PRT":
                    output.AppendLine("value1 = 0");
                    output.Append("IF");
                    foreach (var dt in newsDates)
                        output.Append($" Date[0] = {dt.Year}{dt.Month:D2}{dt.Day:D2} or");
                    output.Length -= 3;
                    output.AppendLine(" THEN");
                    output.AppendLine("value1 = 1");
                    output.AppendLine("ENDIF");
                    output.AppendLine("RETURN value1");
                    break;
                case "MT4":
                    output.AppendLine("int NewsEventN()");
                    output.AppendLine("{");
                    output.AppendLine("\tint value1 = 0;");
                    output.Append("\tif (");
                    foreach (var dt in newsDates)
                        output.AppendLine($"\t\tTimeToStr(Time[1],TIME_DATE) == \"{dt.Year}.{dt.Month:D2}.{dt.Day:D2}\" ||");
                    output.Length -= 4;
                    output.AppendLine(")");
                    output.AppendLine("\t{ value1 = 1; }");
                    output.AppendLine("\treturn value1;");
                    output.AppendLine("}");
                    break;
            }
            return output.ToString();
        }
    }
}
