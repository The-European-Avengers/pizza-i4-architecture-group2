# Use the official .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file from the context (Solution root)
# and restore dependencies.
COPY DoughMachine/DoughMachine.csproj DoughMachine/
RUN dotnet restore "DoughMachine/DoughMachine.csproj"

# Copy the rest of the source code for the solution
COPY . .

# Set the working directory to the project folder and publish
WORKDIR "/src/DoughMachine"
RUN dotnet publish "DoughMachine.csproj" -c Release -o /app/publish

# Create final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DoughMachine.dll"]