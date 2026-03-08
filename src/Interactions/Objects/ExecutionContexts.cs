using Nixill.Twitch.Interactions.Attributes;
using Nixill.Twitch.Interactions.Objects.Commands;
using TwitchLib.EventSub.Core.Models.Chat;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;

namespace Nixill.Twitch.Interactions.Objects;

/// <summary>
///   Base class for all interaction contexts exposed by this library.
/// </summary>
public abstract class InteractionContext
{
  /// <summary>
  ///   Get: The display name of the user that triggered an interaction.
  ///   Is <see langword="null"/> for interactions that aren't
  ///   user-triggered.
  /// </summary>
  public abstract string? UserDisplayName { get; }

  /// <summary>
  ///   Get: The ID of the user that triggered an interaction. Is <see langword="null"/>
  ///   for interactions that aren't user-triggered.
  /// </summary>
  public abstract string? UserID { get; }

  /// <summary>
  ///   Get: The login username of the user that triggered the
  ///   interaction. Is <see langword="null"/> for interactions that
  ///   aren't user-triggered.
  /// </summary>
  public abstract string? UserLogin { get; }

  /// <summary>
  ///   Get: The message associated with the interaction. Is <see langword="null"/>
  ///   for interactions that don't have an associated message.
  /// </summary>
  public abstract string? Message { get; }

  /// <summary>
  ///   Get or init: The type of this execution context as far as limits
  ///   are concerned.
  /// </summary>
  protected internal abstract LimitTarget LimitedAs { get; }

  /// <summary>
  ///   Replies to the sender of the interaction.
  /// </summary>
  /// <remarks>
  ///   This method is automatically called if the interaction-handling
  ///   method returns an <see cref="InteractionResponse"/> that contains
  ///   a string value.
  /// </remarks>
  /// <param name="message">The message to send.</param>
  /// <returns>(Task, void.)</returns>
  public abstract Task ReplyAsync(string message, bool sourceOnly = true);

  /// <summary>
  ///   Sends a message in response to the interaction, without using any
  ///   reply feature.
  /// </summary>
  /// <remarks>
  ///   This method should do the same as <see cref="ReplyAsync(string)"/>,
  ///   except that if a "reply" feature is used in the former, this
  ///   method does not use that.
  /// </remarks>
  /// <param name="message">The message to send.</param>
  /// <returns>(Task, void.)</returns>
  public abstract Task MessageAsync(string message, bool sourceOnly = true);

  /// <summary>
  ///   Signals that an interaction succeeded.
  /// </summary>
  /// <remarks>
  ///   This method is automatically called if the interaction-handling
  ///   method returns an <see cref="InteractionResponse"/> with its bool
  ///   value set to <see langword="true">.
  /// </remarks>
  /// <returns>(Task, void.)</returns>
  public abstract Task SucceedAsync();

  /// <summary>
  ///   Signals that an interaction failed.
  /// </summary>
  /// <remarks>
  ///   This method is automatically called if the interaction-handling
  ///   method returns an <see cref="InteractionResponse"/> with its bool
  ///   value set to <see langword="false"/>, or if it throws an exception.
  /// </remarks>
  /// <returns>(Task, void.)</returns>
  public abstract Task FailAsync();
}

/// <summary>
///   Interaction context that is passed to chat command handling methods.
/// </summary>
public class CommandContext(ChannelChatMessage msg, CommandDispatchModule module) : InteractionContext
{
  public readonly ChannelChatMessage ChatMessage = msg;

  public readonly CommandDispatchModule CommandDispatchModule = module;

  /// <inheritdoc/>
  public override string? UserDisplayName => ChatMessage.ChatterUserName;

  /// <inheritdoc/>
  public override string? UserID => ChatMessage.ChatterUserId;

  /// <inheritdoc/>
  public override string? UserLogin => ChatMessage.ChatterUserLogin;

  /// <inheritdoc/>
  public override string? Message => ChatMessage.Message.Text;

  /// <inheritdoc/>
  protected internal override LimitTarget LimitedAs => LimitTarget.Command;

  /// <summary>
  ///   Does nothing.
  /// </summary>
  /// <returns>(Task, void.)</returns>
  public override Task FailAsync() => Task.CompletedTask;

  public override Task MessageAsync(string message, bool sourceOnly = true)
    => CommandDispatchModule.SendChatMessage(message, sourceOnly: sourceOnly);

  public override Task ReplyAsync(string message, bool sourceOnly = true)
    => CommandDispatchModule.SendChatMessage(message, sourceOnly: sourceOnly, inReplyTo: ChatMessage.MessageId);

  /// <summary>
  ///   Does nothing.
  /// </summary>
  /// <returns>(Task, void.)</returns>
  public override Task SucceedAsync() => Task.CompletedTask;
}