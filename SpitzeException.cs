using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using log4net;
using System.Diagnostics;
namespace WebApplication3.CustomExceptions
{

    public class SpitzeException : Exception
    {
        const string defaultErrorMessage = "There is an error here";
        private const string StopwatchKey = "DebugLoggingStopWatch";

        public string ErrorMessage { get; set; }

        private string errorType;
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static LogTestEntities db = new LogTestEntities();

        public SpitzeException()
        {
            this.errorType = this.GetType().Name;
            this.ErrorMessage = defaultErrorMessage;
        }
        public SpitzeException(string message)
        {
            this.errorType = this.GetType().Name;
            this.ErrorMessage = message;
        }

        public static void Log(Exception ex)
        {
            LogException(ex);
        }

        public void Log()
        {
            LogException(this);
        }

        private static void LogException(Exception ex)
        {
            int haveInnerException = 0;
            string testStackTrace = ex.StackTrace.ToString();
            string exType = ex.GetType().Name;
            string actionName = ex.TargetSite.ToString();
            string exMessage = ex.Message;
            if (ex.InnerException != null)
            {
                haveInnerException = 1;
            }
            AddLogEntry(testStackTrace, exType, actionName, exMessage, haveInnerException);
            if (haveInnerException == 1)
                SpitzeException.Log(ex.InnerException);
        }

        public void Log(string exceptionMessage)
        {
            this.ErrorMessage = exceptionMessage;
            this.Log();
        }

        public static void AddLogEntry(string stackTrace,string exceptionType,string actionName,string exceptionMessage, int hasInnerException)
        {
            DateTime now = DateTime.Now;
            dynamic moduleInfo = ParseStackTrace(stackTrace);
            string controllerName = String.Empty;
            int lineNumber = -1;

            if(!(moduleInfo == null))
            {
                controllerName = moduleInfo.ControllerName;
                lineNumber = moduleInfo.LineNumber;
            }

            var LogItem = new LogEntry
            {
                Type = exceptionType,
                //action = "sdkjac hbjadscdsacasd sdacasdcsaddascds bsdajkcasd bhjadsc adscbhj dshcbjasdkhc djcbhadsc dsacbhj bhc hjbsdckjasdkcdsbcj hbcasdkcadsbc",
                Action = actionName,
                Time = now,
                Controller = controllerName,
                Message = exceptionMessage,
                LineNumber = lineNumber,
                InnerException = hasInnerException,
            };
            try {
                db.LogEntries.Add(LogItem);
                db.SaveChanges();
            }
            catch (SqlException ex)
            {
                logger.Fatal(ex.StackTrace.ToString());
            }
            catch (Exception ex)
            {
                SpitzeException.Log(ex);
            }
        }

        private static dynamic ParseStackTrace(string stackTrace)
        {
            dynamic parsedStacktrace = null;
            try
            {
                var lastSlashIndex = stackTrace.LastIndexOf(@"\");
                if (lastSlashIndex == -1)
                {
                    throw new Exception("Invalid format for stackTrace");
                }

                stackTrace = stackTrace.Substring(lastSlashIndex + 1);
                string[] splitAtColonArray = stackTrace.Split(':');
                string lineNumberString = splitAtColonArray[1].Substring(splitAtColonArray[1].IndexOf(" ") + 1);
                int lineNumber;
                if (!Int32.TryParse(lineNumberString,out lineNumber))
                {
                    throw new Exception(String.Format("Failed to parse line number. '{0}' is not an integer", lineNumberString));
                }
                var controllerName = splitAtColonArray[0];

                parsedStacktrace = new { ControllerName = controllerName, LineNumber = lineNumber };
            }
            catch (Exception ex)
            {
                SpitzeException.Log(ex);
                throw;
            }

            return parsedStacktrace;
        }
    }
}