using System.Reflection;
using Nixill.Extensions.TwitchInteractionsLib.Internal;
using Nixill.Extensions.TwitchInteractionsLib.WhereNotExtension;
using Nixill.Twitch.Interactions.Attributes;
using Nixill.Twitch.Interactions.Objects.Common;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
using TwitchLib.Api.Helix.Models.Channels.SendChatMessage;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Core.SubscriptionTypes.Channel;

namespace Nixill.Twitch.Interactions.Objects.Commands;

/// <summary>
///   A <see cref="ChannelConnector"/> module that takes chat events and
///   dispatches commands from them as applicable.
/// </summary>
public class CommandDispatchModule
{
  /// <summary>
  ///   The prefixes with which commands may be used on this bot.
  /// </summary>
  private readonly string[] PrefixOptions;

  /// <summary>
  ///   The <see cref="ChannelConnector"/> that spawned this
  ///   CommandDispatchModule.
  /// </summary>
  private readonly ChannelConnector Connector;

  /// <summary>
  ///   Get: The actual collection of chat commands in this dispatcher.
  /// </summary>
  private readonly Dictionary<string, ChatCommand> CodeCommands = new(StringComparer.InvariantCultureIgnoreCase);

  /// <summary>
  ///   Get: The collection of simple commands in this dispatcher.
  /// </summary>
  private readonly Dictionary<string, SimpleCommand> SimpleCommands = new(StringComparer.InvariantCultureIgnoreCase);

  /// <summary>
  ///   The number of words in the longest command name.
  /// </summary>
  public int LongestCommandName { get; private set; }

  /// <summary>
  ///   Constructor.
  /// </summary>
  /// <param name="prefixOptions">Allowed prefixes.</param>
  /// <param name="channelConnector">Channel connector.</param>
  internal CommandDispatchModule(string[] prefixOptions, ChannelConnector channelConnector)
  {
    PrefixOptions = prefixOptions;
    Connector = channelConnector;
  }

  /// <summary>
  ///   Attempts to register all the chat command methods in an assembly.
  /// </summary>
  /// <remarks>
  ///   Only <see langword="public"/> <see langword="static"/> methods
  ///   within <see langword="public"/> types are considered. All others
  ///   are ignored.
  /// </remarks>
  /// <param name="asm">The assembly.</param>
  /// <returns>The result of registration.</returns>
  public CommandRegistrationResult RegisterCodeCommands(Assembly asm)
  {
    List<ChatCommand> successes = [];
    List<FailedCommand> failures = [];

    foreach (Type t in asm.GetTypes())
    {
      if (!t.IsPublic) continue;

      var result = RegisterCodeCommands(t);
      successes.AddRange(result.Successes);
      failures.AddRange(result.Failures);
    }

    return new CommandRegistrationResult(successes, failures);
  }

  /// <summary>
  ///   Attempts to register all the chat command methods in a type.
  /// </summary>
  /// <remarks>
  ///   Only <see langword="public"/> <see langword="static"/> methods
  ///   are considered. All others are ignored.
  /// </remarks>
  /// <param name="t">The type.</param>
  /// <returns>The result of registration.</returns>
  public CommandRegistrationResult RegisterCodeCommands(Type t)
  {
    List<ChatCommand> successes = [];
    List<FailedCommand> failures = [];

    foreach (MethodInfo method in t.GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Where(m => m.GetCustomAttribute<CommandAttribute>() is not null))
    {
      try
      {
        successes.Add(RegisterCodeCommand(method));
      }
      catch (IllegalCommandException ex)
      {
        failures.Add(new(method, ex));
      }
    }

    return new CommandRegistrationResult(successes, failures);
  }

  /// <summary>
  ///   Attempts to register a method as a chat command.
  /// </summary>
  /// <param name="method">The method.</param>
  /// <returns>The ChatCommand made from that method.</returns>
  /// <exception cref="IllegalCommandException">
  ///   The method could not be registered as a chat command.
  /// </exception>
  public ChatCommand RegisterCodeCommand(MethodInfo method)
  {
    if (!method.IsPublic)
      throw new IllegalCommandException(method,
        $"Only public methods may be commands. (Actual: {method.GetVisibility()})");

    if (!method.IsStatic)
      throw new IllegalCommandException(method, "Only static methods may be commands.");

    CommandAttribute attr = method.GetCustomAttribute<CommandAttribute>()
      ?? throw new IllegalCommandException(method, "Only methods tagged with [Command] may be commands.");

    var pars = method.GetParameters();

    if (pars.Length == 0)
      throw new IllegalCommandException(method, "It must have at least one parameter. (Actual: 0)");

    if (method.ReturnType != typeof(Task) && method.ReturnType != typeof(Task<InteractionResponse>))
      throw new IllegalCommandException(method,
        $"It must return Task or Task<InteractionResponse>. (Actual: {method.ReturnType})");

    if (!pars[0].ParameterType.IsAssignableFrom(typeof(CommandContext)))
      throw new IllegalCommandException(method,
        $"Its first parameter must be CommandContext (or a less derived type). (Actual: {pars[0].ParameterType}).");

    var unusableTypes = pars.Skip(1).Select(p => p.ParameterType).WhereNot(Deserializers.CanDeserialize);
    if (unusableTypes.Any())
      throw new IllegalCommandException(method,
        $"No deserializer(s) exist for the following types: {string.Join(", ", unusableTypes)}");

    if (pars[0].GetCustomAttribute<LongTextAttribute>() is not null)
      throw new IllegalCommandException(method, $"The {pars[0].ParameterType} may not be [LongText].");

    if (pars.SkipLast(1).Any(p => p.GetCustomAttribute<LongTextAttribute>() is not null))
      throw new IllegalCommandException(method, $"Only the final parameter of a command may be [LongText].");

    if (pars.SkipLast(1).Select(p => p.ParameterType).Any(Deserializers.IsEnumerableType))
      throw new IllegalCommandException(method, $"Only the final parameter of a command may be [LongText]."
        + " (Arrays and IEnumerable<> are automatically [LongText].)");

    // post condition checks
    List<InteractionParameter> iPars = [];

    foreach (ParameterInfo p in pars.Skip(1))
    {
      iPars.Add(new(
        Name: p.Name!,
        Type: p.ParameterType,
        IsLongText: p.GetCustomAttribute<LongTextAttribute>() is not null,
        IsOptional: p.HasDefaultValue || (Nullable.GetUnderlyingType(p.ParameterType) is not null),
        DefaultValue: p.HasDefaultValue ? p.DefaultValue : null
      ));
    }

    ChatCommand command = new(
      Name: attr.Name,
      Aliases: attr.Aliases,
      Parameters: [.. iPars],
      Restrictions: [.. method.GetCustomAttributes<LimitAttribute>()],
      Method: method
    );

    LongestCommandName = Math.Max(LongestCommandName,
      attr.Aliases.Prepend(attr.Name).Max(x => x.Count(c => c == ' ' + 1)));

    CodeCommands[attr.Name] = command;

    foreach (string alias in attr.Aliases)
    {
      CodeCommands.TryAdd(alias, command);
    }

    return command;
  }

  /// <summary>
  ///   Adds multiple <see cref="SimpleCommand"/>s from an enumerable source.
  /// </summary>
  /// <param name="commands">The commands.</param>
  public void AddSimpleCommands(IEnumerable<SimpleCommand> commands)
  {
    foreach (SimpleCommand cmd in commands)
    {
      SimpleCommands[cmd.CommandName] = cmd;
    }
  }

  /// <summary>
  ///   Adds a single <see cref="SimpleCommand"/>.
  /// </summary>
  /// <param name="cmd">The command.</param>
  public void AddSimpleCommand(SimpleCommand cmd)
  {
    SimpleCommands[cmd.CommandName] = cmd;
  }

  /// <summary>
  ///   Removes a single <see cref="SimpleCommand"/>.
  /// </summary>
  /// <param name="name">The command.</param>
  public void RemoveSimpleCommand(string name)
  {
    SimpleCommands.Remove(name);
  }

  /// <summary>
  ///   Checks whether a given <see cref="SimpleCommand"/> exists.
  /// </summary>
  /// <param name="name">The name.</param>
  /// <returns>Whether or not that command exists.</returns>
  public bool HasSimpleCommand(string name) => (!CodeCommands.ContainsKey(name)) && SimpleCommands.ContainsKey(name);

  /// <summary>
  ///   Gets all simple commands registered to this CommandDispatchModule.
  /// </summary>
  /// <returns>The sequence of simple commands.</returns>
  public IEnumerable<SimpleCommand> GetSimpleCommands() => SimpleCommands.Values;

  /// <summary>
  ///   Checks a chat message for commands, dispatching them if necessary.
  /// </summary>
  /// <param name="sender">(Ignored.)</param>
  /// <param name="e">
  ///   The channel chat message event being reacted to.
  /// </param>
  /// <returns>(Task, void.)</returns>
  internal async Task CheckChatMessage(object? sender, ChannelChatMessageArgs e)
  {
    ChannelChatMessage ev = e.Payload.Event;
    string text = ev.Message.Text;

    string? prefix = PrefixOptions.FirstOrDefault(p => text.StartsWith(p, StringComparison.InvariantCultureIgnoreCase));

    // no command prefix found
    if (prefix is null) return;

    string message = text[prefix.Length..];
    string[] words = message.Split(' ');

    int wordsCount = Math.Min(words.Length, LongestCommandName);
    List<string> commandNameWords = [.. words[..wordsCount]];
    List<string> commandParamWords = [.. words[wordsCount..]];

    while (commandNameWords.Count > 0)
    {
      string commandName = string.Join(" ", commandNameWords);

      if (CodeCommands.TryGetValue(commandName, out var cmd))
      {
        await DispatchCodeCommand(commandParamWords, ev, commandName, cmd);
        return;
      }
      else if (SimpleCommands.TryGetValue(commandName, out var scmd))
      {
        await DispatchSimpleCommand(scmd, ev);
        return;
      }

      string word = commandNameWords[^1];
      commandNameWords.RemoveAt(commandNameWords.Count - 1);
      commandParamWords.Insert(0, word);
    }
  }

  internal async Task DispatchCodeCommand(List<string> paramWords, ChannelChatMessage ev, string commandName,
    ChatCommand cmd)
  {
    CommandContext ctx = new(ev, this);
    bool allowed = true;
    string? failMessage = null;

    foreach (var limit in cmd.Restrictions)
    {
      bool? result = await limit.PassesCondition(ctx, Connector);
    }
  }

  public Task SendChatMessage(string message, bool sourceOnly = false, string? inReplyTo = null)
    => ChannelConnector.APIClient.Helix.Chat.SendChatMessage(new SendChatMessageRequest
    {
      SenderId = Connector.ChatBotUID,
      BroadcasterId = Connector.StreamerUID,
      ForSourceOnly = sourceOnly,
      Message = message,
      ReplyParentMessageId = inReplyTo
    }, Connector.AppToken);

}