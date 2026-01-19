# Config Variables
ARG DOTNET_VERSION=9.0

#######################################
# Build Dotnet App
#######################################
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION} AS build
WORKDIR /src

# copy project and restore dependencies
COPY *.csproj ./
RUN dotnet restore

# copy remaining and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish


#######################################
# Run Dotnet App
#######################################
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION} AS runner
WORKDIR /app

# copy published info
COPY --from=build /app/publish .

# set looking port
ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081

# run the app
ENTRYPOINT ["dotnet", "UpdateServices.dll"]