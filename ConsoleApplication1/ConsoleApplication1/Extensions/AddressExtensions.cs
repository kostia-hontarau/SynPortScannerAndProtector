using System;
using System.Collections.Generic;
using System.Linq;

using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Packets.Ethernet;
using PcapDotNet.Packets.IpV4;


namespace ConsoleApplication1.Extensions
{
    internal static class AddressExtension
    {
        public static List<byte> GetBytesList(this MacAddress address)
        {
            UInt48 value = address.ToValue();
            byte[] array = BitConverter.GetBytes(value).Take(6).ToArray();
            return array.Reverse().ToList();
        }
        public static List<byte> GetBytesList(this IpV4Address address)
        {
            uint value = address.ToValue();
            byte[] array = BitConverter.GetBytes(value).Take(6).ToArray();
            return array.Reverse().ToList();
        }
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
        public static IpV4Address GetMask(this DeviceAddress address)
        {
            if (address != null && address.Address.Family == SocketAddressFamily.Internet)
            {
                string asString = address.Netmask.ToString();
                int spacePos = asString.IndexOf(" ", StringComparison.Ordinal);
                asString = asString.Substring(spacePos, asString.Length - spacePos);
                return new IpV4Address(asString);
            }
            return IpV4Address.Zero; ;
        }
    }
}
