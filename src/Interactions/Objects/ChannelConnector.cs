using System.Reflection;
using Nixill.Twitch.Interactions.Attributes;
using Nixill.Twitch.Interactions.Objects.Commands;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
using TwitchLib.EventSub.Websockets;

namespace Nixill.Twitch.Interactions.Objects;

/// <summary>
///   The object that facilitates a connection between the Twitch API and
///   the rest of this library.
/// </summary>
/// <param name="eventSub">An EventSub websocket client.</param>
/// <param name="appToken">The app access token for this app.</param>
/// <param name="streamerToken">
///   The user access token for the broadcaster that will be connected.
/// </param>
/// <param name="streamerUID">
///   The user ID for the broadcaster that will be connected, who is
///   authenticated with the <paramref name="streamerToken"/> and the
///   <paramref name="appToken"/>.
/// </param>
/// <param name="chatBotUID">
///   The user ID for the chat bot that will be connected, who is
///   authenticated with the <paramref name="appToken"/>. If not
///   specified, defaults to the same as <paramref name="streamerUID"/>,
///   though that user is still expected to be authenticated as a chat bot.
/// </param>
public class ChannelConnector(EventSubWebsocketClient eventSub, string appToken, string streamerToken,
  string streamerUID, string? chatBotUID = null)
{
  /// <summary>
  ///   Get: A Twitch API client used internally by the library.
  /// </summary>
  internal static readonly TwitchAPI APIClient = new();

  /// <summary>
  ///   Get: The EventSub websocket client used by this ChannelConnector.
  /// </summary>
  internal readonly EventSubWebsocketClient EventSubClient = eventSub;

  /// <summary>
  ///   Get or set: The app access token used by this ChannelConnector.
  /// </summary>
  internal string AppToken = appToken;

  /// <summary>
  ///   Get or set: The streamer user access token used by this
  ///   ChannelConnector.
  /// </summary>
  internal string StreamerToken = streamerToken;

  /// <summary>
  ///   Get: The UID of the streamer authenticated to this ChannelConnector.
  /// </summary>
  internal readonly string StreamerUID = streamerUID;

  /// <summary>
  ///   Get: The UID of the chat bot authenticated to this ChannelConnector.
  /// </summary>
  internal readonly string ChatBotUID = chatBotUID ?? streamerUID;

  /// <summary>
  ///   Get or set: The <see cref="CommandDispatchModule"/> associated
  ///   with this ChannelConnector.
  /// </summary>
  internal CommandDispatchModule? Commands;

  /// <summary>
  ///   Updates the app token of this ChannelConnector.
  /// </summary>
  /// <param name="newToken">The new token to use.</param>
  public void UpdateAppToken(string newToken) => AppToken = newToken;

  /// <summary>
  ///   Updates the streamer token of this ChannelConnector.
  /// </summary>
  /// <param name="newToken">The new token to use.</param>
  public void UpdateStreamerToken(string newToken) => StreamerToken = newToken;

  /// <summary>
  ///   Enables chat command support for this ChannelConnector, creating a
  ///   <see cref="CommandDispatchModule"/> to do so.
  /// </summary>
  /// <remarks>
  ///   This method by itself does not add any commands to the dispatcher.
  ///   You must call one of its AddCommands methods.
  ///   <para/>
  ///   Only one <see cref="CommandDispatchModule"/> may be created per
  ///   ChannelConnector. If this method is reused, the existing
  ///   CommandDispatchModule is returned, without modification.
  ///   <para/>
  ///   To receive chat commands, the EventSub websocket client in this
  ///   ChannelConnector must be subscribed to <c>channel.chat.message</c>
  ///   events, which requires the chat bot user to be authenticated to
  ///   the app with <c>user:read:chat</c> and <c>user:bot</c> scopes, and
  ///   either the chat bot user must be a moderator in the streamer's
  ///   chat, or the streamer must be authenticated to the app with the
  ///   <c>channel:bot</c> scope.
  ///   <para/>
  ///   To send replies or followup messages, either via the
  ///   <see cref="CommandContext"/> or the return value of the methods,
  ///   the chat bot user must also be authenticated to the app with the
  ///   <c>user:write:chat</c> scope.
  /// </remarks>
  /// <param name="prefixes">
  ///   One or more valid prefixes for chat commands.
  /// </param>
  /// <returns>
  ///   The <see cref="CommandDispatchModule"/> associated with this
  ///   ChannelConnector's command support.
  /// </returns>
  public CommandDispatchModule EnableCommands(params IEnumerable<string> prefixes)
  {
    if (Commands != null) return Commands;

    string[] prefixOptions = [.. prefixes];

    Commands = new(prefixOptions, this);

    EventSubClient.ChannelChatMessage += Commands.CheckChatMessage;

    return Commands;
  }

  /// <summary>
  ///   Get or set: The cached channel information.
  /// </summary>
  ChannelInformation? ChannelInfo;

  /// <summary>
  ///   Get or set: The time at which the channel information was last
  ///   updated.
  /// </summary>
  DateTime ChannelInfoUpdated = DateTime.MinValue;

  /// <summary>
  ///   Gets the channel information, updating it if the cache is stale or
  ///   nonexistant.
  /// </summary>
  /// <returns>The channel information.</returns>
  internal async Task<ChannelInformation> GetChannelInformation()
  {
    if (ChannelInfo is null || DateTime.UtcNow > (ChannelInfoUpdated + TimeSpan.FromHours(1)))
    {
      ChannelInfo = (await APIClient.Helix.Channels.GetChannelInformationAsync(StreamerUID, AppToken))
        .Data.First();
      ChannelInfoUpdated = DateTime.UtcNow;
    }

    return ChannelInfo;
  }
}