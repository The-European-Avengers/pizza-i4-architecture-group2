# --- Stage 1: Builder ---
FROM golang:1.25 AS builder
WORKDIR /app

COPY go.mod go.sum ./
RUN go mod download

COPY . .
RUN CGO_ENABLED=0 GOOS=linux GOARCH=amd64 go build -o main .

# --- Stage 2: Runtime ---
FROM gcr.io/distroless/base-debian12
WORKDIR /app
COPY --from=builder /app/main .
USER nonroot:nonroot
ENTRYPOINT ["./main"]
