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
        private int ProcessId, OpenFilesCount, OpenFilesRepetition, LimitExceededCount;
        private static int REPETITIONTHRESHOLD = 10, OPENFILESLIMIT = 550, OPENSOCKETSLIMIT = 4, COUNTDOWN = 3;

        private async Task Start()
        {
            ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;

            openFileChecker = new System.Threading.Timer(checkOpenFiles, null, 30000, 30000);

            await Task.Delay(-1);
        }

        private void checkOpenFiles(object stateinfo)
        {
            try
            {
                using (var MopsBot = System.Diagnostics.Process.GetProcessesByName("dotnet").Where(x => x.Id != ProcessId && x.HandleCount > 140).First())
                {
                    int openSockets = GetCloseWaitSockets();
                    Console.WriteLine($"{System.DateTime.Now} MopsBot, {MopsBot.ProcessName}: {MopsBot.Id}, handles: {MopsBot.HandleCount}, waiting-sockets: {openSockets}, threads: {MopsBot.Threads.Count}, RAM: {(MopsBot.WorkingSet64/1024)/1024}");

                    if (MopsBot.HandleCount >= OPENFILESLIMIT || openSockets >= OPENSOCKETSLIMIT)
                    {
                        if(--COUNTDOWN == 0){
                            Console.WriteLine($"\nShutting down due to {MopsBot.HandleCount} open files / {openSockets} open sockets!");
                            MopsBot.Kill();
                        }
                    } else {
                        COUNTDOWN = 3;
                    }

                    if (OpenFilesCount == MopsBot.HandleCount)
                    {
                        if (++OpenFilesRepetition >= REPETITIONTHRESHOLD)
                        {
                            Console.WriteLine("\nShutting down due to 10 repetitions!");
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

        private int GetCloseWaitSockets(){
            using (var prc = new System.Diagnostics.Process())
            {
                prc.StartInfo.RedirectStandardOutput = true;
                prc.StartInfo.FileName = "/bin/bash";
                prc.StartInfo.Arguments = $"-c \"netstat -peanut | grep dotnet | grep CLOSE | wc -l\"";

                prc.Start();

                int count = int.Parse(prc.StandardOutput.ReadToEnd());

                prc.WaitForExit();

                return count;
            }
        }
    }
}
