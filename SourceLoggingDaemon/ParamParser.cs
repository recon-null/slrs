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

#endregion

namespace SourceLoggingDaemon
{
    /// <summary>
    /// Command line parameter parser
    /// </summary>
    public static class ParamParser
    {
        #region Parse

        /// <summary>
        /// Parses a string array of command line parameters
        /// </summary>
        /// <param name="args">
        /// The parameters to parse
        /// </param>
        /// <returns>
        /// A dictionary of parameters
        /// </returns>
        public static Dictionary<string, string> Parse(string[] args)
        {
            // Create the results dictionary
            Dictionary<string, string> results
                = new Dictionary<string, string>();

            // Get all the params
            for (int i = 0; i < args.Length; )
            {
                // Get the key
                string key = args[i];
                string value = "";
                
                // Is there a value?
                if (args.Length >= i + 2 && !args[i + 1].StartsWith("-"))
                {
                    // Yes
                    value = args[i + 1];

                    // Move to the next param
                    i += 2;
                }
                else

                    // Move to the next param
                    i++;

                // Add the param
                results.Add(key, value);
            }

            // Return the results array
            return results;
        }

        #endregion

        #region GetIPAndPort

        /// <summary>
        /// Gets an IP and port from a dictionary
        /// of parameters
        /// </summary>
        /// <param name="parameters">
        /// The parameters dictionary
        /// </param>
        /// <param name="ip">
        /// The IP will be stored here
        /// </param>
        /// <param name="port">
        /// The port will be stored here
        /// </param>
        /// <remarks>
        /// If parameters can't be found, an exception will be thrown
        /// </remarks>
        public static void GetIPAndPort(Dictionary<string, string> parameters,
                                      out string ip, out int port)
        {
            // Check to see if the user supplied the IP and port
            // and if not, throw the proper exceptions
            //
            // If the user supplied everything they are supposed to,
            // return the params

            if (parameters.ContainsKey("-ip"))
                ip = parameters["-ip"];
            else
                throw new ArgumentException("No listen IP address specified (-ip).");

            if (parameters.ContainsKey("-port"))
                port = int.Parse(parameters["-port"]);
            else
                throw new ArgumentException("No listen port specified (-port).");
        }

        #endregion
    }
}
