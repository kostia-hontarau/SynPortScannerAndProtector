using System;
using System.Linq;
using System.Net;

using PcapDotNet.Core;
using PcapDotNet.Packets.IpV4;

using ConsoleApplication1.Model;


namespace ConsoleApplication1
{
    class Program
    {
        private static ScanningOptions options;
        private static void Main(string[] args)
        {
            options = new ScanningOptions();
            SynPortScanner scanner = new SynPortScanner();

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("1 - Setup scanning options;");
                Console.WriteLine(!scanner.IsBusy ?
                    "2 - Start scanning;" :
                    "2 - Stop scanning;");
                Console.WriteLine("3 - Exit.");
                int command = GetIntFromConsole(x => x >=1 && x <= 3);

                switch (command)
                {
                    case 1:
                        PrintDevices();
                        int deviceNum = GetIntFromConsole(num => (num >= 1) && (num <= LivePacketDevice.AllLocalMachine.Count));
                        options.Device = LivePacketDevice.AllLocalMachine[deviceNum - 1];
                        Console.Write("IP address:");
                        options.TargetIP = GetIPFromConsole();
                        Console.Write("From port:");
                        options.StartPort = (ushort) GetIntFromConsole(x => x > 0 && x <= 65535);
                        Console.Write("To port:");
                        options.EndPort = (ushort) GetIntFromConsole(x => x > 0 && x <= 65535);
                        Console.WriteLine("Press Enter to continue...");
                        break;
                    case 2:
                        try
                        {
                            if (!scanner.IsBusy) scanner.Scan(options);
                            else scanner.StopCurrentScanning();
                        }
                        catch (ArgumentException exc)
                        {
                            Console.WriteLine(exc.Message);
                        }
                        break;
                    case 3:
                        exit = true;
                        Console.WriteLine("Bye-bye!");
                        break;
                    default:
                        Console.WriteLine("Wrong comand!");
                        break;
                }
                Console.ReadLine();
                Console.Clear();
            }
            Console.WriteLine("Press Enter to close the application...");
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
        private static IpV4Address GetIPFromConsole()
        {
            IPAddress address = IPAddress.None;
            while (true)
            {
                string input = Console.ReadLine();
                bool success = (input != null) && IPAddress.TryParse(input, out address);
                if (!success) Console.WriteLine("You must enter an IP address!");
                else break;
            }
            return new IpV4Address(address.ToString());
        }

        private static void PrintDevices()
        {
            int i = 1;
            foreach (LivePacketDevice device in LivePacketDevice.AllLocalMachine)
            {
                string addresses = device.Addresses
                    .Select(address => address.Address.ToString())
                    .Aggregate("", (s, s1) => s += s1 + ", ");
                if (String.IsNullOrWhiteSpace(addresses)) addresses = "No addresses on interface";
                Console.WriteLine("((({0}))) - {1}", i++, addresses);
            }
        }
    }
}

