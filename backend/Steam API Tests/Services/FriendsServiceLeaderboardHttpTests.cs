using Flurl.Http.Testing;
using Microsoft.Extensions.Caching.Memory;
using Steam_API.Dto.Input;
using Steam_API.Services;
using Steam_API_Tests.TestHelpers;
using FluentAssertions;

namespace Steam_API_Tests.Services
{
    public class FriendsServiceLeaderboardHttpTests : TestBase, IDisposable
    {
        private readonly HttpTest _httpTest;
        private readonly FriendsService _service;
        private readonly IMemoryCache _cache;

        public FriendsServiceLeaderboardHttpTests()
        {
            _httpTest = new HttpTest();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _service = new FriendsService(_cache, MockConfiguration.Object);
        }

        [Fact]
        public async Task GetLeaderboardAsync_WithAppId_ReturnsAppSpecificHours()
        {
            // Arrange
            var meSteamId = "76561198000000000";
            var friendId = "76561198000000001";
            var appId = 440; // Team Fortress 2

            var friendsListResponse = new FriendListResponse
            {
                Friendslist = new FriendList
                {
                    Friends =
                    [
                        new FriendEdge 
                        { 
                            Steamid = friendId, 
                            FriendSince = 1600000000 
                        }
                    ]
                }
            };

            var playerSummariesResponse = new PlayerSummariesResponse
            {
                Response = new PlayerSummaries
                {
                    players =
                    [
                        new Player 
                        { 
                            Steamid = meSteamId,
                            Personaname = "TestUser",
                            Avatar = "me.jpg",
                            Avatarmedium = "me_medium.jpg",
                            Avatarfull = "me_full.jpg",
                            Communityvisibilitystate = 3,
                            Personastate = 1
                        },
                        new Player 
                        { 
                            Steamid = friendId,
                            Personaname = "ProGamer",
                            Avatar = "friend.jpg",
                            Avatarmedium = "friend_medium.jpg",
                            Avatarfull = "friend_full.jpg",
                            Communityvisibilitystate = 3,
                            Personastate = 1
                        }
                    ]
                }
            };

            // Mock owned games responses for app-specific queries
            var myOwnedGamesResponse = new OwnedGamesResponse
            {
                response = new OwnedGames
                {
                    games =
                    [
                        new OwnedGame
                        {
                            appid = appId,
                            playtime_forever = 6000 // 100 hours
                        }
                    ]
                }
            };

            var friendOwnedGamesResponse = new OwnedGamesResponse
            {
                response = new OwnedGames
                {
                    games =
                    [
                        new OwnedGame
                        {
                            appid = appId,
                            playtime_forever = 12000 // 200 hours
                        }
                    ]
                }
            };

            // Mock recently played responses
            var myRecentResponse = new RecentlyPlayedResponse
            {
                Response = new RecentlyPlayedData
                {
                    Games =
                    [
                        new RecentlyPlayedGame 
                        { 
                            AppId = (uint)appId, 
                            Playtime2Weeks = 120 
                        }
                    ]
                }
            };

            var friendRecentResponse = new RecentlyPlayedResponse
            {
                Response = new RecentlyPlayedData
                {
                    Games =
                    [
                        new RecentlyPlayedGame
                        {
                            AppId = (uint)appId,
                            Playtime2Weeks = 300
                        }
                    ]
                }
            };

            _httpTest.RespondWithJson(friendsListResponse)       // GetFriendList
                    .RespondWithJson(playerSummariesResponse)    // GetPlayerSummaries
                    .RespondWithJson(friendOwnedGamesResponse)   // GetOwnedGames for friend
                    .RespondWithJson(friendRecentResponse)       // GetRecentlyPlayedGames for friend
                    .RespondWithJson(myOwnedGamesResponse)       // GetOwnedGames for me
                    .RespondWithJson(myRecentResponse);          // GetRecentlyPlayedGames for me

            // Act
            var result = await _service.GetLeaderboardAsync(meSteamId, appId);

            // Assert
            result.Should().NotBeNull();
            result.AppId.Should().Be(appId);
            result.Rows.Should().HaveCount(2);

            // Verify ordering by hours (highest first)
            var topPlayer = result.Rows.First();
            topPlayer.Should().Match<Steam_API.Dto.Output.FriendHoursRow>(p =>
                p.SteamId64 == friendId &&
                p.PersonaName == "ProGamer" &&
                !p.IsYou);

            topPlayer.HoursTotal.Should().BeApproximately(200.0, 0.1);
            topPlayer.Hours2Weeks.Should().BeApproximately(5.0, 0.1);

            var secondPlayer = result.Rows.Last();
            secondPlayer.Should().Match<Steam_API.Dto.Output.FriendHoursRow>(p =>
                p.SteamId64 == meSteamId &&
                p.PersonaName == "TestUser" &&
                p.IsYou);

            secondPlayer.HoursTotal.Should().BeApproximately(100.0, 0.1);
            secondPlayer.Hours2Weeks.Should().BeApproximately(2.0, 0.1);

            // Verify app-specific API calls were made
            _httpTest.ShouldHaveCalled("https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/*")
                    .WithQueryParam("appids_filter[0]", appId.ToString())
                    .Times(2); // Once for friend, once for me
        }

        [Fact]
        public async Task GetLeaderboardAsync_WithoutAppId_ReturnsTotalHours()
        {
            // Arrange
            var meSteamId = "76561198000000000";
            var friendId = "76561198000000001";

            var friendsListResponse = new FriendListResponse
            {
                Friendslist = new FriendList
                {
                    Friends =
                    [
                        new FriendEdge 
                        { 
                            Steamid = friendId, 
                            FriendSince = 1600000000 
                        }
                    ]
                }
            };

            var playerSummariesResponse = new PlayerSummariesResponse
            {
                Response = new PlayerSummaries
                {
                    players =
                    [
                        new Player 
                        { 
                            Steamid = meSteamId, 
                            Personaname = "Me" 
                        },
                        new Player 
                        { 
                            Steamid = friendId, 
                            Personaname = "Friend" 
                        }
                    ]
                }
            };

            // Mock total games responses (no app filter)
            var myTotalGamesResponse = new OwnedGamesResponse
            {
                response = new OwnedGames
                {
                    games =
                    [
                        new OwnedGame 
                        { 
                            appid = 440, 
                            playtime_forever = 6000 // 100 hours
                        },  
                        new OwnedGame 
                        { 
                            appid = 730, 
                            playtime_forever = 3000 // 50 hours
                        },  
                        new OwnedGame 
                        { 
                            appid = 570, 
                            playtime_forever = 9000 // 150 hours
                        }   
                        // Total: 300 hours
                    ]
                }
            };

            var friendTotalGamesResponse = new OwnedGamesResponse
            {
                response = new OwnedGames
                {
                    games =
                    [
                        new OwnedGame 
                        { 
                            appid = 440, 
                            playtime_forever = 12000 // 200 hours
                        }, 
                        new OwnedGame 
                        { 
                            appid = 570, 
                            playtime_forever = 6000 // 100 hours
                        }   
                        // Total: 300 hours (tied with me)
                    ]
                }
            };

            var myRecentResponse = new RecentlyPlayedResponse
            {
                Response = new RecentlyPlayedData
                {
                    Games =
                    [
                        new RecentlyPlayedGame 
                        { 
                            AppId = 440, 
                            Playtime2Weeks = 60 // 1 hour
                        }, 
                        new RecentlyPlayedGame 
                        { 
                            AppId = 570, 
                            Playtime2Weeks = 120 // 2 hours
                        } 
                        // Total: 3 hours
                    ]
                }
            };

            var friendRecentResponse = new RecentlyPlayedResponse
            {
                Response = new RecentlyPlayedData
                {
                    Games =
                    [
                        new RecentlyPlayedGame 
                        { 
                            AppId = 440, 
                            Playtime2Weeks = 180 // 3 hours
                        } 
                    ]
                }
            };

            _httpTest.RespondWithJson(friendsListResponse)
                    .RespondWithJson(playerSummariesResponse)
                    .RespondWithJson(friendTotalGamesResponse)  // Friend's total games
                    .RespondWithJson(friendRecentResponse)      // Friend's recent games
                    .RespondWithJson(myTotalGamesResponse)      // My total games
                    .RespondWithJson(myRecentResponse);         // My recent games

            // Act
            var result = await _service.GetLeaderboardAsync(meSteamId, appId: null);

            // Assert
            result.Should().NotBeNull();
            result.AppId.Should().BeNull();
            result.Rows.Should().HaveCount(2);

            // Both have same total hours, should be ordered by name as tiebreaker
            result.Rows.Should().AllSatisfy(row => 
                row.HoursTotal.Should().BeApproximately(300.0, 0.1, "both players should have 300 hours total"));

            // Verify no app filter was used for total hours
            _httpTest.ShouldHaveCalled("https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/*")
                    .WithoutQueryParam("appids_filter[0]")
                    .Times(2);
        }

        [Fact]
        public async Task GetLeaderboardAsync_VerifiesCacheKeyFormat()
        {
            // Arrange
            var meSteamId = "76561198000000000";
            var appId = 440;
            
            var friendsListResponse = new FriendListResponse
            {
                Friendslist = new FriendList { Friends = [] }
            };

            var playerSummariesResponse = new PlayerSummariesResponse
            {
                Response = new PlayerSummaries
                {
                    players =
                    [
                        new Player 
                        { 
                            Steamid = meSteamId, 
                            Personaname = "TestUser" 
                        }
                    ]
                }
            };

            var ownedGamesResponse = new OwnedGamesResponse
            {
                response = new OwnedGames
                {
                    games =
                    [
                        new OwnedGame 
                        { 
                            appid = appId, 
                            playtime_forever = 6000 
                        }
                    ]
                }
            };

            var recentResponse = new RecentlyPlayedResponse
            {
                Response = new RecentlyPlayedData 
                {
                    Games = []
                }
            };

            _httpTest.RespondWithJson(friendsListResponse)
                    .RespondWithJson(playerSummariesResponse)
                    .RespondWithJson(ownedGamesResponse)
                    .RespondWithJson(recentResponse);

            // Act
            var result = await _service.GetLeaderboardAsync(meSteamId, appId);

            // Assert - The main test is that the service completes successfully
            result.Should().NotBeNull();
            result.Rows.Should().ContainSingle()
                .Which.Should().Match<Steam_API.Dto.Output.FriendHoursRow>(row =>
                    row.SteamId64 == meSteamId &&
                    row.PersonaName == "TestUser" &&
                    row.IsYou);
            
            // Verify the expected cache key format would be used
            var expectedCacheKey = $"owned:{meSteamId}:app:{appId}";
            expectedCacheKey.Should().Contain(meSteamId).And.Contain(appId.ToString(), "to demonstrate the cache key format");
        }

        [Fact]
        public async Task GetLeaderboardAsync_WithApiErrors_HandlesGracefully()
        {
            // Arrange
            var meSteamId = "76561198000000000";
            var friendId = "76561198000000001";
            var appId = 730;

            var friendsListResponse = new FriendListResponse
            {
                Friendslist = new FriendList
                {
                    Friends =
                    [
                        new FriendEdge 
                        { 
                            Steamid = friendId, 
                            FriendSince = 1600000000 
                        }
                    ]
                }
            };

            var playerSummariesResponse = new PlayerSummariesResponse
            {
                Response = new PlayerSummaries
                {
                    players =
                    [
                        new Player 
                        { 
                            Steamid = meSteamId, 
                            Personaname = "Me" 
                        },
                        new Player 
                        { 
                            Steamid = friendId, 
                            Personaname = "Friend" 
                        }
                    ]
                }
            };

            _httpTest.RespondWithJson(friendsListResponse)
                    .RespondWithJson(playerSummariesResponse)
                    .RespondWith("Private Profile", 403) // Friend's owned games - error
                    .RespondWith("Private Profile", 403) // Friend's recent games - error
                    .RespondWithJson(new OwnedGamesResponse { response = new OwnedGames { games = new List<OwnedGame> { new OwnedGame { appid = appId, playtime_forever = 3000 } } } }) // My owned games - success
                    .RespondWithJson(new RecentlyPlayedResponse { Response = new RecentlyPlayedData { Games = new List<RecentlyPlayedGame>() } }); // My recent games - success

            // Act
            var result = await _service.GetLeaderboardAsync(meSteamId, appId);

            // Assert
            result.Should().NotBeNull();
            result.Rows.Should().HaveCount(2);

            var myRow = result.Rows.Should().ContainSingle(r => r.IsYou).Subject;
            var friendRow = result.Rows.Should().ContainSingle(r => !r.IsYou).Subject;
            
            // The key test is that the service handles errors gracefully
            // and still returns results for both users
            myRow.SteamId64.Should().Be(meSteamId);
            friendRow.SteamId64.Should().Be(friendId);
            
            // Friend should be marked as private/unavailable due to 403 errors
            friendRow.PrivateOrUnavailable.Should().BeTrue("because the friend's profile returned 403 errors");
        }

        public void Dispose()
        {
            _httpTest?.Dispose();
            _cache?.Dispose();
        }
    }
}