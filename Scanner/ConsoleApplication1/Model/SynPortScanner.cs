using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Transport;

using ConsoleApplication1.Model.PacketFactories;
using ConsoleApplication1.Model.Validation;


namespace ConsoleApplication1.Model
{
    internal sealed class SynPortScanner : IDisposable
    {
        #region ReadOnly Members
        private readonly SortedSet<PortInfo> scanResults;
        #endregion

        #region Data Members
        private IValidator<ScanningOptions> validator;
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
            this.validator = new ScanningOptionsValidator();
            this.IsBusy = false;
        }
        #endregion

        #region Members
        public void Scan(ScanningOptions options)
        {
            if (this.IsBusy)
                throw new InvalidOperationException("The scanner is busy now.");

            options.CorrectPorts();
            bool valid = this.validator.Validate(options);
            if (valid) this.options = options;
            else throw new ArgumentException("Options are incorrect!", "options");

            this.scanResults.Clear();
            Console.WriteLine("Scanning...");
            this.IsBusy = true;
            this.SetupAndRunTasks();
        }
        public void StopCurrentScanning()
        {
            this.cts.Cancel();
        }
        #endregion

        #region Assistant
        private void SetupAndRunTasks()
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
            macResolvingTask.ContinueWith(
                task => this.ReceivePackets(this.cts.Token),
                this.cts.Token,
                TaskContinuationOptions.OnlyOnRanToCompletion,
                TaskScheduler.Default
                );
            macResolvingTask.Start();
        }
        private void ScanningCanceled()
        {
            this.cts.Dispose();
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
            const int PacketsInOneTime = 20;
            CancellationToken ct = (CancellationToken)arg;

            using (PacketCommunicator communicator = options.Device.Open(65535, PacketDeviceOpenAttributes.Promiscuous | PacketDeviceOpenAttributes.NoCaptureLocal, 1000))
            {
                int portsAmount = this.options.EndPort - this.options.StartPort + 1;
                int buffersAmount = portsAmount / PacketsInOneTime;
                for (int i = 0; i <= buffersAmount; i++)
                {
                    if (ct.IsCancellationRequested) return;

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
            const int PacketsBufferSize = 102400;
            using (PacketSendBuffer buffer = new PacketSendBuffer(PacketsBufferSize))
            {
                for (ushort port = from; port <= to; port++)
                {
                    Packet packet = TcpPacketFactory.CreateSynPacketFor(this.options, port);
                    buffer.Enqueue(packet);
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
                    if (ct.IsCancellationRequested) return;

                    Packet responce;
                    PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out responce);

                    if (result == PacketCommunicatorReceiveResult.Ok)
                    {
                        PortInfo info = ParsePortInfo(responce);
                        Packet rstPacket = TcpPacketFactory.CreateRstPacketFor(this.options, info.Number);
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
        #endregion

        #region IDisposable Members
        public void Dispose()
        {
            this.cts.Dispose();
        } 
        #endregion
    }
}
