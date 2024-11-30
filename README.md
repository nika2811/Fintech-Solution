# Fintech Microservices Application

A microservice-based fintech application for managing company identities, processing payments, and handling transactions securely.

## Architecture Overview

The application consists of three main microservices:

1. **Identity Service**: Manages company registration and API credentials
2. **Order Service**: Handles order creation and verification
3. **Payment Processor Service**: Processes payments through different networks

## Features

- Company registration and API key management
- Secure order creation and processing
- Payment processing via web interface or API endpoint
- Transaction routing based on card details
- Order status tracking and updates
- Asynchronous order computation

## Technical Stack

- .NET Core
- PostgreSQL
- Entity Framework Core
- RabbitMQ
- Docker
- Swagger/OpenAPI
- OTEL Collector
- Grafana
- Loki
- Tempo
- Prometheus

## Service Details

### Identity Service

#### Entities
- Company
  - Id
  - Name
  - APIKey
  - APISecret

#### Endpoints
- `POST /companies`: Create new company
- `GET /companies/{id}`: Retrieve company details

### Order Service

#### Entities
- Order
  - OrderId
  - CompanyId
  - Amount
  - Currency
  - Status
  - CreatedAt

#### Endpoints
- `POST /orders`: Create new order
- `GET /orders/compute`: Compute total orders

#### Business Rules
- Daily completed order limit: $10,000 per company

### Payment Processor Service

#### Option 1: Front-End Interface
- URL: `http://localhost/pay/{OrderId}`
- Inputs: Card number, expiry date
- Features:
  - Order validation
  - Payment routing logic
  - Status notifications

#### Option 2: API Endpoint
- `POST /process`
- Parameters:
  - OrderId
  - CardNumber
  - ExpiryDate
- Features:
  - Input validation
  - Payment routing
  - Status updates

## Workflow

1. **Company Registration**
   - Register via Identity Service
   - Receive API credentials

2. **Order Creation**
   - Authenticate using API credentials
   - Create order through Order Service
   - Receive unique OrderId

3. **Payment Processing**
   - Choose payment method (web interface or API)
   - Enter payment details
   - System routes to appropriate service based on card number
   - Update order status

4. **Order Computation**
   - Request order computation
   - Async processing with 2-minute simulation
   - Receive results via webhook/notification

## Setup Instructions

### Prerequisites
- Docker and Docker Compose
- .NET Core SDK
- PostgreSQL

### API Documentation
API documentation is available via Swagger at:

- Identity Service: http://localhost:8080/swagger
- Order Service: http://localhost:8082/swagger
- Payment Service: http://localhost:8084/swagger
- RabbitMQ: http://localhost:15672/
- Grafana: http://localhost:3000/

### Installation

```bash
# Clone repository
git clone [repository-url]

# Navigate to project directory
cd fintech-microservices

# Start services using Docker Compose
docker-compose up

