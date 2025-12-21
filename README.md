<div align="center">

# 🖥️ NoBS Desktop Organizer

<img src="https://i.imgur.com/k4tGez0.png" alt="NoBS Desktop Organizer Logo" width="220"/>

<br/>

**A no-bullshit Windows workspace and desktop layout manager.**  
Launch apps. Restore layouts. Stay out of your way. No fluff, much organization.

</div>

---

<div align="center">

NoBS Desktop Organizer is a **lightweight Windows desktop workspace manager** that launches applications, tracks their windows, and remembers their size and position — without locking your desktop or fighting user input.

This is **not** a tiling window manager.  
This is **not** an overlay-based gimmick.  
This is a tool built for people who want their desktop to behave consistently **without losing control**.

</div>

---

## ❓ Why NoBS Desktop Organizer Exists

Most Windows “workspace” or “window manager” tools fall into one of two categories:

- **Too weak** – only launch apps, no real tracking, no recovery  
- **Too aggressive** – constantly force windows back into place, break Electron apps, and ignore user movement

**NoBS Desktop Organizer sits in the middle — intentionally.**

It:
- Launches apps when you want
- Watches windows after launch
- Learns when *you* move or resize them
- Updates the saved layout instead of fighting you

You move it.  
It remembers.  
It shuts the hell up.

---

## 🧠 Core Concepts

### 📦 Workspace Profiles
A **Workspace Profile** is a saved desktop setup containing:
- A list of applications
- Their window size and position
- Runtime behavior flags (kill on switch, wait for stability, etc.)

Switch profiles → desktop reorganizes itself.

---

### 🚀 Application Launching
When you apply a profile:
1. Existing windows are minimized
2. Selected apps are optionally terminated (if flagged)
3. Apps are launched in sequence
4. Windows are detected once they actually exist (important for Electron/UWP)
5. Positions are restored **once**, not permanently locked

---

### 👀 Live Window Monitoring (The Important Part)
Instead of blindly forcing positions, NoBS:

- Monitors the app’s **actual window**
- Detects:
  - Window recreation (Electron apps do this constantly)
  - Size changes
  - Style changes
- Updates the profile when *you* move or resize the window

**Result:**  
You can drag, resize, maximize, snap — and NoBS adapts instead of snapping it back.

---

## ⚙️ How It Works (Under the Hood)

- Uses native Win32 window detection
- Tracks:
  - Process ID (PID)
  - Window Handle (HWND)
  - Window bounds
  - Window styles
- Detects when apps:
  - Recreate their window
  - Delay window creation
  - Change styles after launch
- Handles:
  - Classic Win32 apps
  - Electron apps
  - UWP apps (as much as Windows allows)

No polling nonsense.  
No global hooks.  
No overlays.

---

## 🖱️ UI Philosophy

- Simple
- Clear
- Status-driven
- Tray-friendly

Live indicators show:
- Not Running
- Launching
- Running
- Failed

Switch profiles, switch views, come back later — **status stays accurate**.

---

## 🧩 Features

- ✅ Profile-based desktop layouts
- ✅ App launch sequencing
- ✅ Live window tracking
- ✅ PID & HWND persistence
- ✅ Electron / UWP friendly
- ✅ Tray icon support
- ✅ Kill-on-switch support
- ✅ Launch delay per app
- ✅ Wait-for-stable-window option
- ✅ No forced window locking
- ✅ Updates layout when *you* move windows

---

## 🆚 Why It’s Better Than Other Tools

| Feature | NoBS | Most Tools |
|------|------|-----------|
| Window recreation detection | ✅ | ❌ |
| Electron-friendly | ✅ | ❌ |
| Lets user move windows freely | ✅ | ❌ |
| Profile-based workflows | ✅ | ⚠️ |
| No overlays / tiling | ✅ | ❌ |
| Lightweight | ✅ | ❌ |

Most tools treat the desktop like a chessboard.  
**NoBS treats it like a workspace.**

---

## 🛠️ Built With

- C#
- WinForms
- Native Win32 APIs
- Zero external dependencies
- Designed for Windows 10 & 11

---

## 🧪 Current Status

**Stable, actively developed**

The core system is solid and already handles:
- App launching
- Window tracking
- Profile switching
- Real-time updates

---

## 🔮 Planned / WIP Features

> These are actively planned and designed — not vaporware.

- 🔜 Multi-monitor profile awareness
- 🔜 Per-app “force position for X seconds”
- 🔜 Smarter stability detection for slow Electron apps
- 🔜 Export / import profiles
- 🔜 Hotkey-based profile switching
- 🔜 Optional startup profile
- 🔜 Read-only “presentation mode”
- 🔜 UI polish and animations
- 🔜 Portable build

---

## 🧠 Philosophy (TL;DR)

- The desktop belongs to **you**
- Software should **adapt**, not dominate
- If you move a window, that’s intentional
- Tools should help — not nag

---

<div align="center">

**NoBS Desktop Organizer**  
Because your desktop doesn't need to deal with bullshit

</div>
