# OpenTTD IRC Info

Command-line application to periodically print out company information to IRC.

## Synopsis

```
dotnet run [options]
```

## Options

- `--ottd-server <ottd-server>`

  OpenTTD server hostname or IP address.

- `--ottd-port <ottd-port>`

  OpenTTD server port (default: 3979).

- `--irc-server <irc-server>`

  IRC server hostname or IP address.

- `--irc-port <irc-port>`

  IRC server port (default: 6667). TLS is not supported currently.

- `--irc-nickname <irc-nickname>`

  IRC nickname (default: OTTDBot).

- `--irc-channel <irc-channel>`

  IRC channel name (with prefix, e.g. `"#openttd"`).

## Example IRC output

```
<OTTDBot> 1990 - Silver Transport (128,135 += 20,482), Other Company Name (123,456 += 12,345)
<OTTDBot> 1991 - Silver Transport might be in trouble! Money: 113,085 Yearly income: -118,366
```
