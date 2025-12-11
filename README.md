# Tobey's Timestamp Logger for BepInEx

A configurable BepInEx patcher to log the current timestamp.

## Why?

When troubleshooting, it's useful to know the time and date of the log file you're looking at. Contrary to popular belief, BepInEx does not log this by default - the timestamp at the top of the log file is the timestamp of the modified date of the game's executable, which is still useful but for different reasons.

Sometimes, users will be unaware that the log file they're reading is irrelevant because it is from a previous run of the game prior to making changes. Additionally, when users share their log files with helpful folks donating their time to help them out, they have no way of knowing the log file's timestamp, which can often lead to wasted time for all involved.

To address this, Tobey.BepInEx.Timestamp prints a UTC timestamp in the logs to make it easier for users and helpers alike to determine whether the log file they're looking at is relevant.

By default, it does this by contacting a remote endpoint (http://google.com) and parsing the time and date from the response headers, falling back to the system clock if something goes wrong.

### Why contact a remote endpoint for the timestamp? Every computer has a system clock, so why not just use it?

In a perfect world we could depend upon the validity of the system clock, however sometimes users have system clocks which are incorrectly set for a variety of reasons. For example, some users manually change their system clock to artificially extend the length of free software trials. My opinion is that a false timestamp in the logs is just as likely to lead to wasted time from voluntary helpers as having no timestamp at all. Likewise, I opine that the system clock of a major web server such as Google's is more reliable - at least when it comes to the current date and time - than a user's system clock.

If for whatever reason you don't want your computer reaching out to http://google.com when you load the game, you can edit the configuration to either change the remote endpoint used, or altenatively disable remote timestamp acquisition altogether.

## Usage

Just plop the contents of the downloaded .zip from [the releases page](https://github.com/toebeann/Tobey.BepInEx.Timestamp/releases) into your game folder (after installing [BepInEx](https://github.com/BepInEx/BepInEx), of course).

If you would like to configure the behaviour of remote timestamp acquisition, you can edit the file `BepInEx` > `config` > `Tobey.BepInEx.Timestamp.cfg`:

```cfg
## Settings file was created by plugin Tobey.BepInEx.Timestamp v1.1.0
## Plugin GUID: Tobey.BepInEx.Timestamp

[Remote]

## Allow acquiring timestamp from remote endpoints
# Setting type: Boolean
# Default value: true
Enabled = true

## Endpoint URI for remote timestamp acquisition, which will be parsed from the response headers
## The endpoint's response must contain a "date" header in the format: ddd, dd MMM yyyy HH:mm:ss GMT
## Example: Wed, 02 Oct 2024 12:09:25 GMT
## HTTPS is not supported
# Setting type: String
# Default value: http://google.com
Endpoint = http://google.com

## How long to wait in milliseconds before giving up on the remote endpoint
# Setting type: Int32
# Default value: 2000
Timeout = 2000
```
