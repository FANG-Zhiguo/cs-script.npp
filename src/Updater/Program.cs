﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;

namespace CSScriptNpp.Deployment
{
    class Program
    {
        const string elevationArg = "/elevated";

        static void Main(string[] args)
        {
            try
            {
                if (IsAdmin())
                {
                    if (args.Contains(elevationArg))
                        args = args.Where(a => a != elevationArg).ToArray();

                    Debug.Assert(args.Length == 2);

                    if (EnsureNppNotRunning())
                    {
                        string zipFile = args[0];
                        string pluginDir = args[1];
                        string nppExe = Path.Combine(pluginDir, @"..\\notepad++.exe");
                        Updater.Deploy(zipFile, pluginDir);
                        if (File.Exists(nppExe))
                            Process.Start(nppExe);
                        else
                            MessageBox.Show("The update has been successfully installed.", "CS-Script Update");
                    }
                }
                else
                {
                    if (!args.Contains(elevationArg)) //has not been attempted to elevate yet
                        RestartElevated(args);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Update has not succeeded.\nError: " + e, "CS-Script Update");
            }
        }

        static bool IsAdmin()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            WindowsPrincipal p = new WindowsPrincipal(id);
            return p.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static bool RestartElevated(string[] arguments)
        {
            string args = elevationArg;
            for (int i = 0; i < arguments.Length; i++)
                args += " \"" + arguments[i] + "\"";

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = Assembly.GetExecutingAssembly().Location;
            startInfo.Arguments = args;

            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
                startInfo.Verb = "runas";

            Process.Start(startInfo);
            return true;
        }

        static bool EnsureNppNotRunning()
        {
            int count = 0;
            while (Process.GetProcessesByName("notepad++").Any())
            {
                count++;
                
                var buttons = MessageBoxButtons.OKCancel;
                var prompt = "Please close any running instance of Notepad++ and press OK to proceed.";

                if (count > 1)
                {
                    prompt = "Please close any running instance of Notepad++ and try again.";
                    buttons = MessageBoxButtons.RetryCancel;
                }
                
                if (MessageBox.Show(prompt, "CS-Script Update", buttons) == DialogResult.Cancel)
                    return false;
            }
            return true;
        }
    }
}