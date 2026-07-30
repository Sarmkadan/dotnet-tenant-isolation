using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Xunit;
using TenantIsolation.Formatters;

namespace dotnet_tenant_isolation.Tests
{
    public class ApiResponseTests
    {
        // -----------------------------------------------------------------
        // Success
        // -----------------------------------------------------------------
        [Fact]
        public void Success_ReturnsTrue_ForValidData()
        {
            // Arrange
            var data = new object();
            var message = "Operation completed successfully";
            var response = new ApiResponse<object>
            {
                Success = true,
                Data = data,
                Message = message
            };

            // Assert
            Assert.True(response.Success);
            Assert.Equal(data, response.Data);
            Assert.Equal(message, response.Message);
        }

        [Fact]
        public void Success_ReturnsTrue_ForNullData()
        {
            // Arrange
            var response = new ApiResponse<object>
            {
                Success = true,
                Data = null,
                Message = "Operation completed successfully"
            };

            // Assert
            Assert.True(response.Success);
            Assert.Null(response.Data);
            Assert.Equal("Operation completed successfully", response.Message);
        }

        [Fact]
        public void Success_ReturnsTrue_ForEmptyMessage()
        {
            // Arrange
            var data = new object();
            var response = new ApiResponse<object>
            {
                Success = true,
                Data = data,
                Message = string.Empty
            };

            // Assert
            Assert.True(response.Success);
            Assert.Equal(data, response.Data);
            Assert.Equal(string.Empty, response.Message);
        }

        // -----------------------------------------------------------------
        // Error
        // -----------------------------------------------------------------
        [Fact]
        public void Error_ReturnsFalse_ForValidMessageAndErrors()
        {
            // Arrange
            var message = "An error occurred";
            var errors = new Dictionary<string, string[]> { { "Error1", new[] { "Error 1.1", "Error 1.2" } } };
            var response = new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Errors = errors
            };

            // Assert
            Assert.False(response.Success);
            Assert.Equal(message, response.Message);
            Assert.Equal(errors, response.Errors);
        }

        [Fact]
        public void Error_ReturnsFalse_ForNullMessageAndErrors()
        {
            // Arrange
            var errors = new Dictionary<string, string[]> { { "Error1", new[] { "Error 1.1", "Error 1.2" } } };
            var response = new ApiResponse<object>
            {
                Success = false,
                Message = null,
                Errors = errors
            };

            // Assert
            Assert.False(response.Success);
            Assert.Null(response.Message);
            Assert.Equal(errors, response.Errors);
        }

        [Fact]
        public void Error_ReturnsFalse_ForEmptyMessageAndErrors()
        {
            // Arrange
            var errors = new Dictionary<string, string[]> { { "Error1", new[] { "Error 1.1", "Error 1.2" } } };
            var response = new ApiResponse<object>
            {
                Success = false,
                Message = string.Empty,
                Errors = errors
            };

            // Assert
            Assert.False(response.Success);
            Assert.Equal(string.Empty, response.Message);
            Assert.Equal(errors, response.Errors);
        }

        // -----------------------------------------------------------------
        // PaginatedResponse
        // -----------------------------------------------------------------
        [Fact]
        public void PaginatedResponse_ReturnsTrue_ForValidData()
        {
            // Arrange
            var data = new List<object> { new object() };
            var total = 10;
            var page = 1;
            var pageSize = 10;
            var paginatedData = new PaginatedResponse<object>
            {
                Items = data,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
            var response = new ApiResponse<PaginatedResponse<object>>
            {
                Success = true,
                Data = paginatedData
            };

            // Assert
            Assert.True(response.Success);
            Assert.Equal(data, ((PaginatedResponse<object>)response.Data).Items);
            Assert.Equal(total, ((PaginatedResponse<object>)response.Data).Total);
            Assert.Equal(page, ((PaginatedResponse<object>)response.Data).Page);
            Assert.Equal(pageSize, ((PaginatedResponse<object>)response.Data).PageSize);
        }

        [Fact]
        public void PaginatedResponse_ReturnsTrue_ForNullData()
        {
            // Arrange
            var total = 10;
            var page = 1;
            var pageSize = 10;
            var paginatedData = new PaginatedResponse<object>
            {
                Items = null,
                Total = total,
                Page = page,
                PageSize = pageSize
            };
            var response = new ApiResponse<PaginatedResponse<object>>
            {
                Success = true,
                Data = paginatedData
            };

            // Assert
            Assert.True(response.Success);
            Assert.Null(((PaginatedResponse<object>)response.Data).Items);
            Assert.Equal(total, ((PaginatedResponse<object>)response.Data).Total);
            Assert.Equal(page, ((PaginatedResponse<object>)response.Data).Page);
            Assert.Equal(pageSize, ((PaginatedResponse<object>)response.Data).PageSize);
        }

        // -----------------------------------------------------------------
        // Additional Properties
        // -----------------------------------------------------------------
        [Fact]
        public void Timestamp_IsSetToUtcNow_ByDefault()
        {
            // Arrange
            var response = new ApiResponse<object>();

            // Assert
            Assert.InRange(response.Timestamp, DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
        }

        [Fact]
        public void Path_And_TraceId_CanBeSet()
        {
            // Arrange
            var response = new ApiResponse<object>
            {
                Path = "/test/path",
                TraceId = "trace-123"
            };

            // Assert
            Assert.Equal("/test/path", response.Path);
            Assert.Equal("trace-123", response.TraceId);
        }
    }
}