using System.Reflection;
using Nixill.Twitch.Interactions.Attributes;
using Nixill.Twitch.Interactions.Objects.Common;

namespace Nixill.Twitch.Interactions.Objects.Commands;

/// <summary>
///   Represents a chat command.
/// </summary>
/// <param name="Name">The command's primary name.</param>
/// <param name="Aliases">The command's aliases.</param>
/// <param name="Parameters">The command's parameters.</param>
/// <param name="Restrictions">The command's restrictions.</param>
/// <param name="Method">
///   The method that is tagged with this command.
/// </param>
public readonly record struct ChatCommand(
  string Name,
  string[] Aliases,
  InteractionParameter[] Parameters,
  LimitAttribute[] Restrictions,
  MethodInfo Method
);

/// <summary>
///   Represents a failed (invalid) Command function method.
/// </summary>
/// <param name="TaggedMethod">
///   The method tagged with <see cref="CommandAttribute"/> that was
///   not successfully registered.
/// </param>
/// <param name="Reason">
///   The exception that caused this method to not be registered.
/// </param>
public readonly record struct FailedCommand(
  MethodInfo TaggedMethod,
  Exception Reason
);

/// <summary>
///   The output of a registration attempt, listing successful and failed
///   Commands.
/// </summary>
/// <param name="Successes">Successfully registered Commands.</param>
/// <param name="Failures">
///   Unsuccessfully registered Commands.
/// </param>
public readonly record struct CommandRegistrationResult(
  IEnumerable<ChatCommand> Successes,
  IEnumerable<FailedCommand> Failures
);

/// <summary>
///   A simple command with static responses. A future version may support
///   tokenization.
/// </summary>
/// <param name="CommandName">The command name.</param>
/// <param name="Response">The response message.</param>
/// <param name="RequireModerator">
///   If <see langword="true"/>, only moderators may use this command.
/// </param>
/// <param name="RequireTitle">
///   If <see langword="true"/>, the command name (and prefix used) must
///   be present in the stream's title.
/// </param>
/// <param name="RequireTag">
///   If <see langword="true"/>, the command name (without spaces, without
///   the prefix used) must be present as one of the stream's tags.
/// </param>
public readonly record struct SimpleCommand(
  string CommandName,
  string Response,
  bool RequireModerator = false,
  bool RequireTitle = false,
  bool RequireTag = false
);