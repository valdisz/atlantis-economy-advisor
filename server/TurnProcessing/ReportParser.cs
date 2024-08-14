namespace advisor.TurnProcessing;

using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;
using advisor.Model;
using Newtonsoft.Json;

public interface IReportParser {
    JReport Read(TextReader reader);
    Task<JReport> ToReportAsync(TextReader reader);
    Task<JObject> ToJsonAsync(TextReader reader);
    Task<string> ToJsonStringAsync(TextReader reader);
}

public class ReportParser : IReportParser {
    private readonly JsonSerializer serializer = JsonSerializer.CreateDefault();

    public JReport Read(TextReader reader) {
        using var jsonReader = new JsonTextReader(reader);
        JReport report = serializer.Deserialize<JReport>(jsonReader);
        return report;
    }

    public async Task<JReport> ToReportAsync(TextReader reader) {
        using var atlantisReader = new AtlantisReportJsonConverter(reader);
        var report = await atlantisReader.ReadAs<JReport>();

        return report;
    }

    public async Task<JObject> ToJsonAsync(TextReader reader) {
        using var atlantisReader = new AtlantisReportJsonConverter(reader);
        var json = await atlantisReader.ReadAsJsonAsync();

        return json;
    }

    public async Task<string> ToJsonStringAsync(TextReader reader) {
        var json = await ToJsonAsync(reader);
        return json.ToString(Formatting.None);
    }
}
