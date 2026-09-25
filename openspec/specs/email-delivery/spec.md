# Email Delivery Specification

## Purpose
Delivers transactional emails (such as one-time passcodes) to users through a configured SMTP transport, isolating credential handling and delivery failures from the features that request email.

## Requirements

### Requirement: Transactional email delivery via SMTP
The system SHALL send transactional emails through an SMTP transport configured with host, port, sender address, and credentials. Credentials (username and app password) SHALL be treated as secrets: they SHALL be supplied through untracked configuration (environment variables or local settings) and SHALL NEVER be committed to source control or written to logs.

When SMTP is not configured, features that depend on email (OTP registration and password reset) SHALL fail with a clear server error rather than silently reporting success; the system SHALL NOT fabricate a fake or no-op delivery.

#### Scenario: OTP email is dispatched over SMTP
- **WHEN** a feature requests an OTP email for a valid recipient and SMTP is configured
- **THEN** the system sends the message through the SMTP transport to that recipient address

#### Scenario: Missing SMTP configuration fails loudly
- **WHEN** an email send is requested but SMTP credentials are not configured
- **THEN** the operation fails with a server error and the dependent feature does not report a successful send

#### Scenario: Credentials are never exposed
- **WHEN** an email is sent or a send fails
- **THEN** application logs contain no SMTP password or raw credential values
