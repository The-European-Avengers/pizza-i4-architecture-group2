# ------------ Python Service Dockerfile ------------
FROM python:3.11-slim AS base

# Set working directory
WORKDIR /app


# Copy dependency file first for caching
COPY requirements.txt .

# Install Python deps
RUN pip install --no-cache-dir -r requirements.txt

# Copy service code
COPY order_dispatcher/ /app/order_dispatcher/
COPY py_helpers/ /app/py_helpers/

# Non root user for security
RUN useradd -m appuser
USER appuser

# Default command (you can override in docker-compose)
CMD ["python", "-m", "order_dispatcher.main"]