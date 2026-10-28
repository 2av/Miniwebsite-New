using MiniWebsite.Domain.Common;

namespace MiniWebsite.UnitTests;

public class UserCodeGeneratorTests
{
    [Fact]
    public void GenerateDbCode_IsDbPrefixPlusEightCharactersStartingWithADigit()
    {
        for (var i = 0; i < 40; i++)
        {
            var code = UserCodeGenerator.GenerateDbCode();
            Assert.Matches(@"^DB [0-9][A-Z0-9]{7}$", code);
            Assert.Contains(code.Skip(3), char.IsLetter);
        }
    }

    [Theory]
    [InlineData("DB4E5T8Y9C", "DB 4E5T8Y9C")]
    [InlineData("DB 4E5T8Y9C", "DB 4E5T8Y9C")]
    [InlineData("db 4e5t8y9c", "DB 4E5T8Y9C")]
    public void FormatDbId_ShowsTheSavedForm(string raw, string expected)
    {
        Assert.Equal(expected, UserCodeGenerator.FormatDbId(raw));
    }
}
