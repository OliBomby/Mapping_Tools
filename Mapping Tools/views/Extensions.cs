using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Mapping_Tools.Views {
    public static class Extensions {
        public static double GetDouble(this TextBox textBox, double defaultValue=-1) {
            try {
                DataTable dt = new DataTable();
                string text = textBox.Text.Replace(",", ".");
                var v = dt.Compute(text, "");
                return Convert.ToDouble(v);
            } catch {
                return defaultValue;
            } 
        }

        public static double GetInt(this TextBox textBox, int defaultValue = -1) {
            try {
                DataTable dt = new DataTable();
                string text = textBox.Text.Replace(",", ".");
                var v = dt.Compute(text, "");
                return Convert.ToInt32(v);
            } catch {
                return defaultValue;
            }
        }
    }
}
