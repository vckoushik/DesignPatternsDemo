using System;
using DesignPatternsDemo.Services.Interfaces;
namespace DesignPatternsDemo.Services.Patterns;

/// <summary>
/// A simple console logger implementation that logs messages to the console.
/// This is a singleton class, ensuring that only one instance of the logger exists throughout the application.
/// We are creating it using AddSingleton pattern, which is a creational design pattern that ensures a class has only one instance and provides a global point of access to it.
/// </summary>
public sealed class ConsoleLogger : IConsoleLogger
{
    public void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now}] {message}");
    }
}
