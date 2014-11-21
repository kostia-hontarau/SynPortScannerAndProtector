using System;
using System.Collections.Generic;
using System.Linq;
using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;

namespace lab2
{
    internal static class AddressExtension
    {
        public static string GetIP(this DeviceAddress address)
        {
            if (address == null) return IpV4Address.Zero.ToString();

            if (address.Address.Family == SocketAddressFamily.Internet)
            {
                string asString = address.Address.ToString();
                int spacePos = asString.IndexOf(" ", StringComparison.Ordinal);
                asString = asString.Substring(spacePos, asString.Length - spacePos);
                return asString;
            }
            return null;
        }
    }
}
