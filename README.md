# GameNetworking
GameNetworking BSIT4122

# PROTOCOL ZERO

## 📖 About the Project

**Protocol Zero** is a 2-player co-op multiplayer FPS shooter integrated with a cyberpunk-themed chat terminal system. The project combines real-time shooting mechanics with a fully functional multiplayer chat system, demonstrating core networking concepts including client-server architecture, Remote Procedure Calls (RPCs), network synchronization, and real-time multiplayer interactions.

---

## ✨ Features

### 🎯 Gameplay
- WASD movement with sprint (Left Shift)
- FPS-style mouse aiming
- Shooting mechanics with bullet physics
- Zombie AI (spawn, chase, attack)
- Health system with respawn
- Ammo system with reload (R key)

### 💬 Chat System (CyberLobby Terminal)
- Cyberpunk-themed UI (dark bg, neon green text)
- Dynamic player handles (User_XXX)
- Chat commands: `/nick`, `/clear`, `/ping`
- Server-authoritative messaging (ServerRpc/ClientRpc)
- Left menu panel with interactive buttons
- Scroll support (mouse wheel, Page Up/Down)

### 🖥️ HUD
- Health bar (top-left)
- Ammo counter (bottom-right)
- Weapon icon display

### 🌐 Networking
- Client-Server architecture
- Unity Netcode for GameObjects
- Unity Transport (127.0.0.1:7777)
- ServerRpc / ClientRpc communication
- Player spawning and synchronization

---

## 🎮 How to Play

### Quick Start
1. Open the project in Unity 2022.3 LTS or newer
2. Open **StartMenu** scene
3. Press Play → Click **"HOST GAME"**
4. Build and run standalone → Click **"JOIN GAME"**
5. Press **T** to open chat
6. Type **/nick YourName** to change your name
7. **WASD** to move, **Mouse** to aim, **Left Click** to shoot
8. **R** to reload

### Testing Without StartMenu
1. Open **CyberLobbyScene**
2. Press Play
3. Press **H** → Start as Host
4. Press **C** → Start as Client (in another instance)

---

## 🎯 Controls

| Action | Input |
|--------|-------|
| Move | WASD |
| Sprint | Left Shift |
| Aim | Mouse Movement |
| Shoot | Left Click |
| Reload | R |
| Open Chat | T |
| Close Chat | Escape |
| Chat Scroll Up | Page Up |
| Chat Scroll Down | Page Down |

---

## 💬 Chat Commands

| Command | Description | Example |
|---------|-------------|---------|
| `/nick <name>` | Change your display name | `/nick CyberHacker` |
| `/clear` | Clear chat history | `/clear` |
| `/ping` | Show network latency | `/ping` |

---

## 🛠️ Technologies Used

| Technology | Version | Purpose |
|------------|---------|---------|
| Unity | 2022.3 LTS | Game Engine |
| Netcode for GameObjects | 1.12.2 | Network Framework |
| Unity Transport | Latest | Network Transport |
| TextMeshPro | Latest | UI Text Rendering |

---

## 📁 Project Structure
GameNetworking/
├── Assets/
│ ├── Scenes/
│ │ ├── StartMenu.unity
│ │ └── CyberLobbyScene.unity
│ ├── Scripts/
│ │ ├── PlayerController.cs
│ │ ├── ChatManagerCyber.cs
│ │ ├── SpawnManager.cs
│ │ ├── ZombieSpawner.cs
│ │ ├── ZombieAI.cs
│ │ ├── HealthSystem.cs
│ │ ├── Projectile.cs
│ │ ├── PlayerHUD.cs
│ │ ├── StartMenu.cs
│ │ ├── GameSceneManager.cs
│ │ ├── NetworkPositionSync.cs
│ │ └── MenuButtonHandler.cs
│ └── Prefabs/
│ ├── Player.prefab
│ ├── Bullet.prefab
│ └── Zombie.prefab
├── Builds/
│ ├── ProtocolZero_Host.exe
│ └── ProtocolZero_Client.exe

