using RudeBot.Services.DuplicateDetectorService;

namespace RudeBot.Tests 
{
    public class DuplicateDetectorServiceUnitTests
    {

        private const string LoremIpsum1 = "Lorem ipsum dolor sit amet.";
        private const string LoremIpsum2 = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec faucibus tortor et lacus ornare, nec tincidunt leo molestie. Duis ultricies feugiat erat.";
        private const string LoremIpsum3 = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec faucibus tortor et lacus ornare, nec tincidunt leo molestie. Duis ultricies feugiat erat. Vivamus quis egestas lacus. Quisque eu ex nibh. Donec in massa dolor. Quisque pharetra, quam sit amet luctus hendrerit, sem mauris porttitor lectus, quis rutrum lorem mi sit amet libero. In hac habitasse platea dictumst.";
        private const string LirimOpossum1 = "Proin sed porttitor ipsum.";
        private const string LirimOpossum2 = "Proin sed porttitor ipsum. Curabitur volutpat neque varius erat malesuada, sit amet dictum lectus convallis. Fusce vitae mauris a metus mollis faucibus.";
        private const string LirimOpossum3 = "Proin sed porttitor ipsum. Curabitur volutpat neque varius erat malesuada, sit amet dictum lectus convallis. Fusce vitae mauris a metus mollis faucibus. Ut commodo turpis nibh, et fringilla urna auctor in. Suspendisse eros dolor, dictum sit amet ante ac, vehicula ultricies neque.";
        
        [Theory]
        [InlineData(LoremIpsum2, LoremIpsum2)]
        [InlineData(LoremIpsum3, LoremIpsum3)]
        [InlineData(LirimOpossum2, LirimOpossum2)]
        [InlineData(LirimOpossum3, LirimOpossum3)]
        
        public void IsEquals_WithEqualData_ShouldReturnNonEmptyList(string text1, string text2)
        {
            // Arrange
            var duplicateDetectorService = new DuplicateDetectorService(TimeSpan.FromMinutes(30));
            
            // Act
            duplicateDetectorService.FindDuplicates(123, 123, text1);
            var duplicates = duplicateDetectorService.FindDuplicates(123, 123, text2);

            // Assert
            Assert.True(duplicates.Any());
        }
        
        [Theory]
        [InlineData(LoremIpsum1, LoremIpsum1)]
        [InlineData(LirimOpossum1, LirimOpossum1)]
        public void IsEquals_WithEqualSmallData_ShouldReturnEmptyList(string text1, string text2)
        {
            // Arrange
            var duplicateDetectorService = new DuplicateDetectorService(TimeSpan.FromMinutes(30));
            
            // Act
            duplicateDetectorService.FindDuplicates(123, 123, text1);
            var duplicates = duplicateDetectorService.FindDuplicates(123, 123, text2);

            // Assert
            Assert.False(duplicates.Any());
        }
        
        [Theory]
        [InlineData(LoremIpsum2, LoremIpsum2)]
        [InlineData(LoremIpsum3, LoremIpsum3)]
        [InlineData(LirimOpossum2, LirimOpossum2)]
        [InlineData(LirimOpossum3, LirimOpossum3)]
        public void IsEquals_WithEqualDataAndDifferentChats_ShouldReturnEmptyList(string text1, string text2)
        {
            // Arrange
            var duplicateDetectorService = new DuplicateDetectorService(TimeSpan.FromMinutes(30));
            
            // Act
            duplicateDetectorService.FindDuplicates(1111, 123, text1);
            var duplicates = duplicateDetectorService.FindDuplicates(2222, 123, text2);

            // Assert
            Assert.False(duplicates.Any());
        }
        
        [Theory]
        [InlineData(LoremIpsum2, LoremIpsum3)]
        [InlineData(LoremIpsum1, LoremIpsum3)]
        [InlineData(LirimOpossum2, LirimOpossum3)]
        [InlineData(LirimOpossum1, LirimOpossum3)]
        
        public void IsEquals_WithDifferentData_ShouldReturnEmptyList(string text1, string text2)
        {
            // Arrange
            var duplicateDetectorService = new DuplicateDetectorService(TimeSpan.FromMinutes(30));
            
            // Act
            duplicateDetectorService.FindDuplicates(123, 123, text1);
            var duplicates = duplicateDetectorService.FindDuplicates(123, 123, text2);

            // Assert
            Assert.False(duplicates.Any());
        }
    }
}
