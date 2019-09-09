using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace Mapping_Tools.Classes.SnappingTools
{
    interface IOverlayElement
    {
        bool highlighted { get; }
        void Highlight();
        void UnHighlight();
    }
}
