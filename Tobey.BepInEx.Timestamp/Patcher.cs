using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;

namespace Tobey.BepInEx.Timestamp;

public static class Patcher
{
    // Without the contents of this region, the patcher will not be loaded by BepInEx - do not remove!
    #region BepInEx Patcher Contract
    public static IEnumerable<string> TargetDLLs { get; } = [];
    public static void Patch(AssemblyDefinition _) { }
    #endregion

    // entry point - do not delete or rename!
    public static void Initialize()
    {
        using var logger = Logger.CreateLogSource("Timestamp");
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var source = "local system clock";

        ConfigFile config = new(
            configPath: Path.Combine(Paths.ConfigPath, "Tobey.BepInEx.Timestamp.cfg"),
            saveOnInit: true);

        var remoteEnabled = config.Bind(
            section: "Remote",
            key: "Enabled",
            defaultValue: true,
            description: "Allow acquiring timestamp from remote endpoints for better accuracy");

        var remoteEndpoint = config.Bind(
            section: "Remote",
            key: "Endpoint",
            defaultValue: "http://google.com",
            description: string.Join(Environment.NewLine,
                [
                    "Endpoint URI for remote timestamp acquisition, which will be parsed from the response headers",
                    "The endpoint's response must contain a \"date\" header in the format: ddd, dd MMM yyyy HH:mm:ss GMT",
                    "Example: Wed, 02 Oct 2024 12:09:25 GMT",
                    "HTTPS is not supported"
                ]));

        if (remoteEnabled.Value)
        {
            string[] sources = [remoteEndpoint.Value, (string)remoteEndpoint.DefaultValue];
            foreach (var endpoint in sources.Distinct())
            {
                try
                {
                    now = GetRemoteTimestamp(endpoint);
                    source = endpoint;
                    break;
                }
                catch
                {
                    logger.LogWarning($"Failed to get remote timestamp from {endpoint}");
                }
            }
        }
        else
        {
            logger.LogInfo("Remote timestamp acquisition disabled");
        }

        logger.LogMessage($"It is currently {now:R} according to {source}");
        Logger.Sources.Remove(logger);
    }

    private static DateTimeOffset GetRemoteTimestamp(string sourceUriString)
    {
        var request = (HttpWebRequest)WebRequest.Create(sourceUriString);
        request.Timeout = 1000;
        using var response = request.GetResponse();

        return DateTimeOffset.ParseExact(
            input: response.Headers["date"],
            format: "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
            CultureInfo.InvariantCulture.DateTimeFormat,
            DateTimeStyles.AssumeUniversal);
    }
}
