using Flurl.Http.Testing;
using Steam_API.Dto.Input;
using Steam_API.Services;
using Steam_API_Tests.TestHelpers;
using FluentAssertions;

namespace Steam_API_Tests.Services
{
    public class SteamApiClientHttpTests : TestBase, IDisposable
    {
        private readonly HttpTest _httpTest;
        private readonly SteamApiClient _client;

        public SteamApiClientHttpTests()
        {
            _httpTest = new HttpTest();
            _client = new SteamApiClient(MockConfiguration.Object);
        }

        [Fact]
        public async Task GetGamePlayTimeAsync_WithValidResponse_ReturnsCorrectGameHours()
        {
            // Arrange
            var steamId = "76561198000000000";
            
            var ownedGamesResponse = new OwnedGamesResponse
            {
                response = new OwnedGames
                {
                    games = new List<OwnedGame>
                    {
                        new OwnedGame
                        {
                            appid = 440,
                            name = "Team Fortress 2",
                            img_icon_url = "e3f595a92552da3d664ad00277fad2107345f743",
                            playtime_forever = 12000 // 200 hours
                        },
                        new OwnedGame
                        {
                            appid = 730,
                            name = "Counter-Strike 2",
                            img_icon_url = "69f7ebe2735c366c65c0b33dae00e12dc40edbe4",
                            playtime_forever = 6000 // 100 hours
                        }
                    }
                }
            };

            var recentlyPlayedResponse = new RecentlyPlayedResponse
            {
                Response = new RecentlyPlayedData
                {
                    Games = new List<RecentlyPlayedGame>
                    {
                        new RecentlyPlayedGame
                        {
                            AppId = 440,
                            Playtime2Weeks = 120 // 2 hours in last 2 weeks
                        },
                        new RecentlyPlayedGame
                        {
                            AppId = 730,
                            Playtime2Weeks = 600 // 10 hours in last 2 weeks
                        }
                    }
                }
            };

            _httpTest.RespondWithJson(ownedGamesResponse)   // GetOwnedGames
                    .RespondWithJson(recentlyPlayedResponse); // GetRecentlyPlayedGames

            // Act
            var result = await _client.GetGamePlayTimeAsync(steamId);

            // Assert
            result.Should().NotBeNull();
            var gamesList = result.ToList();
            gamesList.Should().HaveCount(2);

            // Verify sorting (by hours total descending)
            var tf2 = gamesList.First();
            tf2.Should().Match<Steam_API.Dto.Output.GameHoursDto>(g =>
                g.AppId == 440 &&
                g.Name == "Team Fortress 2" &&
                g.img_icon_url == "e3f595a92552da3d664ad00277fad2107345f743");
            tf2.HoursTotal.Should().BeApproximately(200.0, 0.1);
            tf2.Hours2Weeks.Should().BeApproximately(2.0, 0.1);

            var cs2 = gamesList.Last();
            cs2.Should().Match<Steam_API.Dto.Output.GameHoursDto>(g =>
                g.AppId == 730 &&
                g.Name == "Counter-Strike 2");
            cs2.HoursTotal.Should().BeApproximately(100.0, 0.1);
            cs2.Hours2Weeks.Should().BeApproximately(10.0, 0.1);

            // Verify HTTP calls
            _httpTest.ShouldHaveCalled("https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/*")
                    .WithQueryParam("steamid", steamId)
                    .WithQueryParam("include_appinfo", "1")
                    .WithQueryParam("include_played_free_games", "1")
                    .WithQueryParam("format", "json")
                    .Times(1);

            _httpTest.ShouldHaveCalled("https://api.steampowered.com/IPlayerService/GetRecentlyPlayedGames/v1/*")
                    .WithQueryParam("steamid", steamId)
                    .Times(1);
        }

        [Fact]
        public async Task GetGamePlayTimeAsync_WithEmptyGames_ReturnsEmptyList()
        {
            // Arrange
            var steamId = "76561198000000000";
            
            var ownedGamesResponse = new OwnedGamesResponse
            {
                response = new OwnedGames { games = null }
            };

            var recentlyPlayedResponse = new RecentlyPlayedResponse
            {
                Response = new RecentlyPlayedData { Games = new List<RecentlyPlayedGame>() }
            };

            _httpTest.RespondWithJson(ownedGamesResponse)
                    .RespondWithJson(recentlyPlayedResponse);

            // Act
            var result = await _client.GetGamePlayTimeAsync(steamId);

            // Assert
            result.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public async Task GetGamePlayTimeAsync_WithNoRecentActivity_SetsZeroHours2Weeks()
        {
            // Arrange
            var steamId = "76561198000000000";
            
            var ownedGamesResponse = new OwnedGamesResponse
            {
                response = new OwnedGames
                {
                    games = new List<OwnedGame>
                    {
                        new OwnedGame
                        {
                            appid = 570,
                            name = "Dota 2",
                            img_icon_url = "0bbb630d63262dd66d2fdd0f7d37e8661a410075",
                            playtime_forever = 30000 // 500 hours
                        }
                    }
                }
            };

            var recentlyPlayedResponse = new RecentlyPlayedResponse
            {
                Response = new RecentlyPlayedData 
                { 
                    Games = new List<RecentlyPlayedGame>() // No recent activity
                }
            };

            _httpTest.RespondWithJson(ownedGamesResponse)
                    .RespondWithJson(recentlyPlayedResponse);

            // Act
            var result = await _client.GetGamePlayTimeAsync(steamId);

            // Assert
            result.Should().NotBeNull();
            var dota = result.Should().ContainSingle().Subject;
            dota.Should().Match<Steam_API.Dto.Output.GameHoursDto>(g =>
                g.AppId == 570 &&
                g.Name == "Dota 2");
            dota.HoursTotal.Should().BeApproximately(500.0, 0.1);
            dota.Hours2Weeks.Should().BeApproximately(0.0, 0.1, "because there was no recent activity");
        }

        [Fact]
        public async Task GetGamePlayTimeAsync_WithHttpError_ThrowsException()
        {
            // Arrange
            var steamId = "76561198000000000";
            _httpTest.RespondWith("Unauthorized", 401);

            // Act & Assert
            var act = async () => await _client.GetGamePlayTimeAsync(steamId);
            await act.Should().ThrowAsync<Flurl.Http.FlurlHttpException>();
        }

        [Fact]
        public async Task GetGamePlayTimeAsync_WithTimeout_RespectedFromConfiguration()
        {
            // Arrange
            var steamId = "76561198000000000";
            
            // Simulate a slow response that should timeout
            _httpTest.SimulateTimeout();

            // Act & Assert
            var act = async () => await _client.GetGamePlayTimeAsync(steamId);
            await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task GetGamePlayTimeAsync_HandlesPartialData_GracefullyFillsDefaults()
        {
            // Arrange
            var steamId = "76561198000000000";
            
            var ownedGamesResponse = new OwnedGamesResponse
            {
                response = new OwnedGames
                {
                    games = new List<OwnedGame>
                    {
                        new OwnedGame
                        {
                            appid = 12210,
                            name = null, // Missing name
                            img_icon_url = null, // Missing icon
                            playtime_forever = 1800 // 30 hours
                        }
                    }
                }
            };

            var recentlyPlayedResponse = new RecentlyPlayedResponse
            {
                Response = new RecentlyPlayedData 
                { 
                    Games = new List<RecentlyPlayedGame>()
                }
            };

            _httpTest.RespondWithJson(ownedGamesResponse)
                    .RespondWithJson(recentlyPlayedResponse);

            // Act
            var result = await _client.GetGamePlayTimeAsync(steamId);

            // Assert
            result.Should().NotBeNull();
            var game = result.Should().ContainSingle().Subject;
            game.Should().Match<Steam_API.Dto.Output.GameHoursDto>(g =>
                g.AppId == 12210 &&
                g.Name == string.Empty); // Should default to empty string
            game.HoursTotal.Should().BeApproximately(30.0, 0.1);
            game.Hours2Weeks.Should().BeApproximately(0.0, 0.1);
        }

        public void Dispose()
        {
            _httpTest?.Dispose();
        }
    }
}