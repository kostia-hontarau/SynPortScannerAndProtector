using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;

using ConsoleApplication1.Extensions;


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
        private ScanningOptions options;
        private CancellationTokenSource cts;
        #endregion

        #region Properties
        public PortInfo[] ScanResults
        {
            get { return this.scanResults.ToArray(); }
        }
        public bool IsBusy { get; private set; }
        #endregion

        #region Constructors
        public SynPortScanner()
        {
            this.scanResults = new SortedSet<PortInfo>();
            this.sourcePort = (ushort)(4123 + new Random().Next() % 1000);
            this.IsBusy = false;
        }
        #endregion

        #region Members
        public void Scan(ScanningOptions options)
        {
            if (this.IsBusy)
                throw new InvalidOperationException("The scanner is busy now.");

            bool valid = this.ValidateOptions(options);
            if (valid)
            {
                this.CorrectOptions(options);
                this.options = options;
            }
            else throw new ArgumentException("Options are incorrect!", "options");

            this.scanResults.Clear();
            this.IsBusy = true;
            Console.WriteLine("Scanning...");
            this.SetupTasks();
        }
        public void StopCurrentScanning()
        {
            this.cts.Cancel();
        }
        #endregion

        #region Assistant
        private bool ValidateOptions(ScanningOptions options)
        {
            bool isLocalHost = this.IsTargetLocalHost(options);
            bool isInSubnetwork = this.IsInSubnetwork(options);
            bool isZero = options.TargetIP == IpV4Address.Zero;
            return !isLocalHost && !isZero && isInSubnetwork;
        }
        private bool IsTargetLocalHost(ScanningOptions options)
        {
            foreach (LivePacketDevice device in LivePacketDevice.AllLocalMachine)
            {
                List<IpV4Address> addresses = device.Addresses
                    .Where(addr => addr.Address.Family == SocketAddressFamily.Internet)
                    .Select(addr => new IpV4Address(addr.GetIP()))
                    .ToList();
                if (addresses.Contains(options.TargetIP)) return true;
            }

            return options.TargetIP == new IpV4Address("127.0.0.1");
        }
        private bool IsInSubnetwork(ScanningOptions options)
        {
            List<DeviceAddress> addresses = options.Device.Addresses
                .Where(addr => addr.Address.Family == SocketAddressFamily.Internet)
                .ToList();
            if (addresses.Count > 0)
            {
                foreach (DeviceAddress address in addresses)
                {
                    IpV4Address mask = address.GetMask();
                    IpV4Address localIP = new IpV4Address(address.GetIP());
                    uint subnetwork = localIP.ToValue() & mask.ToValue();
                    uint targetSubnetwork = options.TargetIP.ToValue() & mask.ToValue();
                    if (targetSubnetwork == subnetwork) return true;
                }
            }
            return false;
        }

        private void CorrectOptions(ScanningOptions options)
        {
            if (options.StartPort > options.EndPort)
            {
                int buffer = options.StartPort;
                options.StartPort = options.EndPort;
                options.EndPort = buffer;
            }
        }
        
        private void SetupTasks()
        {
            this.cts = new CancellationTokenSource();
            this.cts.Token.Register(this.ScanningCanceled);
            Task macResolvingTask = new Task(this.ResolveMac, this.cts.Token, this.cts.Token);
            macResolvingTask.ContinueWith(
                task => this.SendPackets(this.cts.Token), 
                this.cts.Token,
                TaskContinuationOptions.OnlyOnRanToCompletion, 
                TaskScheduler.Default
                );
            macResolvingTask.Start();
            new Task(this.ReceivePackets, this.cts.Token, this.cts.Token).Start();
        }
        private void ScanningCanceled()
        {
            this.IsBusy = false;
            Console.WriteLine("Successfully canceled...");
        }

        private void ResolveMac(object arg)
        {
            CancellationToken ct = (CancellationToken)arg;
            ARPMacResolver resolver = new ARPMacResolver();
            resolver.ResolveDestinationMacFor(this.options, ct);
        }
        private void SendPackets(object arg)
        {
            CancellationToken ct = (CancellationToken)arg;

            using (PacketCommunicator communicator = options.Device.Open(65535, PacketDeviceOpenAttributes.Promiscuous | PacketDeviceOpenAttributes.NoCaptureLocal, 1000))
            {
                int portsAmount = this.options.EndPort - this.options.StartPort + 1;
                int buffersAmount = portsAmount / PacketsInOneTime;
                for (int i = 0; i <= buffersAmount; i++)
                {
                    ct.ThrowIfCancellationRequested();

                    //Рассчет диапазона портов для текущего буфера
                    ushort from = (ushort)(this.options.StartPort + i * PacketsInOneTime);
                    ushort to = 0;
                    if (i != buffersAmount) to = (ushort)(from + PacketsInOneTime);
                    else
                    {
                        int residue = portsAmount % PacketsInOneTime;
                        if (residue == 0) to = (ushort)(from + PacketsInOneTime);
                        else to = (ushort)(from + residue - 1);
                    }

                    SendPacketsToPorts(from, to, communicator);
                }
            }
        }
        private void SendPacketsToPorts(ushort from, ushort to, PacketCommunicator communicator)
        {
            using (PacketSendBuffer buffer = new PacketSendBuffer(PacketsBufferSize))
            {
                for (ushort port = from; port <= to; port++)
                {
                    buffer.Enqueue(this.CreateTcpSynPacket(port));
                }
                communicator.Transmit(buffer, true);
            }
        }

        private void ReceivePackets(object arg)
        {
            CancellationToken ct = (CancellationToken)arg;

            using (PacketCommunicator communicator = options.Device.Open(65535, PacketDeviceOpenAttributes.Promiscuous | PacketDeviceOpenAttributes.NoCaptureLocal, 100))
            {
                communicator.SetFilter("tcp and src " + options.TargetIP + " and dst " + options.SourceIP);

                while (true)
                {
                    ct.ThrowIfCancellationRequested();

                    Packet responce;
                    PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out responce);

                    if (result == PacketCommunicatorReceiveResult.Ok)
                    {
                        PortInfo info = ParsePortInfo(responce);
                        Packet rstPacket = CreateTcpRstPacket((ushort)info.Number);
                        communicator.SendPacket(rstPacket);

                        if (scanResults.Add(info)) Console.WriteLine(info.ToString());

                        int totalPorts = options.EndPort - options.StartPort;
                        if (scanResults.Count == totalPorts) break;
                    }
                }
            }
        }
        private PortInfo ParsePortInfo(Packet responce)
        {
            TcpDatagram datagram = responce.Ethernet.IpV4.Tcp;

            TcpControlBits bits = responce.Ethernet.IpV4.Tcp.ControlBits;
            bool isSynAck = bits.HasFlag(TcpControlBits.Acknowledgment) &&
                            bits.HasFlag(TcpControlBits.Synchronize);

            PortInfo info = new PortInfo(datagram.SourcePort, isSynAck);
            return info;
        }

        private Packet CreateTcpSynPacket(ushort targetPort)
        {
            EthernetLayer ethernetLayer = new EthernetLayer
            {
                Source = this.options.SourceMac,
                Destination = this.options.TargetMac,
            };
            IpV4Layer ipV4Layer = new IpV4Layer
            {
                Source = this.options.SourceIP,
                CurrentDestination = this.options.TargetIP,
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
        private Packet CreateTcpRstPacket(ushort targetPort)
        {
            EthernetLayer ethernetLayer = new EthernetLayer
            {
                Source = this.options.SourceMac,
                Destination = this.options.TargetMac,
            };
            IpV4Layer ipV4Layer = new IpV4Layer
            {
                Source = this.options.SourceIP,
                CurrentDestination = this.options.TargetIP,
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
