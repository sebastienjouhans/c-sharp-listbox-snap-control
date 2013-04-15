using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinrou.Controls.Events
{
    /// <summary>
    /// </summary>
    public class ListBoxWithSnapSelectedItemChangedEventArgs : EventArgs
    {
        #region fields

        public int index { get; internal set; }

        #endregion

        #region constructor

        public ListBoxWithSnapSelectedItemChangedEventArgs(int index)
        {
            this.index = index;
        }

        #endregion

    }
}