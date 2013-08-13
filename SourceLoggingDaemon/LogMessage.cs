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

// Regex
using System.Text.RegularExpressions;

#endregion

namespace SourceLoggingDaemon
{
    #region MessageType

    /// <summary>
    /// Log message type
    /// </summary>
    public enum MessageType
    {
        Generic = 0,
        Admin = 1,
        Chat = 2,
        User = 3,
        Targeted = 4
    }

    #endregion

    #region PlayerInfo

    public class PlayerInfo
    {
        /// <summary>
        /// Player's name
        /// </summary>
        public string Name;

        /// <summary>
        /// Player's UID
        /// </summary>
        public string UID;

        /// <summary>
        /// Player's SteamID
        /// </summary>
        public string SteamID;

        /// <summary>
        /// Player's team
        /// </summary>
        public string Team;

        /// <summary>
        /// Null player info value
        /// </summary>
        public static readonly PlayerInfo Null;

        /// <summary>
        /// Static constructor
        /// </summary>
        static PlayerInfo()
        {
            // Create the null value
            Null = new PlayerInfo();
            Null.Name = null;
            Null.SteamID = null;
            Null.Team = null;
            Null.UID = null;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public PlayerInfo()
        {

        }
    }

    #endregion

    /// <summary>
    /// Log line class
    /// </summary>
    internal class LogMessage
    {
        #region Private Objects

        /// <summary>
        /// The raw message
        /// </summary>
        private string _rawMessage = "";
   
        /// <summary>
        /// The clean message
        /// </summary>
        private string _cleanMessage = "";

        /// <summary>
        /// Clean message without the timestamp
        /// </summary>
        private string _pureMessage = "";      

        /// <summary>
        /// The message type for the current message
        /// </summary>
        private MessageType _currentMessageType;

        /// <summary>
        /// The user info for the message
        /// </summary>
        private PlayerInfo _userInfo;

        /// <summary>
        /// The target info for the message
        /// </summary>
        private PlayerInfo _targetInfo;
        
        /// <summary>
        /// The MySQL timestmap for the current log message
        /// </summary>
        private string _mysqlTimestamp = "";

        #endregion

        #region RegEx Match Types

        // ************ README ************  //
        //
        // Documentation for each match type
        // can be found in the regex doc file
        //
        // ************ README ************  //

        /// <summary>
        /// RegEx for type A matches
        /// </summary>
        private static readonly Regex TypeA =
            new Regex(@"^""([^""]+)"" ([^""\(]+) ""([^""]+)""(.*)$");

        /// <summary>
        /// RegEx for type B matches
        /// </summary>
        private static readonly Regex TypeB = new Regex(@"^""([^""]+)"" ([^""\(]+) ""([^""]+)""$");

        /// <summary>
        /// Regex for enter the game, steam ID validates and disconnects
        /// </summary>
        private static readonly Regex TypeC = 
            new Regex(@"^""([^""]+)"" (STEAM USERID validated)|(entered the game)|(disconnected)$");

        /// <summary>
        /// Regex for player info
        /// </summary>
        private static readonly Regex TypePlayerInfo = 
            new Regex ("^\"([^\"]+)\"");

        /// <summary>
        /// Regex for extracting parts from player info
        /// </summary>
        private static readonly Regex TypePlayerInfoParts =
            new Regex(@"^(.+)<(\d+)><((STEAM_[\d]:[\d]:[\d]+)|BOT|Console)><([a-zA-Z ]*)>$");

        #endregion

        #region Property RawMessage

        /// <summary>
        /// Gets or sets the raw log line for instance
        /// </summary>
        public string RawMessage
        {
            get
            {
                return _rawMessage;
            }
            set
            {
                _rawMessage = value;             

                // Process the log message
                ProcessLogMessage();                
            }
        }

        #endregion

        #region Property CleanMessage

        /// <summary>
        /// A clean version of RawMessage
        /// </summary>
        public string CleanMessage
        {
            get
            {
                return _cleanMessage;
            }

        }

        #endregion

        #region Property CurrentMessageType

        /// <summary>
        /// The message type for the current message
        /// </summary>
        public MessageType CurrentMessageType
        {
            get
            {
                return _currentMessageType;
            }

        }

        #endregion

        #region Property MysqlTimestamp

        /// <summary>
        /// The MySQL timestmap for the current log message
        /// </summary>
        public string MysqlTimestamp
        {
            get
            {
                return _mysqlTimestamp;
            }
        }

        #endregion

        #region Property PureMessage

        /// <summary>
        /// Clean message without the timestamp
        /// </summary>
        public string PureMessage
        {
            get
            {
                return _pureMessage;
            }
        }

        #endregion

        #region Property UserInfo

        /// <summary>
        /// The user info for the message
        /// </summary>
        public PlayerInfo UserInfo
        {
            get
            {
                return _userInfo;
            }

        }

        #endregion

        #region Property TargetInfo

        /// <summary>
        /// The target info for the message
        /// </summary>
        public PlayerInfo TargetInfo
        {
            get
            {
                return _targetInfo;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public LogMessage()
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="rawMessage">
        /// The log message
        /// </param>
        public LogMessage(string rawMessage) : this()
        {
            // Set the log message
            this.RawMessage = rawMessage;            
        }

        #endregion

        #region ProcessLogMessage

        /// <summary>
        /// Processes a log message
        /// </summary>
        private void ProcessLogMessage()
        {
            // Figure out where the end of the string is
            int trimLength = _rawMessage.IndexOfAny(new char[] {'\0', '\n'});

            // Get a clean version of the log message
            _cleanMessage = _rawMessage.Substring(0, trimLength > 0 ? trimLength : _rawMessage.Length );

            // Get the pure log message (without the timestamp)
            _pureMessage = _cleanMessage.Remove(0, 25);


            // Get a MySQL timestamp from the log line
            _mysqlTimestamp = DateTime.Parse
                (_cleanMessage.Substring(2, 21).Remove(10, 2)).
                    ToString("yyyy-MM-dd HH:mm:ss");


            // Check for user info
            MatchCollection mc = TypePlayerInfo.Matches(_pureMessage);

            // This is a user message of some kind
            if (mc.Count == 1 && mc[0].Groups.Count == 2)
            {
                // Get the user info
                _userInfo = GetPlayerInfo(mc[0].Groups[1].Value);

                // Did we get valid user info?
                if (_userInfo != PlayerInfo.Null)
                {
                    // Check for target info
                    mc = TypeA.Matches(_pureMessage);

                    // Is there target info?
                    if (mc.Count == 1 && mc[0].Groups.Count > 3)
                    {
                        // Yes
                        _targetInfo = GetPlayerInfo(mc[0].Groups[3].Value);

                        // Did we get valid target information?
                        if (_targetInfo != PlayerInfo.Null)
                        
                            // Yes, targeted event
                            _currentMessageType = MessageType.Targeted;
                        
                        else
               
                            // No
                            _currentMessageType = MessageType.User;
                        
                    }
                    else
                    {
                        // User event
                        _currentMessageType = MessageType.User;

                        // No target info
                        _targetInfo = PlayerInfo.Null;
                    }
                }
                else
                {
                    // Invalid user info

                    // No player info
                    _userInfo = PlayerInfo.Null;

                    // No target info
                    _targetInfo = PlayerInfo.Null;

                    // Default message type
                    _currentMessageType = MessageType.Generic;
                }
            }
            else
            {
                // Not a user event

                // No player info
                _userInfo = PlayerInfo.Null;

                // No target info
                _targetInfo = PlayerInfo.Null;

                // Default message type
                _currentMessageType = MessageType.Generic;
            }
        }

        #endregion

        #region GetPlayerInfo

        /// <summary>
        /// Gets a player's information from a string part
        /// </summary>
        /// <param name="playerInfoPart">
        /// The string part to get the player's info from
        /// </param>
        /// <returns>
        /// The player's info, or null if the string part
        /// didn't parse into valid info
        /// </returns>
        private PlayerInfo GetPlayerInfo(string playerInfoPart)
        {
            // Get the player info parts
            MatchCollection mc 
                = TypePlayerInfoParts.Matches(playerInfoPart);

            if (mc.Count == 1 && mc[0].Groups.Count > 4)
            {
                // Player info
                PlayerInfo pi = new PlayerInfo();

                // Get the player info
                pi.Name = mc[0].Groups[1].Value;
                pi.UID = mc[0].Groups[2].Value;
                pi.SteamID = mc[0].Groups[3].Value;

                // Is this player on a team?
                if (mc[0].Groups.Count > 5)
                    pi.Team = mc[0].Groups[5].Value;

                // Return the player info
                return pi;
            }
            else
                return PlayerInfo.Null;
        }

        #endregion
    }
}