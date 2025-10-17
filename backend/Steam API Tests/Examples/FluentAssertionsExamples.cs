using FluentAssertions;
using Steam_API.Dto.Output;
using Steam_API.Services;

namespace Steam_API_Tests.Examples
{
    /// <summary>
    /// This class demonstrates the difference between traditional Assert and FluentAssertions
    /// </summary>
    public class FluentAssertionsExamples
    {
        [Fact]
        public void TraditionalAssert_Example()
        {
            // Traditional Assert approach
            var gameDto = new GameHoursDto
            {
                AppId = 440,
                Name = "Team Fortress 2",
                HoursTotal = 120.5,
                Hours2Weeks = 10.2
            };

            // Multiple separate assertions
            Assert.NotNull(gameDto);
            Assert.Equal(440, gameDto.AppId);
            Assert.Equal("Team Fortress 2", gameDto.Name);
            Assert.True(gameDto.HoursTotal > 100);
            Assert.InRange(gameDto.Hours2Weeks, 5, 15);
        }

        [Fact]
        public void FluentAssertions_Example()
        {
            // FluentAssertions approach
            var gameDto = new GameHoursDto
            {
                AppId = 440,
                Name = "Team Fortress 2",
                HoursTotal = 120.5,
                Hours2Weeks = 10.2
            };

            // Chainable, readable assertions with better error messages
            gameDto.Should().NotBeNull()
                .And.Match<GameHoursDto>(g => 
                    g.AppId == 440 && 
                    g.Name == "Team Fortress 2");
            gameDto.HoursTotal.Should().BeGreaterThan(100);
            gameDto.Hours2Weeks.Should().BeInRange(5, 15);
        }

        [Fact]
        public void CollectionAssertions_Comparison()
        {
            var games = new List<GameHoursDto>
            {
                new() { AppId = 440, Name = "Team Fortress 2", HoursTotal = 120.5 },
                new() { AppId = 730, Name = "Counter-Strike 2", HoursTotal = 89.3 },
                new() { AppId = 570, Name = "Dota 2", HoursTotal = 200.1 }
            };

            // Traditional Assert - verbose and less readable
            Assert.NotNull(games);
            Assert.Equal(3, games.Count);
            Assert.True(games.All(g => g.HoursTotal > 0));
            Assert.Contains(games, g => g.Name?.Contains("Counter-Strike") == true);

            // FluentAssertions - more expressive and readable
            games.Should().NotBeNull()
                .And.HaveCount(3)
                .And.AllSatisfy(game => game.HoursTotal.Should().BePositive())
                .And.ContainSingle(g => g.Name != null && g.Name.Contains("Counter-Strike"));
        }

        [Fact]
        public void ExceptionAssertions_Comparison()
        {
            // Traditional Assert
            var ex = Assert.Throws<ArgumentNullException>(() => 
                new FriendsService(null!, null!));
            Assert.Equal("cache", ex.ParamName);

            // FluentAssertions - more readable chain
            var act = () => new FriendsService(null!, null!);
            act.Should().Throw<ArgumentNullException>()
                .Which.ParamName.Should().Be("cache");
        }

        [Fact]
        public void StringAssertions_Advanced()
        {
            var steamApiUrl = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/";

            // FluentAssertions provides rich string assertions
            steamApiUrl.Should()
                .StartWith("https://")
                .And.Contain("steampowered.com")
                .And.EndWith("/")
                .And.MatchRegex(@"^https://api\.steampowered\.com/.*");
        }

        [Fact]
        public void NumericAssertions_WithPrecision()
        {
            var hours = 120.333333;
            var expectedHours = 120.33;

            // FluentAssertions handles precision elegantly
            hours.Should().BeApproximately(expectedHours, 0.01, 
                "because we're converting from minutes to hours with rounding");
        }

        [Fact]
        public void CustomMessages_Example()
        {
            var friendsCount = 5;
            var maxFriends = 10;

            // FluentAssertions allows custom failure messages
            friendsCount.Should().BeLessThan(maxFriends, 
                "because the Steam API limits friends list size to {0}", maxFriends);
        }
    }
}