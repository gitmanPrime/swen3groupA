# Sprint 3 submission verification

Verified on 20 October 2026 using Docker Desktop with Linux containers.

## Scope

- API endpoints validate Keycloak bearer access tokens and reject missing or
  invalid credentials with HTTP 401.
- Centralized exception handling maps messaging failures to safe HTTP error
  responses.
- A document upload persists the document and publishes an `OcrRequest` with
  its document ID and filename to the durable `ocr.documents` RabbitMQ queue.
- The OCR worker consumes each request, logs its document ID and filename, and
  explicitly acknowledges successful processing.

## End-to-end verification

1. Started the full stack with:

   ```powershell
   docker compose -f DMS/docker-compose.yml up -d --build --wait
   ```

   The API, frontend, Keycloak, PostgreSQL, RabbitMQ, and OCR worker were
   running; PostgreSQL and RabbitMQ reported healthy status.

2. Logged in to the frontend at http://localhost/ as `student` and uploaded a
   PDF through the authenticated REST API.
3. The OCR worker logged:

   ```text
   Received OCR Request -> Document ID: 9e567928-edf5-475e-8ba4-b41a0fcb1c6a, File Name: 2023 SQM Continuous Quality Self Study.pdf
   ```

4. Queried RabbitMQ after processing:

   ```powershell
   docker compose -f DMS/docker-compose.yml exec -T rabbitmq rabbitmqctl list_queues name messages_ready messages_unacknowledged consumers
   ```

   ```text
   name            messages_ready  messages_unacknowledged  consumers
   ocr.documents   0               0                        1
   ```

   Zero ready messages and zero unacknowledged messages confirm that the OCR
   worker consumed and acknowledged the upload message.

## Local Keycloak note

Fresh Keycloak imports include the `dms-api` audience mapper. For an existing
persisted realm, add the `dms-api-audience` mapper to the `dms-frontend`
dedicated client scope, set its custom audience to `dms-api`, enable it for
access tokens, then log out and sign in again.
