using System.Reflection;

namespace Nixill.Twitch.Interactions;

/// <summary>
///   Marks a public, static method as being a reaction to a channel
///   points reward for this twitch chat bot.
/// </summary>
/// <remarks>
///   Multiple rewards cannot have the same name. If you attempt to add
///   multiple rewards of the same name, the newer overrides the older. If
///   you attempt to add a reward of the same name as a reward not
///   controlled by your client app, the request fails.
///   <para/>
///   The tagged method must be <see langword="static"/> and <see langword="public"/>.
///   It must have at least one parameter, and its first parameter must be
///   a <see cref="RewardContext"/> or a less derived type. All other
///   parameters must be types with a static <c>Parse(string)</c> method
///   or be returned by a method tagged with <see cref="DeserializerAttribute"/>
///   (or be a nullable or enumerable wrapper of such a type). The method
///   must return <see cref="Task"/> or <see cref="Task{T}"/> of
///   <see cref="JoltInteractionResponse"/>.
///   <para/>
///   Methods that don't meet all of the conditions in the foregoing
///   paragraph are skipped as commands when adding by attribute or type,
///   and cause an exception when adding directly.
///   <para/>
///   The reward on Twitch is initially created with the given name. If
///   the developer changes the name of the reward on Twitch, the name
///   must also be changed in the code, unless UUIDs are saved and loaded
///   dynamically.
///   <para/>
///   If the tagged method has only the context parameter, the redemption
///   is created without text entry. If the tagged method has more
///   parameters, the redemption is created with text entry. If the
///   redemption already exists, whether or not it has text entry does not
///   change, but if a no-text-entry redemption is redeemed and the linked
///   method has parameters, an exception will be thrown dispatching it.
///   <para/>
///   If the user of the bot is not affiliated or partnered, the channel
///   points reward methods will do nothing. (However, if one is also
///   tagged as a command, then the method will be usable via that
///   command.)
/// </remarks>
/// <param name="name">
///   The name of the channel points reward.
/// </param>
/// <param name="uuid">
///   The uuid of the channel points reward.
/// </param>
[AttributeUsage(AttributeTargets.Method)]
public class ChannelPointsRewardAttribute(string name) : Attribute
{
  /// <summary>
  ///   Get: The name of the channel points reward.
  /// </summary>
  public readonly string Name = name;

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
      .Where(m => m.GetCustomAttribute<ChannelPointsRewardAttribute>() != null);
}
