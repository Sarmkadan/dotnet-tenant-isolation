using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TenantIsolation.Controllers;
using TenantIsolation.Formatters;
using TenantIsolation.Integration;
using TenantIsolation.Models;
using TenantIsolation.Exceptions;
using Xunit;

namespace dotnet_tenant_isolation.Tests
{
    public class WebhookControllerTests
    {
        private readonly Mock<IWebhookHandler> _handlerMock;
        private readonly Mock<IResponseFormatter> _formatterMock;
        private readonly Mock<ILogger<WebhookController>> _loggerMock;
        private readonly WebhookController _controller;

        public WebhookControllerTests()
        {
            _handlerMock = new Mock<IWebhookHandler>();
            _formatterMock = new Mock<IResponseFormatter>();
            _loggerMock = new Mock<ILogger<WebhookController>>();

            _controller = new WebhookController(
                _handlerMock.Object,
                _formatterMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task RegisterWebhook_ReturnsCreated_WhenRequestIsValid()
        {
            // Arrange
            var tenantId = Guid.NewGuid();
            var request = new WebhookController.RegisterWebhookRequest
            {
                TenantId = tenantId.ToString(),
                EventType = "order.created",
                Url = "https://example.com/webhook",
                Secret = "secret"
            };

            var subscription = new WebhookSubscription
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EventType = request.EventType,
                Url = request.Url,
                Secret = request.Secret
            };

            _handlerMock
                .Setup(h => h.RegisterWebhookAsync(tenantId, request.EventType, request.Url, request.Secret))
                .ReturnsAsync(subscription);

            _formatterMock
                .Setup(f => f.Success(It.IsAny<WebhookSubscription>(), It.IsAny<string>()))
                .Returns((WebhookSubscription s, string? m) => null); // value not needed for status code test

            // Act
            var result = await _controller.RegisterWebhook(request);

            // Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(WebhookController.GetWebhook), createdResult.ActionName);
        }

        [Fact]
        public async Task RegisterWebhook_ReturnsBadRequest_WhenTenantIdIsInvalid()
        {
            // Arrange
            var request = new WebhookController.RegisterWebhookRequest
            {
                TenantId = "not-a-guid",
                EventType = "order.created",
                Url = "https://example.com/webhook"
            };

            _formatterMock
                .Setup(f => f.Error(It.IsAny<string>()))
                .Returns((string? m) => null);

            // Act
            var result = await _controller.RegisterWebhook(request);

            // Assert
            var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetWebhook_ReturnsOk_WhenFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var subscription = new WebhookSubscription { Id = id };

            _handlerMock
                .Setup(h => h.GetWebhookByIdAsync(id))
                .ReturnsAsync(subscription);

            _formatterMock
                .Setup(f => f.Success(It.IsAny<WebhookSubscription>()))
                .Returns((WebhookSubscription s) => null);

            // Act
            var result = await _controller.GetWebhook(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetWebhook_ReturnsNotFound_WhenMissing()
        {
            // Arrange
            var id = Guid.NewGuid();

            _handlerMock
                .Setup(h => h.GetWebhookByIdAsync(id))
                .ReturnsAsync((WebhookSubscription?)null);

            _formatterMock
                .Setup(f => f.Error(It.IsAny<string>()))
                .Returns((string? m) => null);

            // Act
            var result = await _controller.GetWebhook(id);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetTenantWebhooks_ReturnsOk_WithWebhooks()
        {
            // Arrange
            var tenantId = Guid.NewGuid().ToString();
            var parsedTenantId = Guid.Parse(tenantId);
            var list = new List<WebhookSubscription>
            {
                new WebhookSubscription { Id = Guid.NewGuid(), TenantId = parsedTenantId },
                new WebhookSubscription { Id = Guid.NewGuid(), TenantId = parsedTenantId }
            };

            _handlerMock
                .Setup(h => h.GetWebhooksAsync(parsedTenantId, null))
                .ReturnsAsync(list);

            _formatterMock
                .Setup(f => f.Success(It.IsAny<List<WebhookSubscription>>(), It.IsAny<string>()))
                .Returns((List<WebhookSubscription> w, string? m) => null);

            // Act
            var result = await _controller.GetTenantWebhooks(tenantId, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetTenantWebhooks_ReturnsBadRequest_WhenTenantIdInvalid()
        {
            // Arrange
            var tenantId = "invalid-guid";

            _formatterMock
                .Setup(f => f.Error(It.IsAny<string>()))
                .Returns((string? m) => null);

            // Act
            var result = await _controller.GetTenantWebhooks(tenantId, null);

            // Assert
            var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task DeleteWebhook_ReturnsOk_WhenDeleted()
        {
            // Arrange
            var id = Guid.NewGuid();

            _handlerMock
                .Setup(h => h.UnregisterWebhookAsync(id))
                .ReturnsAsync(true);

            _formatterMock
                .Setup(f => f.Success(It.IsAny<object>(), It.IsAny<string>()))
                .Returns((object? o, string? m) => null);

            // Act
            var result = await _controller.DeleteWebhook(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task DeleteWebhook_ReturnsNotFound_WhenMissing()
        {
            // Arrange
            var id = Guid.NewGuid();

            _handlerMock
                .Setup(h => h.UnregisterWebhookAsync(id))
                .ReturnsAsync(false);

            _formatterMock
                .Setup(f => f.Error(It.IsAny<string>()))
                .Returns((string? m) => null);

            // Act
            var result = await _controller.DeleteWebhook(id);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task GetWebhookDeliveries_ReturnsOk_WithDeliveries()
        {
            // Arrange
            var id = Guid.NewGuid();
            var deliveries = new List<WebhookDelivery>
            {
                new WebhookDelivery { Id = Guid.NewGuid(), WebhookId = id },
                new WebhookDelivery { Id = Guid.NewGuid(), WebhookId = id }
            };

            _handlerMock
                .Setup(h => h.GetDeliveryHistoryAsync(id, 10))
                .ReturnsAsync(deliveries);

            _formatterMock
                .Setup(f => f.Success(It.IsAny<List<WebhookDelivery>>(), It.IsAny<string>()))
                .Returns((List<WebhookDelivery> d, string? m) => null);

            // Act
            var result = await _controller.GetWebhookDeliveries(id, 10);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
        }
    }
}
