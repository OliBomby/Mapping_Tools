using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Mapping_Tools.Views {
    public static class Extensions {
        public static double GetDouble(this TextBox textBox) {
            try {
                DataTable dt = new DataTable();
                string text = textBox.Text.Replace(",", ".");
                var v = dt.Compute(text, "");
                return Convert.ToDouble(v);
            } catch {
                return 0;
            } 
        }
    }
}
