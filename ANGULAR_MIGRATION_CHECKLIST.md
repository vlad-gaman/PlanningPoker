# Angular Migration Checklist for Planning Poker Application

## Overview
This checklist provides a step-by-step migration plan from the current ASP.NET Core MVC/Razor Pages application to Angular frontend with .NET WebAPI backend, maintaining web sessions and SignalR functionality. The application will be deployed to a single Azure Web App.

## Current Architecture Analysis
- **Frontend**: ASP.NET Core Razor Pages with jQuery and SignalR client
- **Backend**: ASP.NET Core with SignalR Hub, session management, in-memory storage
- **Key Features**: Real-time voting, room management, statistics, multiple card sets
- **Deployment**: Single Azure Web App with Docker support

---

## Migration Strategy Overview

### Phase 1: Backend API Foundation
**Goal**: Create WebAPI controllers while maintaining existing functionality
**Working State**: Hybrid system with both Razor Pages and API endpoints

### Phase 2: Angular Frontend Setup
**Goal**: Initialize Angular application with basic structure
**Working State**: Angular app can call APIs but Razor Pages still functional

### Phase 3: Core Feature Migration
**Goal**: Migrate room and voting functionality to Angular
**Working State**: Core voting functionality works in Angular

### Phase 4: Real-time Features
**Goal**: Integrate SignalR with Angular
**Working State**: Full real-time functionality in Angular

### Phase 5: Complete Migration
**Goal**: Remove Razor Pages, finalize deployment
**Working State**: Full Angular application deployed to Azure

---

## Phase 1: Backend API Foundation (Milestone 1)

### 1.1 Create WebAPI Controllers
- [ ] **Task**: Create `RoomApiController.cs`
  - Endpoints: `GET /api/rooms/{id}`, `POST /api/rooms/create`, `POST /api/rooms/{id}/join`
  - Maintain session handling for user identity
  - Keep existing `RoomsManagerService` logic
  
- [ ] **Task**: Create `UserApiController.cs`
  - Endpoints: `GET /api/user/profile`, `PUT /api/user/name`, `PUT /api/user/type`
  - Integrate with existing `PeopleManagerService`
  
- [ ] **Task**: Create `VotingApiController.cs`
  - Endpoints: `POST /api/rooms/{id}/vote`, `POST /api/rooms/{id}/clear-votes`, `POST /api/rooms/{id}/reveal-votes`
  - Maintain SignalR integration for real-time updates

### 1.2 Update Startup Configuration
- [ ] **Task**: Add CORS policy for development and production
  ```csharp
  services.AddCors(options => {
      options.AddPolicy("Development", builder => {
          builder.WithOrigins("http://localhost:4200", "https://localhost:4200")
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials();
      });
      options.AddPolicy("Production", builder => {
          builder.WithOrigins("https://yourdomain.azurewebsites.net")
                 .AllowAnyHeader()
                 .AllowAnyMethod()
                 .AllowCredentials();
      });
  });
  ```
- [ ] **Task**: Configure API routing alongside existing MVC routes
- [ ] **Task**: Add session configuration for API calls
  ```csharp
  services.AddSession(options => {
      options.Cookie.SameSite = SameSiteMode.None;
      options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
      options.Cookie.HttpOnly = true;
  });
  ```

### 1.3 Session Management for API
- [ ] **Task**: Create `SessionService.cs` for consistent session handling
- [ ] **Task**: Add session validation middleware for API endpoints
  ```csharp
  public class SessionValidationMiddleware
  {
      public async Task InvokeAsync(HttpContext context, RequestDelegate next)
      {
          if (context.Request.Path.StartsWithSegments("/api"))
          {
              if (string.IsNullOrEmpty(context.Session.GetString("UserId")))
              {
                  context.Response.StatusCode = 401;
                  return;
              }
          }
          await next(context);
      }
  }
  ```
- [ ] **Task**: Add CSRF protection for API endpoints
  ```csharp
  services.AddAntiforgery(options => {
      options.HeaderName = "X-XSRF-TOKEN";
      options.Cookie.SameSite = SameSiteMode.None;
  });
  ```
- [ ] **Task**: Test API endpoints maintain user sessions correctly

### 1.4 Testing Phase 1
- [ ] **Task**: Test all API endpoints with Postman/Swagger
- [ ] **Task**: Verify existing Razor Pages functionality still works
- [ ] **Task**: Confirm session persistence across API and MVC calls

**Milestone 1 Completion**: Hybrid system with working API endpoints and existing Razor Pages functionality.

---

## Phase 2: Angular Frontend Setup (Milestone 2)

### 2.1 Angular Project Initialization
- [ ] **Task**: Create Angular project in `ClientApp` folder
  ```bash
  ng new ClientApp --routing --style=css --skip-git
  cd ClientApp
  npm install @microsoft/signalr chart.js fireworks-js
  npm install @types/signalr @types/chart.js --save-dev
  ```

### 2.2 Angular Project Structure
- [ ] **Task**: Create folder structure:
  ```
  src/
  ├── app/
  │   ├── core/
  │   │   ├── services/
  │   │   └── models/
  │   ├── features/
  │   │   ├── room/
  │   │   ├── voting/
  │   │   └── statistics/
  │   ├── shared/
  │   │   ├── components/
  │   │   └── pipes/
  │   └── environments/
  ```

### 2.3 Core Services Setup
- [ ] **Task**: Create `ApiService` for HTTP calls to WebAPI
- [ ] **Task**: Create `SignalRService` for real-time communication
- [ ] **Task**: Create `SessionService` for user session management
- [ ] **Task**: Create TypeScript models matching C# models

### 2.4 Development Environment Setup
- [ ] **Task**: Configure proxy for Angular dev server to ASP.NET Core
  - Create `proxy.conf.json`:
    ```json
    {
      "/api/*": {
        "target": "https://localhost:5001",
        "secure": true,
        "changeOrigin": true,
        "logLevel": "debug"
      },
      "/hubs/*": {
        "target": "https://localhost:5001",
        "secure": true,
        "changeOrigin": true,
        "ws": true
      }
    }
    ```
  - Update `angular.json` serve options:
    ```json
    "serve": {
      "builder": "@angular-devkit/build-angular:dev-server",
      "options": {
        "proxyConfig": "proxy.conf.json"
      }
    }
    ```
- [ ] **Task**: Create environment configuration files
  - `src/environments/environment.ts`:
    ```typescript
    export const environment = {
      production: false,
      apiUrl: '/api',
      signalRUrl: '/hubs'
    };
    ```
  - `src/environments/environment.prod.ts`:
    ```typescript
    export const environment = {
      production: true,
      apiUrl: '/api',
      signalRUrl: '/hubs'
    };
    ```
- [ ] **Task**: Update ASP.NET Core to serve Angular app in production

### 2.5 Basic Angular Components
- [ ] **Task**: Create `HomeComponent` with basic room creation
- [ ] **Task**: Create `RoomComponent` shell structure
- [ ] **Task**: Create `LoadingComponent` for async operations

**Milestone 2 Completion**: Angular application running alongside ASP.NET Core with basic API integration.

---

## Phase 3: Core Feature Migration (Milestone 3)

### 3.1 Room Management Migration
- [ ] **Task**: Implement room creation in Angular
  - Form validation for room settings
  - Card set selection dropdown
  - Room configuration options
  
- [ ] **Task**: Implement room joining functionality
  - User name input with validation
  - Role selection (dev/test/observer)
  - Session handling for existing users

### 3.2 Voting System Migration
- [ ] **Task**: Create voting components
  - Card selection interface for all 7 card sets
  - Vote status indicators
  - Current user's vote display
  
- [ ] **Task**: Implement voting logic
  - Vote casting with API calls
  - Vote validation and error handling
  - Integration with countdown timer

### 3.3 User Interface Components
- [ ] **Task**: Create participant tables
  - Separate tables for devs, testers, observers
  - Real-time participant updates
  - Owner indicators and controls
  
- [ ] **Task**: Implement room settings modal
  - Card set switching
  - Countdown configuration
  - Fireworks toggle
  - Ownership transfer

### 3.4 Statistics Components
- [ ] **Task**: Create statistics display
  - Chart.js integration for vote visualization
  - Percentage calculations
  - Role-based statistics separation

### 3.5 Testing Phase 3
- [ ] **Task**: Test room creation flow end-to-end
- [ ] **Task**: Test voting functionality with multiple users
- [ ] **Task**: Verify statistics display correctly
- [ ] **Task**: Test all card sets work properly

**Milestone 3 Completion**: Core voting functionality works in Angular with proper session management.

---

## Phase 4: Real-time Features (Milestone 4)

### 4.1 SignalR Integration
- [ ] **Task**: Implement SignalR connection in Angular
  - Connection management with retry logic
  - Group joining for rooms
  - Connection state indicators
  
- [ ] **Task**: Implement SignalR event handlers
  - Vote cast notifications
  - User join/leave events
  - Room configuration updates
  - Health check responses

### 4.2 Real-time Voting Features
- [ ] **Task**: Implement real-time vote updates
  - Vote indicators (▮ symbols)
  - Automatic vote reveal on timer
  - Force reveal by room owner
  
- [ ] **Task**: Implement countdown timer
  - Visual countdown display
  - Auto-reveal functionality
  - Timer configuration changes

### 4.3 Real-time Statistics
- [ ] **Task**: Implement live statistics updates
  - Chart updates on vote changes
  - Fireworks celebration on consensus
  - Real-time percentage calculations

### 4.4 Connection Management
- [ ] **Task**: Implement connection recovery
  - Automatic reconnection with backoff
  - Connection state management
  - User session restoration on reconnect

### 4.5 Testing Phase 4
- [ ] **Task**: Test real-time voting with multiple browsers
- [ ] **Task**: Test connection recovery scenarios
- [ ] **Task**: Verify all SignalR events work correctly
- [ ] **Task**: Test fireworks celebration triggers

**Milestone 4 Completion**: Full real-time functionality working in Angular application.

---

## Phase 5: Complete Migration (Milestone 5)

### 5.1 Remove Razor Pages
- [ ] **Task**: Remove Razor Pages and Views
  - Delete Pages folder contents
  - Remove MVC controllers (IndexController, RoomController)
  - Clean up unused ViewModels
  
- [ ] **Task**: Update routing to serve Angular app
  - Configure SPA routing in `Program.cs`/`Startup.cs`:
    ```csharp
    app.UseStaticFiles();
    app.UseRouting();
    app.UseCors(env.IsDevelopment() ? "Development" : "Production");
    app.UseSession();
    
    app.MapControllers();
    app.MapHub<PlanningPokerHub>("/hubs/planningpoker");
    
    app.MapFallbackToFile("index.html");
    ```
  - Configure Angular routing for deep links:
    ```typescript
    @NgModule({
      imports: [RouterModule.forRoot(routes, {
        useHash: false,
        enableTracing: false
      })],
      exports: [RouterModule]
    })
    ```

### 5.2 Production Build Configuration
- [ ] **Task**: Configure Angular production build
  - Update `angular.json` for production optimization
  - Configure environment-specific API URLs
  - Optimize bundle sizes and assets
  
- [ ] **Task**: Update ASP.NET Core for production
  - Configure static file serving for Angular
  - Update CORS for production domain
  - Remove development-only middleware

### 5.3 Azure Deployment Configuration
- [ ] **Task**: Update Docker configuration
  - Create multi-stage Dockerfile:
    ```dockerfile
    # Build Angular app
    FROM node:18-alpine AS angular-build
    WORKDIR /app
    COPY ClientApp/package*.json ./
    RUN npm ci
    COPY ClientApp/ .
    RUN npm run build --prod
    
    # Build .NET app
    FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-build
    WORKDIR /app
    COPY *.csproj .
    RUN dotnet restore
    COPY . .
    RUN dotnet publish -c Release -o out
    
    # Runtime image
    FROM mcr.microsoft.com/dotnet/aspnet:8.0
    WORKDIR /app
    COPY --from=dotnet-build /app/out .
    COPY --from=angular-build /app/dist/ ./wwwroot/
    EXPOSE 80
    ENTRYPOINT ["dotnet", "PlanningPoker.dll"]
    ```
  
- [ ] **Task**: Update Azure Web App settings
  - Set environment variables:
    - `ASPNETCORE_ENVIRONMENT=Production`
    - `WEBSITE_NODE_DEFAULT_VERSION=18.x`
  - Configure Application Insights if needed
  - Update CORS origins in app settings

### 5.4 Testing and Validation
- [ ] **Task**: Full end-to-end testing
  - All voting scenarios work correctly
  - Session management functions properly
  - Real-time features perform well
  
- [ ] **Task**: Performance testing
  - Load testing with multiple concurrent users
  - Memory usage validation
  - Connection stability testing

### 5.5 Production Deployment
- [ ] **Task**: Deploy to Azure Web App
  - Test deployment process
  - Verify all functionality in production
  - Monitor for errors and performance issues

**Milestone 5 Completion**: Full Angular application successfully deployed to Azure Web App.

---

## Security Considerations

### CSRF Protection
- **Implementation**: Use anti-forgery tokens for state-changing API calls
- **Angular Integration**: Include XSRF token in HTTP headers
- **Validation**: Validate tokens on all POST/PUT/DELETE endpoints

### XSS Prevention
- **Angular Built-in**: Sanitization enabled by default in Angular
- **API Response**: Ensure API responses don't include executable content
- **Input Validation**: Validate and sanitize all user inputs on backend

### Session Security
- **Cookie Settings**: HttpOnly, Secure, SameSite=None for CORS
- **Session Timeout**: Implement proper session expiration
- **HTTPS Only**: Enforce HTTPS in production environment

### SignalR Security
- **Authentication**: Validate user session for SignalR connections
- **Group Authorization**: Ensure users can only join authorized rooms
- **Connection Limits**: Implement connection rate limiting

---

## Technical Considerations

### Session Management Strategy
- **Approach**: Maintain ASP.NET Core sessions for user identity
- **Implementation**: Use session ID in API calls, maintain session cookies
- **Benefits**: Minimal backend changes, existing session logic preserved

### SignalR Integration
- **Connection**: Angular SignalR client connects to existing hub
- **Authentication**: Use session-based user identification
- **Groups**: Maintain existing room-based group logic

### Deployment Architecture
```
Azure Web App
├── .NET WebAPI Backend (Port 80/443)
│   ├── API Controllers
│   ├── SignalR Hub
│   └── Session Management
└── Angular Frontend (Static Files)
    ├── Built Angular App
    ├── Assets and Images
    └── SPA Routing
```

### Migration Rollback Plan
- Each phase maintains working functionality
- Razor Pages can be restored if needed during phases 1-4
- Database/session state remains unchanged throughout migration

### Performance Considerations
- Angular lazy loading for better initial load times
- Chart.js optimization for statistics rendering
- SignalR connection pooling and management
- Asset optimization and caching strategies

---

## Risk Mitigation

### High-Risk Areas
1. **Session Management**: Ensure sessions work correctly with API calls
2. **SignalR Migration**: Maintain real-time functionality during transition
3. **Deployment**: Single Web App deployment complexity

### Testing Strategy
- Unit tests for new Angular components and services
- Integration tests for API endpoints
- End-to-end tests for complete user workflows
- Load testing for real-time features

### Rollback Plans
- Phase 1-4: Keep Razor Pages functional as fallback
- Phase 5: Maintain separate branch with working Razor version
- Azure deployment: Use deployment slots for zero-downtime rollback

---

## Timeline Estimation

- **Phase 1**: 1-2 weeks (Backend API setup)
- **Phase 2**: 1 week (Angular project setup)
- **Phase 3**: 2-3 weeks (Core feature migration)
- **Phase 4**: 2-3 weeks (Real-time features)
- **Phase 5**: 1-2 weeks (Complete migration and deployment)

**Total Estimated Time**: 7-11 weeks

---

## Success Criteria

### Functional Requirements
- [ ] All existing Planning Poker features work in Angular
- [ ] Real-time voting and statistics function correctly
- [ ] Session management maintains user state
- [ ] All 7 card sets display and function properly
- [ ] Fireworks celebrations trigger correctly
- [ ] Room management (creation, joining, configuration) works
- [ ] Statistics and charts display accurately

### Technical Requirements
- [ ] Single Azure Web App deployment successful
- [ ] Performance matches or exceeds current application
- [ ] No regression in functionality
- [ ] Proper error handling and user feedback
- [ ] Mobile responsiveness maintained
- [ ] Security (XSS prevention, input validation) preserved

### User Experience Requirements
- [ ] Intuitive Angular interface matches current UX
- [ ] Fast loading times and smooth interactions
- [ ] Reliable real-time updates
- [ ] Proper handling of connection issues
- [ ] Accessibility features maintained