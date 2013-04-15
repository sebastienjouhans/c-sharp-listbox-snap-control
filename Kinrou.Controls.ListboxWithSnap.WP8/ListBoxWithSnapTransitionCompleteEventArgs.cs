using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinrou.Controls.Events
{
    /// <summary>
    /// </summary>
    public class ListBoxWithSnapTransitionCompleteEventArgs : EventArgs
    {
        #region fields

        public int index { get; internal set; }

        #endregion

        #region constructor

        public ListBoxWithSnapTransitionCompleteEventArgs(int index)
        {
            this.index = index;
        }

        #endregion

    }
}