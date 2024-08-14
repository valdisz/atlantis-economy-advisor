namespace advisor.facts;

using advisor.Persistence;

public class CompressionValueConversionSpec {
    [Fact]
    public void CompressionReducesDataSize() {
        var data = new byte[1000];
        Array.Fill<byte>(data, 1);

        var output = CompressionValueConversion.Compress(data);

        output.Length.Should().BeGreaterThan(0).And.BeLessThan(data.Length);
    }

    [Fact]
    public void EmptyCompressInputProducesEmptyOutput() {
        var data = Array.Empty<byte>();

        var output = CompressionValueConversion.Compress(data);

        output.Length.Should().Be(0);
    }

    [Fact]
    public void EmptyDecompressInputProducesEmptyOutput() {
        var data = Array.Empty<byte>();

        var output = CompressionValueConversion.Decompress(data);

        output.Length.Should().Be(0);
    }
}
