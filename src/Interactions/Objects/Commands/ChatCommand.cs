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