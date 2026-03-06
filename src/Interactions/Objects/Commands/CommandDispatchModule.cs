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
  private string[] PrefixOptions;

  /// <summary>
  ///   The <see cref="ChannelConnector"/> that spawned this
  ///   CommandDispatchModule.
  /// </summary>
  private ChannelConnector Connector;

  /// <summary>
  ///   Get: The actual collection of chat commands in this dispatcher.
  /// </summary>
  private readonly Dictionary<string, ChatCommand> Commands = [];

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


}