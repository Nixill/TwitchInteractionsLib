namespace Nixill.Twitch.Interactions;

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
  public string? UserDisplayName { get; protected init; }

  /// <summary>
  ///   Get: The ID of the user that triggered an interaction. Is <see langword="null"/>
  ///   for interactions that aren't user-triggered.
  /// </summary>
  public string? UserID { get; protected init; }

  /// <summary>
  ///   Get: The login username of the user that triggered the
  ///   interaction. Is <see langword="null"/> for interactions that
  ///   aren't user-triggered.
  /// </summary>
  public string? UserLogin { get; protected init; }

  /// <summary>
  ///   Get: The message associated with the interaction. Is <see langword="null"/>
  ///   for interactions that don't have an associated message.
  /// </summary>
  public string? Message { get; protected init; }

  /// <summary>
  ///   Get or init: The type of this execution context as far as limits
  ///   are concerned.
  /// </summary>
  protected internal LimitTarget LimitedAs { get; protected init; }

  /// <summary>
  ///   Get: The function that lets a method reply to the sender.
  /// </summary>
  public Func<string, Task> ReplyAsync { get; protected init; } = async a => { };

  /// <summary>
  ///   Get: The function that lets a method reply to the sender.
  /// </summary>
  public Func<string, Task> MessageAsync { get; protected init; } = async a => { };
}