# Builds the Angular client, publishes the API with it, and runs the result.
# The Node and .NET versions are pinned here: Node 24 (see PlanningPoker.Client/.nvmrc), .NET 10.

# Stage 1: build the Angular client into /src/PlanningPoker.Api/wwwroot
# (npm run build:api copies the build there, relative to the client folder).
FROM node:24-bookworm-slim AS client
WORKDIR /src/PlanningPoker.Client
COPY PlanningPoker.Client/package.json PlanningPoker.Client/package-lock.json ./
RUN npm ci
COPY PlanningPoker.Client/ ./
RUN npm run build:api

# Stage 2: publish the API with the built client in wwwroot.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS publish
WORKDIR /src
COPY PlanningPoker.Api/PlanningPoker.Api.csproj PlanningPoker.Api/
COPY PlanningPoker.Common/PlanningPoker.Common.csproj PlanningPoker.Common/
COPY PlanningPoker.Controller/PlanningPoker.Controller.csproj PlanningPoker.Controller/
COPY PlanningPoker.Service/PlanningPoker.Service.csproj PlanningPoker.Service/
COPY PlanningPoker.Repository/PlanningPoker.Repository.csproj PlanningPoker.Repository/
RUN dotnet restore PlanningPoker.Api/PlanningPoker.Api.csproj
COPY PlanningPoker.Api/ PlanningPoker.Api/
COPY PlanningPoker.Common/ PlanningPoker.Common/
COPY PlanningPoker.Controller/ PlanningPoker.Controller/
COPY PlanningPoker.Service/ PlanningPoker.Service/
COPY PlanningPoker.Repository/ PlanningPoker.Repository/
COPY --from=client /src/PlanningPoker.Api/wwwroot/ PlanningPoker.Api/wwwroot/
RUN dotnet publish PlanningPoker.Api/PlanningPoker.Api.csproj --configuration Release --no-restore --output /app

# Stage 3: the runtime image. Listens on port 8080 (the aspnet image default).
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
LABEL org.opencontainers.image.source="https://github.com/vlad-gaman/PlanningPoker"
WORKDIR /app
COPY --from=publish /app ./
# On Azure App Service for Linux, /home is the persistent mount. The app creates the folder.
ENV Persistence__ConnectionString="Data Source=/home/data/planningpoker.db"
EXPOSE 8080
ENTRYPOINT ["dotnet", "PlanningPoker.Api.dll"]
