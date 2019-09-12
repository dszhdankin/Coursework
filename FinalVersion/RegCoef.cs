using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace Prototype
{
    class RegCoef
    {
        public double coef, standCoef;
        public string Name { get; set; }
        public string Coef
        {
            set
            {
                try { coef = double.Parse(value, CultureInfo.InvariantCulture); }
                catch (Exception) { }
            }
            get { return $"{coef:F4}"; }
        }
        public string StandCoef
        {
            set
            {
                try { standCoef = double.Parse(value, CultureInfo.InvariantCulture); }
                catch (Exception) { }
            }
            get { return $"{standCoef:F4}"; }
        }
    }
}
