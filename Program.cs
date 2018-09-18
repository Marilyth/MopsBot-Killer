﻿using System;
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

            openFileChecker = new System.Threading.Timer(checkOpenFiles, null, 0, 60000);

            await Task.Delay(-1);
        }

        private void checkOpenFiles(object stateinfo)
        {
            try
            {
                var MopsBot = System.Diagnostics.Process.GetProcessesByName("dotnet").Where(x => x.Id != ProcessId && x.HandleCount > 140).First();
                Console.WriteLine("MopsBot: " + $"{MopsBot.ProcessName}: {MopsBot.Id}, handles: {MopsBot.HandleCount}");

                /*using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo.FileName = "/bin/bash";
                    process.StartInfo.Arguments = $"-c \"ls -lisa /proc/{MopsBot.Id}/fd | wc -l\"";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    process.WaitForExit();

                    string result = process.StandardOutput.ReadToEnd();
                    int openFiles = Convert.ToInt32(result);
                    Console.WriteLine("\n" + System.DateTime.Now + $" open files were {openFiles}");

                    if (OpenFilesCount == openFiles)
                    {
                        if (++OpenFilesRepetition == OpenFilesRepetitionThreshold)
                        {
                            Console.WriteLine("\nShutting down due to 5 repetitions!");
                            Environment.Exit(-1);
                        }
                    }

                    else
                        OpenFilesRepetition = 0;


                    if (OpenFilesCount > 600)
                    {
                        Console.WriteLine("\nShutting down due to too many open files!");
                        Environment.Exit(-1);
                    }

                    OpenFilesCount = openFiles;
                }*/



            }
            catch (Exception e)
            {
                Console.WriteLine("\n[FILE READING ERROR]: " + System.DateTime.Now + $" {e.Message}\n{e.StackTrace}");
                //Environment.Exit(-1);
            }
        }
    }
}
