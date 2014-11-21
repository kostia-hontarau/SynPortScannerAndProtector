using System;
using System.Linq;
using lab2.ScanProtecting;
using lab2.ScanProtecting.ScanReactors;
using PcapDotNet.Core;


namespace lab2
{
    class Program
    {
        private static void Main(string[] args)
        {
            ProtectionOptions options = SetupProtectionOptions();
            ScanProtector protector = new ScanProtector(options);
            protector.Start();
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        private static ProtectionOptions SetupProtectionOptions()
        {
            LivePacketDevice device = GetDevice();
            IScanningReaction reaction = GetScanningReaction();
            Console.WriteLine("Enter max connection tryings:");
            int maxConnections = GetIntFromConsole(num => num >= 1);
            ProtectionOptions options = new ProtectionOptions(device)
            {
                MaxConnectionsFromIP = maxConnections,
                Reacting = reaction,
            };

            return options;
        }
        private static LivePacketDevice GetDevice()
        {
            Console.WriteLine("Select device to protect...");
            int i = 1;
            foreach (LivePacketDevice device in LivePacketDevice.AllLocalMachine)
            {
                string addresses = device.Addresses
                    .Select(address => address.Address.ToString())
                    .Aggregate("", (s, s1) => s += s1 + ", ");
                if (String.IsNullOrWhiteSpace(addresses)) addresses = "No addresses on interface";
                Console.WriteLine("((({0}))) - {1}", i++, addresses);
            }
            int deviceNum = GetIntFromConsole(num => (num >= 1) && (num <= LivePacketDevice.AllLocalMachine.Count));
            return LivePacketDevice.AllLocalMachine[deviceNum - 1];
        }
        private static IScanningReaction GetScanningReaction()
        {
            Console.WriteLine("How must I react to any port scanning?");
            Console.WriteLine("1 - Inform about scanning;");
            Console.WriteLine("2 - Block scanning with the firewall;");
            Console.WriteLine("Other - Ignore scanning;");
            string input = Console.ReadLine();
            int number;
            int.TryParse(input, out number);
            switch (number)
            {
                case 1:
                    return new InformScanningReaction();
                case 2:
                    return new BlockIPScanningReaction();
                default:
                    return new IgnoreScanningReaction();
            }
        }

        private static int GetIntFromConsole(Func<int, bool> predicate)
        {
            int number = 0;
            while (true)
            {
                string answer = Console.ReadLine();
                bool success = int.TryParse(answer, out number);
                if (!success) Console.WriteLine("You must enter a number!");
                else if (!predicate(number)) Console.WriteLine("The number is not applicable for this context!");
                else break;
            }
            return number;
        }
    }
}
