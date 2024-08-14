namespace advisor.facts;

using System.Linq;
using Pidgin;

public static class ParserResultEx {
    public static Result<char, T> AssertParsed<T>(this ITestOutputHelper output, Result<char, T> res) {
        if (!res.Success) {
            if (!string.IsNullOrWhiteSpace(res.Error.Message)) {
                output.WriteLine(res.Error.Message);
            }

            if (res.Error.EOF) {
                output.WriteLine("Reached EOF");
            }

            var unex = res.Error.Unexpected;
            var pos = res.Error.ErrorPos;

            output.WriteLine($"Error at (Ln {pos.Line}, Col {pos.Col})");

            foreach (var e in res.Error.Expected) {
                var tokens = e.Tokens;
                var expected = tokens.Any()
                    ? string.Join("", tokens)
                    : e.Label;

                output.WriteLine($"Expected \"{expected}\"");
            }

            if (unex.HasValue) {
                output.WriteLine($"Found \"{unex.Value}\"");
            }
        }

        Assert.True(res.Success);

        return res;
    }
}
