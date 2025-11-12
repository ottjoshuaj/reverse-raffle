# Demo Overlay Buttons

## Overview

Added a "Demo" section to the Settings tab in frmConsole with four buttons that allow testing each overlay type with sample data. This makes it easy to preview and demonstrate the overlay system without running an actual raffle.

## Changes Made

### 1. New UI Controls (frmConsole.Designer.cs)

**Added Group Box:**
- `radGroupBox6` - Container for demo buttons
- Header text: "Demo"
- Location: (542, 254) - Right column, below "Ticket UI Settings"
- Size: (315, 200)
- Theme: CrystalDark

**Added Buttons:**

1. **btnDemoInfo** - "Show Info"
   - Location: (8, 22)
   - Shows info overlay for undrawn tickets

2. **btnDemoLoser** - "Show Loser"
   - Location: (8, 65)
   - Shows loser overlay

3. **btnDemoWinner** - "Show Winner"
   - Location: (8, 108)
   - Shows regular winner overlay (periodic winner)

4. **btnDemoRaffleWinner** - "Show Raffle Winner (Final)"
   - Location: (8, 151)
   - Shows final raffle winner overlay with animations and long name list

All buttons:
- Size: (299, 35)
- Theme: CrystalDark
- Vertical spacing: 43 pixels

### 2. Event Handlers (frmConsole.cs)

**Location:** Lines 601-674

**New Region:** `#region Demo Overlay Buttons`

**Methods:**

```csharp
private void btnDemoInfo_Click(object sender, EventArgs e)
```
- Validates numberBoard is visible
- Shows info overlay with sample data
- Ticket: "42"
- Names: "John Smith", "ABC Company", "XYZ Corporation"

```csharp
private void btnDemoLoser_Click(object sender, EventArgs e)
```
- Validates numberBoard is visible
- Shows loser overlay with sample data
- Ticket: "87"
- Names: "Jane Doe", "Main Street Business", "Downtown LLC"

```csharp
private void btnDemoWinner_Click(object sender, EventArgs e)
```
- Validates numberBoard is visible
- Shows winner overlay with sample data
- Ticket: "123"
- Names: "Bob Johnson", "Local Shop", "Community Center"

```csharp
private void btnDemoRaffleWinner_Click(object sender, EventArgs e)
```
- Validates numberBoard is visible
- Shows raffle winner overlay with:
  - Tickets: "5, 12, 18, 42, 87, 123"
  - **75 names** (25 sets of 3) to test scrolling
  - Includes animated confetti/stars
  - Tests auto-scrolling feature

**Validation:**
All buttons check if `numberBoard` exists and is visible:
```csharp
if (numberBoard == null || !numberBoard.Visible)
{
    RadMessageBox.Show("Please start the raffle first (click 'Begin Raffle' button)",
        "Number Board Required", MessageBoxButtons.OK, RadMessageIcon.Info);
    return;
}
```

### 3. Demo Methods (frmNumberBoard.cs)

**Location:** Lines 412-446

**New Region:** `#region Demo Methods`

**Public Methods:**

```csharp
public void ShowDemoInfo(string ticketNumber, IEnumerable<string> names)
```
- Wraps `_overlay.ShowInfo()` with close button enabled
- Auto-close disabled (0 ms)

```csharp
public void ShowDemoLoser(string ticketNumber, IEnumerable<string> names)
```
- Wraps `_overlay.ShowLoser()` with close button enabled
- Auto-close disabled (0 ms)

```csharp
public void ShowDemoWinner(string ticketNumber, IEnumerable<string> names)
```
- Wraps `_overlay.ShowWinner()` with close button enabled
- Auto-close disabled (0 ms)

```csharp
public void ShowDemoRaffleWinner(string ticketNumbers, IEnumerable<string> names)
```
- Wraps `_overlay.ShowRaffleWinner()` with close button enabled
- Auto-close disabled (0 ms)
- Shows animated confetti/stars
- Demonstrates name scrolling with long list

## Sample Data

### Info Overlay
```
Ticket # 42
A.) John Smith
B.) ABC Company
C.) XYZ Corporation
```

### Loser Overlay
```
Ticket # 87
A.) Jane Doe
B.) Main Street Business
C.) Downtown LLC
```

### Winner Overlay
```
Ticket # 123
A.) Bob Johnson
B.) Local Shop
C.) Community Center
```

### Raffle Winner Overlay
```
Ticket # 5, 12, 18, 42, 87, 123

25 sets of names (75 total):
- John Smith, ABC Company, XYZ Corporation
- Jane Doe, Main Street Business, Downtown LLC
- Bob Johnson, Local Shop, Community Center
- Alice Williams, Tech Solutions Inc, Global Enterprises
- Charlie Brown, Hardware Store, Retail Group
- Diana Prince, Wonder Consulting, Justice League LLC
- Frank Castle, Security Services, Vigilante Corp
- Grace Hopper, Computing Innovations, Navy Research Lab
- Henry Ford, Motor Company, Assembly Line Industries
- Irene Adler, Private Investigations, Holmes & Watson Ltd
- Jack Ryan, CIA Operations, Analyst Division
- Karen Starr, Power Industries, Kryptonian Tech
- Larry Page, Search Engines Inc, Alphabet Corporation
- Marie Curie, Radioactive Research, Nobel Prize Foundation
- Neil Armstrong, Apollo Missions, NASA Space Center
... and more
```

## Usage Instructions

### To Test Overlays:

1. **Start the application**
2. **Click "Begin Raffle"** button on Raffle Manager tab
   - This opens the Number Board window
3. **Navigate to Settings tab**
4. **Scroll to "Demo" group box** (right column, bottom)
5. **Click any demo button:**
   - **Show Info** - Blue info overlay
   - **Show Loser** - Gray loser overlay with red X
   - **Show Winner** - Gold winner overlay with trophy
   - **Show Raffle Winner (Final)** - Gold overlay with animated particles and scrolling names

### Expected Behavior:

- **Number Board Required:**
  - If raffle not started, shows error message
  - Must click "Begin Raffle" first

- **Overlay Display:**
  - Appears on Number Board window
  - Semi-transparent dark background
  - Gold/gray/blue card (depending on type)
  - Close button (X) in top-right corner
  - No auto-close (stays until manually closed)

- **Raffle Winner Specifics:**
  - 60 animated particles (stars and confetti)
  - Particles shoot from trophy center
  - Names auto-scroll vertically
  - 2-second pause when scrolling completes
  - Then restarts from top

## Testing Features

This demo section allows testing:

✅ **Overlay Rendering** - Verify visual appearance
✅ **Animation System** - Test particle physics (raffle winner only)
✅ **Name Scrolling** - Test auto-scroll with long lists
✅ **Close Button** - Verify X button works
✅ **Responsive Layout** - Test on different screen sizes
✅ **Theme Colors** - Verify gold/gray/blue themes
✅ **Icon Drawing** - Test trophy/X/info icons
✅ **Text Positioning** - Verify no overlaps
✅ **Clipping Regions** - Verify names don't overflow

## Keyboard Shortcuts

None currently. Must click buttons with mouse.

## Developer Notes

**Why Number Board Required?**
- Overlay is a child control of Number Board's panel
- Overlay needs parent container to render
- Could be enhanced to work standalone, but requires refactoring

**Why No Auto-Close in Demo?**
- Allows extended viewing for testing
- User controls when to close (via X button or clicking overlay)
- Prevents frustration during testing

**Extending Sample Data:**
- Edit event handlers in `frmConsole.cs:601-674`
- Modify `sampleNames` arrays
- Change ticket numbers in method calls

**Adding More Demo Buttons:**
1. Add button to `radGroupBox6` in Designer
2. Wire up event handler
3. Call appropriate `numberBoard.ShowDemo*()` method
4. Update group box height if needed

## Layout

Settings tab structure (right column):

```
┌─────────────────────────────┐
│ Ticket UI Settings          │
│  - Ticket Width             │
│  - Ticket Height            │
│                             │
└─────────────────────────────┘
         (spacing)
┌─────────────────────────────┐
│ Demo                        │
│  [Show Info]                │
│  [Show Loser]               │
│  [Show Winner]              │
│  [Show Raffle Winner Final] │
└─────────────────────────────┘
```

## Future Enhancements

Potential improvements:
- Standalone demo window (doesn't require raffle started)
- Editable sample data (text boxes for ticket numbers/names)
- Animation speed sliders
- Loop demo mode (cycles through all overlays)
- Screenshot/export functionality
- Compare view (show multiple overlays side-by-side)
- Theme switcher
- Font size preview
