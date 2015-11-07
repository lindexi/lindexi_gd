using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace incorporates
{
    public class passengerTerminal:ViewModel.notify_property
    {
        public passengerTerminal(property _property)
        {
            this._property = _property;
            ng_耐久 = new ng_耐久_durable(1000);

            t = new train();
            berth_停靠 = false;
        }
        public ng_耐久_durable ng_耐久
        {
            set
            {
                _耐久_durable = value;
            }
            get
            {
                return _耐久_durable;
            }
        }

        public bool berth_停靠
        {
            set
            {
                _停靠_berth = value;
            }
            get
            {
                return _停靠_berth;
            }
        }

        public double per_停靠率
        {
            set
            {
                double max = 1;
                double min = 0;
                _停靠率 = instrument.region(max,min,value);
            }
            get
            {
                return _停靠率;
            }
        }

        public double maintenanceCosts_维修费用
        {
            set
            {
                _维修费用_maintenanceCosts = value;
            }
            get
            {
                return _维修费用_maintenanceCosts;
            }
        }

        public double num_电量
        {
            set
            {
                _电量 = value;
            }
            get
            {
                return _电量;
            }
        }

        public train t
        {
            set
            {
                _train = value;
            }
            get
            {
                return _train;
            }

        }

        public train 停靠()
        {
            if (berth_停靠 && t.num_电量 <= 0)
            {
                _property.money += t.money_价格;
                berth_停靠 = false;
                reminder = "获得金钱：" + t.money_价格.ToString();
            }

            if (!berth_停靠)
            {
                berth_停靠 = true;
                t.newtrain();               
                return t;
            }
            else
            {
                return null;
            }
        }

        private ng_耐久_durable _耐久_durable;
        private bool _停靠_berth;
        private double _停靠率;
        private double _维修费用_maintenanceCosts;
        private double _电量;
        private property _property;
        private train _train;

    }
}
