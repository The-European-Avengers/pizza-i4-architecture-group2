# ------------ Python Service Dockerfile ------------
FROM python:3.11-slim AS base

# Set working directory
WORKDIR /app

# Install system dependencies (optional, only if you need them)
# RUN apt-get update && apt-get install -y gcc && rm -rf /var/lib/apt/lists/*

# Copy dependency file first for caching
COPY requirements.txt .

# Install Python deps
RUN pip install --no-cache-dir -r requirements.txt

# Copy service code
COPY . .

# Non root user for security
RUN useradd -m appuser
USER appuser

# Default command (you can override in docker-compose)
CMD ["python", "freezer.py"]
