using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Founds_Center
{
    public class CenterData
    {
        public int center { get; private set; }
        public string text { get; private set; }

        public CenterData(int centerT, string textT)
        {
            this.center = centerT;
            this.text = textT;
        }

    }
}
