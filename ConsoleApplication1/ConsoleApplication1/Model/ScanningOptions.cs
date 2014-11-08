using System;
using System.Linq;
using ConsoleApplication1.Extensions;
using PcapDotNet.Core;
using PcapDotNet.Core.Extensions;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;

namespace ConsoleApplication1.Model
{
    internal sealed class ScanningOptions
    {
        #region Data Members
        private readonly LivePacketDevice device;
        private readonly MacAddress sourceMac;
        private readonly IpV4Address sourceIP;

        private ushort startPort;
        private ushort endPort;
        #endregion

        #region Properties
        public LivePacketDevice Device
        {
            get { return this.device; }
        }
        public IpV4Address SourceIP
        {
            get { return this.sourceIP; }
        }
        public MacAddress SourceMac
        {
            get { return this.sourceMac; }
        }
        public IpV4Address TargetIP { get; set; }
        public MacAddress TargetMac { get; set; }
        public int StartPort
        {
            get { return this.startPort; }
            set
            {
                if (0 < value && value <= 65535) this.startPort = (ushort) value;
                else throw new ArgumentException("The port number should be between 0 and 65535!", "value");
            }
        }
        public int EndPort
        {
            get { return this.endPort; }
            set
            {
                if (0 < value && value <= 65535) this.endPort = (ushort) value;
                else throw new ArgumentException("The port number should be between 0 and 65535!", "value");
            }
        }
        #endregion

        #region Constructors
        private ScanningOptions()
        {
            this.TargetIP = IpV4Address.Zero;
            this.TargetMac = MacAddress.Zero;
            this.StartPort = 1;
            this.EndPort = 1;
        }
        public ScanningOptions(LivePacketDevice device) : this()
        {
            this.device = device;
            DeviceAddress buffer = device.Addresses.FirstOrDefault(addr => addr.Address.Family == SocketAddressFamily.Internet);
            this.sourceMac = device.GetMacAddress();
            this.sourceIP = new IpV4Address(buffer.GetIP());
        }
        public ScanningOptions(LivePacketDevice device, IpV4Address targetIp, int startPort, int endPort)
            : this(device)
        {
            this.TargetMac = MacAddress.Zero;
            this.TargetIP = targetIp;
            this.StartPort = startPort;
            this.EndPort = endPort;
        } 
        #endregion
    }
}
