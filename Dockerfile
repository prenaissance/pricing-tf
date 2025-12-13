FROM rust:1.92.0-alpine3.23 AS builder
WORKDIR /app
# Busybox bug on arm64, remove the flags when alpine is fixed
RUN apk upgrade --no-cache --scripts=no apk-tools && apk add --no-cache protobuf-dev postgresql-dev

COPY Cargo.toml Cargo.lock build.rs ./
COPY ./protos/ ./protos/
COPY ./migrations/ ./migrations/
# Simulate src folder to install dependencies & run build script
RUN mkdir src && echo "fn main() {}" > src/main.rs && cargo build --release && rm -rf src

COPY src ./src
RUN cargo build --release
RUN strip ./target/release/pricing-tf

# FROM debian:trixie-slim
FROM alpine:latest
WORKDIR /app

EXPOSE 8080

COPY --from=builder /app/target/release/pricing-tf ./pricing-tf
ENTRYPOINT ["./pricing-tf"]
