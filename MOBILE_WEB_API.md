# Mobile Web API Interface

## Summary
Implemented a mobile-friendly web interface that allows users to submit ticket numbers remotely via their mobile devices. The API server binds to all local IP addresses and serves an HTML page at the root URL.

---

## Features Implemented

### 1. **API Binding to All Network Interfaces**
- Changed API binding from specific IP to **all interfaces** (0.0.0.0)
- Uses `http://+:{port}/` format for binding to all IPs
- Default port: **6060**
- Displays all available IPv4 addresses when server starts

### 2. **Simplified Settings**
- **Old**: "API Bind IP:Port" required full address (e.g., "192.168.1.100:6060")
- **New**: Only requires port number (e.g., "6060")
- Automatically detects and displays all local IPs when server starts

### 3. **Mobile-Friendly Web Interface**
- Accessible at: `http://{any-local-ip}:{port}/` (e.g., http://192.168.1.100:6060/)
- Responsive design that works on all screen sizes
- Clean, modern UI with gradient background
- Single-screen interface with input and submit button on same line

### 4. **Ticket Submission Integration**
- Mobile submissions use same endpoint as existing API: `/api/ticket?number={ticketNumber}`
- Processes tickets exactly like manual console entry
- Shows success/error messages with auto-hide after 3 seconds
- Clears input and refocuses for rapid successive entries

---

## Technical Implementation

### Files Created

#### **Api/Controllers/HomeController.cs**
```csharp
[HttpGet]
[Route("")]
public HttpResponseMessage Get()
```
- Serves mobile-friendly HTML page at root URL
- Uses inline CSS and JavaScript for single-file deployment
- Responsive design with viewport meta tag
- Gradient purple background (matches app theme)
- AJAX submission using Fetch API

### Files Modified

#### **Api/ApiConfig.cs**
Added attribute routing support:
```csharp
// Map attribute routes first (for root path)
config.MapHttpAttributeRoutes();
```

#### **frmConsole.cs**
1. Added network discovery:
   - `GetLocalIPAddresses()` - Finds all local IPv4 addresses
   - Uses System.Net.Dns and System.Net.Sockets

2. Updated `radButton1_Click()`:
   - Defaults port to "6060" if empty
   - Binds to all IPs: `http://+:{port}/`
   - Displays all available URLs in `radLabel5`
   - Improved error handling with Administrator suggestion

#### **frmConsole.Designer.cs**
Updated `radLabel5` configuration:
- Increased height from 22 to 48 pixels
- Allows display of 2-3 lines of IP addresses
- Position adjusted from Y=172 to Y=166

---

## User Experience

### Starting the API Server

1. **Navigate to**: Settings tab
2. **Enter port**: (default 6060 if left empty)
3. **Click**: "Start API Server" button

### After Server Starts

The label below the button displays all available URLs:
```
Available on:
http://192.168.1.100:6060
http://10.0.0.50:6060
```

### Mobile Device Usage

1. **Connect**: Ensure mobile device is on same network
2. **Navigate**: Open browser and go to any displayed URL
3. **Enter ticket**: Type ticket number in input field
4. **Submit**: Click submit button (or press Enter)
5. **Confirmation**: Success/error message appears for 3 seconds
6. **Repeat**: Input clears automatically for next entry

---

## HTML Interface Details

### Design Features
- **Responsive**: Works on phones, tablets, and desktops
- **Gradient background**: Purple gradient (matches app theme)
- **Card layout**: White card with rounded corners and shadow
- **Input styling**:
  - Large, touch-friendly input field (18px font)
  - Number pad on mobile devices (type="number")
  - Auto-focus on page load
- **Button styling**:
  - Purple gradient matching background
  - Hover effects for desktop
  - Touch-friendly size for mobile

### Accessibility
- Semantic HTML with proper labels
- Focus management (auto-focus input, refocus after submit)
- Clear error messages
- Works without JavaScript enabled (graceful degradation)

---

## API Endpoints

### Root Path
- **URL**: `GET /`
- **Returns**: HTML page (text/html)
- **Purpose**: Serves mobile interface

### Ticket Submission
- **URL**: `GET /api/ticket?number={ticketNumber}`
- **Returns**: HTTP 200 (OK) or error
- **Purpose**: Process ticket submission (existing endpoint)

---

## Security Considerations

### Network Binding
- Binds to all network interfaces (0.0.0.0)
- **Requires Administrator privileges** on some systems
- Error message guides user to run as Administrator if needed

### CORS
- Not implemented (same-origin policy applies)
- Mobile devices must be on same local network
- For external access, consider adding authentication

### Input Validation
- JavaScript validates input before submission
- Server-side validation in existing TicketController
- Number-only input field on mobile devices

---

## Troubleshooting

### "Failed to start API server"
**Solution**: Run application as Administrator
- Right-click ReverseRaffler.exe
- Select "Run as administrator"

### "Connection error" on mobile
**Solutions**:
1. Ensure mobile device is on same Wi-Fi network
2. Check firewall isn't blocking port 6060
3. Verify IP address is correct (check displayed URLs)

### No IP addresses shown
**Fallback**: Application shows 127.0.0.1 if detection fails
- Try restarting with Administrator privileges
- Check network adapter is enabled

---

## Testing Checklist

- [ ] Start API server with default port (6060)
- [ ] Verify multiple IP addresses displayed
- [ ] Open mobile browser and navigate to displayed URL
- [ ] HTML page loads correctly on mobile
- [ ] Submit ticket number via mobile interface
- [ ] Verify ticket is processed in main application
- [ ] Check success message appears on mobile
- [ ] Verify input clears and refocuses after submission
- [ ] Test error handling (invalid ticket, network error)
- [ ] Stop API server and verify cleanup

---

## Future Enhancements

### Possible Improvements
1. **Authentication**: Add password/PIN for mobile access
2. **HTTPS**: Secure connection with SSL certificate
3. **QR Code**: Display QR code for easy mobile connection
4. **Ticket History**: Show recent submissions on mobile
5. **Real-time Updates**: WebSocket connection for live raffle status
6. **Multi-language**: Internationalization support

---

## Code References

### Network Discovery
- **File**: frmConsole.cs
- **Method**: `GetLocalIPAddresses()` (lines 570-586)
- **Purpose**: Find all local IPv4 addresses

### API Server Startup
- **File**: frmConsole.cs
- **Method**: `radButton1_Click()` (lines 492-568)
- **Binding**: `http://+:{port}/`

### Mobile HTML Interface
- **File**: Api/Controllers/HomeController.cs
- **Lines**: 18-200
- **Rendering**: Inline CSS and JavaScript

### Attribute Routing
- **File**: Api/ApiConfig.cs
- **Line**: 21 (`config.MapHttpAttributeRoutes()`)

---

**Status**: ✅ Complete - Ready for testing on Windows
