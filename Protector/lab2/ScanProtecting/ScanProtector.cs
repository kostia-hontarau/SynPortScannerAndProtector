using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading;
using System.Threading.Tasks;

using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;

using lab2.ScanProtecting.ScanReactors;


namespace lab2.ScanProtecting
{
    internal sealed class ScanProtector
    {
        #region Data Members
        private readonly Dictionary<IpV4Address, byte> attempts = new Dictionary<IpV4Address, byte>();
        private readonly ProtectionOptions options;

        private bool isRunning;
        private CancellationTokenSource cts;
        #endregion

        #region Properties
        public bool IsRunning
        {
            get { return this.isRunning; }
        }
        #endregion

        #region Constructors
        public ScanProtector(ProtectionOptions options)
        {
            this.isRunning = false;
            this.options = options;
        }
        #endregion
        
        #region Members
        public void Start()
        {
            if (this.isRunning)
                throw new InvalidOperationException("Protection is already enabled!");

            this.isRunning = true;
            this.cts = new CancellationTokenSource();
            this.cts.Token.Register(this.ProtectionStopped);
            Console.WriteLine("Protection activated!");
            Task.Run(() => this.ReceivePackets(this.cts.Token), this.cts.Token);
        }
        public void Stop()
        {
            this.cts.Cancel();
        }
        #endregion

        #region Assistants
        private void ReceivePackets(CancellationToken ct)
        {
            using (PacketCommunicator communicator = this.options.Device.Open(65535, PacketDeviceOpenAttributes.Promiscuous | PacketDeviceOpenAttributes.NoCaptureLocal, 100))
            {
                communicator.SetFilter("tcp and not src net " + this.options.LocalIP.ToString());

                while (true)
                {
                    if (ct.IsCancellationRequested) return;

                    Packet responce;
                    PacketCommunicatorReceiveResult result = communicator.ReceivePacket(out responce);

                    if (result == PacketCommunicatorReceiveResult.Ok)
                    {
                        this.ProceedResponce(responce);
                    }
                }
            }
        }
        private void ProceedResponce(Packet responce)
        {
            IpV4Datagram ipDatagram = responce.Ethernet.IpV4;
            TcpDatagram tcpDatagram = ipDatagram.Tcp;

            TcpControlBits bits = tcpDatagram.ControlBits;
            bool isSyn = bits.HasFlag(TcpControlBits.Synchronize) &&
                         !bits.HasFlag(TcpControlBits.Acknowledgment);

            if (!isSyn) return;
            if (this.attempts.ContainsKey(ipDatagram.Source)) this.attempts[ipDatagram.Source]++;
            else this.attempts.Add(ipDatagram.Source, 1);
            Console.WriteLine("{0}:{1} is trying to connect {2}:{3}.", 
                ipDatagram.Source, 
                tcpDatagram.SourcePort,
                ipDatagram.Destination, 
                tcpDatagram.DestinationPort
                );

            this.CheckConnections();
        }
        private void CheckConnections()
        {
            List<IpV4Address> scanners = this.attempts
                .Where(pair => pair.Value > this.options.MaxConnectionsFromIP)
                .Select(pair => pair.Key)
                .ToList();

            if (scanners.Count > 0)
            {
                foreach (IpV4Address address in scanners)
                {
                    this.ReactOnScanner(address);
                }
            }
        }
        private void ReactOnScanner(IpV4Address address)
        {
            try
            {
                this.options.Reacting.React(address);
            }
            catch (InvalidOperationException exception)
            {
                Console.WriteLine(exception.Message);
                Console.WriteLine("Now and then all scanners will be ignored...");
                this.options.Reacting = new IgnoreScanningReaction();
            }
        }

        private void ProtectionStopped()
        {
            this.cts.Dispose();
            this.isRunning = false;
            Console.WriteLine("Protection stopped");
        }
        #endregion
    }
}
