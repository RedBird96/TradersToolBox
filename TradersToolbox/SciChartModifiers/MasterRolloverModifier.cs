using SciChart.Charting.ChartModifiers;
using SciChart.Charting.Model.ChartData;
using SciChart.Core.Utility.Mouse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TradersToolbox.ViewModels;
using TradersToolbox.ViewModels.ChartViewModels;

namespace TradersToolbox.SciChartModifiers
{
    public class MasterRolloverModifier : RolloverModifier
    {
        private List<RolloverModifier> childModifiers = new List<RolloverModifier>();
        public MasterRolloverModifier()
        {
        }

        public void RegisterChildModifiers(RolloverModifier modifier)
        {
            this.childModifiers.Add(modifier);
        }

        public void RemoveChildModifiers(RolloverModifier modifier)
        {
            this.childModifiers.Remove(modifier);
        }


        // Forward on the mouse move event to child modifiers
        public override void OnModifierMouseMove(ModifierMouseArgs e)
        {
            base.OnModifierMouseMove(e);

            if (e.IsMaster)
            {
                foreach (var child in this.childModifiers)
                {
                    e.IsMaster = false;
                    child?.OnModifierMouseMove(e);
                }
            }
        }

        public override void OnModifierMouseDown(ModifierMouseArgs e)
        {
            base.OnModifierMouseDown(e);

            if (e.IsMaster)
            {
                foreach (var child in this.childModifiers)
                {
                    e.IsMaster = false;
                    child?.OnModifierMouseDown(e);
                }
            }
        }

        public override void OnModifierMouseUp(ModifierMouseArgs e)
        {
            base.OnModifierMouseUp(e);

            if (e.IsMaster)
            {
                foreach (var child in this.childModifiers)
                {
                    e.IsMaster = false;
                    child?.OnModifierMouseUp(e);
                }
            }
        }

        protected override IEnumerable<SciChart.Charting.Model.ChartData.SeriesInfo> GetSeriesInfoAt(Point point)
        {
            var result = base.GetSeriesInfoAt(point);
            var vm = (this.DataContext as CandleChartViewModel);
            return vm.GetCustomSeriesInfo(result);
        }

    }
}
