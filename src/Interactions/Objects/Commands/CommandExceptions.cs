using System.Reflection;

namespace Nixill.Twitch.Interactions.Objects.Commands;

/// <summary>
///   Exception thrown when a method cannot be registered as a chat command.
/// </summary>
/// <param name="m">The method.</param>
/// <param name="message">The reason.</param>
public class IllegalCommandException(MethodInfo m, string message) : Exception($"The method {m} is an invalid deserializer: {message}")
{
  /// <summary>
  ///   The method that could not become a command.
  /// </summary>
  public MethodInfo Method = m;
}