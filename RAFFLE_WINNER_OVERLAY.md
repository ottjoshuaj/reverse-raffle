# Raffle Winner Overlay with Animated Confetti & Stars

## Overview

Added a special "Raffle Winner(s)" overlay that displays when the final winners are announced (when the raffle ends). This overlay features animated stars and confetti particles that appear to shoot out from the trophy icon.

## Changes Made

### 1. New Overlay Type: RaffleWinner

**Location:** `Controls/RaffleOverlay.cs`

Added new overlay type to enum:
```csharp
private enum OverlayType
{
    Winner,      // Periodic winners
    Loser,       // Losing tickets
    Info,        // Ticket information
    RaffleWinner // Final raffle winners (NEW)
}
```

### 2. Particle Animation System

**New Fields:**
```csharp
// Particle animation fields
private List<Particle> _particles;
private Timer _particleTimer;
private Random _random;
```

**Particle Class:**
- X, Y position
- VelocityX, VelocityY (movement speed)
- Rotation, RotationSpeed (for spinning effect)
- Type (Star or Confetti)
- Color (randomized from gold/yellow/orange/purple/blue palette)
- Size (6-14 pixels)

**Animation Features:**
- 60 particles spawn from trophy center
- Particles shoot upward and outward with physics
- Gravity pulls particles down over time
- Particles spin/rotate as they fall
- Mix of 5-pointed stars and rectangular confetti
- Colorful palette: gold, orange, yellow, purple, light blue

### 3. ShowRaffleWinner Method

**Signature:**
```csharp
public void ShowRaffleWinner(string ticketNumbers, IEnumerable<string> names,
                             int autoCloseMs = 0, bool showCloseButton = false)
```

**Features:**
- Displays "Raffle Winner(s)" text (instead of "WINNER!")
- Initializes 60 animated particles
- Shows close button (X) in top-right corner
- Supports auto-close timer

### 4. DrawRaffleWinnerAnnouncement Method

**Visual Design:**
- Gold/yellow gradient background (same as regular winner)
- Trophy icon centered at top
- "Raffle Winner(s)" text below trophy (36pt bold)
- Ticket numbers displayed below title
- Winner names listed at bottom
- Animated stars and confetti particles behind the card

**Animation Details:**
- Particles originate from trophy center (bounds.Y + 40 + 60)
- Spread pattern: -4 to +4 horizontal, -8 to -2 vertical
- Gravity: 0.3 acceleration
- Rotation speed: -5 to +5 degrees per frame
- Update rate: 30ms (~33 FPS)

### 5. Updated frmNumberBoard

**Location:** `frmNumberBoard.cs:217`

Changed ShowFinalWinners() to use new overlay:
```csharp
// OLD: _overlay.ShowWinner(winningTickets, winners, autoCloseMs);
// NEW:
_overlay.ShowRaffleWinner(winningTickets, winners, autoCloseMs);
```

This ensures the animated raffle winner overlay is shown when the final winners are announced (when TotalTicketsNotDrawn == AvailableWinners).

## Usage

The raffle winner overlay will automatically display when:
1. User clicks "Begin Raffle"
2. Tickets are drawn until only X tickets remain (configured in "Total Winning Tickets")
3. The final ticket is marked, triggering `ShowFinalWinners()`
4. The animated overlay appears with confetti/stars shooting from the trophy

**User sees:**
- Semi-transparent dark overlay background
- Gold card with trophy icon
- "Raffle Winner(s)" text
- Animated colorful particles (stars and confetti)
- Ticket numbers
- List of winner names
- Close button (X) in top-right
- Auto-close countdown (if enabled)

## Technical Details

**Particle Physics:**
- Initial position: Trophy center (trophyX, trophyY)
- Velocity range:
  - X: -4 to 4 pixels/frame (horizontal spread)
  - Y: -8 to -2 pixels/frame (upward launch)
- Gravity: 0.3 pixels/frame² (downward acceleration)
- Damping: 0.99 horizontal velocity multiplier (air resistance)

**Drawing Order:**
1. Semi-transparent background (dark overlay)
2. Particles (behind card)
3. Gold card with gradient
4. Trophy icon
5. Text (title, ticket numbers, names)
6. Close button (if enabled)

**Performance:**
- 60 particles
- 33 FPS animation
- Particles rendered with anti-aliasing
- Graphics transform used for rotation

## Differences from Regular Winner Overlay

| Feature | Regular Winner | Raffle Winner |
|---------|---------------|---------------|
| Text | "WINNER!" | "Raffle Winner(s)" |
| Animation | None | Stars & confetti |
| Font size | 42pt | 36pt |
| When used | Periodic winners | Final winners only |
| Particles | No | Yes (60 particles) |
| Timers | 1 (auto-close) | 2 (auto-close + particles) |

## Cleanup

The overlay properly cleans up resources when closed:
```csharp
public void Close()
{
    _autoCloseTimer?.Stop();
    _autoCloseTimer?.Dispose();

    _particleTimer?.Stop();      // NEW: Stop particle animation
    _particleTimer?.Dispose();
    _particleTimer = null;

    // ... rest of cleanup
}
```

## Future Enhancements

Potential improvements:
- Configurable particle count
- Adjustable particle colors
- Different particle shapes (hearts, tickets, etc.)
- Sound effects on particle spawn
- Particle trails/motion blur
- 3D perspective effects
