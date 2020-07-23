using System;
using System.IO;
using System.Diagnostics;
using System.Text;

namespace Logs
{
    /// <remarks>
    /// Abstract class to dictate the format for the logs that our logger will use.
    /// </remarks>
    public abstract class Log
    {
        /// <value>Available message severities</value>
        public enum MessageType
        {
            Informacion = 1,
            Falla = 2,
            Warning = 3,
            Error = 4,
            Debug = 5
        }

        public abstract void RecordMessage(Exception Message, MessageType Severity);

        public abstract void RecordMessage(string Message, MessageType Severity);
    }


    /// Log messages to a file location.

    public class FileLog : Log
    {
        // Internal log file name value
        private string _FileName = "";

        public string FileName
        {
            get { return this._FileName; }
            set { this._FileName = value; }
        }

        /// Constructor
        public FileLog()
        {
            string path = Directory.GetCurrentDirectory();
            this.FileName = path + "\\VID_FactRemota.log";
            if (FactRemota.globals.gpLogFile != "")
            {
                this.FileName = FactRemota.globals.gpLogFile;
            }
        }

        /// Log an exception.
        public override void RecordMessage(Exception Message, Log.MessageType Severity)
        {
            this.RecordMessage(Message.Message, Severity);
        }

        /// Log a message.
        public override void RecordMessage(string Message, Log.MessageType Severity)
        {
            FileStream fileStream = null;
            StreamWriter writer = null;
            StringBuilder message = new StringBuilder();

            try
            {
                fileStream = new FileStream(this._FileName, FileMode.OpenOrCreate, FileAccess.Write);
                writer = new StreamWriter(fileStream);

                // Set the file pointer to the end of the file
                writer.BaseStream.Seek(0, SeekOrigin.End);

                // Create the message
                message.Append(Severity.ToString() + ": " + System.DateTime.Now.ToString()).Append(" - ").Append(Message);

                // Force the write to the underlying file
                writer.WriteLine(message.ToString());
                writer.Flush();
            }
            finally
            {
                if (writer != null) writer.Close();
            }
        }
    }


    /// Log messages to the Windows Event Log.

    public class EventLog : Log
    {
        // Internal EventLogName destination value
        private string _EventLogName = "";
        /// <value>Get or set the name of the destination log</value>
        public string EventLogName
        {
            get { return this._EventLogName; }
            set { this._EventLogName = value; }
        }

        // Internal EventLogSource value
        private string _EventLogSource;
        /// <value>Get or set the name of the source of entry</value>
        public string EventLogSource
        {
            get { return this._EventLogSource; }
            set { this._EventLogSource = value; }
        }

        // Internal MachineName value
        private string _MachineName = "";
        /// <value>Get or set the name of the computer</value>
        public string MachineName
        {
            get { return this._MachineName; }
            set { this._MachineName = value; }
        }

        /// Constructor
        public EventLog()
        {
            this.MachineName = ".";
            this.EventLogName = "VID Log";
            this.EventLogSource = "SBO Batch Service";
        }

        /// Log an exception.
        public override void RecordMessage(Exception Message, Log.MessageType Severity)
        {
            this.RecordMessage(Message.Message, Severity);
        }

        /// Log a message.

        public override void RecordMessage(string Message, Log.MessageType Severity)
        {
            StringBuilder message = new StringBuilder();
            System.Diagnostics.EventLog eventLog = new System.Diagnostics.EventLog();

            // Create the source if it does not already exist
            if (!System.Diagnostics.EventLog.SourceExists(this._EventLogSource))
            {
                System.Diagnostics.EventLog.CreateEventSource(this._EventLogSource, this._EventLogName);
            }
            eventLog.Source = this._EventLogSource;
            eventLog.MachineName = this._MachineName;

            // Determine what the EventLogEventType should be 
            // based on the LogSeverity passed in
            EventLogEntryType type = EventLogEntryType.Information;

            switch (Severity.ToString().ToUpper())
            {
                case "INFORMACION":
                    type = EventLogEntryType.Information;
                    break;
                case "FALLA":
                    type = EventLogEntryType.FailureAudit;
                    break;
                case "WARNING":
                    type = EventLogEntryType.Warning;
                    break;
                case "ERROR":
                    type = EventLogEntryType.Error;
                    break;
                case "DEBUG":
                    type = EventLogEntryType.Error;
                    break;
            }
            message.Append(Severity.ToString()).Append(" - ").Append(Message);
            eventLog.WriteEntry(message.ToString(), type);
        }
    }


    /// Managing class to provide the interface for and control 
    /// application logging.  It utilizes the logging objects in 
    /// ErrorLog.Logs to perform the actual logging as configured.  

    public class Logger
    {
        /// <value>Available log types.</value>
        public enum LogTypes
        {
            Event = 1,
            File = 2
        }

        // Internal logging object
        private Log _Logger;

        // Internal log type
        private LogTypes _LogType;

        public LogTypes LogType
        {
            get { return this._LogType; }
            set
            {
                // Set the Logger to the appropriate log when
                // the type changes.
                switch (value)
                {
                    case LogTypes.Event:
                        this._Logger = new Logs.EventLog();
                        break;
                    default:
                        this._Logger = new Logs.FileLog();
                        break;
                }
            }
        }

        /// Constructor
        public Logger()
        {
            this.LogType = LogTypes.File;
        }

        /// Log an exception.
        public void RecordMessage(Exception Message, Log.MessageType Severity)
        {
            this._Logger.RecordMessage(Message, Severity);
        }

        /// Log a message.
        public void RecordMessage(string Message, Log.MessageType Severity)
        {
            this._Logger.RecordMessage(Message, Severity);
        }

        public void LogMsg(String Msg, String tipoMsg, String severityMsg)
        {
            Log.MessageType type = Log.MessageType.Informacion;
            switch (severityMsg.ToUpper())
            {
                case "I":
                    type = Log.MessageType.Informacion;
                    break;
                case "F":
                    type = Log.MessageType.Falla;
                    break;
                case "W":
                    type = Log.MessageType.Warning;
                    break;
                case "E":
                    type = Log.MessageType.Error;
                    break;
                case "D":
                    type = Log.MessageType.Debug;
                    break;
            }

            // Menasjes informativos siempre
            if (tipoMsg == "A" || type == Log.MessageType.Informacion)
            {
                this.LogType = Logger.LogTypes.File;
                this.RecordMessage(Msg, type);
                return;
            }

            //Sin Debug no muestra mensajes tipo Debug - false debe ser variable que indica debug o no
            else if ((false) && (type == Log.MessageType.Debug))
                return;

             // tipoMsg -> E: evento, F: File, A:All
            else if (tipoMsg == "E" || tipoMsg == "A")
            {
                //
                // Messages al EventLog de windows deshabilitados
                //
                //            this.LogType = Logger.LogTypes.Event;
                //            this.RecordMessage(Msg, type);
            }
            else if (tipoMsg == "F" || tipoMsg == "A")
            {
                this.LogType = Logger.LogTypes.File;
                this.RecordMessage(Msg, type);
            }
        }
    }
}