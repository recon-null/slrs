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

// For use of sockets
using System.Net.Sockets;
using System.Net;

// Sources table adapter
using tblsourcesTableAdapter 
    = SourceLoggingDaemon.dsSLRSTableAdapters.
      tblsourcesTableAdapter;

// MySQL client
using MySql.Data.MySqlClient;

// Configuration manager
using ConfigurationManager 
   = System.Configuration.ConfigurationManager;

#endregion

namespace SourceLoggingDaemon
{  
    class Program
    {
        static void Main(string[] args)
        {
            // Create the AppLogger
            AppLogger mainLog = new AppLogger("SLRD", LogLocation.File | LogLocation.Console);

            // Log SLRD start
            mainLog.LogEvent(EventSeverity.Information, "Daemon started.");

            // Listen IP and port
            string listenIP;
            int listenPort;

            // Sources table adapter
            tblsourcesTableAdapter taSources;                
                      
            // Sources dictionary
            Dictionary<string, int> sources = null;

            // DB connection
            MySqlConnection dbConn = null;

            // DB command
            MySqlCommand dbCmd = null;

            // UTF Encoder / Decoder
            UTF8Encoding utfEncoder = new UTF8Encoding();


            // Read buffer
            byte[] buffer = new byte[1024];

            // Number of bytes in the read buffer
            int bytesRead = -1;

            // The sid of the source that sent the current
            // log message
            int sid = -1;

            // Source IP:Port
            string sourceIPPort = string.Empty;

            // Source end points
            EndPoint endptSource = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));
            IPEndPoint ipSource = null;

            // Log line buffer
            string logLine = string.Empty;

            // Log message processor
            LogMessage logMsg = new LogMessage();

            // Create the server
            try
            {
                // Get the command line params
                ParamParser.GetIPAndPort(
                          ParamParser.Parse(args),
                          out listenIP, out listenPort);
             
                // Create the sources table adapter
                taSources = new tblsourcesTableAdapter();
               
                // Create the sources dictionary
                sources = LoadSources(taSources);

                // Log that we loaded the sources
                mainLog.LogEvent(EventSeverity.Information,
                                "Loaded authorized sources.");

                // Create the endpoint
                IPEndPoint logEndPt = new IPEndPoint
                    (IPAddress.Parse(listenIP), listenPort);

                // Create the socket
                Socket logSock = new Socket
                    (AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                // Bind the socket to the endpoint
                logSock.Bind(logEndPt);

                // Log that we are listening
                mainLog.LogEvent(EventSeverity.Information,
                                 String.Format("Listening on {0}:{1}.",
                                               logEndPt.Address.ToString(),
                                               logEndPt.Port));

                // Create a connection to the database
                dbConn = new MySqlConnection
                    (taSources.Connection.ConnectionString);               


                // Open the connection
                dbConn.Open();

                // Create the DB command
                dbCmd = new MySqlCommand("INSERT INTO tblLogMessages(sid, messageDT, messageType, " +
                                         "userName, userSteam, userTeam, targetName, targetSteam, " +
                                         "targetTeam, logLine)" +
                                         " VALUES(?sid, ?messageDT , ?messageType, " +
                                         "?userName, ?userSteam, ?userTeam, ?targetName, " + 
                                         "?targetSteam, ?targetTeam ,?logLine)", dbConn);

                

                // Setup the parameters
                CreateLogMessageParams(dbCmd);

                
                // We are connected to the DB and ready to insert log messages
                mainLog.LogEvent(EventSeverity.Information, "Connected to the database.");
                               

                // Main receive loop
                while (true)
                {
                    // Internal catch
                    // (this way, a single invalid packet won't
                    //  cause SLRD to stop receiving
                    try
                    {
                        bytesRead = logSock.ReceiveFrom(buffer, ref endptSource);

                        // Get the source IP                
                        ipSource = (IPEndPoint)endptSource;

                        // Get the source's IP:Port
                        sourceIPPort = ipSource.Address.ToString() 
                            + ":" + ipSource.Port.ToString();                                               
                            
                        // Get the source sid
                        sid = GetSourceSID(sources, sourceIPPort);

                        // Check for buffer underflow and overflow
                        if (bytesRead > 1024 || bytesRead < 4)

                            // We have a problem
                            throw new IndexOutOfRangeException
                                ("Invalid number of bytes in the log packet.");

                        // Validate the check value
                        ValidateCheckValue(buffer, sid);
                           

                        // Everything is valid, decode the log line
                        logLine = utfEncoder.GetString(buffer, 5, bytesRead);
                                                
                        // Parse the log line
                        logMsg.RawMessage = logLine;                        
                              
                        // Set the parameters
                        UpdateLogMessageParams(dbCmd, logMsg, sid);
                        
                        // Execute the query
                        if (dbCmd.ExecuteNonQuery() != 1)
                            throw new Exception("Query failed.");                        

                    }

                    catch (SocketException ex)
                    {
                        // Log the exception
                        mainLog.LogSocketException(ex, EventSeverity.Error);
                    }

                    catch (Exception ex)
                    {
                        // Log the exception
                        mainLog.LogException(ex, EventSeverity.Error);
                    }                   
                }
            }

            catch (SocketException ex)
            {
                // Log the exception
                mainLog.LogSocketException(ex, EventSeverity.Critical);
            }
            catch (Exception ex)
            {
                // Log the exception
                mainLog.LogException(ex, EventSeverity.Critical);
            }

            finally
            {
                // If we have an open connection to the database, close it
                if (dbConn != null && 
                        dbConn.State != System.Data.ConnectionState.Closed)
                    dbConn.Close();
            }

            // Log that we are stopping
            mainLog.LogEvent(EventSeverity.Information, "Daemon stopped.");


        }        

        #region GetSourceSID

        /// <summary>
        /// Gets a source's SID
        /// </summary>
        /// <param name="sources">
        /// The list of sources
        /// </param>
        /// <param name="sourceIPPort">
        /// The source IP
        /// </param>
        /// <returns>
        /// The source's SID
        /// </returns>
        private static int GetSourceSID(Dictionary<string, int> sources, string sourceIPPort)
        {
            // Is this source allowed to send us log messages?
            if (!sources.ContainsKey(sourceIPPort))

                // No, throw an exception
                throw new System.Security.SecurityException(
                    String.Format
                        ("Received a packet from an unauthorized source. Source: {0}.",
                        sourceIPPort)
                    );
            
            // Return the SID
            return sources[sourceIPPort];
        }

        #endregion

        #region ValidateCheckValue

        /// <summary>
        /// Checks to see if a log packet's check
        /// value is valid
        /// </summary>
        /// <param name="buffer">
        /// The packet to check
        /// </param>
        private static void ValidateCheckValue(byte[] buffer, int sid)
        {
            // Check the first four bytes
            for (int i = 0; i < 4; i++)            
                if (buffer[i] != 255)
                    throw new FormatException
                               (String.Format("Invalid log packet from SID: {0}.", sid));

        }

        #endregion

        #region LoadSources

        /// <summary>
        /// Creates a dictionary of sources
        /// from the database table
        /// </summary>
        /// <param name="ta">
        /// The table adapter to use to fill the dictionary
        /// </param>
        /// <returns>
        /// The dictionary of sources
        /// </returns>
        private static Dictionary<string, int> LoadSources(tblsourcesTableAdapter ta)
        {
            // Dataset
            dsSLRS _dsSLRS = new dsSLRS();

            // Create the result dictionary
            Dictionary<string, int> result 
                = new Dictionary<string, int>();
        
            // Get the sources
            ta.Fill(_dsSLRS.tblsources);

            // Add the source rows to the dictionary
            foreach (dsSLRS.tblsourcesRow row in _dsSLRS.tblsources)
                result.Add(row.ip + ":" + row.port, row.sid);

            // Return the result
            return result;
        }

        #endregion        

        #region UpdateLogMessageParams

        /// <summary>
        /// Updates the parameters for a log message insert
        /// </summary>
        /// <param name="dbCmd">
        /// The command to update
        /// </param>
        /// <param name="logMsg">
        /// The log message to use
        /// </param>
        /// <param name="sid">
        /// The source ID for the log message
        /// </param>
        private static void UpdateLogMessageParams(MySqlCommand dbCmd, LogMessage logMsg, int sid)
        {
            // Set the parameters
            dbCmd.Parameters["?sid"].Value = sid;
            dbCmd.Parameters["?messageDT"].Value = logMsg.MysqlTimestamp;
            dbCmd.Parameters["?messageType"].Value = logMsg.CurrentMessageType.ToString();
            dbCmd.Parameters["?userName"].Value = logMsg.UserInfo.Name;
            dbCmd.Parameters["?userSteam"].Value = logMsg.UserInfo.SteamID;
            dbCmd.Parameters["?userTeam"].Value = logMsg.UserInfo.Team;
            dbCmd.Parameters["?targetName"].Value = logMsg.TargetInfo.Name;
            dbCmd.Parameters["?targetSteam"].Value = logMsg.TargetInfo.SteamID;
            dbCmd.Parameters["?targetTeam"].Value = logMsg.TargetInfo.Team;
            dbCmd.Parameters["?logLine"].Value = logMsg.PureMessage;            
        }

        #endregion

        #region CreateLogMessageParams

        /// <summary>
        /// Adds the parameters for a log message insert
        /// </summary>
        /// <param name="dbCmd">
        /// The command to add the parameters to
        /// </param>
        private static void CreateLogMessageParams(MySqlCommand dbCmd)
        {
            // Add the parameters
            dbCmd.Parameters.Add("?sid", MySqlDbType.Int32);
            dbCmd.Parameters.Add("?messageDT", MySqlDbType.DateTime);
            dbCmd.Parameters.Add("?messageType", MySqlDbType.VarChar);
            dbCmd.Parameters.Add("?userName", MySqlDbType.VarChar);
            dbCmd.Parameters.Add("?userSteam", MySqlDbType.VarChar);
            dbCmd.Parameters.Add("?userTeam", MySqlDbType.VarChar);
            dbCmd.Parameters.Add("?targetName", MySqlDbType.VarChar);
            dbCmd.Parameters.Add("?targetSteam", MySqlDbType.VarChar);
            dbCmd.Parameters.Add("?targetTeam", MySqlDbType.VarChar);
            dbCmd.Parameters.Add("?logLine", MySqlDbType.Text);
        }

        #endregion

    }
}
