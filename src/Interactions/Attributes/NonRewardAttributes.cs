using System.Reflection;

namespace Nixill.Twitch.Interactions.Attributes;

/// <summary>
///   Marks a public, static method as being a code-based command for this
///   twitch chat bot.
/// </summary>
/// <remarks>
///   The tagged method must be <see langword="static"/> and <see langword="public"/>.
///   It must have at least one parameter, and its first parameter must be
///   a <see cref="CommandContext"/> or a less derived type. All other
///   parameters must be types with a static <c>Parse(string)</c> method
///   or be returned by a method tagged with <see cref="DeserializerAttribute"/>
///   (or be a nullable or enumerable wrapper of such a type). The method
///   must return <see cref="Task"/> or <see cref="Task{T}"/> of
///   <see cref="JoltInteractionResponse"/>.
///   <para/>
///   Methods that don't meet all of the conditions in the foregoing
///   paragraph are skipped as commands when adding by attribute or type,
///   and cause an exception when adding directly.
/// </remarks>
/// <param name="name">
///   The name of the command, excluding the command prefix.
/// </param>
/// <param name="aliases">
///   Aliases of the command, excluding the command prefix.
/// </param>
[AttributeUsage(AttributeTargets.Method)]
public class CommandAttribute(string name, params string[] aliases) : Attribute
{
  /// <summary>
  ///   Get: The name of the command, without the command prefix.
  /// </summary>
  public readonly string Name = name;

  /// <summary>
  ///   Get: Aliases of the command, without the command prefix.
  /// </summary>
  public readonly string[] Aliases = aliases;

  /// <summary>
  ///   Gets all public, static methods tagged with this attribute in the
  ///   given assembly.
  /// </summary>
  /// <param name="asm">Assembly.</param>
  /// <returns>All tagged methods.</returns>
  public static IEnumerable<MethodInfo> GetTaggedMethods(Assembly asm)
    => asm.GetTypes()
      .SelectMany(GetTaggedMethods);

  /// <summary>
  ///   Gets all public, static methods tagged with this attribute in the
  ///   given type.
  /// </summary>
  /// <param name="t">The type.</param>
  /// <returns>All tagged methods.</returns>
  public static IEnumerable<MethodInfo> GetTaggedMethods(Type t)
    => t.GetMethods(BindingFlags.Static | BindingFlags.Public)
      .Where(m => m.GetCustomAttribute<CommandAttribute>() != null);
}

/// <summary>
///   Marks a public, static method as being a parameter deserializer for
///   a twitch command or reward.
/// </summary>
/// <remarks>
///   Multiple deserializers cannot have the same return type. If you
///   attempt to add multiple deserializers of the same type, the newer
///   overrides the older.
///   <para/>
///   The tagged method must be <see langword="static"/> and <see langword="public"/>.
///   It must have exactly two parameters, being <see cref="IList{T}"/> of
///   <see cref="string"/> as the first parameter and <see cref="bool"/>
///   as the second parameter. It must return a type that is not
///   <see cref="Nullable{T}"/> or <see cref="IEnumerable{T}"/>.
///   <para/>
///   Methods that don't meet all of the conditions in the foregoing
///   paragraph are skipped as deserializers when adding by attribute or
///   type, and cause an exception when adding directly.
///   <para/>
///   See <see cref="Deserializers"/> for built-in deserializers.
/// </remarks>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class DeserializerAttribute : Attribute
{
  /// <summary>
  ///   Gets all public, static methods tagged with this attribute in the
  ///   given assembly.
  /// </summary>
  /// <param name="asm">Assembly.</param>
  /// <returns>All tagged methods.</returns>
  public static IEnumerable<MethodInfo> GetTaggedMethods(Assembly asm)
    => asm.GetTypes()
      .SelectMany(GetTaggedMethods);

  /// <summary>
  ///   Gets all public, static methods tagged with this attribute in the
  ///   given type.
  /// </summary>
  /// <param name="t">The type.</param>
  /// <returns>All tagged methods.</returns>
  public static IEnumerable<MethodInfo> GetTaggedMethods(Type t)
    => t.GetMethods(BindingFlags.Static | BindingFlags.Public)
      .Where(m => m.GetCustomAttribute<DeserializerAttribute>() != null);
}

/// <summary>
///   Marks a parameter as using long text. A long text parameter gives
///   its deserializer as many words as possible instead of as few.
/// </summary>
/// <remarks>
///   Only the final parameter may be long text, and the initial context
///   parameter of a command may not be long text. Some types may not use
///   long text.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter)]
public class LongTextAttribute : Attribute { }
