namespace Nixill.Twitch.Interactions.Objects.Common;

public readonly record struct InteractionResponse(string? ReplyMessage = null, bool? Result = null)
{
  public static implicit operator InteractionResponse(string input) => new(ReplyMessage: input);
  public static implicit operator InteractionResponse(bool result) => new(Result: result);
  public static implicit operator InteractionResponse((string message, bool result) input)
    => new(input.message, input.result);
  public static implicit operator InteractionResponse((bool result, string message) input)
    => new(input.message, input.result);
}