namespace advisor;

public static class SetExtensions {
    /// <summary>
    /// Tests if the value is in the set.
    /// </summary>
    public static bool In<T>(this T self, params T[] values) {
        return values?.Length > 0 && System.Array.IndexOf(values, self) >= 0;
    }
}
