namespace Neodroid.Utilities.Unsorted {
  /// <summary>
  /// 
  /// </summary>
  /// <typeparam name="T1"></typeparam>
  /// <typeparam name="T2"></typeparam>
  public class Pair<T1, T2> {
    public Pair(T1 first, T2 second) {
      this.First = first;
      this.Second = second;
    }

    /// <summary>
    /// 
    /// </summary>
    public T1 First { get; }

    /// <summary>
    /// 
    /// </summary>
    public T2 Second { get; }
  }

  /// <summary>
  /// 
  /// </summary>
  public static class Pair {
    /// <summary>
    /// 
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <returns></returns>
    public static Pair<T1, T2> New<T1, T2>(T1 first, T2 second) {
      var tuple = new Pair<T1, T2>(first, second);
      return tuple;
    }
  }
}
