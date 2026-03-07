using Nixill.Twitch.Interactions.Objects;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;

namespace Nixill.Twitch.Interactions.Attributes;

/// <summary>
///   Limits the usage of a command or channel points reward method based
///   on stream or user information.
/// </summary>
/// <param name="order">
///   The order in which modifications should be applied. Lower numbers
///   are applied first.
/// </param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public abstract class LimitAttribute(int order) : Attribute
{
  /// <summary>
  ///   Get: The order in which modifications should be applied. Lower
  ///   numbers are applied first.
  /// </summary>
  public readonly int Order = order;

  /// <summary>
  ///   Get or init: The types of execution context to which this limit
  ///   applies. May be further limited by the type of limit in question.
  /// </summary>
  public LimitTarget AppliesTo { get; init; } = LimitTarget.All;

  /// <summary>
  ///   Get or init: The types of execution context to which this type of
  ///   limit may apply.
  /// </summary>
  protected LimitTarget TypeAppliesTo { get; init; } = LimitTarget.All;

  /// <summary>
  ///   Get or init: The text to be displayed when execution fails, if
  ///   it's possible to send a message to this type of execution context.
  /// </summary>
  public string? FailWarning { get; init; } = null;

  /// <summary>
  ///   Get or init: Whether or not the condition should be inverted (the
  ///   limit passes when the condition does not pass).
  /// </summary>
  public bool Invert { get; init; } = false;

  /// <summary>
  ///   Get or init: Whether or not to stop checking other limits when
  ///   this limit does not pass.
  /// </summary>
  public bool StopOnDeny { get; init; } = true;

  /// <summary>
  ///   Get or init: Whether or not to stop checking other limits when
  ///   this limit passes.
  /// </summary>
  public bool StopOnAllow { get; init; } = false;

  /// <summary>
  ///   Determines whether or not a limit passes.
  /// </summary>
  /// <param name="ctx">The execution context.</param>
  /// <param name="ctor">
  ///   The channel connector for which the limit's being checked.
  /// </param>
  /// <returns>
  ///   <see langword="null"/> if the limit does not apply to the given
  ///   execution context, <see langword="true"/> if the limit passes, or
  ///   <see langword="false"/> if the limit does not pass.
  /// </returns>
  public async Task<bool?> PassesCondition(InteractionContext ctx, ChannelConnector ctor)
  {
    if (!AppliesTo.HasFlag(ctx.LimitedAs)) return null;
    return await ConditionCheck(ctx, ctor) != Invert;
  }

  /// <summary>
  ///   Determines whether or not a limit passes.
  /// </summary>
  /// <param name="ctx">The execution context.</param>
  /// <param name="ctor">
  ///   The channel connector for which the limit's being checked.
  /// </param>
  /// <returns>
  ///   <see langword="true"/> if the limit passes, or <see langword="false"/>
  ///   if the limit does not pass.
  /// </returns>
  protected abstract Task<bool> ConditionCheck(InteractionContext ctx, ChannelConnector ctor);
}

/// <summary>
///   Valid targets for a <see cref="LimitAttribute"/>.
/// </summary>
[Flags]
public enum LimitTarget
{
  /// <summary>
  ///   The limit applies to chat commands (checked at the time the
  ///   command is used).
  /// </summary>
  Command = 1,

  /// <summary>
  ///   The limit applies to channel points reward redemptions (checked at
  ///   the time the reward is used).
  /// </summary>
  RewardUsage = 2,

  /// <summary>
  ///   The limit applies to reward pre-checks (checked at the time of a
  ///   stream information update).
  /// </summary>
  RewardPrecheck = 4,

  /// <summary>
  ///   The limit applies to timed messages (checked at the time the
  ///   message attempts to send).
  /// </summary>
  TimedMessage = 8,

  /// <summary>
  ///   The limit applies to internal usage from a streamer user interface
  ///   (such as a stream deck or web UI, checked at the moment of usage).
  /// </summary>
  Internal = 16,

  /// <summary>
  ///   The limit applies to any mode.
  /// </summary>
  All = Command | RewardUsage | RewardPrecheck | TimedMessage | Internal,

  /// <summary>
  ///   The limit applies to most modes, but not Internal.
  /// </summary>
  Common = Command | RewardUsage | RewardPrecheck | TimedMessage,

  /// <summary>
  ///   The limit applies to modes that supply a User (commands and reward
  ///   redemptions).
  /// </summary>
  HasUser = Command | RewardUsage
}