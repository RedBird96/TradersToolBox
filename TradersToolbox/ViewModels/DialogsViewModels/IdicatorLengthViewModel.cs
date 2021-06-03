using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradersToolbox.ViewModels.DialogsViewModels
{
    public class IdicatorLengthViewModel
    {
        public List<IIndicatorField> Fields { get; set; }
    }

    public class IndicatorField<T> : IIndicatorField
    {
        public string Title { get; set; }

        public string Name { get; set; }

        public T Value { get; set; }
    }

    public interface IIndicatorField
    {
        string Title { get; set; }

        string Name { get; set; }
    }


}
