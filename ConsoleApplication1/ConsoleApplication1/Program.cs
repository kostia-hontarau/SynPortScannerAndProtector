using System;
using System.Collections.Generic;
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
            options = new ScanningOptions(LivePacketDevice.AllLocalMachine[2]);
            SynPortScanner scanner = new SynPortScanner();

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("1 - Setup scanning options;");
                Console.WriteLine(scanner.CanScan ? 
                    "2 - Start scanning;" : 
                    "2 - Scan results (Open ports);");
                Console.WriteLine("3 - Exit.");
                int command = GetIntFromConsole();
                
                switch (command)
                {
                    case 1:
                        Console.Write("IP address:");
                        options.TargetIP = GetIPFromConsole();
                        Console.Write("From port:");
                        options.StartPort = GetIntFromConsole(x => x > 0);
                        Console.Write("To port:");
                        options.EndPort = GetIntFromConsole(x => x > 0);
                        Console.WriteLine("Press Enter to continue...");
                        break;
                    case 2:
                        try
                        {
                            if (scanner.CanScan)
                            {
                                Console.WriteLine("Scanning...");
                                scanner.Scan(options);
                            }
                            else
                            {
                                List<PortInfo> openPorts = scanner.ScanResults.Where(result => result.IsOpen).ToList();
                                Console.WriteLine(openPorts.Count > 0 ? "Open ports:" : "There are no open ports in results!");
                                foreach (PortInfo info in openPorts)
                                {
                                    Console.WriteLine(info.ToString());
                                }
                            }
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

        private static int GetIntFromConsole()
        {
            int number = 0;
            while (true)
            {
                string answer = Console.ReadLine();
                bool success = int.TryParse(answer, out number);
                if (!success) Console.WriteLine("You must enter a number!");
                else break;
            }
            return number;
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
            IPAddress address;
            while (true)
            {
                string input = Console.ReadLine();
                bool success = IPAddress.TryParse(input, out address);
                if (!success) Console.WriteLine("You must enter an IP address!");
                else break;
            }
            return new IpV4Address(address.ToString());
        }
    }
}

