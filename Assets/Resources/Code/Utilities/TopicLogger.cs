using System;
using System.Collections.Generic;
using UnityEngine;

// Log level enumeration
public enum LogLevel
{
    VERBOSE,
    DEBUG,
    INFO,
    WARN,
    ERROR,
    CRIT
}

// Topics enumeration
public enum LogTopic
{
    Player,
    Interaction,
    General,
    Equipment_Controller,
    Installation
}

public static class TopicLogger
{
    // Default log level
    private static LogLevel defaultLogLevel = LogLevel.INFO;

    // Topic-specific log levels
    private static Dictionary<LogTopic, LogLevel> topicLogLevels = new Dictionary<LogTopic, LogLevel>();

    // Set the log level for a specific topic
    public static void SetLogLevel(LogTopic topic, LogLevel level)
    {
        topicLogLevels[topic] = level;
    }

    // Set the default log level
    public static void SetDefaultLogLevel(LogLevel level)
    {
        defaultLogLevel = level;
    }

    // Log a message with a specific log level and topic
    public static void Log(LogTopic topic, LogLevel level, string message)
    {
        LogLevel topicLogLevel = topicLogLevels.ContainsKey(topic) ? topicLogLevels[topic] : defaultLogLevel;

        // Check if the log level is high enough to log
        if (level >= topicLogLevel)
        {
            string logMessage = $"[{DateTime.Now:HH:mm:ss}] [{level}] [{topic}] {message}";

            // Output to Unity's console
            switch (level)
            {
                case LogLevel.VERBOSE:
                case LogLevel.DEBUG:
                case LogLevel.INFO:
                    Debug.Log(logMessage);
                    break;
                case LogLevel.WARN:
                    Debug.LogWarning(logMessage);
                    break;
                case LogLevel.ERROR:
                case LogLevel.CRIT:
                    Debug.LogError(logMessage);
                    break;
            }
        }
    }
}