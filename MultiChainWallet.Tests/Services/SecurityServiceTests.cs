using System;
using System.Text;
using System.Threading.Tasks;
using MultiChainWallet.Infrastructure.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MultiChainWallet.Tests.Services
{
    /// <summary>
    /// 安全服务测试类
    /// Security service test class
    /// </summary>
    [TestClass]
    public class SecurityServiceTests
    {
        private SecurityService _securityService;

        [TestInitialize]
        public void Initialize()
        {
            _securityService = new SecurityService();
        }

        [DataTestMethod]
        [DataRow("", false, "密码不能为空")]
        [DataRow("short", false, "密码长度必须至少为12个字符")]
        [DataRow("onlylowercase123!", false, "密码必须包含大写字母")]
        [DataRow("ONLYUPPERCASE123!", false, "密码必须包含小写字母")]
        [DataRow("NoNumbers!!!", false, "密码必须包含数字")]
        [DataRow("NoSpecial123ABC", false, "密码必须包含特殊字符")]
        [DataRow("Valid@Password123", true, null)]
        public void ValidatePasswordStrength_ShouldReturnExpectedResult(
            string password, 
            bool expectedIsValid, 
            string expectedErrorMessage)
        {
            // Act
            var result = _securityService.ValidatePasswordStrength(password);

            // Assert
            Assert.AreEqual(expectedIsValid, result.IsValid);
            if (!expectedIsValid)
            {
                Assert.IsTrue(result.ErrorMessage.Contains(expectedErrorMessage));
            }
        }

        [TestMethod]
        public async Task EncryptAndDecryptPrivateKey_ShouldReturnOriginalValue()
        {
            // Arrange
            string originalPrivateKey = "my-private-key-123";
            string password = "Valid@Password123";

            // Act
            var encryptedKey = await _securityService.EncryptPrivateKeyAsync(originalPrivateKey, password);
            var decryptedKey = await _securityService.DecryptPrivateKeyAsync(encryptedKey, password);

            // Assert
            Assert.AreEqual(originalPrivateKey, decryptedKey);
        }

        [TestMethod]
        public async Task DecryptPrivateKey_WithWrongPassword_ShouldThrowException()
        {
            // Arrange
            string originalPrivateKey = "my-private-key-123";
            string correctPassword = "Valid@Password123";
            string wrongPassword = "Wrong@Password123";

            // Act
            var encryptedKey = await _securityService.EncryptPrivateKeyAsync(originalPrivateKey, correctPassword);

            // Assert
            await Assert.ThrowsExceptionAsync<System.Security.Cryptography.CryptographicException>(
                () => _securityService.DecryptPrivateKeyAsync(encryptedKey, wrongPassword));
        }

        [TestMethod]
        public void GenerateSecurePassword_ShouldMeetAllRequirements()
        {
            // Act
            var password = _securityService.GenerateSecurePassword();

            // Assert
            var validationResult = _securityService.ValidatePasswordStrength(password);
            Assert.IsTrue(validationResult.IsValid);
            Assert.AreEqual(16, password.Length); // Default length
        }

        [TestMethod]
        public void GenerateSecurePassword_WithCustomLength_ShouldHaveCorrectLength()
        {
            // Arrange
            int customLength = 20;

            // Act
            var password = _securityService.GenerateSecurePassword(customLength);

            // Assert
            Assert.AreEqual(customLength, password.Length);
            var validationResult = _securityService.ValidatePasswordStrength(password);
            Assert.IsTrue(validationResult.IsValid);
        }

        [TestMethod]
        public async Task CreateAndVerifyPasswordHash_ShouldWork()
        {
            // Arrange
            string password = "Valid@Password123";

            // Act
            var hash = await _securityService.CreatePasswordHashAsync(password);
            var isValid = await _securityService.VerifyPasswordHashAsync(password, hash);

            // Assert
            Assert.IsTrue(isValid);
        }

        [TestMethod]
        public async Task VerifyPasswordHash_WithWrongPassword_ShouldReturnFalse()
        {
            // Arrange
            string correctPassword = "Valid@Password123";
            string wrongPassword = "Wrong@Password123";

            // Act
            var hash = await _securityService.CreatePasswordHashAsync(correctPassword);
            var isValid = await _securityService.VerifyPasswordHashAsync(wrongPassword, hash);

            // Assert
            Assert.IsFalse(isValid);
        }

        [TestMethod]
        public void GenerateSessionToken_ShouldReturnUniqueTokens()
        {
            // Act
            var token1 = _securityService.GenerateSessionToken();
            var token2 = _securityService.GenerateSessionToken();

            // Assert
            Assert.AreNotEqual(token1, token2);
            Assert.IsFalse(string.IsNullOrEmpty(token1));
            Assert.IsFalse(string.IsNullOrEmpty(token2));
        }
    }
}
