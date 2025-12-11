# ------------ Python Service Dockerfile ------------
FROM python:3.11-slim AS base

# Set working directory
WORKDIR /app


# Copy dependency file first for caching
COPY requirements.txt .

# Install Python deps
RUN pip install --no-cache-dir -r requirements.txt

# Copy service code
COPY internal_goods_provider/ /app/internal_goods_provider/
COPY py_helpers/ /app/py_helpers/

# Non root user for security
RUN useradd -m appuser
USER appuser

# Default command (you can override in docker-compose)
CMD ["python", "-m", "internal_goods_provider.main"]