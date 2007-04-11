/*
 * Copyright (C) 2007 Krzysztof 'DeadwooD' Smiechowicz
 * Original project page: http://sourceforge.net/projects/calindor/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

namespace Calindor.Server
{
    public enum LogSource
    {
        Server,
        World,
        Listener,
        Communication,
        Other
    }

    public enum LogType
    {
        Progress,
        Warning,
        Error
    }

    public interface ILogger
    {
        void LogProgress(LogSource src, string message);
        void LogProgress(LogSource src, string message, Exception ex);
        void LogWarning(LogSource src, string message, Exception ex);
        void LogError(LogSource src, string message, Exception ex);
    }

    public class MultiThreadedLogger : ILogger
    {
        private const string LOG_PROGRESS = "progress";
        private const string LOG_WARNING = "warning";
        private const string LOG_ERROR = "error";
        private const string LOG_DIRECTORY = "logs";

        private StreamWriter swProgress = null;
        private StreamWriter swWarning = null;
        private StreamWriter swError = null;

        private object swWarningBlock = new object();
        private object swErrorBlock = new object();

        private string logFilePath = null;
        private string logFileTimeStamp = null;
        
        private MultiThreadedLogger()
        {
        }

        private string getTimeString()
        {
            return DateTime.Now.ToString("yyyy_MM_dd__HH_mm_ss");
        }

        public MultiThreadedLogger(string appPath)
        {
            if (!Directory.Exists(appPath))
                throw new DirectoryNotFoundException(appPath);

            logFilePath = Path.Combine(appPath, LOG_DIRECTORY);

            if (!Directory.Exists(logFilePath))
                Directory.CreateDirectory(logFilePath);


            logFileTimeStamp = getTimeString();
            
            // progress
            createLogFile(ref swProgress, LOG_PROGRESS);

        }

        public void createLogFile(ref StreamWriter sw, string filePartName)
        {
            string fileName = logFileTimeStamp + "_" + filePartName + ".txt";
            sw = new StreamWriter(Path.Combine(logFilePath, fileName), false);
        }

        private void writeMessage(StreamWriter sw, LogType type, LogSource src, string message, Exception ex)
        {
            string line = getTimeString() + " : " + type.ToString() + " : " + src.ToString() + " : " + message;
            
            if (ex != null)
                line += ", EXCEPTION: " + ex.ToString();

            sw.WriteLine(line);
            sw.Flush();

            // TODO: Remove, test only
            Console.WriteLine(line);
        }

        #region ILogger Members

        public void LogProgress(LogSource src, string message)
        {
            LogProgress(src, message, null);
        }

        public void LogProgress(LogSource src, string message, Exception ex)
        {
            Monitor.TryEnter(swProgress, 10);

            try
            {
                writeMessage(swProgress, LogType.Progress, src, message, ex);
            }
            catch
            { 
            }
            finally
            {
                Monitor.Exit(swProgress);
            }
        }

        public void LogWarning(LogSource src, string message, Exception ex)
        {
            // create if not existing
            if (swWarning == null)
            {
                lock (swWarningBlock)
                {
                    if (swWarning == null)
                        createLogFile(ref swWarning, LOG_WARNING);
                }
            }

            Monitor.TryEnter(swWarning, 10);

            try
            {
                writeMessage(swWarning, LogType.Warning, src, message, ex);
            }
            catch
            {
            }
            finally
            {
                Monitor.Exit(swWarning);
            }

            LogProgress(src, message, ex);
        }

        public void LogError(LogSource src, string message, Exception ex)
        {
            // create if not existing
            if (swError == null)
            {
                lock (swErrorBlock)
                {
                    if (swError == null)
                        createLogFile(ref swError, LOG_ERROR);
                }
            }

            Monitor.TryEnter(swError, 10);

            try
            {
                writeMessage(swError, LogType.Error, src, message, ex);
            }
            catch
            {
            }
            finally
            {
                Monitor.Exit(swError);
            }

            LogProgress(src, message, ex);
        }

        #endregion
    }

    public class DummyLogger : ILogger
    {
        #region ILogger Members

        public void LogProgress(LogSource src, string message)
        {
        }

        public void LogProgress(LogSource src, string message, Exception ex)
        {
        }

        public void LogWarning(LogSource src, string message, Exception ex)
        {
        }

        public void LogError(LogSource src, string message, Exception ex)
        {
        }

        #endregion
    }
}
