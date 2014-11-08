using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;

namespace ConsoleApplication1.Model
{
    internal sealed class SynPortScanner
    {
        #region Constants and ReadOnly Members
        private const ushort WindowSize = 8192;
        private const int PacketsInOneTime = 20;
        private const int PacketsBufferSize = 102400;

        private readonly SortedSet<PortInfo> scanResults;
        private readonly ushort sourcePort;
        #endregion

        #region Data Members
        private readonly Thread sendingThread;
        private readonly Thread captureThread;
        #endregion

        #region Properties
        public PortInfo[] ScanResults
        {
            get { return this.scanResults.ToArray(); }
        }
        #endregion

        #region Constructors
        public SynPortScanner()
        {
            this.scanResults = new SortedSet<PortInfo>();
            this.sourcePort = (ushort)(4123 + new Random().Next() % 1000);

            this.sendingThread = new Thread(this.SendPackets);
            this.captureThread = new Thread(this.ReceivePackets);
        }
        #endregion

        #region Members
        public void Scan(ScanningOptions options)
        {
            if (options.TargetIP == IpV4Address.Zero)
                throw new ArgumentException("Ошибка в настройках сканирования!", "options");

            this.scanResults.Clear();
            ARPMacResolver resolver = new ARPMacResolver();
            resolver.ResolveDestinationMacFor(options);

            if (options.StartPort > options.EndPort)
            {
                int buffer = options.StartPort;
                options.StartPort = options.EndPort;
                options.EndPort = buffer;
            }
            this.captureThread.Start(options);
            this.sendingThread.Start(options);
        }
        #endregion

        #region Assistant
        private void SendPackets(object argument)
        {
            ScanningOptions options = argument as ScanningOptions;
            if (options == null) return;

            using (PacketCommunicator communicator = options.Device.Open(65535, PacketDeviceOpenAttributes.Promiscuous | PacketDeviceOpenAttributes.NoCaptureLocal, 1000))
            {
                int portsAmount = options.EndPort - options.StartPort+1;
                int buffersAmount = portsAmount / PacketsInOneTime;
                for (int i = 0; i <= buffersAmount; i++)
                {
                    ushort from = (ushort)(options.StartPort + i * PacketsInOneTime);
                    ushort to = 0;
                    if (i != buffersAmount) to = (ushort)(from + PacketsInOneTime);
                    else
                    {
                        int residue = portsAmount % PacketsInOneTime;
                        if (residue == 0) to = (ushort)(from + PacketsInOneTime);
                        else to = (ushort)(from + residue-1);
                    }

                    SendPacketsToPorts(from, to, options, communicator);
                }
            }
        }
        private void SendPacketsToPorts(ushort from, ushort to, ScanningOptions options, PacketCommunicator communicator)
        {
            using (PacketSendBuffer buffer = new PacketSendBuffer(PacketsBufferSize))
            {
                for (ushort port = from; port <= to; port++)
                {
                    buffer.Enqueue(this.CreateTcpSynPacket(options, port));
                }
                communicator.Transmit(buffer, true);
            }
        }

        private void ReceivePackets(object argument)
        {
            ScanningOptions options = argument as ScanningOptions;
            if (options == null) return;

            using (PacketCommunicator communicator = options.Device.Open(65535, PacketDeviceOpenAttributes.Promiscuous | PacketDeviceOpenAttributes.NoCaptureLocal, 100))
            {
                communicator.SetFilter("tcp and src " + options.TargetIP + " and dst " + options.SourceIP);

                while (true)
                {
                    Packet responce;
                    PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out responce);

                    if (result == PacketCommunicatorReceiveResult.Ok)
                    {
                        TcpDatagram datagram = responce.Ethernet.IpV4.Tcp;

                        TcpControlBits bits = responce.Ethernet.IpV4.Tcp.ControlBits;
                        bool isSynAck = bits.HasFlag(TcpControlBits.Acknowledgment) &&
                                        bits.HasFlag(TcpControlBits.Synchronize);

                        Packet rstPacket = CreateTcpRstPacket(options, datagram.SourcePort);
                        communicator.SendPacket(rstPacket);

                        PortInfo info = new PortInfo(datagram.SourcePort, isSynAck);
                        scanResults.Add(info);
                        Console.WriteLine("Порт: {0}; Состояние: {1};", info.Number, info.IsOpen ? "Открыт" : "Закрыт");
                        if (scanResults.Count == options.EndPort - options.StartPort) break;
                    }
                }
            }
        }
        private Packet CreateTcpSynPacket(ScanningOptions options, ushort targetPort)
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
                Ttl = 128,
                Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
            };
            TcpLayer tcpLayer = new TcpLayer
            {
                SourcePort = sourcePort,
                DestinationPort = targetPort,
                ControlBits = TcpControlBits.Synchronize,
                Window = WindowSize,
            };
            return PacketBuilder.Build(DateTime.Now, ethernetLayer, ipV4Layer, tcpLayer);
        }
        private Packet CreateTcpRstPacket(ScanningOptions options, ushort targetPort)
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
                Ttl = 128,
                Fragmentation = new IpV4Fragmentation(IpV4FragmentationOptions.DoNotFragment, 0),
                Identification = (ushort)new Random().Next()
            };
            TcpLayer tcpLayer = new TcpLayer
            {
                SourcePort = sourcePort,
                DestinationPort = targetPort,
                SequenceNumber = (uint)new Random().Next(),
                ControlBits = TcpControlBits.Reset,
                Window = WindowSize,
            };
            return PacketBuilder.Build(DateTime.Now, ethernetLayer, ipV4Layer, tcpLayer);
        }
        #endregion
    }
}
