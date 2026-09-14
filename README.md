# GameNetworking
GameNetworking BSIT4122
# PROTOCOL ZERO

## A 2-Player Co-op FPS with Lobby, Chat, and Zombie Waves
---

## 📖 About the Project

**Protocol Zero** is a 2-player co-op multiplayer first-person shooter built in Unity with **Mirror Networking**. It combines:

- A **3-scene session flow** (Menu → Lobby → Gameplay)
- A **server-authoritative lobby** with readiness and role selection
- **Real-time FPS combat** against zombies
- An **in-game chat terminal** with commands
- A **pause overlay** with Resume, Back to Menu, and Exit

The project demonstrates client-server architecture, RPC communication, scene transitions, and network synchronization — covered across Lab Lessons 1 through 5.

---

## ✨ Features

### 🎮 Gameplay
- WASD movement + sprint (Left Shift)
- FPS-style mouse aiming
- Shooting mechanics with ammo & reload
- Health system with respawn
- Zombie AI (chase, attack, die)

### 🗺️ Multi-Scene Flow
- `0_OfflineScene` — Main menu with server IP + username inputs
- `1_LobbyScene` — Ready toggle, role selection (Officer / Soldier)
- `2_GameplayScene` — Full FPS + zombie waves

### 🌐 Networking (Mirror)
- Host + Client model via `KcpTransport`
- Server-authoritative scene transitions
- `[SyncVar]` based player state sync
- `[Command]` / `[ClientRpc]` messaging
- Client-side prediction for bullets and rotation

### 💬 Chat System
- `T` opens chat · `Enter` sends · `Esc` closes
- Commands: `/nick <name>`, `/clear`, `/ping`
- Color-coded system messages
- Server-side username storage

### ⏸️ Pause Menu
- `F1` opens overlay
- Resume · Back to Menu · Exit Game
- Cursor and input handled cleanly

### 🧟 Zombie Waves
- Trigger-based spawning (shooting range style)
- Each trigger spawns a configurable number of zombies
- Spawn delay with animation support
- Multiple waves placed along the path

---

## 🛠️ Technologies Used

| Technology | Version | Purpose |
|------------|---------|---------|
| Unity | 2022.3 LTS | Game engine |
| Mirror | Latest | Networking library |
| KcpTransport | Bundled | UDP transport |
| TextMeshPro | Latest | UI text rendering |
| Blender | 4.0+ | 3D models (zombies) |

---
