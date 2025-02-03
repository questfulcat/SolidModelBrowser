using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolidModelBrowser
{
    public partial class MainWindow
    {
        string help = $@"Solid Model Browser v {version}

F1 - Show/hide this help
F2 - Diffuse material on/off
F3 - Specular material on/off
F4 - Emissive material on/off
F5 - Reload file, reset camera
F6 - Save image, snapshot to PNG
F7 - Open current file in external application (if defined)
F8 - Switch camera Perspective/Orthographic modes
C - Turn camera at model center
A, D - Rotate model around Z axis
S, W - Rotate model around X axis
E, Q - Rotate model around Y axis
G, R - Set Camera at model frontside/backside view
T, B - Set Camera at model top/bottom view
F, H - Set Camera at model left/right view
O - Show/hide XYZ axes
P - Show/hide ground polygon
CTRL+F - Recreate smooth mesh in flat mode (flat polygon lighting)
CTRL+W - Wireframe mode
CTRL+S - Export current model
I - show/hide model info
LeftShift - hold left shift to drag window by viewport

Grigoriy E. (questfulcat) 2025 (C)";
    }
}
