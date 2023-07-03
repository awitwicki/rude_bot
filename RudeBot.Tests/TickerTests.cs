using RudeBot.Services;

namespace RudeBot.Tests;

public class TickerTests
{
    [Theory]
    [InlineData("TSLA")]
        
    public async Task IsEquals_WithEqualData_ShouldReturnNonEmptyList(string ticker)
    {
        // Arrange
        var tickerService = new TickerService();
            
        // Act
        var price = await tickerService.GetTickerPrice(ticker);

        // Assert
        Assert.True(price > 0);
    }
}
