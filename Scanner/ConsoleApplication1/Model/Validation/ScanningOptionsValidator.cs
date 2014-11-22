using System.Collections.Generic;
using System.Linq;
using PcapDotNet.Core;
using PcapDotNet.Packets.IpV4;

using ConsoleApplication1.Extensions;


namespace ConsoleApplication1.Model.Validation
{
    class ScanningOptionsValidator : IValidator<ScanningOptions>
    {
        #region Members
        public bool Validate(ScanningOptions obj)
        {
            bool isLocalHost = this.IsTargetLocalHost(obj);
            bool isInSubnetwork = this.IsInSubnetwork(obj);
            bool isZero = obj.TargetIP == IpV4Address.Zero;
            return !isLocalHost && !isZero && isInSubnetwork;
        }
        #endregion

        #region Assistants
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
        #endregion
    }
}
