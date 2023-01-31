using log4net.Appender;
using log4net.Core;
using System;

namespace NanoTwitchLeafs
{
    public delegate void MessageLoggedDelegate(Level level, DateTime logTime, string message);

    public class NanoTwitchLeafsAppender : IAppender
    {
        public event MessageLoggedDelegate OnMessageLogged;

        public string Name { get; set; } = "NanoTwitchLeafsAppender";

        public void Close()
        {
            // Do nothing
        }

        public void DoAppend(LoggingEvent loggingEvent)
        {
            OnMessageLogged?.Invoke(loggingEvent.Level, loggingEvent.TimeStamp, loggingEvent.RenderedMessage);
        }
    }
}