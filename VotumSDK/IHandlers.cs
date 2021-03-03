using System;
using System.Collections.Generic;
using System.Text;

namespace Votum
{
    interface IHandlers
    {
        event EventHandler ReceivedSignal;
        void GetPaket();
    }
}
