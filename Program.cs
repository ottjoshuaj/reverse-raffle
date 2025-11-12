using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Windows.Forms;
using Telerik.WinControls;

namespace ReverseRaffler
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Register and apply CrystalDark theme globally
            ThemeResolutionService.ApplicationThemeName = "CrystalDark";

            Application.Run(new frmConsole());
        }
    }
}
