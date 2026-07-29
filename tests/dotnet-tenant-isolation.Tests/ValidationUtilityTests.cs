using System;
using Xunit;
using TenantIsolation.Utilities;
using TenantIsolation.Exceptions;

namespace dotnet_tenant_isolation.Tests
{
    public class ValidationUtilityTests
    {
        // -----------------------------------------------------------------
        // IsValidEmail
        // -----------------------------------------------------------------
        [Fact]
        public void IsValidEmail_ReturnsTrue_ForValidEmail()
        {
            Assert.True(ValidationUtility.IsValidEmail("john.doe@example.com"));
        }

        [Fact]
        public void IsValidEmail_ReturnsFalse_ForInvalidEmails()
        {
            // null / empty
            Assert.False(ValidationUtility.IsValidEmail(null!));
            Assert.False(ValidationUtility.IsValidEmail(string.Empty));

            // missing @
            Assert.False(ValidationUtility.IsValidEmail("john.doeexample.com"));

            // too long (255 chars)
            var longLocal = new string('a', 250);
            var longEmail = $"{longLocal}@example.com";
            Assert.False(ValidationUtility.IsValidEmail(longEmail));
        }

        // -----------------------------------------------------------------
        // IsValidSlug
        // -----------------------------------------------------------------
        [Fact]
        public void IsValidSlug_ReturnsTrue_ForValidSlug()
        {
            Assert.True(ValidationUtility.IsValidSlug("my-tenant-123"));
        }

        [Fact]
        public void IsValidSlug_ReturnsFalse_ForInvalidSlugs()
        {
            // null / empty
            Assert.False(ValidationUtility.IsValidSlug(null!));
            Assert.False(ValidationUtility.IsValidSlug(string.Empty));

            // too short / too long
            Assert.False(ValidationUtility.IsValidSlug("ab"));
            var longSlug = new string('a', 64);
            Assert.False(ValidationUtility.IsValidSlug(longSlug));

            // uppercase or invalid chars
            Assert.False(ValidationUtility.IsValidSlug("MyTenant"));
            Assert.False(ValidationUtility.IsValidSlug("tenant_underscore"));
        }

        // -----------------------------------------------------------------
        // IsValidGuid
        // -----------------------------------------------------------------
        [Fact]
        public void IsValidGuid_ReturnsTrue_ForValidGuid()
        {
            var guid = Guid.NewGuid().ToString();
            Assert.True(ValidationUtility.IsValidGuid(guid));
        }

        [Fact]
        public void IsValidGuid_ReturnsFalse_ForInvalidGuid()
        {
            Assert.False(ValidationUtility.IsValidGuid(null!));
            Assert.False(ValidationUtility.IsValidGuid(string.Empty));
            Assert.False(ValidationUtility.IsValidGuid("not-a-guid"));
            // malformed but matches regex pattern partially
            Assert.False(ValidationUtility.IsValidGuid("12345678-1234-1234-1234-1234567890abz"));
        }

        // -----------------------------------------------------------------
        // RequireNotEmpty
        // -----------------------------------------------------------------
        [Fact]
        public void RequireNotEmpty_Throws_ForNullOrWhitespace()
        {
            Assert.Throws<TenantIsolationException>(() => ValidationUtility.RequireNotEmpty(null, "Field"));
            Assert.Throws<TenantIsolationException>(() => ValidationUtility.RequireNotEmpty(string.Empty, "Field"));
            Assert.Throws<TenantIsolationException>(() => ValidationUtility.RequireNotEmpty("   ", "Field"));
        }

        [Fact]
        public void RequireNotEmpty_DoesNotThrow_ForNonEmpty()
        {
            ValidationUtility.RequireNotEmpty("value", "Field");
        }

        // -----------------------------------------------------------------
        // RequireMinLength
        // -----------------------------------------------------------------
        [Fact]
        public void RequireMinLength_Throws_WhenTooShort()
        {
            Assert.Throws<TenantIsolationException>(() => ValidationUtility.RequireMinLength("ab", 3, "Field"));
            Assert.Throws<TenantIsolationException>(() => ValidationUtility.RequireMinLength(null, 1, "Field"));
        }

        [Fact]
        public void RequireMinLength_DoesNotThrow_WhenValid()
        {
            ValidationUtility.RequireMinLength("abcd", 3, "Field");
        }

        // -----------------------------------------------------------------
        // RequireMaxLength
        // -----------------------------------------------------------------
        [Fact]
        public void RequireMaxLength_Throws_WhenTooLong()
        {
            Assert.Throws<TenantIsolationException>(() => ValidationUtility.RequireMaxLength("abcdef", 5, "Field"));
        }

        [Fact]
        public void RequireMaxLength_DoesNotThrow_WhenValid()
        {
            ValidationUtility.RequireMaxLength("abc", 5, "Field");
            // also should not throw when value is null/empty
            ValidationUtility.RequireMaxLength(null, 5, "Field");
        }

        // -----------------------------------------------------------------
        // RequireLengthBetween
        // -----------------------------------------------------------------
        [Fact]
        public void RequireLengthBetween_Throws_WhenOutOfRange()
        {
            // too short
            Assert.Throws<TenantIsolationException>(() => ValidationUtility.RequireLengthBetween("ab", 3, 5, "Field"));
            // too long
            Assert.Throws<TenantIsolationException>(() => ValidationUtility.RequireLengthBetween("abcdef", 3, 5, "Field"));
        }

        [Fact]
        public void RequireLengthBetween_DoesNotThrow_WhenWithinRange()
        {
            ValidationUtility.RequireLengthBetween("abcd", 3, 5, "Field");
        }

        // -----------------------------------------------------------------
        // RequireValidEmail
        // -----------------------------------------------------------------
        [Fact]
        public void RequireValidEmail_Throws_ForInvalid()
        {
            Assert.Throws<TenantIsolationException>(() => ValidationUtility.RequireValidEmail(null));
            Assert.Throws<TenantIsolationException>(() => ValidationUtility.RequireValidEmail("invalid-email"));
        }

        [Fact]
        public void RequireValidEmail_DoesNotThrow_ForValid()
        {
            ValidationUtility.RequireValidEmail("alice@example.org");
        }

        // -----------------------------------------------------------------
        // RequireValidSlug
        // -----------------------------------------------------------------
        [Fact]
        public void RequireValidSlug_Throws_ForInvalid()
        {
            Assert.Throws<TenantIsolationException>(() => ValidationUtility.RequireValidSlug(null));
            Assert.Throws<TenantIsolationException>(() => ValidationUtility.RequireValidSlug("AB"));
        }

        [Fact]
        public void RequireValidSlug_DoesNotThrow_ForValid()
        {
            ValidationUtility.RequireValidSlug("valid-slug-01");
        }

        // -----------------------------------------------------------------
        // RequireValidGuid
        // -----------------------------------------------------------------
        [Fact]
        public void RequireValidGuid_Throws_ForInvalid()
        {
            Assert.Throws<TenantIsolationException>(() => ValidationUtility.RequireValidGuid(null, "GuidField"));
            Assert.Throws<TenantIsolationException>(() => ValidationUtility.RequireValidGuid("not-a-guid", "GuidField"));
        }

        [Fact]
        public void RequireValidGuid_DoesNotThrow_ForValid()
        {
            var guid = Guid.NewGuid().ToString();
            ValidationUtility.RequireValidGuid(guid, "GuidField");
        }
    }
}
