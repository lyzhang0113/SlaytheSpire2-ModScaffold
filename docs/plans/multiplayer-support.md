# Multiplayer Support for STS2MenuControl

## Goal
Allow STS2MenuControl to host/join multiplayer games programmatically via the HTTP API.

## Multiplayer Flow (from decompiled code)

### Hosting (LAN)
1. `new NetHostGameService()` → `StartENetHost(port, maxClients)`
2. `NCharacterSelectScreen.InitializeMultiplayerAsHost(service, maxPlayers)`
3. Same flow as singleplayer: select character, all players ready → game starts

### Hosting (Steam)  
1. `new NetHostGameService()` → `await StartSteamHost(maxClients)` (async!)
2. Same as LAN after that

### Joining
1. `new JoinFlow()` → `await flow.Begin(initializer, sceneTree)` (async!)
2. `NCharacterSelectScreen.InitializeMultiplayerAsClient(service, response)`

## New Actions

### `open_multiplayer_character_select`
- Parameters: `mode` ("lan" or "steam"), `max_players` (default 4), `port` (default 33771, LAN only)
- For LAN: `new NetHostGameService()` → `StartENetHost(port, max_players)` → `InitializeMultiplayerAsHost`
- For Steam: `new NetHostGameService()` → `StartSteamHost(max_players)` → `InitializeMultiplayerAsHost`
- Returns lobby info (lobby code, net_id, etc.)

### `join_multiplayer_game`
- Parameters: `mode` ("lan" or "steam"), `host` (ip for LAN), `port` (for LAN), `steam_id` (for Steam), `lobby_id` (for Steam)
- Creates `JoinFlow` → `Begin(connectionInitializer, sceneTree)`
- For LAN: `new ENetClientConnectionInitializer(netId, host, port)`
- For Steam: `SteamClientConnectionInitializer.FromLobby(lobbyId)` or `.FromPlayer(steamId)`
- Returns join result

### `set_ready` / `set_unready`
- Calls `_lobby.SetReady(true/false)` on current character select screen
- In singleplayer, SetReady directly starts the game

## Implementation Notes
- `StartSteamHost` is async (returns Task<NetErrorInfo?>). Need to handle async in RunOnMainThread.
- `JoinFlow.Begin` is also async.
- For LAN hosting, `StartENetHost` is synchronous.
- The NCharacterSelectScreen is both the IStartRunLobbyListener and the UI.
- After hosting, the character select screen manages the lobby. Players connect, select characters, and ready up.

## State Changes
- CHARACTER_SELECT screen should report:
  - `is_multiplayer`: bool
  - `lobby_type`: "host" | "client" | "singleplayer"
  - `players`: list of player info (id, character, ready state)
  - `max_players`: int
  - `lobby_code` or `net_id`: for LAN identification
