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
        private int ProcessId, OpenFilesCount, RAMRepetition, LimitExceededCount;
        private long RAM;
        private static int REPETITIONTHRESHOLD = 20, OPENFILESLIMIT = 1000, OPENSOCKETSLIMIT = 4, COUNTDOWN = 6;
        private static DatePlot plot;

        private async Task Start()
        {
            ProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            
            plot = new DatePlot("MopsKiller", relativeTime: false, multipleLines: true);
            openFileChecker = new System.Threading.Timer(checkOpenFiles, null, 30000, 30000);

            await Task.Delay(-1);
        }

        private void checkOpenFiles(object stateinfo)
        {
            try
            {
                using (var MopsBot = System.Diagnostics.Process.GetProcessesByName("dotnet").Where(x => x.Id != ProcessId && x.HandleCount > 140).OrderByDescending(x => x.WorkingSet64).First())
                {
                    int openSockets = GetCloseWaitSockets();
                    long ram = (MopsBot.WorkingSet64/1024)/1024;
                    Console.WriteLine($"{System.DateTime.Now} MopsBot, {MopsBot.ProcessName}: {MopsBot.Id}, handles: {MopsBot.HandleCount}, waiting-sockets: {openSockets}, threads: {MopsBot.Threads.Count}, RAM: {ram}, Runtime: {(DateTime.Now - MopsBot.StartTime).ToString(@"h\h\:m\m\:s\s")}, Heartbeat: {(DateTime.Now - GetLastHeartbeat()).ToString(@"m\m\:s\s")} ago");

                    //Reset plot if 1 day old
                    if(plot.PlotDataPoints.Count > 23040) plot = new DatePlot("MopsKiller", relativeTime: false, multipleLines: true);
                    plot.AddValueSeperate("RAM", (MopsBot.WorkingSet64/1024)/1024, relative: false);
                    plot.AddValueSeperate("Handles", MopsBot.HandleCount, relative: false);
                    plot.AddValueSeperate("Waiting-Sockets", openSockets, relative: false);
                    plot.AddValueSeperate("Threads", MopsBot.Threads.Count, relative: false);
                    plot.DrawPlot();

                    if (MopsBot.HandleCount >= OPENFILESLIMIT /*|| openSockets >= OPENSOCKETSLIMIT*/)
                    {
                        if(--COUNTDOWN == 0){
                            Console.WriteLine($"\nShutting down due to {MopsBot.HandleCount} open files / {openSockets} open sockets!");
                            MopsBot.Kill();
                            plot.AddValueSeperate("Mops-Killed", 0, relative: false);
                            plot.AddValueSeperate("Mops-Killed", MopsBot.HandleCount, relative: false);
                            plot.AddValueSeperate("Mops-Killed", 0, relative: false);
                        }
                    } else {
                        COUNTDOWN = 6;
                    }

                    /*if (RAM == ram)
                    {
                        if (++RAMRepetition >= REPETITIONTHRESHOLD)
                        {
                            Console.WriteLine("\nShutting down due to 10 repetitions!");
                            MopsBot.Kill();
                        }
                    }
                    else
                        RAMRepetition = 0;

                    if((DateTime.Now - GetLastLogEntry()) >= FREEZEDURATION && RAMRepetition >= 6){
                        Console.WriteLine("\nShutting down due >= 3m log freeze!");
                        MopsBot.Kill();
                    }*/

                    if((DateTime.Now - GetLastHeartbeat()) >= TimeSpan.FromMinutes(1.5) && (DateTime.Now - MopsBot.StartTime) >= TimeSpan.FromMinutes(1.5)){
                        Console.WriteLine("\nShutting down due to no heartbeat!");
                        MopsBot.Kill();
                    }

                    OpenFilesCount = MopsBot.HandleCount;
                    RAM = ram;

                    if(openSockets > 0){
                        //CloseCloseWaitSockets();
                    }
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

        private DateTime GetLastLogEntry(){
            return File.GetLastWriteTime("/usr/applications/MopsBot/mopsdata/log");
        }

        private DateTime GetLastHeartbeat(){
            using (var prc = new System.Diagnostics.Process())
            {
                prc.StartInfo.RedirectStandardOutput = true;
                prc.StartInfo.FileName = "/bin/bash";
                prc.StartInfo.Arguments = $"-c \"read_mopslog | grep -B 2 Heartbeat | grep Verbose | tail -n 1\"";

                prc.Start();

                string time = prc.StandardOutput.ReadToEnd();

                prc.WaitForExit();

                return DateTime.Parse(string.Join("", time.Skip(13)));
            }
        }

        private void CloseCloseWaitSockets(){
            using (var prc = new System.Diagnostics.Process())
            {
                prc.StartInfo.RedirectStandardOutput = true;
                prc.StartInfo.FileName = "/bin/bash";
                prc.StartInfo.Arguments = $"-c \"/usr/applications/kill-close-wait-connections/kill_close_wait_connections.pl\"";

                prc.Start();

                prc.WaitForExit();
            }       
        }
    }
}
