using System;
using System.Linq;
using RudeBot.Services.DuplicateDetectorService;
using Xunit;

namespace Tests
{
    public class DuplicateDetectorServiceUnitTests
    {
        private string LoremIpsum = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec faucibus tortor et lacus ornare, nec tincidunt leo molestie. Duis ultricies feugiat erat. Vivamus quis egestas lacus. Quisque eu ex nibh. Donec in massa dolor. Quisque pharetra, quam sit amet luctus hendrerit, sem mauris porttitor lectus, quis rutrum lorem mi sit amet libero. In hac habitasse platea dictumst.";
        private string LoremIpsum2 = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec faucibus tortor et lacus ornare, nec tincidunt leo molestie. Duis ultricies feugiat erat. Vivamus quis egestas lacus. Quisque eu ex nibh. Donec in massa dolor. Quisque pharetra, quam sit amet luctus hendrerit, sem mauris porttitor lectus, quis rutrum lorem mi sit amet libero. In hac habitasse platea rtyurtyu";
        private string LoremIpsum3 = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec faucibus tortor et lacus ornare, nec tincidunt leo molestie. Duis ultricies feugiat erat. Vivamus quis egestas lacus. Quisque eu ex nibh. Donec in massa dolor. Quisque pharetra, quam sit amet luctus hendrerit, sem mauris porttitor lectus, quis rutrum lorem mi sit amet libero. In hac habitasse platea dictumst. sdfsd sdfdsf.";
        private string LoremIpsum4 = "Proin sed porttitor ipsum. Curabitur volutpat neque varius erat malesuada, sit amet dictum lectus convallis. Fusce vitae mauris a metus mollis faucibus. Ut commodo turpis nibh, et fringilla urna auctor in. Suspendisse eros dolor, dictum sit amet ante ac, vehicula ultricies neque.";
        private string LoremIpsum5 = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Donec faucibus tortor et lacus ornare";

        [Fact]
        public void StressTest()
        {
            DuplicateDetectorService duplicateDetectorService = new DuplicateDetectorService(TimeSpan.FromMinutes(30), 0.9f);

            Assert.False(duplicateDetectorService.FindDuplicates(123, 123, LoremIpsum).Any());
            Assert.False(duplicateDetectorService.FindDuplicates(124, 123, LoremIpsum).Any());
            Assert.True(duplicateDetectorService.FindDuplicates(123, 124, LoremIpsum2).Any());
            Assert.True(duplicateDetectorService.FindDuplicates(123, 125, LoremIpsum3).Any());
            Assert.True(duplicateDetectorService.FindDuplicates(123, 126, LoremIpsum).Any());
            Assert.True(duplicateDetectorService.FindDuplicates(123, 127, LoremIpsum2).Any());
            Assert.True(duplicateDetectorService.FindDuplicates(123, 131, LoremIpsum3).Any());
            Assert.False(duplicateDetectorService.FindDuplicates(123, 132, LoremIpsum4).Any());
            Assert.False(duplicateDetectorService.FindDuplicates(123, 133, LoremIpsum5).Any());
        }

        [Fact]
        public void TestForSimilarity()
        {
            DuplicateDetectorService duplicateDetectorService = new DuplicateDetectorService(TimeSpan.FromMinutes(30), 0.9f);

            duplicateDetectorService.FindDuplicates(123, 123, LoremIpsum);

            Assert.True(duplicateDetectorService.FindDuplicates(123, 123, LoremIpsum2).Any());
        }

        [Fact]
        public void TestTwoChats()
        {
            DuplicateDetectorService duplicateDetectorService = new DuplicateDetectorService(TimeSpan.FromMinutes(30), 0.9f);

            duplicateDetectorService.FindDuplicates(123, 123, LoremIpsum);

            Assert.False(duplicateDetectorService.FindDuplicates(124, 123, LoremIpsum).Any());
        }
    }
}
