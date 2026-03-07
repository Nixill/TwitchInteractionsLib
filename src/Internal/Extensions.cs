using System.Reflection;

namespace Nixill.Extensions.TwitchInteractionsLib
{
  namespace PopExtension
  {
    public static class PopExtension
    {
      public static T Pop<T>(this IList<T> list)
      {
        T output = list[0];
        list.RemoveAt(0);
        return output;
      }
    }
  }

  namespace WhereNotExtension
  {
    public static class WhereNotExtension
    {
      public static IEnumerable<T> WhereNot<T>(this IEnumerable<T> input, Func<T, bool> condition)
        => input.Where(i => !condition(i));
    }
  }

  namespace Internal
  {
    internal static class InternalExtensions
    {
      internal static Visibility GetVisibility(this MethodInfo mi)
      {
        if (mi.IsPublic) return Visibility.Public;
        if (mi.IsPrivate) return Visibility.Private;
        if (mi.IsAssembly) return Visibility.Internal;
        if (mi.IsFamily) return Visibility.Protected;
        if (mi.IsFamilyAndAssembly) return Visibility.InternalAndProtected;
        if (mi.IsFamilyOrAssembly) return Visibility.ProtectedOrInternal;
        return Visibility.None;
      }
    }

    internal enum Visibility
    {
      None = 0,
      Public = 1,
      Private = 2,
      Internal = 3,
      Protected = 4,
      ProtectedOrInternal = 5,
      InternalAndProtected = 6
    }
  }
}