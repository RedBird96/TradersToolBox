using DevExpress.Mvvm.Native;
using SciChart.Charting.Model.DataSeries;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TradersToolbox.Core;
using TradersToolbox.Data;
using TradersToolbox.ViewModels.DialogsViewModels;

namespace TradersToolbox.ChartIndicators
{
    public static class IndicatorUtils
    {

        public static List<IXyDataSeries> GetDataSeriesForIndicator(IndicatorType IndicatorType, ObservableCollection<TradingData> ChartDataSource, IndicatorInputType InputType, List<IIndicatorField> fields)
        {
            ChartDataSource = ChartDataSource.OrderBy(var => var.Date).ToObservableCollection();
            List<IXyDataSeries> dataSeriesList = new List<IXyDataSeries>();
            IXyDataSeries DataSeries = null;

            switch (IndicatorType)
            {
                case IndicatorType.ATR:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.ATR(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.Autocor:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.Autocor(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "depth")), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.BollingerBand:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.BollingerBand(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "mult")), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.ConsecutiveHigher:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.ConsecutiveHigher(ChartDataSource.Select(var => (InputType == IndicatorInputType.High) ? var.High : (InputType == IndicatorInputType.Low) ? var.Low : (InputType == IndicatorInputType.Open) ? var.Open : var.Close).Select(var => (float)var).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.ConsecutiveLower:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.ConsecutiveLower(ChartDataSource.Select(var => (InputType == IndicatorInputType.High) ? var.High : (InputType == IndicatorInputType.Low) ? var.Low : (InputType == IndicatorInputType.Open) ? var.Open : var.Close).Select(var => (float)var).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.DayOfMonth:
                    {
                        var dataSeries = new XyDataSeries<DateTime, int>();
                        var dataArray = Indicators.DayOfMonth(ChartDataSource.Select(var => GetHumanReadableDateTime(var.Date)).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.DayOfWeek:
                    {
                        var dataSeries = new XyDataSeries<DateTime, int>();
                        var dataArray = Indicators.DayOfWeek(ChartDataSource.Select(var => GetHumanReadableDateTime(var.Date)).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.Doji:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.Doji(ChartDataSource.Select(var => var.Open).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.EMA:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
//                        Console.WriteLine("ChartDataSource Count: " + ChartDataSource.Count);
                        var dataArray = Indicators.EMA(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.Hammer:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.Hammer(ChartDataSource.Select(var => var.Open).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.Highest:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.Highest(ChartDataSource.Select(var => (InputType == IndicatorInputType.High) ? var.High : (InputType == IndicatorInputType.Low) ? var.Low : (InputType == IndicatorInputType.Open) ? var.Open : var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.Hurst:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var atr = Indicators.ATR(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        var dataArray = Indicators.Hurst(atr, ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.IBS:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.IBS(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.Inverted:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.Inverted(ChartDataSource.Select(var => var.Open).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.KaufmanER:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.KaufmanER(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.Lowest:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.Lowest(ChartDataSource.Select(var => (InputType == IndicatorInputType.High) ? var.High : (InputType == IndicatorInputType.Low) ? var.Low : (InputType == IndicatorInputType.Open) ? var.Open : var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.MaxRange:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.MaxRange(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.MinRange:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.MinRange(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.Month:
                    {
                        var dataSeries = new XyDataSeries<DateTime, int>();
                        var dataArray = Indicators.Month(ChartDataSource.Select(var => GetHumanReadableDateTime(var.Date)).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.PercentChange:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.PercentChange(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.PerformanceMTD:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.PerformanceMTD(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => GetHumanReadableDateTime(var.Date)).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.PerformanceQTD:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.PerformanceQTD(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => GetHumanReadableDateTime(var.Date)).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.PerformanceYTD:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.PerformanceYTD(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => GetHumanReadableDateTime(var.Date)).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.PivotPP:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.PivotPP(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.PivotR1:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.PivotR1(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.PivotR2:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.PivotR2(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.PivotS1:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.PivotS1(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.PivotS2:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.PivotS2(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.Quarter:
                    {
                        var dataSeries = new XyDataSeries<DateTime, int>();
                        var dataArray = Indicators.Quarter(ChartDataSource.Select(var => GetHumanReadableDateTime(var.Date)).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.Range:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.Range(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.RSI:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.RSI(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.SMA:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.SMA(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.TDLM:
                    {
                        var dataSeries = new XyDataSeries<DateTime, int>();
                        var dataArray = Indicators.TDLM(ChartDataSource.Select(var => GetHumanReadableDateTime(var.Date)).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.TDOM:
                    {
                        var dataSeries = new XyDataSeries<DateTime, int>();
                        var dataArray = Indicators.TDOM(ChartDataSource.Select(var => GetHumanReadableDateTime(var.Date)).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.ValueChart:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.ValueChart(ChartDataSource.Select(var => (InputType == IndicatorInputType.High) ? var.High : (InputType == IndicatorInputType.Low) ? var.Low : (InputType == IndicatorInputType.Open) ? var.Open : var.Close).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.Volume:
                    {
                        var dataSeries = new XyDataSeries<DateTime, double>();
                        
                        foreach (TradingData data in ChartDataSource)
                        {
                            int index = dataSeries.FindIndex(data.Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);
                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > data.Date) ? index : index + 1, data.Date, data.Volume);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.WeekNumber:
                    {
                        var dataSeries = new XyDataSeries<DateTime, int>();
                        var dataArray = Indicators.WeekNumber(ChartDataSource.Select(var => GetHumanReadableDateTime(var.Date)).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.WinsLast:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.WinsLast(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;



                case IndicatorType.KeltnerChannel:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.KeltnerChannel(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")), GetFieldValue<float>(fields.First(var => var.Name == "mult")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.ParabolicSAR:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.ParabolicSAR(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "delta")), GetFieldValue<int>(fields.First(var => var.Name == "max")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.CCI:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.CCI(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.ADX:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.ADX(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.DMIpositive:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.DMIpositive(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.DMInegative:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.DMInegative(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.CompositeRSI:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.CompositeRSI(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "min")), GetFieldValue<int>(fields.First(var => var.Name == "max")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.CompositeATR:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.CompositeATR(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "min")), GetFieldValue<int>(fields.First(var => var.Name == "max")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.CompositeHurst:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.CompositeHurst(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "min")), GetFieldValue<int>(fields.First(var => var.Name == "max")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.CompositeStochastic:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.CompositeStochastic(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "min")), GetFieldValue<int>(fields.First(var => var.Name == "max")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.CompositeSuperSmooth:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.CompositeSuperSmooth(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray());
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.CompositeSMA:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.CompositeSMA(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "len1")), GetFieldValue<int>(fields.First(var => var.Name == "len2")), GetFieldValue<int>(fields.First(var => var.Name == "len3")), GetFieldValue<int>(fields.First(var => var.Name == "len4")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.CompositeEMA:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.CompositeEMA(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "len1")), GetFieldValue<int>(fields.First(var => var.Name == "len2")), GetFieldValue<int>(fields.First(var => var.Name == "len3")), GetFieldValue<int>(fields.First(var => var.Name == "len4")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.MACD:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.MACD(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "short_len")), GetFieldValue<int>(fields.First(var => var.Name == "long_len")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.MACDhist:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.MACDhist(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "short_len")), GetFieldValue<int>(fields.First(var => var.Name == "long_len")), GetFieldValue<int>(fields.First(var => var.Name == "signal_len")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.RateOfChange:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.RateOfChange(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.Momentum:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.Momentum(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;
                case IndicatorType.Stochastics:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.Stochastics(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")));
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;

                case IndicatorType.Median:
                    {
                        var dataSeries = new XyDataSeries<DateTime, float>();
                        var dataArray = Indicators.Median(new float[][] { ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray() });
                        for (int i = 0; i < ChartDataSource.Count; i++)
                        {
                            int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                            dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, dataArray[i]);
                        }
                        DataSeries = dataSeries;
                    }
                    break;

                case IndicatorType.BollingerBands:
                    {
                        float[] upper, lower, middle;

                        Indicators.BollingerBands(ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "mult")), GetFieldValue<int>(fields.First(var => var.Name == "length")), out upper, out lower, out middle);
                        {
                            var dataSeries = new XyDataSeries<DateTime, float>();
                            for (int i = 0; i < ChartDataSource.Count; i++)
                            {
                                int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                                dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, upper[i]);
                            }
                            DataSeries = dataSeries;
                        }

                        {
                            var dataSeries = new XyDataSeries<DateTime, float>();
                            for (int i = 0; i < ChartDataSource.Count; i++)
                            {
                                int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                                dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, lower[i]);
                            }
                            dataSeriesList.Add(dataSeries);
                        }

                        {
                            var dataSeries = new XyDataSeries<DateTime, float>();
                            for (int i = 0; i < ChartDataSource.Count; i++)
                            {
                                int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                                dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, middle[i]);
                            }
                            dataSeriesList.Add(dataSeries);
                        }
                    }
                    break;
                case IndicatorType.KeltnerChannels:
                    {
                        float[] upper, lower, middle;

                        Indicators.KeltnerChannel(ChartDataSource.Select(var => var.High).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Low).Select(var => (float)var).ToArray(), ChartDataSource.Select(var => var.Close).Select(var => (float)var).ToArray(), GetFieldValue<int>(fields.First(var => var.Name == "length")), GetFieldValue<float>(fields.First(var => var.Name == "mult")), out upper, out lower, out middle);
                        {
                            var dataSeries = new XyDataSeries<DateTime, float>();
                            for (int i = 0; i < ChartDataSource.Count; i++)
                            {
                                int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                                dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, upper[i]);
                            }
                            DataSeries = dataSeries;
                        }

                        {
                            var dataSeries = new XyDataSeries<DateTime, float>();
                            for (int i = 0; i < ChartDataSource.Count; i++)
                            {
                                int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                                dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, lower[i]);
                            }
                            dataSeriesList.Add(dataSeries);
                        }

                        {
                            var dataSeries = new XyDataSeries<DateTime, float>();
                            for (int i = 0; i < ChartDataSource.Count; i++)
                            {
                                int index = dataSeries.FindIndex(ChartDataSource[i].Date, SciChart.Charting.Common.Extensions.SearchMode.RoundDown);

                                dataSeries.Insert((index == -1) ? 0 : (index == 0 && dataSeries.XValues[index] > ChartDataSource[i].Date) ? index : index + 1, ChartDataSource[i].Date, middle[i]);
                            }
                            dataSeriesList.Add(dataSeries);
                        }
                    }
                    break;
            }

            dataSeriesList.Add(DataSeries);

            return dataSeriesList;
        }

        public static T GetFieldValue<T>(IIndicatorField filed)
        {
            return (filed as IndicatorField<T>).Value;
        }

        public static List<IIndicatorField> GetFieldsForIndicator(IndicatorType type)
        {
            List<IIndicatorField> result = new List<IIndicatorField>();

            MethodInfo method = null;
            if (type == IndicatorType.KeltnerChannels)
                method = typeof(Indicators).GetMembers().Where(mt => mt.Name == "KeltnerChannel").First() as MethodInfo;
            else
                method = typeof(Indicators).GetMethod(type.ToString());

            if (method != null)
            {
                var parameters = method.GetParameters();

                foreach (var parameter in parameters)
                {
                    if (!parameter.ParameterType.IsArray)
                    {
                        if (parameter.ParameterType == typeof(double))
                            result.Add(new IndicatorField<double>() { Title = parameter.Name, Name = parameter.Name, Value = parameter.HasDefaultValue ? (double)parameter.DefaultValue : 1.0 });
                        else if (parameter.ParameterType == typeof(float))
                            result.Add(new IndicatorField<float>() { Title = parameter.Name, Name = parameter.Name, Value = parameter.HasDefaultValue ? (float)parameter.DefaultValue : 1f });
                        else if (parameter.ParameterType == typeof(int))
                            result.Add(new IndicatorField<int>() { Title = parameter.Name, Name = parameter.Name, Value = parameter.HasDefaultValue ? (int)parameter.DefaultValue : 1 });
                    }
                }
            }

            return result;
        }

        private static int GetHumanReadableDateTime(DateTime date)
        {
            return int.Parse(date.ToString("yyyy-MM-dd").Replace("-", string.Empty));
        }
    }

}
