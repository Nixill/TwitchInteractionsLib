namespace Nixill.Twitch.Interactions.Objects.Common;

public readonly record struct InteractionParameter(
  string Name,
  Type Type,
  bool IsLongText,
  bool IsOptional,
  object? DefaultValue
)
{
  public object Deserialize(IList<string> input) => Deserializers.Deserialize(Type, input, IsLongText);
}