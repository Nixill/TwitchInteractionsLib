namespace Nixill.Extensions.TwitchInteractionsLib.PopExtension;

public static class InternalExtensions
{
  public static T Pop<T>(this IList<T> list)
  {
    T output = list[0];
    list.RemoveAt(0);
    return output;
  }
}