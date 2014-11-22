using System;

using PcapDotNet.Packets;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Ethernet;

using ConsoleApplication1.Extensions;


namespace ConsoleApplication1.Model.PacketFactories
{
    internal static class ArpPacketFactory
    {
        #region Members
        public static Packet CreateRequestFor(ScanningOptions options)
        {
            EthernetLayer ethernetLayer = new EthernetLayer
            {
                Source = options.SourceMac,
                Destination = options.TargetMac,
                EtherType = EthernetType.Arp,
            };
            ArpLayer arpLayer = new ArpLayer
            {
                ProtocolType = EthernetType.IpV4,
                Operation = ArpOperation.Request,
                SenderHardwareAddress = options.SourceMac.GetBytesList().AsReadOnly(),
                SenderProtocolAddress = options.SourceIP.GetBytesList().AsReadOnly(),
                TargetHardwareAddress = options.TargetMac.GetBytesList().AsReadOnly(),
                TargetProtocolAddress = options.TargetIP.GetBytesList().AsReadOnly(),
            };
            PacketBuilder builder = new PacketBuilder(ethernetLayer, arpLayer);
            return builder.Build(DateTime.Now);
        }
        #endregion
    }
}
