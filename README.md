# Planning Poker Application

A **real-time collaborative Planning Poker application** used by software development teams for **story point estimation** during agile sprint planning sessions.

## Table of Contents

- [Core Business Purpose](#core-business-purpose)
- [User Roles & Personas](#user-roles--personas)
- [Room Management](#room-management)
- [Voting System](#voting-system)
- [Statistics & Analytics](#statistics--analytics)
- [Real-time Communication](#real-time-communication)
- [User Experience](#user-experience)
- [Security & Validation](#security--validation)
- [Configuration & Customization](#configuration--customization)
- [Technical Architecture](#technical-architecture)
- [Performance & Scalability](#performance--scalability)
- [Getting Started](#getting-started)
- [API Endpoints](#api-endpoints)

## Core Business Purpose

This application enables software development teams to conduct efficient, collaborative story point estimation sessions using the Planning Poker technique. Teams can vote anonymously on story complexity, view comprehensive statistics, and reach consensus through real-time collaboration.

## User Roles & Personas

### User Types
- **Developers (dev)** - Primary voters, can cast votes and see all statistics
- **Testers (test)** - Secondary voters, can cast votes and see all statistics  
- **Observers (obs)** - Non-voting participants, can watch but cannot vote

### Room Ownership
- **Room Owner** - First person to create the room, has administrative privileges
- **Ownership Transfer** - Can transfer ownership to other participants
- **Auto-transfer** - If owner leaves, ownership automatically transfers to another participant

## Room Management

### Room Creation
- **Multiple Creation Methods:**
  - Standard GUID-based rooms (`/Room/{guid}`)
  - Fun named rooms using creative name generation (e.g., "42_amazing_red_elephant")
- **Room Configuration Options:**
  - Card set selection (7 different sets available)
  - Countdown duration (1-30 seconds)
  - Fireworks celebrations (enable/disable)
  - Maximum participants (2-100, default 50)

### Room Access
- **Direct URL Access** - Users can join via `/Room/{room-id}` 
- **Session Management** - Server-side session tracking for user persistence
- **Name Input Form** - New users enter their name and select role type
- **Auto-join** - Returning users with valid sessions join automatically

### Room Lifecycle
- **Health Checks** - 5-second intervals to detect disconnected users
- **Auto-cleanup** - Removes inactive participants
- **Room Disposal** - Rooms are disposed when empty
- **Connection Recovery** - Automatic reconnection with exponential backoff

## Voting System

### Available Card Sets (7 Sets)

1. **Fibonacci** - 0, 0.5, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, ?, ☕
2. **Modified Fibonacci** - 0, 0.5, 1, 2, 3, 5, 8, 13, 20, 40, 100, ?, ☕
3. **Linear** - 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, ?, ☕
4. **Powers of 2** - 0, 1, 2, 4, 8, 16, 32, 64, 128, ?, ☕
5. **T-Shirt Sizes** - XS, S, M, L, XL, XXL, ?, ☕
6. **Traffic Lights** - 🟢, 🟡, 🔴
7. **Fist of Five** - Hand gesture images (0-5 fingers)

### Voting Process
- **Hidden Votes** - Votes are hidden until revealed
- **Vote Indicators** - Shows ▮ when someone has voted
- **Real-time Updates** - Instant notification when votes are cast
- **Vote Changes** - Users can change votes before reveal
- **Force Reveal** - Manual vote revelation by room owner/admin

### Countdown System
- **Auto-reveal** - Automatic vote revelation after countdown expires
- **Configurable Duration** - 0-30 seconds (0 = instant reveal, default: 5 seconds)
- **Countdown Display** - Real-time countdown timer
- **Vote Editing** - Users can still change votes during countdown

## Statistics & Analytics

### Vote Analysis
- **Overall Statistics** - All participants combined
- **Role-based Statistics** - Separate stats for Developers vs Testers
- **Average Calculations** - Numeric values only (excludes ?, ☕)
- **Percentage Distribution** - Vote distribution with percentages
- **Consensus Detection** - Identifies when everyone votes the same

### Data Visualization
- **Bar Charts** - Visual representation using Chart.js
- **Color Coding** - Green for highest percentage, blue for others
- **Image Support** - Can display PNG images in charts (Fist of Five)
- **Responsive Charts** - Auto-update with vote changes

### Celebration Features
- **Fireworks Animation** - Triggers on 100% consensus (configurable)
- **Multiple Bursts** - 10 firework bursts over 10 seconds
- **Participation Threshold** - Only triggers with 2+ eligible voters

## Real-time Communication

### SignalR Integration
- **WebSocket Connection** - Real-time bidirectional communication
- **Group Management** - Users join room-specific groups
- **Connection Recovery** - Automatic reconnection with retry logic
- **Health Monitoring** - Regular connection health checks

### Real-time Events
- **Vote Cast** - Immediate notification of new votes
- **Vote Revealed** - Broadcast when votes are shown
- **User Join/Leave** - Real-time participant updates
- **Configuration Changes** - Live updates of room settings
- **Ownership Transfer** - Instant ownership change notifications

## User Experience

### Client-side Caching
- **Name Persistence** - User names cached locally and in server session
- **Room Preferences** - Creation settings cached (card set, countdown, etc.)
- **Card Set Caching** - 24-hour cache for available card sets
- **Cross-session Sync** - Client and server-side name synchronization

### User Interface
- **Responsive Design** - Works on desktop and mobile
- **Participant Tables** - Separate tables for Devs, Testers, Observers
- **Vote Cards** - Interactive card selection interface
- **Settings Modal** - Room configuration interface
- **Connection Status** - Visual connection state indicators

### Accessibility & Usability
- **Unicode Support** - Non-ASCII character encoding
- **Input Validation** - XSS prevention and input sanitization
- **Error Handling** - Graceful failure handling
- **Loading States** - User feedback during async operations

## Security & Validation

### Input Security
- **XSS Prevention** - Sanitization of dangerous characters (`<`, `>`, `"`, `'`, `&`, etc.)
- **Input Length Limits** - Max 20 chars for names, 100 for room IDs
- **Character Encoding** - Non-ASCII character handling
- **Regex Validation** - Room name format validation

### Session Security
- **Server-side Sessions** - Secure session management
- **GUID-based Identity** - Unique user identification
- **Connection Validation** - Connection ID verification
- **Room Access Control** - Valid room existence checks

## Configuration & Customization

### Room Settings
- **Card Set Selection** - Choose from 7 predefined sets
- **Countdown Timing** - 0-30 seconds (0 = instant reveal)
- **Fireworks Toggle** - Enable/disable celebration animations
- **Participant Limits** - Maximum room capacity (2-100, default: 50)
- **Ownership Management** - Transfer room ownership

### System Configuration
- **Default Settings** - Fallback values for all configurations
- **Timer Management** - Configurable timer intervals
- **Health Check Frequency** - Connection monitoring intervals (5 seconds)
- **Cache Duration** - Client-side cache expiration times (24 hours for card sets)

## Technical Architecture

### Backend Requirements
- **ASP.NET Core 6.0** - Web framework
- **SignalR** - Real-time communication
- **In-memory Storage** - ConcurrentDictionary for rooms/users
- **Session Management** - HTTP session-based user tracking
- **Timer Management** - Custom timer implementation

### Frontend Requirements
- **Vanilla JavaScript** - No framework dependencies
- **Chart.js** - Data visualization library
- **SignalR Client** - Real-time connectivity
- **LocalStorage** - Client-side data persistence
- **Fireworks.js** - Animation library for celebrations

### Data Models
- **Room** - Central entity with voting, timers, configuration
- **Person** - User with role, connection state, identity
- **Vote** - Individual vote with user association
- **Statistics** - Aggregated voting results and analytics
- **Configuration** - Room-specific settings and preferences

## Performance & Scalability

### Optimization Features
- **Concurrent Collections** - Thread-safe data structures
- **Connection Pooling** - Efficient SignalR connection management
- **Cache Strategy** - Client-side caching to reduce server load
- **Timer Optimization** - Proper cleanup to prevent memory leaks

### Monitoring & Health
- **Connection Health Checks** - Regular connectivity validation
- **Automatic Cleanup** - Remove inactive users and rooms
- **Error Logging** - Console-based debugging and error tracking
- **Performance Metrics** - Timer-based performance monitoring

## Getting Started

### Prerequisites
- .NET 6.0 SDK
- Modern web browser with WebSocket support

### Installation
1. Clone the repository
2. Navigate to `PlanningPokerUi` directory
3. Run `dotnet restore`
4. Run `dotnet run`
5. Open browser to `https://localhost:5001`

### Creating a Room
1. Visit the main page (`/`)
2. Enter your name and select role (Dev/Tester/Observer)
3. Click "⚙️ More Settings" to configure room options
4. Click "Create room"

### Joining a Room
1. Access room via direct URL: `/Room/{room-id}`
2. Enter your name and select role
3. Click "Join room"

## API Endpoints

### REST Endpoints
- `GET /` - Main page for room creation
- `GET /Room/{guid}` - Room access (shows join form or room)
- `POST /Room/{guid}` - Join room with user details
- `GET /api/cardsets` - Get available card sets
- `POST /api/updatename` - Update user name in session

### SignalR Hub Methods
- `JoinRoom(guid)` - Join a specific room
- `Vote(mark)` - Cast a vote
- `ClearVotes()` - Clear all votes (owner only)
- `ForceShowVotes()` - Reveal votes immediately (owner only)
- `ChangePersonType(type)` - Change user role
- `UpdateRoomConfiguration(config)` - Update room settings (owner only)
- `TransferOwnership(targetPersonGuid)` - Transfer room ownership (owner only)
- `HealthCheck()` - Respond to health check ping

### SignalR Events (Client Receives)
- `RoomJoined` - Successful room join with participant list
- `PersonJoined` - New participant joined
- `PersonExited` - Participant left
- `VoteCast` - Someone cast a vote
- `VoteStatusUpdate` - Voting progress update
- `VotesShown` - Votes revealed with statistics
- `VotesCleared` - All votes cleared
- `CountdownUpdate` - Timer countdown progress
- `PersonTypeChanged` - Someone changed their role
- `RoomConfigurationUpdated` - Settings changed
- `OwnershipTransferred` - Room ownership changed
- `RoomDisposed` - Room was closed
- `HealthCheckRequest` - Server requesting health check response

## File Structure

```
PlanningPokerUi/
├── Controllers/
│   ├── IndexController.cs      # Main page and API endpoints
│   └── RoomController.cs       # Room access and joining
├── Hubs/
│   └── RoomHub.cs             # SignalR real-time communication
├── Models/
│   ├── Room.cs                # Core room entity
│   ├── Person.cs              # User representation
│   ├── Vote.cs                # Individual vote
│   ├── Statistics.cs          # Vote analytics
│   ├── CardSets.cs            # Available card sets
│   └── RoomConfiguration.cs   # Room settings
├── Services/
│   ├── RoomsManagerService.cs # Room lifecycle management
│   ├── PeopleManagerService.cs # User session management
│   └── RoomNameGenerator.cs   # Fun room name generation
├── Pages/
│   ├── Index.cshtml           # Main landing page
│   └── Shared/
│       ├── Room.cshtml        # Main poker room interface
│       ├── RoomJoin.cshtml    # Join room form
│       └── _RoomSettings.cshtml # Room configuration modal
└── wwwroot/
    └── js/
        └── site.js            # Client-side functionality
```

---

This Planning Poker application serves as a comprehensive tool for agile teams to conduct efficient, real-time story point estimation sessions with rich statistics, multiple voting systems, and robust real-time collaboration features.