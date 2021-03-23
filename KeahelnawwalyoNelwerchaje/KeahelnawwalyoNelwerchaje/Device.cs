using System.Collections.Generic;

namespace KeahelnawwalyoNelwerchaje
{
    public class Device
    {
        public string MainIp { set; get; }

        public int DeviceCount { set; get; }

        public List<Node> NodeList { set; get; }
    }
}