using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;
using ShareFile.Api.Powershell.Properties;

namespace ShareFile.Api.Powershell.Log
{
    /// <summary>
    /// ShareFile Logger class to log messages using NLog logger
    /// Logger.Instance property to retrieve the NLog.Logger instance
    /// Logger file name is specified in Resources files 
    /// Logger is configured in NLog.config file
    /// </summary>
    class Logger
    {
        /// <summary>
        /// Configure & Return the NLog.Logger object
        /// </summary>
        public static NLog.Logger Instance
        {
            get 
            {
                // if LogManager failed to configure automatically then configure it manually by specifying the paths
                // (due to path issues (as assemble path is different & current location is different) it will not configure automatically)
                if (LogManager.Configuration == null)
                {
                    String targetName = "logfile";
                    String directory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    LogManager.Configuration = new XmlLoggingConfiguration(Path.Combine(directory, Resources.LogConfigFile));

                    directory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var fileTarget = LogManager.Configuration.FindTargetByName(targetName) as FileTarget;
                    fileTarget.FileName = Path.Combine(directory, Resources.AppName, Resources.LogFile);
                }

                return LogManager.GetCurrentClassLogger();
            }
        }
    }
}
