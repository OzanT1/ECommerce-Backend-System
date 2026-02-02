# E-Commerce Backend

A multi-container backend system for e-commerce applications built with ASP.NET Core 9.0. The platform handles authentication & authorization, product management, shopping carts, orders, payments and email notifications through an event-driven architecture.

## Architecture

### Components/Docker Containers

- **ECommerce.API** (ASP.NET Core)
  - REST API for authentication & authorization, products, cart management, orders, and payments (Stripe)
  - JWT-based authentication with role-based access control
  - Rate limiting and exception handling middleware
  - Caching using Redis for performance boost
  - Swagger/OpenAPI documentation

- **ECommerce.Worker** (Background Service)
  - Consumes events from message queue (RabbitMQ) for asynchronous processing
  - Sends email notifications for order confirmations and updates using SendGrid Email API
  - Handles long-running tasks without blocking the API

- **PostgreSQL** (Database)
  - Persistent storage for users, products, and order related data

- **Redis** (Cache & Session Store)
  - Caches frequently accessed data (products, rate limits etc.) for faster responses
  - Powers the rate limiter for IP-based throttling

- **RabbitMQ** (Message Broker)
  - Decouples API from background processing
  - Enables asynchronous event handling (e.g., sending emails without blocking the API)
  - Ensures reliable message delivery for critical workflows

## Why This Design?

- **RabbitMQ + Background Worker**: Orders can be created and returned to clients immediately while emails are sent asynchronously, improving API response times and reliability.
- **Redis Caching**: Reduces database load by caching product data and rate limiting data to enable faster API responses.
- **Containerization**: Each service runs independently, making the system scalable and easy to deploy.

## Setup

### Prerequisites

- .NET 9.0 SDK
- Docker & Docker Compose
- Git

### Instructions

1. **Clone the repository**

   ```bash
   git clone <repository-url>
   cd ECommerce
   ```

2. **Create environment file**

   ```bash
   cp .env.example .env
   ```

   Update `.env` with your configuration (API keys, JWT secrets, etc.)

3. **Start all containers**

   ```bash
   docker compose up --build
   ```

4. **Access the services**
   - API: `http://localhost:5000`
   - Swagger UI: `http://localhost:5000/swagger`
   - RabbitMQ Management: `http://localhost:15672` (guest/guest)

## 🔑 Key Components

### Services (Interface-Driven Design)

All services follow a clean architecture pattern with dependency injection:

- **IAuthService** - User authentication, JWT token generation
- **IProductService** - Product CRUD operations
- **ICartService** - Shopping cart management
- **IOrderService** - Order creation and status tracking
- **IPaymentService** - Payment processing (Stripe/Fake)
- **ICacheService** - Redis operations (get, set, remove with TTL)
- **IRabbitMQService** - Message publishing and subscription
- **IRateLimitService** - API rate limiting

### Middleware

- **ExceptionHandlingMiddleware** - Global error handler with structured JSON responses
- **RateLimitMiddleware** - Per-IP request throttling using Redis

### Database Models

- **User** - Customer accounts with auth details
- **Product** - Catalog with pricing and inventory
- **Category** - Product categorization
- **CartItem** - Shopping cart entries
- **Order** - Customer orders with status tracking
- **OrderItem** - Individual items within an order

## 🔐 Authentication & Security

- **JWT Bearer Tokens**: Stateless authentication with configurable expiration
- **Role-Based Access Control**: "Customer" and admin role support
- **Password Hashing**: Secure storage with industry-standard algorithms
- **CORS Support**: Configured for frontend integration
- **Rate Limiting**: Distributed rate limiting via Redis keyed by IP address

## 📝 API Endpoints

### Authentication

- `POST /api/auth/register` - Register a new user
- `POST /api/auth/login` - Login and receive JWT token

### Products

- `GET /api/products` - List all products (paginated)
- `GET /api/products/{id}` - Get product details
- `POST /api/products` - Create product (admin only)
- `PUT /api/products/{id}` - Update product (admin only)
- `DELETE /api/products/{id}` - Delete product (admin only)

### Cart

- `GET /api/cart` - Get current cart
- `POST /api/cart/items` - Add item to cart
- `PUT /api/cart/items/{id}` - Update cart item quantity
- `DELETE /api/cart/items/{id}` - Remove item from cart

### Orders

- `POST /api/orders` - Create new order
- `GET /api/orders` - Get user's orders
- `GET /api/orders/{id}` - Get order details

### Payments

- `POST /api/orders/{id}/confirm-payment` - Process/Complete payment (Stripe or Fake for testing)

See [Swagger UI](http://localhost:5000/swagger) for complete API documentation with live testing.

## 📊 Logging

The application uses **Serilog** for structured logging:

- **Console Output**: Real-time development feedback
- **File Output**: Daily rolling logs in `logs/ecommerce-.log`
- **Log Levels**: Configure in appsettings.json

## 🚢 Deployment

### Docker Build & Run

```bash
# Build all images
docker compose build

# Start all services
docker compose up -d

# View logs
docker compose logs -f api

# Stop all services
docker compose down
```

### Production Considerations

1. **Security**
   - Use strong JWT secret (minimum 32 characters)
   - Enable HTTPS in production
   - Implement CORS properly for your frontend domain

2. **Performance**
   - Enable Redis connection pooling
   - Use read replicas for PostgreSQL
   - Implement caching headers for static content
   - Monitor RabbitMQ queue depths

3. **Monitoring**
   - Aggregate logs with centralized logging (ELK, Splunk)
   - Monitor API response times and error rates
   - Set up alerts for critical service failures
   - Track RabbitMQ queue health

## 📚 Technology Stack

| Layer                | Technologies                      |
| -------------------- | --------------------------------- |
| **API**              | ASP.NET Core 9.0, C# 13           |
| **Database**         | PostgreSQL, Entity Framework Core |
| **Caching**          | Redis, StackExchange.Redis        |
| **Messaging**        | RabbitMQ, RabbitMQ.Client         |
| **Authentication**   | JWT, System.IdentityModel.Tokens  |
| **Payments**         | Stripe.net                        |
| **Logging**          | Serilog                           |
| **API Docs**         | Swagger/OpenAPI                   |
| **Containerization** | Docker, Docker Compose            |

## 📝 License

This project is provided as-is for educational use.

## 👥 Contributing

1. Create a feature branch: `git checkout -b feature/<your-feature-name>`
2. Commit changes: `git commit -am 'Add your feature'`
3. Push to branch: `git push origin feature/<your-feature-name>`
4. Submit a pull request

## 📧 Support

For issues, questions, or suggestions, please create an issue in the repository.

---

**Last Updated**: January 2026
