using System;
using System.Runtime.Serialization;
using Xunit;
using TenantIsolation.Controllers;

namespace dotnet_tenant_isolation.Tests
{
    public class TenantApiControllerJsonExtensionsTests
    {
        private static TenantApiController CreateUninitializedController()
        {
            // Create an instance without invoking the constructor to avoid needing real services.
            return (TenantApiController)FormatterServices.GetUninitializedObject(typeof(TenantApiController));
        }

        [Fact]
        public void ToJson_ReturnsJsonString_ForValidController()
        {
            var controller = CreateUninitializedController();

            var json = controller.ToJson();

            Assert.False(string.IsNullOrWhiteSpace(json));
            Assert.StartsWith("{", json);
            Assert.EndsWith("}", json);
        }

        [Fact]
        public void ToJson_WithIndentation_ProducesIndentedJson()
        {
            var controller = CreateUninitializedController();

            var json = controller.ToJson(indented: true);

            // Indented JSON should contain at least one newline character.
            Assert.Contains("\n", json);
        }

        [Fact]
        public void FromJson_ValidJson_ReturnsControllerInstance()
        {
            var json = "{}";

            var result = TenantApiControllerJsonExtensions.FromJson(json);

            Assert.NotNull(result);
        }

        [Fact]
        public void FromJson_InvalidJson_ReturnsNull()
        {
            var json = "{ invalid json }";

            var result = TenantApiControllerJsonExtensions.FromJson(json);

            Assert.Null(result);
        }

        [Fact]
        public void FromJson_NullOrEmpty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => TenantApiControllerJsonExtensions.FromJson(null!));
            Assert.Throws<ArgumentException>(() => TenantApiControllerJsonExtensions.FromJson(string.Empty));
        }

        [Fact]
        public void TryFromJson_ValidJson_ReturnsTrueAndInstance()
        {
            var json = "{}";

            var success = TenantApiControllerJsonExtensions.TryFromJson(json, out var value);

            Assert.True(success);
            Assert.NotNull(value);
        }

        [Fact]
        public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
        {
            var json = "{ invalid json }";

            var success = TenantApiControllerJsonExtensions.TryFromJson(json, out var value);

            Assert.False(success);
            Assert.Null(value);
        }

        [Fact]
        public void TryFromJson_NullOrEmpty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => TenantApiControllerJsonExtensions.TryFromJson(null!, out _));
            Assert.Throws<ArgumentException>(() => TenantApiControllerJsonExtensions.TryFromJson(string.Empty, out _));
        }
    }
}
