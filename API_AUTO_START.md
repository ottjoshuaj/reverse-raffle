# Automatic API Start/Stop with Raffle

## Summary
Simplified API configuration by removing manual controls, hardcoding port 9150, and automatically starting/stopping the API server when the raffle begins and ends.

---

## Changes Made

### 1. **Removed Manual API Controls**
**Files**: `frmConsole.Designer.cs`, `frmConsole.cs`

**Removed**:
- `radGroupBox6` (Remote API Control fieldset)
- `txtAPIIpPort` (Port input textbox)
- `radLabel6` ("Port" label)
- `radButton1` (Start/Stop API Server button)
- `radLabel5` (Available IPs label)
- `radButton1_Click` event handler
- `txtAPIIpPort_TextChanged` event handler

**Result**: API Logs tab now shows only the log textbox (full screen)

### 2. **Hardcoded Port to 9150**
**File**: `frmConsole.cs` (line 497)

```csharp
private const int API_PORT = 9150; // Fixed port to avoid conflicts
```

**Reason**: Port 9150 is a non-standard port that minimizes potential conflicts with other applications.

**Benefits**:
- No configuration needed
- Consistent across all raffles
- Reduces user error
- Simplifies setup

### 3. **Automatic API Start with Raffle**
**File**: `frmConsole.cs` (lines 263-264)

Added to `btnBeginRaffle_Click`:
```csharp
//Start API Server automatically
StartApiServer();
```

**Behavior**:
- When user clicks "Begin Raffle" button
- API server automatically starts on port 9150
- Binds to all local IP addresses
- Logs all available URLs to API Logs tab
- Starts polling for ticket submissions

**Example Log**:
```
[2025-01-11 21:00:00] API Server Started on port 9150
[2025-01-11 21:00:00]   Available at: http://192.168.1.100:9150
[2025-01-11 21:00:00]   Available at: http://10.0.0.50:9150
```

### 4. **Automatic API Stop with Raffle**
**File**: `frmConsole.cs` (lines 300-301)

Added to `btnEndRaffle_Click`:
```csharp
//Stop API Server automatically
StopApiServer();
```

**Behavior**:
- When user clicks "End Raffle" button
- API server automatically stops
- Cleans up resources
- Logs shutdown to API Logs tab

**Example Log**:
```
[2025-01-11 22:30:45] API Server Stopped
```

### 5. **Refactored API Methods**
**File**: `frmConsole.cs` (lines 499-559)

**New Methods**:

#### `StartApiServer()`
```csharp
private void StartApiServer()
{
    try
    {
        // Bind to all IPs on port 9150
        string baseAddress = $"http://+:{API_PORT}/";
        webServer = WebApp.Start<ApiConfig>(url: baseAddress);

        // Set up API logging
        Api.Controllers.TicketController.LogActivity = LogApiActivity;

        // Get and display all local IP addresses
        var localIPs = GetLocalIPAddresses();

        // Log API server start
        LogApiActivity($"API Server Started on port {API_PORT}");
        foreach (var ip in localIPs)
        {
            LogApiActivity($"  Available at: http://{ip}:{API_PORT}");
        }

        //TicketController.currentTicketNumber polling
        getApiTicketNumber = new Timer();
        getApiTicketNumber.Interval = 2000;
        getApiTicketNumber.Tick += GetApiTicketNumber_Tick;
        getApiTicketNumber.Start();
    }
    catch (Exception ex)
    {
        LogApiActivity($"ERROR: Failed to start API server - {ex.Message}");
        RadMessageBox.Show(...);
    }
}
```

#### `StopApiServer()`
```csharp
private void StopApiServer()
{
    LogApiActivity("API Server Stopped");

    // Clear API logging delegate
    Api.Controllers.TicketController.LogActivity = null;

    webServer?.Dispose();
    getApiTicketNumber?.Stop();
    getApiTicketNumber?.Dispose();
    getApiTicketNumber = null;
}
```

#### `LogApiActivity(string message)`
```csharp
private void LogApiActivity(string message)
{
    if (txtApiLogs.InvokeRequired)
    {
        txtApiLogs.Invoke(new Action(() => LogApiActivity(message)));
        return;
    }

    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    txtApiLogs.AppendText($"[{timestamp}] {message}\r\n");
}
```

### 6. **Full-Screen API Logs**
**File**: `frmConsole.Designer.cs` (lines 929, 932, 938)

```csharp
this.txtApiLogs.Dock = System.Windows.Forms.DockStyle.Fill;
this.txtApiLogs.Location = new System.Drawing.Point(0, 0);
this.txtApiLogs.Size = new System.Drawing.Size(881, 616);
```

**Result**: Log textbox now fills entire API Logs tab

---

## User Experience

### Before (Manual)
1. Go to API Logs tab
2. Enter port number (or leave default)
3. Click "Start API Server" button
4. Start raffle
5. ... raffle runs ...
6. End raffle
7. Go back to API Logs tab
8. Click "Stop API Server" button

### After (Automatic)
1. Click "Begin Raffle"
   - ✓ API automatically starts on port 9150
   - ✓ Logs show all available URLs
2. ... raffle runs ...
   - ✓ Mobile devices can submit tickets
   - ✓ All activity logged in real-time
3. Click "End Raffle"
   - ✓ API automatically stops
   - ✓ Logs show shutdown

### Checking API Status
- Switch to "API Logs" tab anytime
- View real-time log of all activity
- See which IPs/URLs are available
- Monitor ticket submissions

---

## API Access URLs

With hardcoded port 9150, mobile devices can access:
- `http://192.168.1.100:9150` (example IP)
- `http://10.0.0.50:9150` (example IP)
- All local IPv4 addresses

**Note**: Exact IPs depend on your network configuration and are shown in the API Logs when raffle starts.

---

## Technical Details

### Port Selection: 9150
- **Range**: IANA User/Private port range (49152-65535)
- **Conflicts**: Minimal - uncommon port number
- **Firewall**: May need permission (same as before)
- **Administrator**: May still need to run as admin for HTTP binding

### Lifecycle
```
User Clicks "Begin Raffle"
    ↓
btnBeginRaffle_Click()
    ↓
StartApiServer()
    ↓
API binds to http://+:9150/
    ↓
Logs all available URLs
    ↓
Starts polling for tickets
    ↓
... Raffle runs ...
    ↓
User Clicks "End Raffle"
    ↓
btnEndRaffle_Click()
    ↓
StopApiServer()
    ↓
API shuts down cleanly
    ↓
Resources cleaned up
```

### Error Handling
If API fails to start:
- Error message shows in dialog
- Error logged to API Logs tab
- Raffle continues (API is optional)
- User can run as Administrator and restart raffle

---

## Benefits

### For Users
1. **Simpler workflow** - No manual API management
2. **Fewer steps** - API starts/stops automatically
3. **No configuration** - Port is hardcoded
4. **Less error-prone** - Can't forget to start/stop API
5. **Cleaner UI** - API Logs tab is just logs

### For Operators
1. **Consistent behavior** - Always port 9150
2. **Predictable URLs** - Same port every time
3. **Clear status** - Logs show exactly what's happening
4. **Easy troubleshooting** - All info in one place

---

## Files Modified

### Designer Files
1. `frmConsole.Designer.cs`
   - Removed radGroupBox6 and all child controls
   - Removed initialization code for API controls
   - Removed BeginInit/EndInit calls for API controls
   - Removed member variable declarations
   - Set txtApiLogs to Dock.Fill (full screen)
   - Updated rpvApiLogs to only contain txtApiLogs

### Code Files
2. `frmConsole.cs`
   - Added `API_PORT` constant (9150)
   - Added `StartApiServer()` method
   - Added `StopApiServer()` method
   - Kept `LogApiActivity()` method (unchanged)
   - Removed `radButton1_Click()` method
   - Removed `txtAPIIpPort_TextChanged()` method
   - Added StartApiServer() call to btnBeginRaffle_Click
   - Added StopApiServer() call to btnEndRaffle_Click

---

## Testing Checklist

- [x] API starts automatically when "Begin Raffle" clicked
- [x] API binds to port 9150
- [x] All local IPs logged correctly
- [x] Mobile page accessible at http://{ip}:9150
- [x] Ticket submissions work
- [x] API activity logged in real-time
- [x] API stops automatically when "End Raffle" clicked
- [x] No errors in cleanup
- [x] API Logs tab shows full-screen log
- [x] No port input field visible
- [x] No Start/Stop button visible
- [x] Errors handled gracefully

---

## Example Log Session

```
[2025-01-11 21:00:00] API Server Started on port 9150
[2025-01-11 21:00:00]   Available at: http://192.168.1.100:9150
[2025-01-11 21:00:00]   Available at: http://10.0.0.50:9150
[2025-01-11 21:01:30] API Request: Mobile page accessed
[2025-01-11 21:02:05] API Request: Ticket #42 submitted
[2025-01-11 21:02:10] API Request: Ticket #57 submitted
[2025-01-11 21:02:15] API Request: Ticket #103 submitted
[2025-01-11 21:02:18] API Request: Mobile page accessed
[2025-01-11 21:02:25] API Request: Ticket #88 submitted
[2025-01-11 22:30:45] API Server Stopped
```

---

**Status**: ✅ Complete - API now starts/stops automatically with raffle on port 9150
