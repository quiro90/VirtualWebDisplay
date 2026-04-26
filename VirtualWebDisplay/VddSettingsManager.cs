using System.Xml.Linq;

/// <summary>
/// Generates and manages the VDD configuration file at
/// C:\VirtualDisplayDriver\vdd_settings.xml.
/// </summary>
public static class VddSettingsManager
{
    public static readonly string SettingsPath =
        Path.Combine(@"C:\VirtualDisplayDriver", "vdd_settings.xml");

    private static readonly (int W, int H)[] BaseResolutions =
    [
        (800,  600),  (600,  800),
        (1024, 768),  (768,  1024),
        (1280, 960),  (960,  1280),
        (1400, 1050), (1050, 1400),
        (1600, 1200), (1200, 1600),
        (1280, 720),  (720,  1280),
        (1366, 768),  (768,  1366),
        (1600, 900),  (900,  1600),
        (1920, 1080), (1080, 1920),
        (1280, 800),  (800,  1280),
        (1440, 900),  (900,  1440),
        (1680, 1050), (1050, 1680),
        (1920, 1200), (1200, 1920),
        (1280, 1024), (1024, 1280),
        (1920, 1280), (1280, 1920),
    ];

    private static readonly int[] GlobalRates = [30, 60, 90, 120, 144];

    public static void WriteSettings(int monitorCount)
    {
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);

        var resolutions = BaseResolutions
            .Distinct()
            .OrderBy(r => r.W > r.H ? 0 : 1)
            .ThenBy(r => (long)r.W * r.H)
            .ToList();

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("vdd_settings",
                new XElement("monitors",
                    new XElement("count", monitorCount)),
                new XElement("gpu",
                    new XElement("friendlyname", "default")),
                new XElement("global",
                    GlobalRates.Select(r => new XElement("g_refresh_rate", r))),
                new XElement("resolutions",
                    resolutions.Select(r =>
                        new XElement("resolution",
                            new XElement("width",        r.W),
                            new XElement("height",       r.H),
                            new XElement("refresh_rate", 60)))),
                new XElement("options",
                    new XElement("CustomEdid",      "false"),
                    new XElement("PreventSpoof",    "false"),
                    new XElement("EdidCeaOverride", "false"),
                    new XElement("HardwareCursor",  "true"),
                    new XElement("SDR10bit",        "false"),
                    new XElement("HDRPlus",         "false"),
                    new XElement("logging",         "false"),
                    new XElement("debuglogging",    "false"))));

        doc.Save(SettingsPath);
    }

    public static int ReadMonitorCount()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return 0;

            var doc = XDocument.Load(SettingsPath);
            var value = doc.Root?.Element("monitors")?.Element("count")?.Value;
            return int.TryParse(value, out var count) ? count : 0;
        }
        catch
        {
            return 0;
        }
    }

    public static bool NeedsUpdate(int desiredMonitorCount) =>
        ReadMonitorCount() != desiredMonitorCount;
}
