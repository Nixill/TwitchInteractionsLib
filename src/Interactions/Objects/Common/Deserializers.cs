using System.Reflection;
using Nixill.Extensions.TwitchInteractionsLib.PopExtension;
using Nixill.Twitch.Interactions.Attributes;

namespace Nixill.Twitch.Interactions.Objects.Common;

/// <summary>
///   Static methods for deserializing command parameters.
/// </summary>
public static class Deserializers
{
  /// <summary>
  ///   Dictionary of deserializers.
  /// </summary>
  static readonly Dictionary<Type, Func<IList<string>, bool, object>> DeserializerList = [];

  /// <summary>
  ///   Returns whether or not a type can ultimately be deserialized.
  /// </summary>
  /// <remarks>
  ///   This returns <see langword="true"/> for:
  ///   <list type="bullet">
  ///     <item>
  ///       Any type for which a method tagged with <see cref="DeserializerAttribute"/>
  ///       has been registered.
  ///     </item>
  ///     <item>
  ///       Any type which contains a public static method named "Parse",
  ///       which has a single argument of type <see cref="string"/>.
  ///     </item>
  ///     <item>
  ///       Any <see langword="enum"/> type.
  ///     </item>
  ///     <item>
  ///       An array, or <see cref="IEnumerable{T}"/> or <see cref="Nullable{T}"/>,
  ///       of any above type. Note that while technically, multiple
  ///       arrays or enumerables could be nested, in practice all
  ///       "parent" objects will only have a single "child" until the
  ///       lowest level is reached.
  ///     </item>
  ///   </list>
  /// </remarks>
  /// <param name="type">The type to check.</param>
  /// <returns>See above.</returns>
  public static bool CanDeserialize(Type type)
  {
    // IEnumerable<T>: Repeatedly deserialize T
    if (type.IsConstructedGenericType && (type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
      || type.GetGenericTypeDefinition() == typeof(Nullable<>)))
    {
      return CanDeserialize(type.GenericTypeArguments[0]);
    }

    // T[]: Repeatedly deserialize T
    if (type.IsArray)
    {
      return CanDeserialize(type.GetElementType()!);
    }

    // Check for existing deserializer
    if (DeserializerList.ContainsKey(type))
    {
      return true;
    }

    // If no deserializer exists, there are a couple cases that can still work:
    // If the type is an enum, parse it as an enum constant.
    if (type.IsEnum)
    {
      return true;
    }

    // Otherwise, if the type has a public static method named Parse that
    // takes exactly a single string argument, invoke that method.
    MethodInfo? parseMethod = type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, [typeof(string)]);
    if (parseMethod != null)
    {
      return true;
    }

    // That's it, if we're here there's nothing we can do.
    return false;
  }

  /// <summary>
  ///   Deserialize command parameters.
  /// </summary>
  /// <param name="type">The type to deserialize to.</param>
  /// <param name="input">
  ///   The list of yet-to-be-parsed parameters (space separated words
  ///   from the original message).
  /// </param>
  /// <param name="isLongText">
  ///   Whether or not the final parameter of a command should use all
  ///   remaining words, rather than the minimum to parse.
  /// </param>
  /// <returns>The deserialized object.</returns>
  /// <exception cref="NoDeserializerException">
  ///   No deserializer exists for the given type. (You can use
  ///   <see cref="CanDeserialize(Type)"/> to check if this exception will
  ///   be thrown.)
  /// </exception>
  public static object Deserialize(Type type, IList<string> input, bool isLongText)
  {
    if (input.Count == 0) throw new NoValueException("(unknown parameter)");

    // IEnumerable<T>: Repeatedly deserialize T
    if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
    {
      return DeserializeEnumerable(type.GenericTypeArguments[0], input).ToArray();
    }

    // T[]: Repeatedly deserialize T
    if (type.IsArray)
    {
      return DeserializeEnumerable(type.GetElementType()!, input).ToArray();
    }

    // Nullable<T>: Deserialize T
    if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
    {
      return Deserialize(type.GenericTypeArguments[0], input, isLongText);
    }

    // Check for existing deserializer
    if (DeserializerList.TryGetValue(type, out Func<IList<string>, bool, object>? des))
    {
      return des(input, isLongText);
    }

    // If no deserializer exists, there are a couple cases that can still work:
    // If the type is an enum, parse it as an enum constant.
    if (type.IsEnum)
    {
      string value = input.Pop();
      return Enum.Parse(type, input[0]);
    }

    // Otherwise, if the type has a public static method named Parse that
    // takes exactly a single string argument, invoke that method.
    MethodInfo? parseMethod = type.GetMethod("Parse", BindingFlags.Public | BindingFlags.Static, [typeof(string)]);
    if (parseMethod != null)
    {
      string value;
      if (isLongText)
      {
        value = string.Join(' ', input);
        input.Clear();
      }
      else
      {
        value = input.Pop();
      }

      return parseMethod.Invoke(null, [value])!;
    }

    // That's it, if we're here there's nothing we can do.
    throw new NoDeserializerException(type);
  }

  /// <summary>
  ///   Deserializes multiple values of an enumerable.
  /// </summary>
  /// <param name="type"></param>
  /// <param name="input"></param>
  /// <returns></returns>
  static IEnumerable<object> DeserializeEnumerable(Type type, IList<string> input)
  {
    while (input.Count > 0)
    {
      yield return Deserialize(type, input, false);
    }
  }

  /// <summary>
  ///   Attempts to register all the deserializer methods in an assembly.
  /// </summary>
  /// <remarks>
  ///   Only <see langword="public"/> <see langword="static"/> methods
  ///   within <see langword="public"/> types are considered. All others
  ///   are ignored.
  /// </remarks>
  /// <param name="t">The assembly.</param>
  /// <returns>The result of deserialization.</returns>
  public static DeserializerRegistrationResult RegisterDeserializers(Assembly asm)
  {
    List<Deserializer> successes = [];
    List<FailedDeserializer> failures = [];

    foreach (Type t in asm.GetTypes())
    {
      if (!t.IsPublic) continue;

      var result = RegisterDeserializers(t);
      successes.AddRange(result.Successes);
      failures.AddRange(result.Failures);
    }

    return new DeserializerRegistrationResult(successes, failures);
  }

  /// <summary>
  ///   Attempts to register all the deserializer methods in a type.
  /// </summary>
  /// <remarks>
  ///   Only <see langword="public"/> <see langword="static"/> methods are
  ///   considered. All others are ignored.
  /// </remarks>
  /// <param name="t">The type.</param>
  /// <returns>The result of deserialization.</returns>
  public static DeserializerRegistrationResult RegisterDeserializers(Type t)
  {
    List<Deserializer> successes = [];
    List<FailedDeserializer> failures = [];

    foreach (MethodInfo m in t.GetMethods(BindingFlags.Public | BindingFlags.Static))
    {
      var result = RegisterDeserializers(t);

      try
      {
        var pars = m.GetParameters();
        if (pars.Length != 2)
          throw new IllegalDeserializerException(m, $"It must have exactly two parameters. (Actual: {pars.Length})");
        if (pars[0].ParameterType != typeof(IList<string>))
          throw new IllegalDeserializerException(m,
            $"Its first parameter must be an IList<string>. (Actual: {pars[0].ParameterType})");
        if (pars[1].ParameterType != typeof(bool))
          throw new IllegalDeserializerException(m,
            $"Its second parameter must be a bool. (Actual: {pars[1].ParameterType})");

        var del = m.CreateDelegate<Func<IList<string>, bool, object>>();
        DeserializerList[m.ReturnType] = del;
        successes.Add(new(m.ReturnType, del));
      }
      catch (IllegalDeserializerException ex)
      {
        failures.Add(new(ex.Method, ex));
      }
    }

    return new DeserializerRegistrationResult(successes, failures);
  }
}

/// <summary>
///   Represents a deserializer function with its associated type.
/// </summary>
/// <param name="Type">
///   The type to which objects are deserialized.
/// </param>
/// <param name="Method">
///   The deserialization method.
/// </param>
public readonly record struct Deserializer(
  Type Type,
  Func<IList<string>, bool, object> Method
);

/// <summary>
///   Represents a failed (invalid) deserializer function method.
/// </summary>
/// <param name="TaggedMethod">
///   The method tagged with <see cref="DeserializerAttribute"/> that was
///   not successfully registered.
/// </param>
/// <param name="Reason">
///   The exception that caused this method to not be registered.
/// </param>
public readonly record struct FailedDeserializer(
  MethodInfo TaggedMethod,
  Exception Reason
);

/// <summary>
///   The output of a registration attempt, listing successful and failed
///   deserializers.
/// </summary>
/// <param name="Successes">Successfully registered deserializers.</param>
/// <param name="Failures">
///   Unsuccessfully registered deserializers.
/// </param>
public readonly record struct DeserializerRegistrationResult(
  IEnumerable<Deserializer> Successes,
  IEnumerable<FailedDeserializer> Failures
);