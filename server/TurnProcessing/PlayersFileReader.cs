namespace advisor.TurnProcessing;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class PlayersFileReader : IEnumerable<FactionRecord> {
    public PlayersFileReader(Stream stream) {
        this.stream = stream;
    }

    private readonly Stream stream;

    public IEnumerator<FactionRecord> GetEnumerator() {
        static (string key, string value) parse(string s) {
            var kv = s.Split(":");
            return (kv[0].Trim(), kv[1].Trim());
        }

        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream, leaveOpen: true);

        string line;
        while ((line = reader.ReadLine()) != null && !line.StartsWith("Faction:")) {
            // skip until "Faction:"
        }

        FactionRecord rec = null;
        do {
            var (key, value) = parse(line);

            switch (key) {
                case "Faction": {
                    if (rec != null) {
                        yield return rec;
                    }

                    int? number = value.Equals("new", StringComparison.OrdinalIgnoreCase)
                        ? null
                        : int.Parse(value);

                    rec = new FactionRecord(number);
                    break;
                }

                case "Name": {
                    if (!rec.IsNew) {
                        var numberLen = rec.Number.ToString().Length + 2;
                        value = value[..^numberLen].Trim();
                    }

                    rec.Add(key, value);
                    break;
                }

                default: {
                    rec.Add(key, value);
                    break;
                }
            }
        }
        while ((line = reader.ReadLine()) != null);

        if (rec != null) {
            yield return rec;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
