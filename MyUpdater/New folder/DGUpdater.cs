using System;
using System.IO;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using System.Windows.Forms;
using AutoUpdaterDotNET;

namespace DGUpdater
{
    public partial class DGUpdater : Form
    {
        public DGUpdater()
        {
            var version = File.ReadAllLines("version")[0];
            // MessageBox.Show(version);
            AutoUpdater.AppTitle = "DG|DarkRust ";
            AutoUpdater.ExecutablePath = "DGLauncher.exe";
            AutoUpdater.RunUpdateAsAdmin = true;
            AutoUpdater.ShowSkipButton = false;
            AutoUpdater.InstalledVersion = new Version(version);

            if (!File.Exists("DGLauncher.exe") || !File.Exists("AntiCheat.exe") || !File.Exists("version"))
            {
                Process.GetCurrentProcess().Kill();
            }

            foreach (var process in Process.GetProcesses())
            {
                if (process.ProcessName == "DGLauncher" || process.ProcessName == "Rust207")
                {
                    process.Kill();
                }
            }

            AutoUpdater.Start($"http://5.42.223.21/newupdate/Update207.xml");
            InitializeComponent();
        }
    }
}