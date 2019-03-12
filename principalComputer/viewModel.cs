using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace principalComputer
{
    public class viewModel:notify_property
    {
        public viewModel()
        {
            //_principal_computer = new principal_computer(str=>
            //{
            //    reminder = str;
            //});

            ReceiveAction = str =>
            {
                reminder = str.Trim('\0' , ' ');
            };
        }
        public void principal()
        {
             _principal_computer = new principal_Computer(ReceiveAction);

            //_principal_computer.principal();
        }

        public void slave()
        {
            _slaveComputer = new slaveComputer(ReceiveAction);
            _slaveComputer.access("10.21.71.130");

            //_principal_computer.slave();
        }

        public void send()
        {
            if (_slaveComputer != null)
            {
                reminder = "send " + text;
                _slaveComputer.send(text);
            }
            else if (_principal_computer != null)
            {
                reminder = "send " + text;
                _principal_computer.send(text);
            }
            else
            {
                reminder = "没有连接";
            }
        }

        public string text
        {
            set
            {
                _text = value;
            }
            get
            {
                return _text;
            }
        }
        private System.Action<string> ReceiveAction;
        private string _text;
        private principal_Computer _principal_computer;
        private slaveComputer _slaveComputer;

    }
}
