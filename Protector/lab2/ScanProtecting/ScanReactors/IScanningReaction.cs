using PcapDotNet.Packets.IpV4;


namespace lab2.ScanProtecting.ScanReactors
{
    internal interface IScanningReaction
    {
        void React(IpV4Address scanner);
    }
}
