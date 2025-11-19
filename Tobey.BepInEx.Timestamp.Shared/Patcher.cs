using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
#if IL2CPP
using BepInEx.Preloader.Core.Patching;
#else
using Mono.Cecil;
using System.Collections.Generic;
#endif

namespace Tobey.BepInEx.Timestamp;

#if IL2CPP
[PatcherPluginInfo(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public sealed class Patcher : BasePatcher
#else
public static class Patcher
#endif
{
#if !IL2CPP
    // Without the contents of this region, the patcher will not be loaded by BepInEx 5 - do not remove!
    #region BepInEx Patcher Contract
    public static IEnumerable<string> TargetDLLs { get; } = [];
    public static void Patch(AssemblyDefinition _) { }
    #endregion
#endif

    // entry point - do not delete or rename!
#if IL2CPP
    public override void Initialize()
#else
    public static void Initialize()
#endif
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
            description: "Allow acquiring timestamp from remote endpoints");

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
            string[] endpoints = [remoteEndpoint.Value, (string)remoteEndpoint.DefaultValue];
            foreach (var endpoint in endpoints.Distinct())
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
