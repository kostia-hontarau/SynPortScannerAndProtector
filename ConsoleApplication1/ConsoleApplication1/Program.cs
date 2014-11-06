using System;
using System.Net;
using ConsoleApplication1.Model;
using PcapDotNet.Core;
using PcapDotNet.Packets.IpV4;


namespace ConsoleApplication1
{
    class Program
    {
        private static ScanningOptions options;
        private static void Main(string[] args)
        {
            options = new ScanningOptions(LivePacketDevice.AllLocalMachine[1]);
            SynPortScanner scanner = new SynPortScanner();

            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("1 - Настройка сканирования;");
                Console.WriteLine("2 - Сканировать согласно настройкам;");
                Console.WriteLine("3 - Выход.");
                int command = GetIntFromConsole();

                switch (command)
                {
                    case 1:
                        Console.Write("IP адрес:");
                        options.TargetIP = GetIPFromConsole();
                        Console.Write("Порты с:");
                        options.StartPort = GetIntFromConsole(x => x > 0);
                        Console.Write("Порты по:");
                        options.EndPort = GetIntFromConsole(x => x > 0);
                        break;
                    case 2:
                        try
                        {
                            scanner.Scan(options);
                        }
                        catch (ArgumentException exc)
                        {
                            Console.WriteLine(exc.Message);
                        }
                        break;
                    case 3:
                        Console.WriteLine("Пока!");
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Вы ошиблись цифрой!");
                        break;
                }
                Console.WriteLine("Нажмите Enter для продолжения...");
                Console.ReadLine();
                Console.Clear();
            }
        }

        private static int GetIntFromConsole()
        {
            int number = 0;
            while (true)
            {
                string answer = Console.ReadLine();
                bool success = int.TryParse(answer, out number);
                if (!success) Console.WriteLine("Вы ввели не число!");
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
                if (!success) Console.WriteLine("Вы ввели не число!");
                else if (!predicate(number)) Console.WriteLine("Число не подходит в данном контексте!");
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
                if (!success) Console.WriteLine("Вы ввели не IP!");
                else break;
            }
            return new IpV4Address(address.ToString());
        }
    }
}

