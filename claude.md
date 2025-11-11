# Reverse Raffle Application Documentation

## Overview

**ReverseRaffler** is a Windows Forms desktop application built in C# for managing reverse raffle events. In a reverse raffle, tickets are drawn one by one and marked as "losers" until a predetermined number of tickets remain - those remaining tickets are the winners. This creates excitement as the value of remaining tickets increases as more tickets are eliminated.

**Platform:** .NET Framework 4.8 with Telerik WinControls UI
**Database:** SQL Server Express LocalDB
**Architecture:** Windows Forms with optional REST API server

---

## What is a Reverse Raffle?

Unlike traditional raffles where one ticket wins immediately, a reverse raffle works by:
1. All tickets start in the "pool"
2. Tickets are drawn one at a time
3. Each drawn ticket is marked as a "loser"
4. As tickets are eliminated, remaining tickets become more valuable
5. When only X tickets remain (configurable), those are the winners
6. Optional: Award periodic winners every N tickets drawn

This creates drama and suspense as ticket holders watch the pool shrink, hoping their ticket survives.

---

## Application Architecture

### Project Structure

```
ReverseRaffler/
├── Program.cs                          # Application entry point
├── Forms/
│   ├── frmConsole.cs                   # Main control panel (464 lines)
│   ├── frmNumberBoard.cs               # Visual ticket display board (192 lines)
│   ├── frmImport.cs                    # CSV import dialog (132 lines)
│   ├── frmTicketWinner.cs              # Winner announcement (72 lines)
│   ├── frmTicketNotWinner.cs           # Loser announcement (71 lines)
│   ├── frmTicketDetails.cs             # Ticket info popup (42 lines)
│   └── frmAbout.cs                     # About/API info (128 lines)
├── Controls/
│   └── ctlNumberSheet.cs               # Individual ticket display control (100 lines)
├── Data/
│   ├── RevRaffleModel.Context.cs       # Entity Framework DbContext
│   ├── RevRaffleModel.edmx             # Database model (EDMX)
│   └── TicketHolder.cs                 # Ticket entity model
├── Objects/
│   └── ImportTicketItem.cs             # CSV import DTO
├── Api/
│   ├── ApiConfig.cs                    # OWIN Web API configuration
│   └── Controllers/
│       └── TicketController.cs         # REST endpoint for remote control
├── CsvFiles/                           # Sample CSV data files
└── App.config                          # Application settings
```

---

## Core Components

### 1. Main Console (frmConsole.cs)

The primary control center for the entire raffle operation.

**Location:** `frmConsole.cs:1-464`

#### Three Main Tabs:

##### A. Raffle Manager Tab
Controls the live raffle drawing process.

**Key Controls:**
- `btnBeginRaffle` - Starts the raffle, displays number board
- `btnEndRaffle` - Ends raffle, closes number board
- `rsTicketNumber` - Spinner for manual ticket number entry
- `btnMarkTicketAsNotGood` - Manually marks a ticket as drawn
- `rsTotalWinningTickets` - Sets how many final tickets should win
- `rsWinnersEveryNumTickets` - Awards a winner every N tickets (0 = disabled)

**Statistics Display:**
- Tickets Not Drawn
- Tickets That Lost
- Total Tickets in Raffle

**Key Code - Begin Raffle (`frmConsole.cs:214-256`):**
```csharp
private void btnBeginRaffle_Click(object sender, EventArgs e)
{
    // Set number of winners
    numberBoard.AvailableWinners = Convert.ToInt64(rsTotalWinningTickets.Value);

    // Add all tickets to the display board
    foreach (var item in RaffleTickets)
        numberBoard.AddTicketRaffle(item);

    // Show the board
    numberBoard.Show();

    // Update UI state
    gridTicketHolders.ReadOnly = true;
    lblTicketsNotDrawn.Text = numberBoard.TotalTicketsNotDrawn.ToString();
    lblTotalTicketsInRaffle.Text = numberBoard.TotalTickets.ToString();
}
```

##### B. Ticket Management Tab
Database grid for adding/editing/deleting tickets manually.

**Grid Features (`frmConsole.cs:94-163`):**
- Auto-generates next ticket number on new row
- Real-time database updates on cell changes
- Supports delete operations
- Shows all ticket holders (TicketHolder1, TicketHolder2, TicketHolder3)

**Key Events:**
```csharp
// Auto-assign next ticket number
gridTicketHolders_DefaultValuesNeeded (Lines 94-97)

// Save new ticket to database
gridTicketHolders_UserAddedRow (Lines 99-110)

// Update ticket holder names
gridTicketHolders_CellValueChanged (Lines 112-144)

// Delete ticket from database
gridTicketHolders_UserDeletingRow (Lines 146-163)
```

##### C. Settings Tab
Configuration and database management.

**Features:**
- Import CSV file to database
- Reset database with test tickets
- Clear all "MarkedAsNotWinner" flags
- Configure ticket display size (width/height)
- Auto-close settings for winner/loser popups
- Start/Stop API Server

**API Server Control (`frmConsole.cs:424-448`):**
```csharp
private void radButton1_Click(object sender, EventArgs e)
{
    if(radButton1.Text == "Start API Server")
    {
        string baseAddress = $"http://{Properties.Settings.Default.APIBindPort}/";
        webServer = WebApp.Start<ApiConfig>(url: baseAddress);

        // Poll API every 2 seconds for submitted tickets
        getApiTicketNumber = new Timer();
        getApiTicketNumber.Interval = 2000;
        getApiTicketNumber.Tick += GetApiTicketNumber_Tick;
        getApiTicketNumber.Start();
    }
}
```

---

### 2. Number Board (frmNumberBoard.cs)

Visual display showing all tickets in the raffle with real-time updates.

**Location:** `frmNumberBoard.cs:1-192`

#### Purpose
Displays every ticket as a small control in a scrollable panel. As tickets are drawn, they show a red "X" overlay.

#### Key Properties
```csharp
public long TotalTickets => panelTickerHolder.Controls.Count;
public long TotalLosingTickets => Tickets.Count(tc => tc.MarkedAsNotWinner == true);
public long TotalTicketsNotDrawn => TotalTickets - TotalLosingTickets;
public long AvailableWinners { get; set; }
public bool RaffleIsOver { get; private set; }
```

#### Core Methods

**AddTicketRaffle (`frmNumberBoard.cs:64-77`)**
```csharp
public void AddTicketRaffle(TicketHolder ticket)
{
    // Create visual control for this ticket
    var ticketControl = new ctlNumberSheet(ticket)
    {
        Width = Properties.Settings.Default.ticketWidth,
        Height = Properties.Settings.Default.ticketHeightt  // Note: typo in settings
    };

    panelTickerHolder.Controls.Add(ticketControl);
    Tickets.Add(ticket);
}
```

**MarkTicketAsLosing (`frmNumberBoard.cs:79-134`)**

This is the CRITICAL method that implements the raffle logic:

```csharp
public void MarkTicketAsLosing(int ticketNumber, bool skipDisplayScreen = false)
{
    var ticket = Tickets.FirstOrDefault(x => x.TicketNumber == ticketNumber);
    if (ticket == null) return;

    // Mark ticket visually
    var ticketControl = panelTickerHolder.Controls
        .OfType<ctlNumberSheet>()
        .FirstOrDefault(x => x.Ticket.TicketNumber == ticketNumber);
    ticketControl?.MarkTicketAsNotWinner();

    // Show losing ticket popup (unless skipped for periodic winners)
    if (!skipDisplayScreen)
    {
        var losingForm = new frmTicketNotWinner(/* ... */);
        losingForm.ShowDialog(this);
    }

    // CHECK IF RAFFLE IS COMPLETE
    if (TotalTicketsNotDrawn == AvailableWinners)
    {
        // Collect all remaining (winning) tickets
        var winners = new List<string>();
        var winningTickets = string.Empty;

        foreach (var t in Tickets.Where(x => !x.MarkedAsNotWinner.GetValueOrDefault()))
        {
            winningTickets += (string.IsNullOrWhiteSpace(winningTickets) ? "" : ", ")
                            + t.TicketNumber;

            // Collect unique holder names
            if (!winners.Contains(t.TicketHolder1)) winners.Add(t.TicketHolder1);
            if (!winners.Contains(t.TicketHolder2)) winners.Add(t.TicketHolder2);
            if (!winners.Contains(t.TicketHolder3)) winners.Add(t.TicketHolder3);
        }

        // Show winner screen
        var winnerForm = new frmTicketWinner(winningTickets, winners);
        winnerForm.ShowDialog(this);
        RaffleIsOver = true;
    }
}
```

**Winner Detection Logic:**
- Compares `TotalTicketsNotDrawn` (remaining) with `AvailableWinners` (target)
- When they match, all remaining tickets are winners
- Collects all unique names from winning tickets
- Displays winner announcement form

---

### 3. Ticket Number Sheet Control (ctlNumberSheet.cs)

Individual ticket display control - the building block of the number board.

**Location:** `Controls/ctlNumberSheet.cs:1-100`

#### Visual Structure
```
ctlNumberSheet (UserControl)
└── panelNumberSheet (White background panel)
    ├── lblNumberDisplay (Shows ticket number, Arial 20pt)
    └── lblTicketNotWinner (Red "X" overlay, initially hidden)
```

#### Constructor (`ctlNumberSheet.cs:22-44`)
```csharp
public ctlNumberSheet(TicketHolder ticket)
{
    Ticket = ticket;
    InitializeComponent();

    // Position the red X overlay on top of the ticket number
    var notWinnerPosition = PointToScreen(lblTicketNotWinner.Location);
    notWinnerPosition = lblNumberDisplay.PointToClient(notWinnerPosition);
    lblTicketNotWinner.Parent = lblNumberDisplay;
    lblTicketNotWinner.Location = notWinnerPosition;
    lblTicketNotWinner.BackColor = Color.Transparent;
    lblTicketNotWinner.Visible = false;

    // Display ticket number
    lblNumberDisplay.Text = ticket.TicketNumber.ToString();

    // Show X if already marked as loser (from previous session)
    if (ticket.MarkedAsNotWinner.GetValueOrDefault())
        lblTicketNotWinner.Visible = true;
}
```

#### Mark As Not Winner (`ctlNumberSheet.cs:69-83`)
```csharp
public void MarkTicketAsNotWinner()
{
    Ticket.MarkedAsNotWinner = true;
    lblTicketNotWinner.Visible = true;  // Show red X

    // Persist to database immediately
    using (var dbModel = new RevRaffleModelEntities())
    {
        var ticket = dbModel.TicketHolders
            .FirstOrDefault(x => x.TicketNumber == Ticket.TicketNumber);

        if (ticket != null)
        {
            ticket.MarkedAsNotWinner = true;
            dbModel.TicketHolders.AddOrUpdate(ticket);
            dbModel.SaveChanges();
        }
    }
}
```

**Key Points:**
- Updates both in-memory object and database
- Visual feedback is instant (red X overlay)
- Database persistence ensures state survives app restarts

---

## How the Raffle Works: Step-by-Step

### Setup Phase

1. **Import Tickets**
   - User clicks "Import File to DB" on Settings tab
   - Selects CSV file with ticket data
   - Application parses CSV and loads into database
   - See CSV format section below

2. **Configure Settings**
   - Set "Total Winning Tickets" (e.g., 2 tickets will win)
   - Optionally set "Winners Every X Tickets" (e.g., award winner every 10 tickets)
   - Configure auto-close timers for popups
   - Adjust ticket display size if needed

3. **Start Raffle**
   - Click "Begin Raffle" button
   - Number board window opens showing all tickets
   - Raffle Manager tab becomes active

### Drawing Phase

**Manual Entry Flow:**

```
User enters ticket number in spinner
    ↓
Presses Enter (or clicks "Mark Ticket As Not Good")
    ↓
frmConsole.HandleTicketSelectionAction(ticketNumber)
    ↓
Validates ticket exists and hasn't been drawn
    ↓
Increments ticketsRemoved counter
    ↓
DECISION POINT:

    Is this a periodic winner?
    (ticketsRemoved == rsWinnersEveryNumTickets.Value)
    ├─ YES → Mark as losing (skip popup)
    │         Show WINNER screen for this ticket
    │         Reset ticketsRemoved to 0
    │
    └─ NO  → Mark as losing (show loser popup)
              Continue to next ticket
    ↓
Update statistics display
    ↓
Check if raffle complete
    (TotalTicketsNotDrawn == AvailableWinners)
    ├─ YES → Collect all remaining tickets as winners
    │         Show WINNER screen with all winners
    │         Set RaffleIsOver = true
    │
    └─ NO  → Continue drawing tickets
```

**Key Code - Handle Ticket Selection (`frmConsole.cs:296-337`):**
```csharp
void HandleTicketSelectionAction(int ticketNumber)
{
    // Verify ticket exists
    if (!numberBoard.VerifyTicketNumberExists(ticketNumber))
    {
        RadMessageBox.Show("Ticket number doesn't exist!");
        return;
    }

    // Check if already drawn
    if (numberBoard.HasTicketBeenSelectedAlready(ticketNumber))
    {
        numberBoard.ShowTicketInfoDialog(ticketNumber);
        return;
    }

    ticketsRemoved++;

    // Check for periodic winner
    if (rsWinnersEveryNumTickets.Value == ticketsRemoved &&
        rsWinnersEveryNumTickets.Value > 0)
    {
        // This ticket WINS (periodic winner)
        numberBoard.MarkTicketAsLosing(ticketNumber, skipDisplayScreen: true);
        numberBoard.ShowWinnerScreen(ticketNumber);
        ticketsRemoved = 0;
    }
    else
    {
        // Normal losing ticket
        numberBoard.MarkTicketAsLosing(ticketNumber);
    }

    // Update statistics
    lblTicketsNotDrawn.Text = numberBoard.TotalTicketsNotDrawn.ToString();
    lblTicketsThatLost.Text = numberBoard.TotalLosingTickets.ToString();
}
```

### Winner Determination

**Two Ways to Win:**

1. **Final Winners** (most common)
   - When `TotalTicketsNotDrawn` equals `AvailableWinners`
   - All remaining tickets are winners
   - Example: Set to 2 winners, when 2 tickets remain, both win

2. **Periodic Winners**
   - Every X tickets drawn, that ticket wins
   - Configured via "Winners Every X Tickets" spinner
   - Example: Every 10th ticket drawn is a winner
   - Set to 0 to disable

---

## CSV Import Process

### Expected CSV Format

**File Location:** Select any CSV file via import dialog

**Required Structure:**
```csv
id,name1,name2,name3
1,John Smith,ABC Company,XYZ Corporation
2,Jane Doe,ABC Company,Main Street Business
3,Bob Johnson,Local Shop,XYZ Corporation
```

**Column Definitions:**
- `id` - Unique ticket number (integer, required)
- `name1` - First ticket holder name (string, optional)
- `name2` - Second ticket holder name (string, optional)
- `name3` - Third ticket holder name (string, optional)

**Important Constraints:**
- Header row MUST be: `id,name1,name2,name3` (exact match)
- Names cannot contain commas (will break parsing)
- Empty names are allowed (will be stored as empty strings)
- Ticket IDs must be unique

### Import Process (`frmImport.cs:63-127`)

```csharp
void frmImport_Shown(object sender, EventArgs e)
{
    try
    {
        var importTickets = new List<TicketHolder>();
        var csvRecords = csvReader.GetRecords<ImportTicketItem>();

        // Parse CSV with CsvHelper library
        csvReader.Read();
        csvReader.ReadHeader();

        while (csvReader.Read())
        {
            var record = csvReader.GetRecord<ImportTicketItem>();
            importTickets.Add(new TicketHolder
            {
                TicketNumber = record.Id,
                TicketHolder1 = record.Name1,
                TicketHolder2 = record.Name2,
                TicketHolder3 = record.Name3
            });
        }

        // Clear existing database
        using (var dbModel = new RevRaffleModelEntities())
        {
            foreach (var item in dbModel.TicketHolders.ToList())
                dbModel.TicketHolders.Remove(item);
            dbModel.SaveChanges();

            // Import with progress tracking
            progressImportStatus.Minimum = 1;
            progressImportStatus.Maximum = importTickets.Count + 1;

            foreach (var resultItem in importTickets)
            {
                progressImportStatus.Text = $"Importing Ticket # {resultItem.TicketNumber}";

                if (!dbModel.TicketHolders.Any(x => x.TicketNumber == resultItem.TicketNumber))
                {
                    dbModel.TicketHolders.Add(resultItem);
                    dbModel.SaveChanges();
                }

                progressImportStatus.Value1++;
            }
        }
    }
    catch
    {
        RadMessageBox.Show("Your CSV is not properly formatted!", "BAD FORMAT");
    }
}
```

**Process Flow:**
1. Open CSV file with CsvHelper library
2. Parse records into `ImportTicketItem` DTOs
3. Delete ALL existing tickets from database (fresh start)
4. Insert each ticket with progress bar
5. Close import dialog automatically

**Note:** Import DELETES existing tickets - it's not additive!

---

## Database Architecture

### Technology Stack
- **Database:** SQL Server Express LocalDB
- **ORM:** Entity Framework 6.5.1 (Database-First)
- **Connection:** LocalDB with file attachment
- **Location:** `|DataDirectory|\Data\RevRaffle.mdf`

### Connection String (`App.config:57-58`)
```xml
<add name="RevRaffleModelEntities"
     connectionString="metadata=res://*/Data.RevRaffleModel.csdl|...;
                       provider=System.Data.SqlClient;
                       provider connection string='
                         data source=(LocalDB)\MSSQLLocalDB;
                         attachdbfilename=|DataDirectory|\Data\RevRaffle.mdf;
                         integrated security=True;
                         MultipleActiveResultSets=True;App=EntityFramework'"
     providerName="System.Data.EntityClient" />
```

### DbContext (`Data/RevRaffleModel.Context.cs`)
```csharp
public partial class RevRaffleModelEntities : DbContext
{
    public RevRaffleModelEntities()
        : base("name=RevRaffleModelEntities")
    {
    }

    public virtual DbSet<TicketHolder> TicketHolders { get; set; }
}
```

### TicketHolder Entity (`Data/TicketHolder.cs`)

**Database Table:** `TicketHolders`

```csharp
public partial class TicketHolder
{
    [Key]
    public int TicketNumber { get; set; }          // Primary Key

    public string TicketHolder1 { get; set; }      // First name
    public string TicketHolder2 { get; set; }      // Second name
    public string TicketHolder3 { get; set; }      // Third name
    public string TicketHolder4 { get; set; }      // Fourth name (unused in UI)

    public bool? MarkedAsNotWinner { get; set; }   // Nullable boolean flag
}
```

**Key Points:**
- Primary key is `TicketNumber` (not auto-increment - user assigned)
- `TicketHolder4` is defined but not used in CSV import or UI
- `MarkedAsNotWinner` is nullable - NULL and false both mean "not drawn yet"
- No foreign keys or related entities

---

## REST API Server

### Purpose
Allows remote systems to submit ticket numbers via HTTP requests.

### Configuration

**Default Binding:** `127.0.0.1:6060` (localhost only)
**Protocol:** HTTP (not HTTPS)

**Binding Configuration (`App.config:36-37`):**
```xml
<setting name="APIBindPort" serializeAs="String">
    <value>127.0.0.1:6060</value>
</setting>
```

### Starting the Server

1. Click "Start API Server" button on Settings tab
2. Server starts on configured port
3. Button text changes to "Stop API Server"
4. Timer starts polling for submitted tickets every 2 seconds

**Server Startup (`frmConsole.cs:424-448`):**
```csharp
string baseAddress = $"http://{Properties.Settings.Default.APIBindPort}/";
webServer = WebApp.Start<ApiConfig>(url: baseAddress);

// Poll for API submissions every 2 seconds
getApiTicketNumber = new Timer();
getApiTicketNumber.Interval = 2000;
getApiTicketNumber.Tick += GetApiTicketNumber_Tick;
getApiTicketNumber.Start();
```

### API Endpoint

**Route:** `GET /api/ticket/{number}`

**Controller (`Api/Controllers/TicketController.cs:1-21`):**
```csharp
public class TicketController : ApiController
{
    public static int currentTicketNumber = 0;

    public bool Get(int number)
    {
        TicketController.currentTicketNumber = number;
        return true;
    }
}
```

**Usage Examples:**
```bash
# Submit ticket #42
curl http://127.0.0.1:6060/api/ticket/42
# Returns: true

# Submit ticket #123
curl http://127.0.0.1:6060/api/ticket/123
# Returns: true
```

### Polling Integration (`frmConsole.cs:450-459`)
```csharp
private void GetApiTicketNumber_Tick(object sender, EventArgs e)
{
    var ticketNumber = TicketController.currentTicketNumber;

    if(ticketNumber > 0)
    {
        TicketController.currentTicketNumber = 0;  // Reset to prevent reprocessing
        HandleTicketSelectionAction(ticketNumber);
    }
}
```

**Process Flow:**
```
Remote system sends HTTP GET request
    ↓
TicketController.Get(number) sets static field
    ↓
Timer fires every 2 seconds in frmConsole
    ↓
Checks if currentTicketNumber > 0
    ↓
If yes: Process ticket via HandleTicketSelectionAction()
    ↓
Reset currentTicketNumber to 0
```

**Note:** Uses static field for ticket storage - potential thread-safety issue with concurrent requests.

---

## Display Forms

### Winner Announcement (frmTicketWinner.cs)

Shows when a ticket wins (either final winner or periodic winner).

**Display Format:**
```
┌─────────────────────────────┐
│   TICKET # 42, 87, 123      │
│   ────────────────────────  │
│   A.) John Smith            │
│   B.) ABC Company           │
│   C.) XYZ Corporation       │
│   ──────────────────────────│
│   A.) Jane Doe              │
│   B.) ABC Company           │
│   C.) Main Street Business  │
└─────────────────────────────┘
```

**Constructor (`frmTicketWinner.cs:15-56`):**
```csharp
public frmTicketWinner(string ticketNumber, IEnumerable<string> namesonticket)
{
    InitializeComponent();

    lblTicketDisplay.Text = string.Format("Ticket # {0}", ticketNumber);

    // Build HTML formatted name list
    var nameList = string.Empty;
    var cnt = 0;

    foreach(var name in namesonticket)
    {
        if (cnt == 3)  // Every 3 names, add separator
        {
            nameList += "<p>--------------------------------------</p>";
            cnt = 0;
        }
        nameList += FormatNameLine(cnt, name);  // A.), B.), C.)
        cnt++;
    }

    lblNamesOnTicket.Text = string.Format(@"<html>{0}</html>", nameList);

    // Auto-close timer (if enabled)
    if (Properties.Settings.Default.EnableAutoCloseWinningTicketWindow == CheckState.Checked)
    {
        var displayTimer = new Timer();
        displayTimer.Interval = Properties.Settings.Default.AutoCloseWinningTicketWindowAfter * 1000;
        displayTimer.Tick += (s, e) => { displayTimer.Stop(); Close(); };
        displayTimer.Start();
    }
}
```

**Auto-Close Settings:**
- Enabled by default
- Default timeout: 15 seconds
- Configurable via Settings tab

### Loser Announcement (frmTicketNotWinner.cs)

Shows when a ticket is drawn and loses (not a winner).

**Similar to Winner Form but:**
- Shows "Ticket #X was drawn..." message
- Shorter auto-close: 5 seconds (default)
- Different visual styling

### Ticket Details (frmTicketDetails.cs)

Information popup when clicking a ticket on the board.

**Triggers:**
- Clicking a ticket control on the number board
- Re-entering an already-drawn ticket number

**Features:**
- Shows ticket number and holder names
- No auto-close (requires user interaction)
- Read-only information

---

## Application Settings

### Configurable Settings (`App.config`)

| Setting | Default | Description |
|---------|---------|-------------|
| **LastWhatTicketsAreWinners** | 2 | Number of final winning tickets |
| **WinnersEveryNumTickets** | 10 | Award winner every X tickets (0=disabled) |
| **EnableAutoCloseLosingTicketWindow** | Checked | Auto-close loser popup |
| **AutoCloseLosingTicketWindowAfter** | 5 | Seconds before closing loser popup |
| **EnableAutoCloseWinningTicketWindow** | Checked | Auto-close winner popup |
| **AutoCloseWinningTicketWindowAfter** | 15 | Seconds before closing winner popup |
| **ticketWidth** | 66 | Pixel width of ticket controls |
| **ticketHeightt** | 42 | Pixel height of ticket controls (typo) |
| **APIBindPort** | 127.0.0.1:6060 | Web API server binding address |

### Settings Persistence

Settings are stored in user-scoped configuration and persist between sessions.

---

## Key Dependencies

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| CsvHelper | 33.0.1 | CSV file parsing |
| EntityFramework | 6.5.1 | ORM framework |
| Microsoft.Owin | 4.2.2 | OWIN hosting |
| Microsoft.Owin.Host.HttpListener | 4.2.2 | HTTP listener |
| System.Web.Http | 5.2.9 | Web API framework |
| System.Web.Http.Owin | 5.2.9 | Web API/OWIN integration |
| Telerik.WinControls | 2023.1.117.48 | UI controls library |
| Newtonsoft.Json | 13.0.0 | JSON serialization |

### Telerik Controls Used

- `RadForm` - Main form base class
- `RadGridView` - Ticket management grid
- `RadButton` - Enhanced buttons
- `RadSpinEditor` - Numeric spinners
- `RadLabel` - Enhanced labels
- `RadPageView` - Tab control
- `RadProgressBar` - Import progress display
- `RadMessageBox` - Modal dialogs

---

## Business Logic Flows

### Complete Raffle Workflow

```
1. PREPARATION
   ├─ Import CSV file with ticket data
   ├─ Verify tickets in Ticket Management grid
   ├─ Configure winner settings
   └─ Optionally start API server

2. BEGIN RAFFLE
   ├─ Click "Begin Raffle" button
   ├─ Number board opens with all tickets displayed
   ├─ Grid becomes read-only (no editing during raffle)
   └─ Statistics displayed

3. DRAW TICKETS (repeat until complete)
   ├─ Enter ticket number (manual or via API)
   ├─ Validate ticket exists and not already drawn
   ├─ Increment counter
   ├─ Check if periodic winner
   │  ├─ YES: Show winner popup, reset counter
   │  └─ NO: Show loser popup
   ├─ Mark ticket with red X on board
   ├─ Update database (MarkedAsNotWinner = true)
   ├─ Update statistics
   └─ Check if raffle complete
      ├─ YES: Show final winners, set RaffleIsOver=true
      └─ NO: Continue drawing

4. END RAFFLE
   ├─ Click "End Raffle" button
   ├─ Close number board
   ├─ Re-enable grid editing
   └─ Reset statistics
```

### Winner Detection Algorithm

**Implemented in:** `frmNumberBoard.MarkTicketAsLosing()` (Lines 89-122)

```
When ticket is marked as losing:
    ↓
Calculate TotalTicketsNotDrawn = Total - Losing
    ↓
IF (TotalTicketsNotDrawn == AvailableWinners)
    ↓
    FOR EACH ticket WHERE MarkedAsNotWinner is NULL or FALSE
        ├─ Add ticket number to winners list
        ├─ Collect TicketHolder1 (if not duplicate)
        ├─ Collect TicketHolder2 (if not duplicate)
        └─ Collect TicketHolder3 (if not duplicate)
    ↓
    Show frmTicketWinner with:
        ├─ Comma-separated ticket numbers
        └─ List of unique holder names
    ↓
    Set RaffleIsOver = TRUE
```

**Example:**
- Total tickets: 100
- Available winners: 2
- After 98 tickets drawn: TotalTicketsNotDrawn = 2
- Condition met: Remaining 2 tickets are winners

---

## Common Operations

### Reset Database
**Location:** Settings Tab > "Reset Database w/ 100 Tickets" button

**Action:**
- Deletes all existing tickets
- Creates 100 test tickets (numbers 1-100)
- All names set to "sample name"

### Reset Losing Flags
**Location:** Settings Tab > "Reset Flag: Marked as Not Winner" button

**Action:**
- Sets `MarkedAsNotWinner = NULL` for all tickets
- Allows re-running raffle with same tickets
- Does not delete tickets

### Manual Ticket Management
**Location:** Ticket Management Tab

**Features:**
- Add new tickets manually (auto-assigns next number)
- Edit ticket holder names
- Delete tickets
- Real-time database sync

---

## Known Issues & Quirks

1. **Settings Typo**
   - `ticketHeightt` has double 't' in config
   - Code correctly references typo version

2. **Unused Field**
   - `TicketHolder4` exists in database but not used in UI or CSV import

3. **Static API Field**
   - `TicketController.currentTicketNumber` is static
   - Potential thread-safety issue with concurrent API requests
   - Only one ticket can be queued at a time

4. **Import Deletes Data**
   - CSV import deletes ALL existing tickets
   - Not additive - completely replaces database
   - No warning dialog before deletion

5. **Generic Exception Handling**
   - Import form catches all exceptions with same message
   - Difficult to diagnose specific CSV format issues

6. **No Transaction Support**
   - Database operations don't use transactions
   - Risk of partial updates if errors occur

---

## File References Quick Index

### Core Application Files
- `Program.cs` - Entry point (25 lines)
- `frmConsole.cs` - Main form (464 lines)
- `frmNumberBoard.cs` - Display board (192 lines)
- `ctlNumberSheet.cs` - Ticket control (100 lines)

### Import & Data
- `frmImport.cs` - CSV import (132 lines)
- `ImportTicketItem.cs` - Import DTO (20 lines)
- `TicketHolder.cs` - Entity model (24 lines)
- `RevRaffleModel.Context.cs` - DbContext (30 lines)

### Display Forms
- `frmTicketWinner.cs` - Winner announcement (72 lines)
- `frmTicketNotWinner.cs` - Loser announcement (71 lines)
- `frmTicketDetails.cs` - Ticket info popup (42 lines)
- `frmAbout.cs` - About dialog (128 lines)

### API
- `Api/ApiConfig.cs` - OWIN config (28 lines)
- `Api/Controllers/TicketController.cs` - REST endpoint (21 lines)

### Configuration
- `App.config` - Application settings (78 lines)
- `Properties/Settings.Designer.cs` - Settings code-gen

---

## Summary

ReverseRaffler is a well-structured Windows Forms application that successfully implements reverse raffle functionality with:

- **Visual Feedback:** Real-time ticket display board with color-coded status
- **Database Persistence:** SQL Server LocalDB with Entity Framework 6
- **Flexible Import:** CSV-based bulk ticket loading
- **Configurable Logic:** Periodic winners, auto-close timers, display settings
- **Remote Control:** REST API for ticket submission from external systems
- **User-Friendly:** Settings storage, progress tracking, automatic cleanup

The application follows a clear separation of concerns between UI (forms/controls), data (EF models), and business logic (raffle algorithms). The architecture supports both manual operation and automated/remote control scenarios.
