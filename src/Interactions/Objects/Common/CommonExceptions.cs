using System.Reflection;

namespace Nixill.Twitch.Interactions.Objects.Common;

/// <summary>
///   Exception thrown when no deserializer exists for a given type.
/// </summary>
/// <param name="t">
///   The type for which a deserializer does not exist.
/// </param>
public class NoDeserializerException(Type t) : Exception($"There is no deserializer for the type {t.Name}")
{
  public readonly Type AttemptedType = t;
}

/// <summary>
///   Exception thrown when attempting to deserialize but no value exists.
/// </summary>
/// <param name="param">
///   The parameter for which a value does not exist.
/// </param>
public class NoValueException(string param) : Exception($"No values remaining for parameter {param}")
{
  public readonly string Parameter = param;
}

/// <summary>
///   Exception thrown when deserialization of a value fails.
/// </summary>
public class InvalidDeserializeException : Exception
{
  /// <summary>
  ///   Get: The type for which deserialization was attempted, such as
  ///   "double".
  /// </summary>
  public readonly Type AttemptedType;

  /// <summary>
  ///   Get: A human-readable name for the type for which deserialization
  ///   was attempted, such as "number".
  /// </summary>
  public readonly string HumanReadableType;

  /// <summary>
  ///   Get: The value for which deserialization was attempted, such as "hi!".
  /// </summary>
  public readonly string Value;

  /// <summary>
  ///   Get: A custom message for deserialization, if one was specified.
  /// </summary>
  public readonly string? CustomMessage;

  /// <summary>
  ///   Main constructor.
  /// </summary>
  /// <param name="type">
  ///   The type for which deserialization was attempted.
  /// </param>
  /// <param name="hrType">
  ///   A human-readable name for the type for which deserialization was
  ///   attempted.
  /// </param>
  /// <param name="value">
  ///   The value for which deserialization was attempted.
  /// </param>
  public InvalidDeserializeException(Type type, string hrType, string value)
  {
    AttemptedType = type;
    HumanReadableType = hrType;
    Value = value;
  }

  /// <summary>
  ///   Main constructor.
  /// </summary>
  /// <param name="type">
  ///   The type for which deserialization was attempted.
  /// </param>
  /// <param name="hrType">
  ///   A human-readable name for the type for which deserialization was
  ///   attempted.
  /// </param>
  /// <param name="value">
  ///   The value for which deserialization was attempted.
  /// </param>
  /// <param name="message">
  ///   A custom message to be provided.
  /// </param>
  public InvalidDeserializeException(Type type, string hrType, string value, string message) : base(message)
  {
    AttemptedType = type;
    HumanReadableType = hrType;
    Value = value;
    CustomMessage = message;
  }

  /// <summary>
  ///   Main constructor.
  /// </summary>
  /// <param name="type">
  ///   The type for which deserialization was attempted.
  /// </param>
  /// <param name="hrType">
  ///   A human-readable name for the type for which deserialization was
  ///   attempted.
  /// </param>
  /// <param name="value">
  ///   The value for which deserialization was attempted.
  /// </param>
  /// <param name="message">
  ///   A custom message to be provided.
  /// </param>
  /// <param name="ex">
  ///   The underlying exception that was thrown when deserialization failed.
  /// </param>
  public InvalidDeserializeException(Type type, string hrType, string value, string message, Exception ex) : base(message, ex)
  {
    AttemptedType = type;
    HumanReadableType = hrType;
    Value = value;
    CustomMessage = message;
  }
}

/// <summary>
///   An exception a command may throw when user input is invalid.
/// </summary>
/// <param name="message">The error message to display.</param>
public class UserInputException(string message) : Exception($"User input exception: {message}")
{
  /// <summary>
  ///   The error message to display.
  /// </summary>
  public readonly string ErrorMessage = message;
}

/// <summary>
///   Exception thrown when a method cannot be registered as a deserializer.
/// </summary>
/// <param name="m">The method.</param>
/// <param name="message">The reason.</param>
public class IllegalDeserializerException(MethodInfo m, string message) : Exception($"The method {m} is an invalid deserializer: {message}")
{
  /// <summary>
  ///   The method that could not become a deserializer.
  /// </summary>
  public MethodInfo Method = m;
}