# Stage 1: Build the application with Maven
# FIX: Use the official Maven image (which includes the JDK)
FROM maven:3.9-eclipse-temurin-21 AS build

# Set the working directory
WORKDIR /app

# Copy the pom.xml and download dependencies
COPY pom.xml .
RUN mvn dependency:go-offline

# Copy the source code and build the application
COPY src ./src
# Build project and skip compiling tests
RUN mvn clean install -Dmaven.test.skip=true

# Stage 2: Create the final lightweight runtime image using a JRE
FROM eclipse-temurin:21-jre-jammy
WORKDIR /app

# Copy the built JAR file from the build stage
COPY --from=build /app/target/*.jar app.jar

# Expose the port (optional, as we're not using HTTP)
EXPOSE 8080

# Set the entrypoint to run the JAR file
ENTRYPOINT ["java", "-jar", "app.jar"]