using System;

using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;


namespace ConsoleApplication1.Model.PacketFactories
{
    internal static class TcpPacketFactory
    {
        #region Constants
        private const byte ttl = 128;
        private const ushort windowSize = 8192;
        #endregion

        #region Data Members
        private static readonly IpV4Fragmentation fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0);
        private static ushort sourcePort;
        private static ushort identification;
        private static uint sequence;
        #endregion

        #region Members
        public static Packet CreateSynPacketFor(ScanningOptions options, ushort targetPort)
        {
            EthernetLayer ethernetLayer = new EthernetLayer
            {
                Source = options.SourceMac,
                Destination = options.TargetMac,
            };
            IpV4Layer ipV4Layer = new IpV4Layer
            {
                Source = options.SourceIP,
                CurrentDestination = options.TargetIP,
                Ttl = TcpPacketFactory.ttl,
                Fragmentation = TcpPacketFactory.fragmentation,
            };
            TcpLayer tcpLayer = new TcpLayer
            {
                SourcePort = TcpPacketFactory.sourcePort,
                DestinationPort = targetPort,
                ControlBits = TcpControlBits.Synchronize,
                Window = TcpPacketFactory.windowSize,
            };
            return PacketBuilder.Build(DateTime.Now, ethernetLayer, ipV4Layer, tcpLayer);
        }
        public static Packet CreateRstPacketFor(ScanningOptions options, ushort targetPort)
        {
            TcpPacketFactory.RandomizeParameters();

            EthernetLayer ethernetLayer = new EthernetLayer
            {
                Source = options.SourceMac,
                Destination = options.TargetMac,
            };
            IpV4Layer ipV4Layer = new IpV4Layer
            {
                Source = options.SourceIP,
                CurrentDestination = options.TargetIP,
                Ttl = TcpPacketFactory.ttl,
                Fragmentation = TcpPacketFactory.fragmentation,
                Identification = TcpPacketFactory.identification
            };
            TcpLayer tcpLayer = new TcpLayer
            {
                SourcePort = sourcePort,
                DestinationPort = targetPort,
                SequenceNumber = TcpPacketFactory.sequence,
                ControlBits = TcpControlBits.Reset,
                Window = TcpPacketFactory.windowSize,
            };
            return PacketBuilder.Build(DateTime.Now, ethernetLayer, ipV4Layer, tcpLayer);
        }
        #endregion

        #region Assistants
        private static void RandomizeParameters()
        {
            Random random = new Random();
            sourcePort = (ushort)(4123 + random.Next() % 1000);
            identification = (ushort)random.Next();
            sequence = (uint)random.Next();
        }
        #endregion
    }
}
