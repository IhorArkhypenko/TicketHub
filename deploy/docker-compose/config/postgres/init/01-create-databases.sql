-- One PostgreSQL instance, a separate database per service (logical isolation
-- while keeping local infrastructure lightweight).
CREATE DATABASE catalog;
CREATE DATABASE identity;
CREATE DATABASE booking;
CREATE DATABASE payment;
