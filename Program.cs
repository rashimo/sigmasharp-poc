using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EDOCS
{
    class Program
    {

        static LogReader sysmon;
        static LogReader security;
        static LogReader system;
        static LogReader powershell;

        static void Main(string[] args)
        {

            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);

           
            
            sysmon = new LogReader("Microsoft-Windows-Sysmon/Operational");
            sysmon.StartReading();

            system = new LogReader("System");
            system.StartReading();

            security = new LogReader("Security");
            security.StartReading();

            powershell = new LogReader("Microsoft-Windows-PowerShell/Operational");
            powershell.StartReading();

        }
        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            sysmon.StopReading();
            system.StopReading();
            security.StopReading();
           
        }
    }
}
