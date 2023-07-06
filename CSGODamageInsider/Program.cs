using System;
using System.IO;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace CSGODamageInsider
{
    class Program
    {
        private static List<string> textLines = new List<string>();
        private static HashSet<string> sentMessages = new HashSet<string>();

        private static string steam = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam");
        private static string logfile = Path.Combine(steam, @"steamapps\common\Counter-Strike Global Offensive\csgo\console.log");

        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-");
            Console.WriteLine("****************** IMPORTANT NOTICE ******************");
            Console.WriteLine("=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-");
            Console.WriteLine("                                                    ");
            Console.ResetColor();

            Console.WriteLine("Before starting, set CS:GO launch options as follows:");
            Console.WriteLine("'-condebug -netconport 2121'");
            Console.WriteLine("This is required for the software to work properly.");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Press any key to continue...");
            Console.ResetColor();

            Console.ReadKey();

            if (!File.Exists(logfile))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Log file not found! Check correctness of the specified path!");

                Console.ReadKey();
                Environment.Exit(0);
            }

            using (FileStream fileStream = new FileStream(logfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader streamReader = new StreamReader(fileStream))
            using (TcpClient client = new TcpClient())
            {
                fileStream.Seek(0, SeekOrigin.End);

                try
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Connecting to the server...");
                    Console.ResetColor();

                    client.Connect("127.0.0.1", 2121);
                }
                catch (SocketException)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Failed to connect to the server!");
                    Console.ResetColor();

                    Console.ReadKey();
                    Environment.Exit(0);
                }

                using (StreamWriter streamWriter = new StreamWriter(client.GetStream()))
                {
                    streamWriter.AutoFlush = true;

                    Console.OutputEncoding = Encoding.Unicode;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Log file tracking started ʕ ᵔᴥᵔ ʔ");
                    Console.ResetColor();

                    Stopwatch resetTimer = new Stopwatch();
                    resetTimer.Start();

                    while (true)
                    {
                        string line = streamReader.ReadLine();

                        if (line != null)
                        {
                            textLines.Add(line);

                            if (textLines.Count > 20)
                            {
                                textLines.RemoveAt(0);
                            }
                        }
                        else
                        {
                            if (textLines.Count > 0)
                            {
                                AnalyzeLogLines(textLines, streamWriter);
                                textLines.Clear();
                            }
                            Thread.Sleep(1000);
                        }

                        if (resetTimer.Elapsed >= TimeSpan.FromMinutes(5))
                        {
                            sentMessages.Clear();
                            resetTimer.Restart();
                        }
                    }
                }
            }
        }

        private static void AnalyzeLogLines(List<string> lines, StreamWriter writer)
        {
            Regex regex = new Regex(@"Damage\sGiven\sto\s""(.+?)""\s-\s(\d+)\sin\s(\d+)\shits?");

            foreach (string line in lines)
            {
                Match match = regex.Match(line);

                if (match.Success)
                {
                    string player = match.Groups[1].Value;
                    int damage = int.Parse(match.Groups[2].Value);
                    int hits = int.Parse(match.Groups[3].Value);

                    if (damage <= 99)
                    {
                        string message = $"Damage given to '{player}': {damage} in {hits} {(hits > 1 ? "hits" : "hit")}";

                        if (!sentMessages.Contains(message))
                        {
                            SendMessageToChat(writer, message);
                            sentMessages.Add(message);
                        }
                    }
                }
            }
        }

        private static void SendMessageToChat(StreamWriter writer, string message)
        {
            writer.WriteLine($"say_team \"{message}\"");
            Thread.Sleep(3000);
        }
    }
}