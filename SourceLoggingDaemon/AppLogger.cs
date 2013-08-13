#region Program Header

// THE BELOW HEADER MAY NOT BE REMOVED OR MODIFIED
//
// This file is part of SLRS (Source Logging and Reporting Services).
//
// SLRS is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// SLRS is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with SLRS.  If not, see <http://www.gnu.org/licenses/>.
//
// THE ABOVE HEADER MAY NOT BE REMOVED OR MODIFIED

#endregion

#region Using

// Default using
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

// Socket exception
using SocketException 
      = System.Net.Sockets.SocketException;

#endregion

namespace SourceLoggingDaemon
{
    #region LogLocation

    /// <summary>
    /// Log location enum
    /// </summary>
    public enum LogLocation
    {
        /// <summary>
        /// Log to a file
        /// </summary>
        File = 0,

        /// <summary>
        /// Log to the console
        /// </summary>
        Console = 1        
    }

    #endregion

    #region EventSeverity

    /// <summary>
    /// Event severity enum
    /// </summary>
    public enum EventSeverity
    {
        Information = 0,
        Warning = 1,
        Error = 2,
        Critical = 3
    }

    #endregion

    /// <summary>
    /// Application logging class
    /// </summary>
    public class AppLogger
    {
        #region Private Objects

        /// <summary>
        /// The location(s) to log to
        /// </summary>
        private LogLocation _logEndpoint;

        /// <summary>
        /// Holds the path to the log directory
        /// </summary>
        private readonly string _logDirPath;

        /// <summary>
        /// Holds the log prefix
        /// </summary>
        private readonly string _prefix;        
        
        /// <summary>
        /// Holds the hour for which the current log file
        /// can accept events
        /// </summary>
        private int _fileHour = -1;

        /// <summary>
        /// The log file writer
        /// </summary>
        private StreamWriter _fileWriter;

        #endregion        

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="prefix">
        /// The prefix to add to log files
        /// (prefix_file_name.log)
        /// </param>
        public AppLogger(string prefix, LogLocation logEndpoint)
        {
            // Save the prefix
            _prefix = prefix;

            // Save the log endpoint
            _logEndpoint = logEndpoint;

            // If we are logging to a file, set up our log path
            if ((_logEndpoint & LogLocation.File) == LogLocation.File)
            {
                // Get the log dir path
                _logDirPath = System.IO.Path.GetDirectoryName(
                                 System.Reflection.Assembly.GetExecutingAssembly()
                                 .GetName().CodeBase.Substring(8).Replace("/", @"\"))
                                 + "\\logs";

                // Create the log directory if it doesn't exist
                if (!Directory.Exists(_logDirPath))
                    Directory.CreateDirectory(_logDirPath);
            }

        }

        #endregion

        #region Destructors

        /// <summary>
        /// Destructor
        /// </summary>
        ~AppLogger()
        {
            // If we are logging to a file, we need to close it
            if ((_logEndpoint & LogLocation.File) == LogLocation.File)
            {
                // Close the log file and
                // ignore any exceptions
                // (since they are probably
                //  from the application closing)
                try
                {
                    _fileWriter.Close();
                }
                catch (Exception)
                {

                }
            }
        }

        #endregion

        #region LogEvent

        /// <summary>
        /// Logs an event
        /// </summary>
        /// <param name="es">
        /// The event severity
        /// </param>
        /// <param name="eventDesc">
        /// The event description
        /// </param>
        public void LogEvent(EventSeverity es, string eventDesc)
        {
            // Create the log line
            string logLine = 
                    String.Format(
                        "{0}    {1}: {2}\n",
                        DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss tt"),
                        es.ToString(),
                        eventDesc);

            // Logging to a file
            if ((_logEndpoint & LogLocation.File) == LogLocation.File)
            {
                // Do we need to open a new log file?
                if (_fileHour != DateTime.Now.Hour)
                    OpenLogFile();

                // Log the event
                _fileWriter.Write(logLine);

                // Write the event to disk
                _fileWriter.Flush();
            }

            // Logging to the console
            if ((_logEndpoint & LogLocation.Console) == LogLocation.Console)
                Console.Write(logLine);
        }

        #endregion

        #region OpenLogFile

        /// <summary>
        /// Open the log file
        /// </summary>
        private void OpenLogFile()
        {
            // Get the current date and
            // time
            DateTime dt = DateTime.Now;

            // Store the hour
            _fileHour = dt.Hour;

            // If a log file is already open, close it
            if (_fileWriter != null)
                _fileWriter.Close();

            // Open the new log file
            _fileWriter = new StreamWriter(
                            String.Format("{0}\\{1}_{2}.log",
                                           _logDirPath,
                                           _prefix,
                                           dt.ToString("MM-dd-yyyy_HH")
                                          ), true);
        }

        #endregion

        #region Custom Exception Handling Methods

        /// <summary>
        /// Logs a socket exception
        /// </summary>
        /// <param name="ex">
        /// The SocketException to log
        /// </param>
        /// <param name="es">
        /// The severity to log
        /// </param>
        public void LogSocketException(SocketException ex, EventSeverity es)
        {
            LogEvent(es, ex.Message);
            LogEvent(es, "Socket Error Code: " + ex.SocketErrorCode);
            LogEvent(es, "Stack Trace:" + ex.StackTrace);
        }

        /// <summary>
        /// Logs an exception
        /// </summary>
        /// <param name="ex">
        /// The exception to log
        /// </param>
        /// <param name="es">
        /// The severity to log
        /// </param>
        public void LogException(Exception ex, EventSeverity es)
        {
            LogEvent(es, ex.Message);
            LogEvent(es, "Stack Trace:" + ex.StackTrace);
        }

        #endregion
    }
}