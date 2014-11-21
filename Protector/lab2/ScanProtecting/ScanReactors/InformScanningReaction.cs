using System;

using PcapDotNet.Packets.IpV4;


namespace lab2.ScanProtecting.ScanReactors
{
    internal sealed class InformScanningReaction : IScanningReaction
    {
        public void React(IpV4Address scanner)
        {
            Console.WriteLine("{0} is trying to scan your ports!", scanner.ToString());
        }
    }
}
