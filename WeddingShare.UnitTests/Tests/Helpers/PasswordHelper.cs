using WeddingShare.Helpers;

namespace WeddingShare.UnitTests.Tests.Helpers
{
    public class PasswordHelperTests
    {
        public PasswordHelperTests()
        {
        }

        [SetUp]
        public void Setup()
        {
        }

        [TestCase("Password1!", true)]
        [TestCase("Password1", false)]
        [TestCase("Password!", false)]
        [TestCase("password1!", false)]
        [TestCase("PASSWORD1!", false)]
        [TestCase("Pass1!", false)]
        public void PasswordHelper_IsValid(string input, bool expected)
        {
            var actual = PasswordHelper.IsValid(input);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase("Password1!", true)]
        [TestCase("Admin1!", true)]
        [TestCase("Q*hgJ8FcSkm9$q7B9Zf#T7*LJ5", false)]
        public void PasswordHelper_IsWeak(string input, bool expected)
        {
            var actual = PasswordHelper.IsWeak(input);
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(12, true, true)]
        [TestCase(4, true, false)]
        [TestCase(12, false, false)]
        [TestCase(4, false, false)]
        public void PasswordHelper_GenerateTempPassword(int length, bool includeSpecial, bool isStrong)
        {
            var password = PasswordHelper.GenerateTempPassword(length, includeSpecial);
            var actual = PasswordHelper.IsValid(password) && !PasswordHelper.IsWeak(password);
            Assert.That(actual, Is.EqualTo(isStrong), $"Password: '{password}' is not valid");
        }

        [TestCase()]
        public void PasswordHelper_GenerateSecretCode()
        {
            var actual = PasswordHelper.GenerateSecretCode();
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.Length, Is.AtLeast(20));
        }

        [TestCase()]
        public void PasswordHelper_GenerateSecretCode_IsDifferent()
        {
            var actual1 = PasswordHelper.GenerateSecretCode();
            var actual2 = PasswordHelper.GenerateSecretCode();
            Assert.That(actual1, Is.Not.EqualTo(actual2));
        }
    }
}