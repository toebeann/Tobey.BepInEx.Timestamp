# Tobey's Timestamp Logger for BepInEx

A configurable BepInEx patcher to log the current timestamp.

## Why?

When troubleshooting, it's useful to know the time and date of the log file you're looking at. Contrary to popular belief, BepInEx does not log this by default - the timestamp at the top of the log file corresponds to the modified date of the game's executable, which is still useful but for different reasons.

Sometimes, users will be unaware that the log file they're reading is irrelevant because it is from a previous run of the game prior to making changes. Additionally, when users share their log files with helpful folks donating their time to help them out, those helpers have no way of knowing the log file's timestamp, which can often lead to wasted time for all involved.

To address this, Tobey.BepInEx.Timestamp prints a UTC timestamp in the logs to make it easier for users and helpers alike to determine whether the log file they're looking at is relevant.

By default, it does this by contacting global NTP (Network Time Protocol) servers commonly used for accurate time synchronisation to retrieve an accurate UTC timestamp. Should this mechanism fail it then pings common global HTTP servers and attempts to parse a UTC timestamp from the HTTP response header, before falling back to the local system clock.

### Why contact a remote server for the timestamp? Every computer has a system clock, so why not just use it?

In a perfect world we could depend upon the validity of a user's system clock, however sometimes they are incorrectly set for a variety of reasons. For example, some users manually change their system clock to artificially extend the length of free software trials. My opinion is that a false timestamp in the logs is just as likely to lead to wasted time from helpers as having no timestamp at all.

If for whatever reason you (or your users) aren't happy with your computer reaching out to remote servers for this purpose when you load your game, you can configure the endpoints used, or altenatively disable remote timestamp acquisition altogether.

## Usage

Just plop the contents of the downloaded .zip from [the releases page](https://github.com/toebeann/Tobey.BepInEx.Timestamp/releases) into your game folder (after installing [BepInEx](https://github.com/BepInEx/BepInEx), of course).

If you would like to configure the behaviour of remote timestamp acquisition, you can edit the file `BepInEx` > `config` > `Tobey.BepInEx.Timestamp.cfg`:

```cfg
## Settings file was created by plugin Timestamp v2.0.0
## Plugin GUID: Tobey.BepInEx.Timestamp

[General]

## How long in milliseconds to wait for a response from each remote endpoint
# Setting type: Int32
# Default value: 1000
Timeout = 1000

[HTTP]

## Allow acquiring timestamp from HTTP endpoints
## When enabled, HTTP endpoints are used as a fallback if NTP endpoints failed or are disabled
# Setting type: Boolean
# Default value: true
Enabled = true

## Comma-separated list of HTTP endpoints for timestamp acquisition in descending order of preference
## The timestamp will be parsed from the response's "date" header, which must be in the format:
## ddd, dd MMM yyyy HH:mm:ss GMT
## Example: Wed, 02 Oct 2024 12:09:25 GMT
# Setting type: String
# Default value: http://cloudflare.com, http://google.com, http://nist.gov
Endpoints = http://cloudflare.com, http://google.com, http://nist.gov

[NTP]

## Allow acquiring timestamp from NTP endpoints
## When enabled, NTP endpoints take precedence over HTTP endpoints
# Setting type: Boolean
# Default value: true
Enabled = true

## Comma-separated list of NTP endpoints for remote acquisition in descending order of preference
## Endpoints must be valid SNTP/NTP servers
## Endpoints should be in the format "address[:port]"
## The port is optional and defaults to 123 if not given
# Setting type: String
# Default value: time.cloudflare.com, pool.ntp.org:123, time.google.com, time.nist.gov
Endpoints = time.cloudflare.com, pool.ntp.org:123, time.google.com, time.nist.gov
```
