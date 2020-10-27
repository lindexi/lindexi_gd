using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace incorporates
{
   public static class instrument
    {
        public static Random ran
        {
            set
            {
                _ran = value;
            }
            get
            {
                return _ran;
            }
        }        
        public static bool stochastic(double per)
        {
            return ran.Next() % 10000 < per * 10000;
        }
        public static double region(double a , double b , double value)
        {
            if (value > a)
            {
                return a;
            }
            else if (value < b)
            {
                return b;
            }
            else
            {
                return value;
            }
        }
        private static Random _ran = new Random();        
    }
}
