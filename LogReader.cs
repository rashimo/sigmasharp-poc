using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Threading;
using System.Management.Automation.Runspaces;
using System.Management.Automation;
using System.Diagnostics.Eventing.Reader;


namespace EDOCS
{
    class LogReader
    {


        public int ReadInterval = 5000;

        public int LogLimit = 5000;

        public string LogName = string.Empty;

        private bool Stop = true;

        private static string DebugPath = ".\\debug.log.txt";
        private Thread readerThread = null;
        private DateTime _lastReadTime = DateTime.UtcNow;
        private const string TimeFormatString = "yyyy-MM-ddTHH:mm:ss.fffffff00K";
        private const string EventReaderQuery = "*[System/TimeCreated/@SystemTime >='{0}']";

        private Sigma sigma;

        public LogReader(string logName)
        {
            LogName = logName;

        }
       
        public void StopReading() {
            if (readerThread != null && readerThread.IsAlive && !Stop) {
                Stop = true;
                readerThread = null;
            }
            LogWriter.WriteToFileThreadSafe("Reading stoped : " + LogName + "\n", DebugPath);
                  
        }
        public void StartReading() {
            if (Stop) {

                Stop = false;
                if(readerThread != null)
                {
                    readerThread = null;
                }
                readerThread = new Thread(ReadLogs);
                readerThread.Start();
                LogWriter.WriteToFileThreadSafe("Start reading: " + LogName + "\n", DebugPath);
            

            }
        }
        public void UsingPowerShell(EventLogRecord record)
        {
            if (this.sigma != null) {
                this.sigma = null;
            }
            this.sigma = new Sigma(record);
            this.sigma.InitBooleanMatrix();

            foreach (String key in this.sigma.sigmas.Keys)
            {
                
                var res = (IEnumerable<KeyValuePair<string, string>>)sigma.sigmas[key];

                if (res.Any()) {
                                       
                    LogWriter.WriteToFileThreadSafe("Rule matached: " + key + "\n", DebugPath);           
                    LogWriter.WriteToFileThreadSafe(record.FormatDescription() + "\n", DebugPath);
                    LogWriter.WriteToFileThreadSafe("----------------", DebugPath);
                }

            }

        }
        public void ReadLogs()
        {
            while (!Stop)
            {
                // 1. Calculate elapsed time since previous read.
                double elapsedTimeSincePreviousRead = (DateTime.UtcNow - _lastReadTime).TotalSeconds;
                DateTime timeSpanToReadEvents = DateTime.UtcNow.AddSeconds(-elapsedTimeSincePreviousRead);
                //string strTimeSpanToReadEvents = timeSpanToReadEvents.ToString(TimeFormatString, CultureInfo.InvariantCulture);
                string strTimeSpanToReadEvents = timeSpanToReadEvents.ToString("o");

                string query = string.Format(EventReaderQuery, strTimeSpanToReadEvents);
                int readEventCount = 0;

                // 2. Create event log query using elapsed time.
                // 3. Read the record using EventLogReader.
                EventLogQuery eventsQuery = new EventLogQuery(LogName, PathType.LogName, query) { ReverseDirection = true };
                EventLogReader logReader;
                try
                {
                    logReader = new EventLogReader(eventsQuery);
                }
                catch (System.Diagnostics.Eventing.Reader.EventLogNotFoundException ) {
                    LogWriter.WriteToFileThreadSafe("Log not found: " + LogName + "\n", DebugPath);
                    
                    
                    StopReading();
                    return;
                }
                catch (System.UnauthorizedAccessException)
                {
                    LogWriter.WriteToFileThreadSafe("Unauthorized for log: " + LogName + "\n", DebugPath);
                    

                    StopReading();
                    return;
                }
                // 4. Set lastReadTime to Date.Now
                _lastReadTime = DateTime.UtcNow;

                //Console.WriteLine(_lastReadTime.ToString());

                for (EventRecord eventdetail = logReader.ReadEvent(); eventdetail != null; eventdetail = logReader.ReadEvent())
                {

                    //EventLogRecord record = new EventLogRecord(eventdetail);
                    EventLogRecord logRecord = (EventLogRecord) eventdetail;
                    //Console.WriteLine(eventdetail.Id);
                    // if(eventdetail.Id==7045)
                    //    Console.WriteLine(eventdetail.FormatDescription());
                    UsingPowerShell(logRecord);
                    


                    // 6. Post only latest InternalLogLimit records, if result of event log query is more than InternalLogLimit.
                    if (++readEventCount >= LogLimit)
                    {
                        break;
                    }
                }
                Thread.Sleep(ReadInterval);
            }

        }



    }
}
