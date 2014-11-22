using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Ethernet;

using ConsoleApplication1.Model.PacketFactories;


namespace ConsoleApplication1.Model
{
    internal sealed class ARPMacResolver
    {
        #region Members
        public void ResolveDestinationMacFor(ScanningOptions options)
        {
            this.ResolveDestinationMacFor(options, CancellationToken.None);
        }
        public void ResolveDestinationMacFor(ScanningOptions options, CancellationToken ct)
        {
            using (PacketCommunicator communicator = options.Device.Open(65535, PacketDeviceOpenAttributes.None, 100))
            {
                Packet request = ArpPacketFactory.CreateRequestFor(options);
                communicator.SetFilter("arp and src " + options.TargetIP + " and dst " + options.SourceIP);
                communicator.SendPacket(request);

                while (true)
                {
                    if (ct.IsCancellationRequested) return;

                    Packet responce;
                    PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out responce);
                    switch (result)
                    {
                        case PacketCommunicatorReceiveResult.Timeout:
                            communicator.SendPacket(request);
                            continue;
                        case PacketCommunicatorReceiveResult.Ok:
                            options.TargetMac = ParseSenderMacFrom(responce);
                            return;
                    }
                }
            }
        }
        #endregion

        #region Assistants
        private MacAddress ParseSenderMacFrom(Packet responce)
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
