# Use the official .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file from the context (Solution root)
# and restore dependencies.
COPY DoughShaper/DoughShaper.csproj DoughShaper/
RUN dotnet restore "DoughShaper/DoughShaper.csproj"

# Copy the rest of the source code for the solution
COPY . .

# Set the working directory to the project folder and publish
WORKDIR "/src/DoughShaper"
RUN dotnet publish "DoughShaper.csproj" -c Release -o /app/publish

# Create final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DoughShaper.dll"]