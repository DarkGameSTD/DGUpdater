using System;
using System.Windows.Forms;

namespace DGUpdaterTest
{
    internal static class Program
    {
        /// <summary>
        ///     Nothing You Bitch!
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DGUpdaterTest());
        }
    }
}