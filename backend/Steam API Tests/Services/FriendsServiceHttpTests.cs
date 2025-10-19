using Flurl.Http.Testing;
using Microsoft.Extensions.Caching.Memory;
using Steam_API.Dto.Input;
using Steam_API.Services;
using Steam_API_Tests.TestHelpers;
using FluentAssertions;

namespace Steam_API_Tests.Services
{
    public class FriendsServiceHttpTests : TestBase, IDisposable
    {
        private readonly HttpTest _httpTest;
        private readonly FriendsService _service;
        private readonly IMemoryCache _cache;

        public FriendsServiceHttpTests()
        {
            _httpTest = new HttpTest();
            _cache = new MemoryCache(new MemoryCacheOptions());
            _service = new FriendsService(_cache, MockConfiguration.Object);
        }

        [Fact]
        public async Task GetFriendsListAsync_WithValidResponse_ReturnsCorrectDto()
        {
            // Arrange
            var steamId = "76561198000000000";
            var friendsListResponse = new FriendListResponse
            {
                Friendslist = new FriendList
                {
                    Friends = new List<FriendEdge>
                    {
                        new() { Steamid = "76561198000000001", FriendSince = 1600000000 },
                        new() { Steamid = "76561198000000002", FriendSince = 1600000001 }
                    }
                }
            };

            var playerSummariesResponse = new PlayerSummariesResponse
            {
                Response = new PlayerSummaries
                {
                    players = new List<Player>
                    {
                        new() 
                        { 
                            Steamid = "76561198000000000",
                            Personaname = "TestUser",
                            Avatar = "avatar1.jpg",
                            Avatarmedium = "avatar1_medium.jpg",
                            Avatarfull = "avatar1_full.jpg",
                            Communityvisibilitystate = 3,
                            Personastate = 1
                        },
                        new() 
                        { 
                            Steamid = "76561198000000001",
                            Personaname = "Friend1",
                            Avatar = "avatar2.jpg",
                            Avatarmedium = "avatar2_medium.jpg",
                            Avatarfull = "avatar2_full.jpg",
                            Communityvisibilitystate = 3,
                            Personastate = 0
                        },
                        new() 
                        { 
                            Steamid = "76561198000000002",
                            Personaname = "Friend2",
                            Avatar = "avatar3.jpg",
                            Avatarmedium = "avatar3_medium.jpg",
                            Avatarfull = "avatar3_full.jpg",
                            Communityvisibilitystate = 3,
                            Personastate = 2
                        }
                    }
                }
            };

            var levelResponse = new 
            { 
                Response = new 
                { 
                    player_level = 25 
                } 
            };

            // Mock HTTP responses
            _httpTest.RespondWithJson(friendsListResponse)  // GetFriendList
                    .RespondWithJson(playerSummariesResponse)  // GetPlayerSummaries
                    .RespondWithJson(levelResponse)  // GetSteamLevel for Friend1
                    .RespondWithJson(levelResponse)  // GetSteamLevel for Friend2
                    .RespondWithJson(levelResponse); // GetSteamLevel for TestUser

            // Act
            var result = await _service.GetFriendsListAsync(steamId, includeSelf: true);

            // Assert
            result.Should().NotBeNull();
            result.Rows.Should().HaveCount(3);
            
            var testUser = result.Rows.FirstOrDefault(r => r.IsYou);
            testUser.Should().NotBeNull();
            testUser.PersonaName.Should().Be("TestUser");
            testUser.SteamId64.Should().Be(steamId);
            testUser.IsYou.Should().BeTrue();

            var friend1 = result.Rows.FirstOrDefault(r => r.SteamId64 == "76561198000000001");
            friend1.Should().NotBeNull();
            friend1.PersonaName.Should().Be("Friend1");
            friend1.IsYou.Should().BeFalse();
            friend1.SteamLevel.Should().Be(25);

            // Verify HTTP calls
            _httpTest.ShouldHaveCalled("https://api.steampowered.com/ISteamUser/GetFriendList/v1/*")
                    .WithQueryParam("steamid", steamId)
                    .WithQueryParam("relationship", "friend")
                    .Times(1);

            _httpTest.ShouldHaveCalled("https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/*")
                    .Times(1);
        }

        [Fact]
        public async Task GetFriendsListAsync_WithEmptyFriendsList_ReturnsOnlyUser()
        {
            // Arrange
            var steamId = "76561198000000000";
            var friendsListResponse = new FriendListResponse
            {
                Friendslist = new FriendList 
                {
                    Friends = []
                }
            };

            var playerSummariesResponse = new PlayerSummariesResponse
            {
                Response = new PlayerSummaries
                {
                    players = new List<Player>
                    {
                        new Player 
                        { 
                            Steamid = steamId,
                            Personaname = "TestUser",
                            Avatar = "avatar.jpg",
                            Avatarmedium = "avatar_medium.jpg",
                            Avatarfull = "avatar_full.jpg",
                            Communityvisibilitystate = 3,
                            Personastate = 1
                        }
                    }
                }
            };

            var levelResponse = new 
            { 
                Response = new 
                { 
                    player_level = 30 
                } 
            };

            _httpTest.RespondWithJson(friendsListResponse)
                    .RespondWithJson(playerSummariesResponse)
                    .RespondWithJson(levelResponse);

            // Act
            var result = await _service.GetFriendsListAsync(steamId, includeSelf: true);

            // Assert
            result.Should().NotBeNull();
            result.Rows.Should().ContainSingle()
                .Which.Should().Match<Steam_API.Dto.Output.FriendSummaryDto>(f => 
                    f.IsYou && 
                    f.PersonaName == "TestUser" && 
                    f.SteamLevel == 30);
        }

        [Fact]
        public async Task GetFriendsListAsync_WithHttpError_ThrowsException()
        {
            // Arrange
            var steamId = "76561198000000000";
            _httpTest.RespondWith("Server Error", 500);

            // Act & Assert
            var act = async () => await _service.GetFriendsListAsync(steamId);
            await act.Should().ThrowAsync<Flurl.Http.FlurlHttpException>();
        }

        [Fact]
        public async Task GetFriendsListAsync_WithoutIncludeSelf_DoesNotIncludeUser()
        {
            // Arrange
            var steamId = "76561198000000000";
            var friendsListResponse = new FriendListResponse
            {
                Friendslist = new FriendList
                {
                    Friends =
                    [
                        new FriendEdge 
                        { 
                            Steamid = "76561198000000001", 
                            FriendSince = 1600000000 
                        }
                    ]
                }
            };

            var playerSummariesResponse = new PlayerSummariesResponse
            {
                Response = new PlayerSummaries
                {
                    players = new List<Player>
                    {
                        new() 
                        { 
                            Steamid = "76561198000000001",
                            Personaname = "Friend1",
                            Avatar = "avatar.jpg",
                            Avatarmedium = "avatar_medium.jpg",
                            Avatarfull = "avatar_full.jpg",
                            Communityvisibilitystate = 3,
                            Personastate = 1
                        }
                    }
                }
            };

            var levelResponse = new 
            { 
                Response = new 
                { 
                    player_level = 15 
                } 
            };

            _httpTest.RespondWithJson(friendsListResponse)
                    .RespondWithJson(playerSummariesResponse)
                    .RespondWithJson(levelResponse);

            // Act
            var result = await _service.GetFriendsListAsync(steamId, includeSelf: false);

            // Assert
            result.Should().NotBeNull();
            result.Rows.Should().ContainSingle()
                .Which.Should().Match<Steam_API.Dto.Output.FriendSummaryDto>(f => 
                    !f.IsYou && f.PersonaName == "Friend1");
        }

        [Fact]
        public async Task GetFriendsListAsync_CachesLevel_DoesNotCallLevelAPITwice()
        {
            // Arrange
            var steamId = "76561198000000000";
            var friendId = "76561198000000001";
            
            // Pre-populate cache
            _cache.Set($"steam:level:{friendId}", 42, TimeSpan.FromMinutes(30));
            
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
                            Steamid = friendId,
                            Personaname = "CachedFriend",
                            Avatar = "avatar.jpg",
                            Avatarmedium = "avatar_medium.jpg",
                            Avatarfull = "avatar_full.jpg",
                            Communityvisibilitystate = 3,
                            Personastate = 1
                        }
                    ]
                }
            };

            _httpTest.RespondWithJson(friendsListResponse)
                    .RespondWithJson(playerSummariesResponse);

            // Act
            var result = await _service.GetFriendsListAsync(steamId, includeSelf: false);

            // Assert
            result.Should().NotBeNull();
            result.Rows.Should().ContainSingle()
                .Which.SteamLevel.Should().Be(42, "because the level should come from cache");

            // Should NOT call the level API due to caching
            _httpTest.ShouldNotHaveCalled("https://api.steampowered.com/IPlayerService/GetSteamLevel/v1/*");
        }

        public void Dispose()
        {
            _httpTest?.Dispose();
            _cache?.Dispose();
        }
    }
}