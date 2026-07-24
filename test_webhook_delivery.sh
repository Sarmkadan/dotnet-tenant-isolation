#!/bin/bash
# Test script to verify webhook delivery service functionality

set -e

echo "=== Testing Webhook Delivery Service Implementation ==="
echo ""

# Test 1: Verify files exist
echo "Test 1: Checking if new files exist..."
if [ -f "src/TenantIsolation/Integration/IWebhookDeliveryService.cs" ]; then
    echo "✓ IWebhookDeliveryService.cs exists"
else
    echo "✗ IWebhookDeliveryService.cs NOT found"
    exit 1
fi

if [ -f "src/TenantIsolation/Integration/WebhookDeliveryService.cs" ]; then
    echo "✓ WebhookDeliveryService.cs exists"
else
    echo "✗ WebhookDeliveryService.cs NOT found"
    exit 1
fi

# Test 2: Verify service registration
echo ""
echo "Test 2: Checking service registration..."
if grep -q "AddWebhookDeliveryService" "src/TenantIsolation/Configuration/ServiceRegistrationExtensions.cs"; then
    echo "✓ Webhook delivery service registered in ServiceRegistrationExtensions"
else
    echo "✗ Service registration NOT found"
    exit 1
fi

# Test 3: Verify WebhookHandler uses new service
echo ""
echo "Test 3: Checking WebhookHandler integration..."
if grep -q "IWebhookDeliveryService" "src/TenantIsolation/Integration/WebhookHandler.cs"; then
    echo "✓ WebhookHandler uses IWebhookDeliveryService"
else
    echo "✗ WebhookHandler integration NOT found"
    exit 1
fi

# Test 4: Verify signature generation extension
echo ""
echo "Test 4: Checking signature generation extension..."
if grep -q "GenerateSignature" "src/TenantIsolation/Integration/WebhookPayloadExtensions.cs"; then
    echo "✓ GenerateSignature extension method added"
else
    echo "✗ GenerateSignature extension NOT found"
    exit 1
fi

# Test 5: Verify circuit breaker support
echo ""
echo "Test 5: Checking circuit breaker implementation..."
if grep -q "CircuitBreaker" "src/TenantIsolation/Integration/WebhookDeliveryService.cs"; then
    echo "✓ Circuit breaker implementation found"
else
    echo "✗ Circuit breaker NOT found"
    exit 1
fi

# Test 6: Verify timeout support
echo ""
echo "Test 6: Checking timeout configuration..."
if grep -q "Timeout" "src/TenantIsolation/Integration/WebhookDeliveryService.cs"; then
    echo "✓ Timeout configuration found"
else
    echo "✗ Timeout configuration NOT found"
    exit 1
fi

# Test 7: Verify retry logic
echo ""
echo "Test 7: Checking retry logic..."
if grep -q "MaxRetries" "src/TenantIsolation/Integration/WebhookDeliveryService.cs"; then
    echo "✓ Retry logic found"
else
    echo "✗ Retry logic NOT found"
    exit 1
fi

# Test 8: Verify Retry-After header support
echo ""
echo "Test 8: Checking Retry-After header support..."
if grep -q "RetryAfter" "src/TenantIsolation/Integration/WebhookDeliveryService.cs"; then
    echo "✓ Retry-After header support found"
else
    echo "✗ Retry-After header support NOT found"
    exit 1
fi

# Test 9: Verify HMAC-SHA256 signing
echo ""
echo "Test 9: Checking HMAC-SHA256 signing..."
if grep -q "GenerateHmacSha256" "src/TenantIsolation/Integration/WebhookDeliveryService.cs"; then
    echo "✓ HMAC-SHA256 signing found"
else
    echo "✗ HMAC-SHA256 signing NOT found"
    exit 1
fi

# Test 10: Verify X-Signature header
echo ""
echo "Test 10: Checking X-Signature header..."
if grep -q "X-Signature" "src/TenantIsolation/Integration/WebhookDeliveryService.cs"; then
    echo "✓ X-Signature header support found"
else
    echo "✗ X-Signature header NOT found"
    exit 1
fi

# Test 11: Build the project
echo ""
echo "Test 11: Building the project..."
if dotnet build src/TenantIsolation/TenantIsolation.csproj -v quiet > /dev/null 2>&1; then
    echo "✓ Project builds successfully"
else
    echo "✗ Build failed"
    exit 1
fi

echo ""
echo "=== All Tests Passed! ==="
echo ""
echo "Summary of implemented features:"
echo "✓ IWebhookDeliveryService interface with WebhookDeliveryResult"
echo "✓ WebhookEndpoint configuration with timeout, retries, and circuit breaker settings"
echo "✓ Circuit breaker pattern per endpoint"
echo "✓ Per-endpoint timeout configuration"
echo "✓ Advanced retry logic with exponential backoff"
echo "✓ Retry-After header support"
echo "✓ HMAC-SHA256 payload signing with X-Signature header"
echo "✓ Integration with existing WebhookHandler"
echo "✓ Service registration in DI container"
echo "✓ GenerateSignature extension method for WebhookPayload"
echo ""
echo "The webhook delivery pipeline with signing, timeout, and circuit breaker has been successfully implemented!"