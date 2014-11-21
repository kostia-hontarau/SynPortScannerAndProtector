using System;
using System.Collections.Generic;
using System.Linq;

using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Ethernet;

using ConsoleApplication1.Extensions;


namespace ConsoleApplication1.Model
{
    internal sealed class ARPMacResolver
    {
        #region Members
        public void ResolveDestinationMacFor(ScanningOptions options)
        {
            using (PacketCommunicator communicator = options.Device.Open(65535, PacketDeviceOpenAttributes.None, 100))
            {
                Packet request = this.BuildArpRequest(options);
                communicator.SetFilter("arp and src " + options.TargetIP + " and dst " + options.SourceIP);
                communicator.SendPacket(request);

                while (true)
                {
                    Packet responce;
                    PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out responce);
                    switch (result)
                    {
                        case PacketCommunicatorReceiveResult.Timeout:
                            communicator.SendPacket(request);
                            continue;
                        case PacketCommunicatorReceiveResult.Ok:
                            options.TargetMac = GetSourceMacFrom(responce);
                            return;
                    }
                }
            }
        }
        #endregion

        #region Assistants
        private Packet BuildArpRequest(ScanningOptions options)
        {
            EthernetLayer ethernetLayer =
                new EthernetLayer
                {
                    Source = options.SourceMac,
                    Destination = options.TargetMac,
                    EtherType = EthernetType.Arp,
                };
            ArpLayer arpLayer =
                new ArpLayer
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
        private MacAddress GetSourceMacFrom(Packet responce)
        {
            ArpDatagram datagram = responce.Ethernet.Arp;
            List<byte> targetMac = datagram.SenderHardwareAddress.Reverse().ToList();
            targetMac.AddRange(new byte[] { 0, 0 });
            Int64 value = BitConverter.ToInt64(targetMac.ToArray(), 0);
            return new MacAddress((UInt48)value);
        }
        #endregion
    }
}
