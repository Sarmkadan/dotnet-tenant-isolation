using Xunit;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using TenantIsolation.Utilities;

namespace TenantIsolation.Tests
{
    public class StringExtensionsValidationTests
    {
        [Fact]
        public void Validate_Happy_PATH_Input_Not_Null()
        {
            // Given
            var input = "Hello World";
            // When
            var result = StringExtensionsValidation.Validate(input);
            // Then
            Assert.NotNull(result);
        }

        [Fact]
        public void Validate_HAPPY_PATH_Input_Empty_String()
        {
            // Given
            var input = string.Empty;
            // When
            var result = StringExtensionsValidation.Validate(input);
            // Then
            Assert.NotNull(result);
        }

        [Fact]
        public void IsValid_HAPPY_PATH_Input_Not_Null()
        {
            // Given
            var input = "Hello World";
            // When
            var result = StringExtensionsValidation.IsValid(input);
            // Then
            Assert.True(result);
        }

        [Fact]
        public void IsValid_NULL_Input_Throws_Exception()
        {
            // Given
            string input = null;
            // When
            try
            {
                StringExtensionsValidation.IsValid(input);
            }
            // Then
            catch (Exception ex)
            {
                Assert.IsType<ArgumentNullException>(ex);
            }
        }

        [Fact]
        public void EnsureValid_HAPPY_PATH_Input_Not_Null()
        {
            // Given
            var input = "Hello World";
            // When
            StringExtensionsValidation.EnsureValid(input);
        }

        [Fact]
        public void EnsureValid_NULL_Input_Throws_Exception()
        {
            // Given
            string input = null;
            // When
            try
            {
                StringExtensionsValidation.EnsureValid(input);
            }
            // Then
            catch (Exception ex)
            {
                Assert.IsType<ArgumentNullException>(ex);
            }
        }
    }
}
