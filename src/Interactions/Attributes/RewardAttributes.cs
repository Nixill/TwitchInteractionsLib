using System.Reflection;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;

namespace Nixill.Twitch.Interactions.Attributes;

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
///   The reward on Twitch is initially created with the given name. When
///   the bot is reloaded, if the bot does not dynamically save and load
///   UUIDs, the name must match either this name or a name in a
///   <see cref="RewardModifierAttribute"/> to match an existing reward.
///   <para/>
///   Whether or not the channel points reward is created with text entry
///   is dependent on whether or not there are additional parameters on
///   the tagged method. The initial name, description, and cost of the
///   reward are set from the properties of the attribute. When linking to
///   an existing reward, no changes are made.
///   <para/>
///   If the user of the bot is not affiliated or partnered, the channel
///   points reward methods will do nothing. (However, if one is also
///   tagged as a command, then the method will be usable via that
///   command.)
/// </remarks>
/// <param name="id">
///   The internal ID of the channel points reward, usable in other
///   methods.
/// </param>
/// <param name="initialName">
///   The initial name of the channel points reward.
/// </param>
/// <param name="initialDescription">
///   The initial description of the channel points reward.
/// </param>
/// <param name="initialCost">
///   The initial cost of the channel points reward.
/// </param>
[AttributeUsage(AttributeTargets.Method)]
public class ChannelPointsRewardAttribute(string id, string initialName, string initialDescription, int initialCost)
  : Attribute
{
  /// <summary>
  ///   Get: The internal ID of the channel points reward, which can be
  ///   passed to certain other methods in this library.
  /// </summary>
  public readonly string ID = id;

  /// <summary>
  ///   Get: The initial name of the channel points reward.
  /// </summary>
  public readonly string Name = initialName;

  /// <summary>
  ///   Get: The initial description of the channel points reward.
  /// </summary>
  public readonly string Description = initialDescription;

  /// <summary>
  ///   Get: The initial channel points cost of the channel points reward.
  /// </summary>
  public readonly int Cost = initialCost;

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

/// <summary>
///   Modifies a channel points reward when a condition is met.
/// </summary>
/// <param name="order">
///   The order in which modifications should be applied. Lower numbers
///   are applied first.
/// </param>
/// <param name="stopIfApplicable">
///   If <see langword="true"/>, stop applying modifications if this one
///   is applied.
/// </param>
[AttributeUsage(AttributeTargets.Method)]
public abstract class RewardModifierAttribute(int order) : Attribute
{
  /// <summary>
  ///   Get: The order in which modifications should be applied. Lower
  ///   numbers are applied first.
  /// </summary>
  /// <remarks>
  ///   During a channel information update, <see cref="RewardModifierAttribute"/>s
  ///   and <see cref="LimitAttribute"/>s are iterated simultaneously
  ///   based on their Order.
  /// </remarks>
  public readonly int Order = order;

  /// <summary>
  ///   Get or Init: If not null, changes the name of this channel points
  ///   reward.
  /// </summary>
  public string? Name { get; init; } = null;

  /// <summary>
  ///   Get or Init: If not null, changes the price of this channel points
  ///   reward.
  /// </summary>
  public int? Price { get; init; } = null;

  /// <summary>
  ///   Get or Init: If not null, changes the description of this channel
  ///   points reward.
  /// </summary>
  public string? Description { get; init; } = null;

  /// <summary>
  ///   Get or Init: If <see langword="false"/>, disables this channel
  ///   points reward. If <see langword="true"/>, enables this channel
  ///   points reward. If null, no change is made.
  /// </summary>
  public bool? EnableState { get; init; } = null;

  /// <summary>
  ///   Get or Init: If true, stops evaluating further modifications if
  ///   this one applies.
  /// </summary>
  public bool StopIfApplicable { get; init; } = false;

  /// <summary>
  ///   Determines whether or not the condition in this modification is
  ///   applicable.
  /// </summary>
  /// <param name="info">Current channel info.</param>
  /// <returns>
  ///   <see langword="true"/> iff the condition is met, <see langword="false"/>
  ///   otherwise.
  /// </returns>
  public abstract bool IsApplicable(ChannelInformation info);
}

/// <summary>
///   Always modifies a channel points reward.
/// </summary>
/// <param name="order">
///   The order in which modifications should be applied. Lower numbers
///   are applied first.
/// </param>
public class DefaultRewardModifierAttribute(int order) : RewardModifierAttribute(order)
{
  /// <summary>
  ///   Always returns <see langword="true"/>.
  /// </summary>
  /// <param name="info">This parameter is ignored.</param>
  /// <returns><see langword="true"/>.</returns>
  public override bool IsApplicable(ChannelInformation info) => true;
}

/// <summary>
///   Modifies a channel points reward when the current category on the
///   channel (the game name) matches a given pattern.
/// </summary>
/// <param name="order">
///   The order in which modifications should be applied. Lower numbers
///   are applied first.
/// </param>
/// <param name="pattern">The pattern to search for.</param>
/// <param name="wholeMatchOnly">
///   If <see langword="false"/> or unspecified, the condition is met when
///   the game name contains <paramref name="pattern"/>. If <see langword="true"/>,
///   the condition is met when the game name equals <paramref name="pattern"/>.
/// </param>
/// <param name="comp">The comparison to use.</param>
public class ModifyRewardWhenGameIsAttribute(int order, string pattern, bool wholeMatchOnly = false,
  StringComparison comp = StringComparison.CurrentCultureIgnoreCase) : RewardModifierAttribute(order)
{
  /// <summary>
  ///   Get or init: The pattern to search for.
  /// </summary>
  public string Pattern { get; init; } = pattern;

  /// <summary>
  ///   Get or init: If <see langword="false"/> or unspecified, the
  ///   condition is met when the game name contains <paramref name="pattern"/>.
  ///   If <see langword="true"/>, the condition is met when the game name
  ///   equals <paramref name="pattern"/>.
  /// </summary>
  public bool WholeMatchOnly { get; init; } = wholeMatchOnly;

  /// <summary>
  ///   Get or init: The string comparison to use.
  /// </summary>
  public StringComparison Comparer { get; init; } = comp;

  /// <summary>
  ///   Determines if the game name is matched.
  /// </summary>
  /// <inheritdoc/>
  public override bool IsApplicable(ChannelInformation info)
  {
    string gameName = info.GameName;

    if (WholeMatchOnly) return gameName.Equals(Pattern, Comparer);
    else return gameName.Contains(Pattern, Comparer);
  }
}

/// <summary>
///   Modifies a channel points reward when the current title on the
///   channel matches a given pattern.
/// </summary>
/// <param name="order">
///   The order in which modifications should be applied. Lower numbers
///   are applied first.
/// </param>
/// <param name="pattern">The pattern to search for.</param>
/// <param name="wholeSectionMatchOnly">
///   If <see langword="false"/> or unspecified, the condition is met when
///   the title contains <paramref name="pattern"/>. If <see langword="true"/>,
///   the condition is met when a whole section of the title equals
///   <paramref name="pattern"/>.
/// </param>
/// <param name="sectionSeparator">
///   If <paramref name="wholeSectionMatchOnly"/> is <see langword="true"/>,
///   this specifies the separator for sections of the title. If blank,
///   the title is treated as a single segment, and the whole title must
///   match for the condition to pass. This parameter is ignored if
///   <paramref name="wholeSectionMatchOnly"/> is <see langword="false"/>.
/// </param>
/// <param name="comp">The comparison to use.</param>
public class ModifyRewardWhenTitleContainsAttribute(int order, string pattern, bool wholeSectionMatchOnly = false,
  string sectionSeparator = "", StringComparison comp = StringComparison.CurrentCultureIgnoreCase)
  : RewardModifierAttribute(order)
{
  /// <summary>
  ///   Get or init: The pattern to search for.
  /// </summary>
  public string Pattern { get; init; } = pattern;

  /// <summary>
  ///   Get or init: If <see langword="false"/> or unspecified, the
  ///   condition is met when the game name contains <paramref name="pattern"/>.
  ///   If <see langword="true"/>, the condition is met when the game name
  ///   equals <paramref name="pattern"/>.
  /// </summary>
  public bool WholeSectionMatchOnly { get; init; } = wholeSectionMatchOnly;

  /// <summary>
  ///   Get or init: If <paramref name="wholeSectionMatchOnly"/> is <see langword="true"/>,
  ///   this specifies the separator for sections of the title. If blank,
  ///   the title is treated as a single segment, and the whole title must
  ///   match for the condition to pass. This property is ignored if
  ///   <paramref name="wholeSectionMatchOnly"/> is <see langword="false"/>.
  /// </summary>
  public string SectionSeparator { get; init; } = sectionSeparator;

  /// <summary>
  ///   Get or init: The string comparison to use.
  /// </summary>
  public StringComparison Comparer { get; init; } = comp;

  /// <summary>
  ///   Determines if the title is matched.
  /// </summary>
  /// <inheritdoc/>
  public override bool IsApplicable(ChannelInformation info)
  {
    string title = info.Title;

    if (!WholeSectionMatchOnly) return title.Contains(Pattern, Comparer);

    string[] sections = (SectionSeparator == "") ? [title] : title.Split(SectionSeparator);

    return sections.Any(s => s.Equals(Pattern, Comparer));
  }
}

/// <summary>
///   Modifies a channel points reward when one of the tags present on the
///   channel matches a given pattern.
/// </summary>
/// <param name="order">
///   The order in which modifications should be applied. Lower numbers
///   are applied first.
/// </param>
/// <param name="pattern">The pattern to search for.</param>
/// <param name="wholeMatchOnly">
///   If <see langword="false"/> or unspecified, the condition is met when
///   any tag contains <paramref name="pattern"/>. If <see langword="true"/>,
///   the condition is met when any tag equals <paramref name="pattern"/>.
/// </param>
/// <param name="comp">The comparison to use.</param>
public class ModifyRewardWhenTagPresentAttribute(int order, string pattern, bool wholeMatchOnly,
  StringComparison comp = StringComparison.CurrentCultureIgnoreCase) : RewardModifierAttribute(order)
{
  /// <summary>
  ///   Get or init: The pattern to search for.
  /// </summary>
  public string Pattern { get; init; } = pattern;

  /// <summary>
  ///   Get or init: If <see langword="false"/> or unspecified, the
  ///   condition is met when the game name contains <paramref name="pattern"/>.
  ///   If <see langword="true"/>, the condition is met when the game name
  ///   equals <paramref name="pattern"/>.
  /// </summary>
  public bool WholeMatchOnly { get; init; } = wholeMatchOnly;

  /// <summary>
  ///   Get or init: The string comparison to use.
  /// </summary>
  public StringComparison Comparer { get; init; } = comp;

  /// <summary>
  ///   Determines if a tag is matched.
  /// </summary>
  /// <inheritdoc/>
  public override bool IsApplicable(ChannelInformation info)
    => WholeMatchOnly
      ? info.Tags.Any(x => x.Equals(Pattern, Comparer))
      : info.Tags.Any(x => x.Contains(Pattern, Comparer));
}