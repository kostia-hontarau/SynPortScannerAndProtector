using PcapDotNet.Packets.IpV4;

namespace lab2.ScanProtecting.ScanReactors
{
    class IgnoreScanningReaction : IScanningReaction
    {
        public void React(IpV4Address scanner) { }
    }
}
