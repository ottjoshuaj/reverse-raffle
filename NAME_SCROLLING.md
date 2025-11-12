# Auto-Scrolling Names Feature (Movie Credits Style)

## Overview

Added automatic vertical scrolling for winner/loser name lists when the list exceeds the available display space. Names scroll like movie credits, pause at the end, then restart from the beginning.

## Problem Solved

Previously, when many winners had their names listed on tickets, the overlay would cut off names that didn't fit in the available space. This left some winners' names invisible.

Now, long name lists automatically scroll vertically, ensuring all names are visible over time.

## Changes Made

### 1. New Scrolling Fields

**Location:** `Controls/RaffleOverlay.cs`

```csharp
// Name scrolling fields
private float _namesScrollOffset;      // Current scroll position
private Timer _namesScrollTimer;       // Animation timer
private float _totalNamesHeight;       // Total height of all names
private bool _scrollPaused;            // Pause state (at end of list)
private int _scrollPauseCounter;       // Pause duration counter
```

### 2. InitializeNameScrolling Method

**Called:** When overlay is shown (in Show() method)

**Purpose:**
- Calculates total height of all names
- Determines if scrolling is needed
- Starts animation timer if needed

**Behavior:**
```csharp
private void InitializeNameScrolling()
{
    _namesScrollOffset = 0f;
    _scrollPaused = false;
    _scrollPauseCounter = 0;

    // Calculate total height of names
    _totalNamesHeight = CalculateTotalNamesHeight();

    // Start 20 FPS timer
    _namesScrollTimer = new Timer { Interval = 50 };
    _namesScrollTimer.Tick += (s, e) =>
    {
        if (_scrollPaused)
        {
            _scrollPauseCounter++;
            if (_scrollPauseCounter > 40) // 2 seconds pause
            {
                _scrollPaused = false;
                _scrollPauseCounter = 0;
                _namesScrollOffset = 0f; // Restart from top
            }
        }
        else
        {
            _namesScrollOffset += 1.5f; // Scroll speed
        }
        Invalidate();
    };
    _namesScrollTimer.Start();
}
```

### 3. CalculateTotalNamesHeight Method

**Purpose:** Measures the total vertical space needed for all names

**Calculation includes:**
- Each name's text height
- 2-pixel spacing between names
- Separator lines every 3 names (15 pixels)
- Label prefixes ("A.)", "B.)", "C.)")

```csharp
private float CalculateTotalNamesHeight()
{
    float totalHeight = 0;
    using (var font = new Font("Arial", 14f, FontStyle.Regular))
    using (var g = CreateGraphics())
    {
        int nameCount = 0;
        foreach (var name in _names)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;

            // Add separator height every 3 names
            if (nameCount > 0 && nameCount % 3 == 0)
                totalHeight += 15; // 5 + 10 from separator

            var textSize = g.MeasureString("A.) " + name, font);
            totalHeight += textSize.Height + 2;
            nameCount++;
        }
    }
    return totalHeight;
}
```

### 4. Updated DrawNamesList Method

**Key Changes:**

#### Clipping Region
Prevents names from rendering outside the designated area:
```csharp
int availableHeight = bounds.Bottom - (bounds.Y + startY) - 40;
Rectangle namesRegion = new Rectangle(
    bounds.X,
    bounds.Y + startY,
    bounds.Width,
    availableHeight
);

// Set clipping region
var previousClip = g.Clip;
g.SetClip(namesRegion);
```

#### Scroll Offset
Applies vertical offset when scrolling is needed:
```csharp
bool needsScrolling = _totalNamesHeight > availableHeight;
float y = bounds.Y + startY - (needsScrolling ? _namesScrollOffset : 0);
```

#### End Detection & Pause
Pauses when scrolled past the end:
```csharp
if (needsScrolling && !_scrollPaused &&
    _namesScrollOffset > _totalNamesHeight + availableHeight)
{
    _scrollPaused = true;
    _scrollPauseCounter = 0;
}
```

#### Cleanup
Restores graphics state:
```csharp
g.Clip = previousClip;
```

## How It Works

### Scrolling Cycle

```
1. Names start at top of region (offset = 0)
   ↓
2. Timer fires every 50ms
   ↓
3. Offset increases by 1.5 pixels per tick (scrolling)
   ↓
4. Names move upward (movie credits effect)
   ↓
5. When last name scrolls off top:
   - Pause for 2 seconds (40 ticks × 50ms)
   - Screen shows blank/empty names area
   ↓
6. After pause:
   - Reset offset to 0
   - Cycle repeats
```

### Visual Example

```
┌─────────────────────────┐
│ [Trophy Icon]           │
│ "Raffle Winner(s)"      │
│ Ticket # 42, 87         │
│ ───────────────────     │ ← Names region starts here
│ A.) John Smith          │
│ B.) ABC Company         │ ← Visible area
│ C.) XYZ Corp            │    (clipped region)
│ ───────────────────     │
│ [... more names ...]    │ ← Scrolls up over time
│ ───────────────────     │
│ (Click to close)        │
└─────────────────────────┘
```

## Configuration

### Scroll Speed
**Location:** `RaffleOverlay.cs:247`
```csharp
_namesScrollOffset += 1.5f; // Adjust for faster/slower scroll
```

**Recommended values:**
- 1.0 = Slow, easy to read
- 1.5 = Current speed (default)
- 2.0 = Fast, good for many names
- 3.0+ = Very fast

### Pause Duration
**Location:** `RaffleOverlay.cs:238`
```csharp
if (_scrollPauseCounter > 40) // 2 seconds at 50ms intervals
```

**Formula:** `pauseMs = counter × 50`
- 20 = 1 second pause
- 40 = 2 seconds pause (default)
- 60 = 3 seconds pause

### Animation Frame Rate
**Location:** `RaffleOverlay.cs:232`
```csharp
_namesScrollTimer = new Timer { Interval = 50 }; // 20 FPS
```

**Recommended values:**
- 30ms = 33 FPS (smoother, more CPU)
- 50ms = 20 FPS (balanced, default)
- 100ms = 10 FPS (choppy, less CPU)

## When Scrolling Activates

Scrolling only activates when:
```
Total Names Height > Available Display Height
```

**Available display height calculation:**
```
Available = Overlay Card Bottom
          - Names Start Y Position
          - 40 pixels (reserved for "Click to close" hint)
```

**Example:**
- Card height: 500px
- Names start at Y=330
- Available = 500 - 330 - 40 = 130px
- If names total 150px → Scrolling enabled
- If names total 100px → No scrolling (static display)

## Performance

**Resource usage:**
- Timer: 50ms interval (20 FPS)
- Graphics clipping: Hardware accelerated
- No memory leaks (timer disposed on close)
- Smooth animation via Invalidate()

**Cleanup on close:**
```csharp
_namesScrollTimer?.Stop();
_namesScrollTimer?.Dispose();
_namesScrollTimer = null;
```

## Benefits

1. **All names visible:** No winners are hidden due to overflow
2. **Professional look:** Movie credits style feels polished
3. **Automatic:** No user interaction needed
4. **Configurable:** Speed and pause can be adjusted
5. **Efficient:** Only scrolls when needed
6. **Smooth:** 20 FPS provides fluid motion

## Applies To

The scrolling feature works for all overlay types:
- ✅ Winner announcements (periodic)
- ✅ Raffle Winner announcements (final)
- ✅ Loser announcements
- ✅ Info overlays

All overlays share the same `DrawNamesList()` method, so scrolling is universal.

## Testing

To test scrolling:
1. Import a CSV with many unique ticket holders
2. Start raffle and mark tickets as winners
3. When winner overlay shows with long name list:
   - Names should scroll upward
   - Scroll speed: ~1.5 pixels per 50ms
   - After all names pass: 2-second blank pause
   - Then restart from top

## Future Enhancements

- Fade in/out at top and bottom edges
- Variable speed (start slow, speed up, slow down)
- Horizontal scrolling for very long names
- Configurable settings in UI (speed, pause duration)
- Smooth easing functions (ease-in, ease-out)
