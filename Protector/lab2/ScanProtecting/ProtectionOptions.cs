using System;
using System.Linq;

using PcapDotNet.Core;
using PcapDotNet.Packets.IpV4;

using lab2.ScanProtecting.ScanReactors;


namespace lab2.ScanProtecting
{
    internal sealed class ProtectionOptions
    {
        #region Data Members
        private readonly LivePacketDevice device;
        private readonly IpV4Address localIP;
        private int maxConnectionsFromIP;
        private IScanningReaction reacting;
        #endregion

        #region Properties
        public int MaxConnectionsFromIP
        {
            get { return this.maxConnectionsFromIP; }
            set
            {
                if (value >= 1) this.maxConnectionsFromIP = value;
                else throw new ArgumentException("At least 1 connection must be accepted", "value");
            }
        }
        public IpV4Address LocalIP
        {
            get { return this.localIP; }
        }
        public LivePacketDevice Device
        {
            get { return this.device; }
        }
        public IScanningReaction Reacting
        {
            get { return this.reacting; }
            set
            {
                if (value != null) this.reacting = value;
                else throw new ArgumentException("There must be some reaction on port scanning!");
            }
        }
        #endregion

        #region Constructors
        public ProtectionOptions(LivePacketDevice device)
        {
            this.device = device;
            DeviceAddress ip = device.Addresses.FirstOrDefault(address => address.Address.Family == SocketAddressFamily.Internet);
            this.localIP = ip != null ? new IpV4Address(ip.GetIP()) : IpV4Address.Zero;
            this.Reacting = new IgnoreScanningReaction();
        }
        #endregion
    }
}
