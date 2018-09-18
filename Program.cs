using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net;

namespace MopsKiller
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Task.Run(() => BuildWebHost(args).Run());
            new Program().Start().GetAwaiter().GetResult();
        }
        private System.Threading.Timer openFileChecker;

        //Open file handling
        private int ProcessId, OpenFilesCount, OpenFilesRepetition;
        private static int OpenFilesRepetitionThreshold = 5;

        private async Task Start()
        {
            ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;

            openFileChecker = new System.Threading.Timer(checkOpenFiles, null, 60000, 60000);

            await Task.Delay(-1);
        }

        private void checkOpenFiles(object stateinfo)
        {
            try
            {
                using (var MopsBot = System.Diagnostics.Process.GetProcessesByName("dotnet").Where(x => x.Id != ProcessId && x.HandleCount > 140).First())
                {
                    Console.WriteLine($"{System.DateTime.Now} MopsBot, {MopsBot.ProcessName}: {MopsBot.Id}, handles: {MopsBot.HandleCount}");

                    if (MopsBot.HandleCount > 600)
                    {
                        Console.WriteLine($"\nShutting down due to {MopsBot.HandleCount} open files!");
                        MopsBot.Kill();
                    }

                    else if (OpenFilesCount == MopsBot.HandleCount)
                    {
                        if (++OpenFilesRepetition == OpenFilesRepetitionThreshold)
                        {
                            Console.WriteLine("\nShutting down due to 5 repetitions!");
                            MopsBot.Kill();
                        }
                    }

                    else
                        OpenFilesRepetition = 0;

                    OpenFilesCount = MopsBot.HandleCount;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\n[FILE READING ERROR]: " + System.DateTime.Now + $" {e.Message}\n{e.StackTrace}");
                //Environment.Exit(-1);
            }
        }
    }
}
