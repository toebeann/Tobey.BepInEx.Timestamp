using BepInEx.Configuration;
using BepInEx.Logging;
using GuerrillaNtp;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

#if IL2CPP
using BepInEx.Preloader.Core.Patching;
#else
using BepInEx;
using Mono.Cecil;
using System.Collections.Generic;
using System.IO;
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
    #region BepInEx Patcher Contract
    public static IEnumerable<string> TargetDLLs { get; } = [];
    public static void Patch(AssemblyDefinition _) { }
    #endregion
#endif

#if IL2CPP
    public override void Initialize() =>
#else
    public static void Initialize() =>
#endif
        Task.Run(Run);

#if IL2CPP
    private async Task
#else
    private static async Task
#endif
        Run()
    {
#if IL2CPP
        ManualLogSource logger = Log;
        ConfigFile config = Config;
#else
            using ManualLogSource logger = Logger.CreateLogSource("Timestamp");
            ConfigFile config = new(
                configPath: Path.Combine(Paths.ConfigPath, "Tobey.BepInEx.Timestamp.cfg"),
                saveOnInit: true);
#endif

        var ntpEnabled = config.Bind(
            section: "NTP",
            key: "Enabled",
            defaultValue: true,
            description: """
                Allow acquiring timestamp from NTP endpoints
                When enabled, NTP endpoints take precedence over HTTP endpoints
                """);

        var ntpEndpoints = config.Bind(
            section: "NTP",
            key: "Endpoints",
            defaultValue: $"time.cloudflare.com, {NtpClient.DefaultHost}:{NtpClient.DefaultPort}, time.google.com, time.nist.gov",
            description: $"""
                Comma-separated list of NTP endpoints for remote timestamp acquisition in descending order of preference
                Endpoints must be valid SNTP/NTP servers
                Endpoints should be in the format "address[:port]"
                The port is optional and defaults to {NtpClient.DefaultPort} if not given
                """);

        var httpEnabled = config.Bind(
            section: "HTTP",
            key: "Enabled",
            defaultValue: true,
            description: """
                Allow acquiring timestamp from HTTP endpoints
                When enabled, HTTP endpoints are used as a fallback if NTP endpoints failed or are disabled
                """);

        var httpEndpoints = config.Bind(
            section: "HTTP",
            key: "Endpoints",
            defaultValue: "http://cloudflare.com, http://google.com, http://nist.gov",
            description: """
                Comma-separated list of HTTP endpoints for remote timestamp acquisition in descending order of preference
                The timestamp will be parsed from response's "date" header, which must be in the format:
                ddd, dd MMM yyyy HH:mm:ss GMT
                Example: Wed, 02 Oct 2024 12:09:25 GMT
                """);

        var timeoutMs = config.Bind(
            section: "General",
            key: "Timeout",
            defaultValue: 1_000,
            description: """
                How long in milliseconds to wait for a response from each remote endpoint
                """);

        bool remoteTimestampAcquired = false;
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var source = "local system clock";

        try
        {
            if (ntpEnabled.Value)
            {
                try
                {
                    foreach (var endpoint in $"{ntpEndpoints.Value}, {ntpEndpoints.DefaultValue}"
                        .Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Distinct())
                    {
                        try
                        {
                            (string host, int port) = endpoint.Split(',') switch
                            {
                                [string h, string p] => (h, int.TryParse(p, out int value) ? value : NtpClient.DefaultPort),
                                [string h] => (h, NtpClient.DefaultPort),
                                _ => (NtpClient.DefaultHost, NtpClient.DefaultPort)
                            };

                            var client = new NtpClient(host, TimeSpan.FromMilliseconds(timeoutMs.Value), port);
                            var clock = await client.QueryAsync();
                            now = clock.UtcNow;
                            source = endpoint;

                            remoteTimestampAcquired = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning($"Failed to get remote timestamp from {endpoint}");
                            logger.LogWarning(ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex);
                }
            }
            else
            {
                logger.LogInfo("NTP timestamp acquisition disabled");
            }

            if (!remoteTimestampAcquired)
            {
                if (httpEnabled.Value)
                {
                    try
                    {
                        foreach (var endpoint in $"{httpEndpoints.Value}, {httpEndpoints.DefaultValue}"
                            .Split(',')
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Distinct())
                        {
                            try
                            {
                                var client = new HttpClient() { Timeout = TimeSpan.FromMilliseconds(timeoutMs.Value) };
                                var response = await client.GetAsync(endpoint);
                                var date = response.Headers.GetValues("date").Single();
                                now = DateTimeOffset.ParseExact(
                                    input: date,
                                    format: "ddd, dd MMM yyyy HH:mm:ss 'GMT'",
                                    CultureInfo.InvariantCulture.DateTimeFormat,
                                    DateTimeStyles.AssumeUniversal);
                                source = endpoint;

                                remoteTimestampAcquired = true;
                                break;
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning($"Failed to get remote timestamp from {endpoint}");
                                logger.LogWarning(ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex);
                    }
                }
                else
                {
                    logger.LogInfo("HTTP timestamp acquisition disabled");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogFatal(ex);
        }
        finally
        {
            logger.LogMessage($"It is currently {now:R} according to {source}");
#if !IL2CPP
            Logger.Sources.Remove(logger);
#endif
        }
    }
}
