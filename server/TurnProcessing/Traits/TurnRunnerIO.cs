namespace advisor.TurnProcessing.Traits;

using System.Text.RegularExpressions;
using advisor.Model;

public interface TurnRunnerIO {
    string WorkingDirectory { get; }
    string EngineFileName { get; }
    string PlayersInFileName { get; }
    string PlayersOutFileName { get; }
    string GameInFileName { get; }
    string GameOutFileName { get; }
    Regex ReportFileFormat { get; }
    Regex TemplateFileFormat { get; }
    Regex ArticleFileFormat { get; }
    string FormatOrdersFileName(FactionNumber factionNumber);
}
