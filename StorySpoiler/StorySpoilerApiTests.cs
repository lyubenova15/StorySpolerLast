using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using StorySpoiler.Models;





namespace StorySpoiler

{
    [TestFixture]
    public class StorySpoilerApiTests
    {
        private RestClient client;
        private static string lastCreatedStoryId;
        private const string BaseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiIzYWUzNmYyNi1mMTUxLTQ5ZjQtYWI4NC01MTE2MWVlN2I2NTIiLCJpYXQiOiIwOC8xNi8yMDI1IDA3OjAxOjQxIiwiVXNlcklkIjoiOWY1NzUwYWEtMmQ1Mi00MjQ1LThlMmUtMDhkZGRiMWExM2YzIiwiRW1haWwiOiJpdmExQGFidi5iZyIsIlVzZXJOYW1lIjoiZXhhbVJlZyIsImV4cCI6MTc1NTM0OTMwMSwiaXNzIjoiU3RvcnlTcG9pbF9BcHBfU29mdFVuaSIsImF1ZCI6IlN0b3J5U3BvaWxfV2ViQVBJX1NvZnRVbmkifQ._GTblNT18jVK1-ruPDSLXpJmi54OoU_RjxIbzFyM6p0";

        private const string LoginUser = "examReg";
        private const string LoginPassword = "qwerty1234567";



        [OneTimeSetUp]



        public void Setup()
        {
            string jwtToken;

            if (string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }

            else
            {
                jwtToken = GetJwtToken(LoginUser, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);



        }

        private string GetJwtToken(string username, string password)
        {

            var tempClient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new
            {
                username,
                password
            });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if(string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrive JWT token from response.");
                }

                return token;

                
            }

            else
            {
                throw new InvalidOperationException($"Failed to authenticate.Status code: {response.StatusCode}, Content: {response.Content}");
            }
            
        }

        //Tests
        [Test, Order(1)]
        public void CreateNewStory()
        {
            var story = new StorySpoiler.Models.StoryDTO
            {
                Title = "New Story",
                Description = "Some description",
                Url = ""
            };

            var request = new RestRequest("/api/Story/Create", Method.Post)
                .AddJsonBody(story);

            var response = client.Execute(request);

            // ?????? ??? ?????
            TestContext.Out.WriteLine("RAW RESPONSE:");
            TestContext.Out.WriteLine(response.Content);

            // 1) 201 Created
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                $"Expected 201 Created, got {(int)response.StatusCode} {response.StatusDescription}. Body: {response.Content}");

            // 2) ?????? JSON ? ????? storyId + msg
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);

            // storyId ?????? id
            string? storyId = json.TryGetProperty("storyId", out var idEl) ? idEl.GetString() : null;
            Assert.That(storyId, Is.Not.Null.And.Not.Empty, "Story Id should not be null or empty");
            lastCreatedStoryId = storyId!;

            // "Successfully created!"
            string? msg = json.TryGetProperty("msg", out var msgEl) ? msgEl.GetString() : null;
            Assert.That(msg, Is.EqualTo("Successfully created!"),
                $"Expected msg='Successfully created!' but got '{msg}'. Body: {response.Content}");
        }


        [Test, Order(2)]

        public void EditStory_ShoudReturnOkAndSuccessMessage()
        {
            // ??? ????????? ?? ?? ?????????? ?? ???, ?? ?????????????
            if (string.IsNullOrWhiteSpace(lastCreatedStoryId))
            {
                Assert.Inconclusive("No StoryId available. Run CreateNewStory first to obtain lastCreatedStoryId.");
            }

            // ?????? ?? ?????????
            var edited = new StorySpoiler.Models.StoryDTO
            {
                Title = "string",
                Description = "string",
                Url = "http://*lK')\"-(?kP7c=i6Ha&E,ExeOuT;;v)\\aWV5+{lk^c@y6lBm#aZ]HFJlaaI~>[&my.bmp"
            };

            // PUT ??? endpoint ? path variable (??? ????? ??? ? ????????, ????? ?? ???)
            var request = new RestRequest($"/api/Story/Edit/{lastCreatedStoryId}", Method.Put)
                .AddJsonBody(edited);

            var response = client.Execute(request);

            // ??? ?? ?????
            TestContext.Out.WriteLine("RAW RESPONSE (EDIT):");
            TestContext.Out.WriteLine(response.Content);

            // 1) ?????? ??? 200 OK
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"Expected 200 OK, got {(int)response.StatusCode} {response.StatusDescription}. Body: {response.Content}");

            // 2) ????????? "Successfully edited" (??????????? ? ?????????? ?? ????? ??????)
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
            string? msg = json.TryGetProperty("msg", out var msgEl) ? msgEl.GetString() : null;

            Assert.That(msg, Is.EqualTo("Successfully edited").Or.EqualTo("Successfully edited!"),
                $"Unexpected msg. Expected 'Successfully edited', got '{msg}'. Body: {response.Content}");
        }

        [Test, Order(3)]
        public void GetAllStories_ShouldReturnOkAndNonEmptyArray()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);

            var response = client.Execute(request);

            // ??? ?? ?????
            TestContext.Out.WriteLine("RAW RESPONSE (GET ALL):");
            TestContext.Out.WriteLine(response.Content);

            // 1) ?????? ??? 200 OK
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"Expected 200 OK, got {(int)response.StatusCode} {response.StatusDescription}. Body: {response.Content}");

            // 2) ???????? JSON-?
            Assert.That(response.Content, Is.Not.Null.And.Not.Empty, "Response body is empty.");

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);

            Assert.That(json.ValueKind, Is.EqualTo(JsonValueKind.Array),
                $"Expected array, got {json.ValueKind}. Body: {response.Content}");

            Assert.That(json.GetArrayLength(), Is.GreaterThan(0),
                "Expected non-empty array of stories.");
        }


        [Test, Order(4)]
        public void DeleteStory_ShouldReturnOkAndSuccessMessage()
        {
            // 0) ??? ?????? StoryId (?????? ?? ????? ?????????????), ?????? ??????? ???
            if (string.IsNullOrWhiteSpace(lastCreatedStoryId))
            {
                var createBody = new StorySpoiler.Models.StoryDTO
                {
                    Title = "Temp Story For Delete",
                    Description = "Temp description",
                    Url = ""
                };

                var createReq = new RestRequest("/api/Story/Create", Method.Post)
                    .AddJsonBody(createBody);

                var createResp = client.Execute(createReq);

                TestContext.Out.WriteLine("RAW RESPONSE (CREATE inside DELETE):");
                TestContext.Out.WriteLine(createResp.Content);

                Assert.That(createResp.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                    $"Create failed before delete. {(int)createResp.StatusCode} {createResp.StatusDescription}. Body: {createResp.Content}");

                var createJson = JsonSerializer.Deserialize<JsonElement>(createResp.Content!);
                lastCreatedStoryId = createJson.GetProperty("storyId").GetString()!;
                Assert.That(lastCreatedStoryId, Is.Not.Null.And.Not.Empty, "Story Id should not be null or empty");
            }

            // 1) ?????????
            var deleteReq = new RestRequest($"/api/Story/Delete/{lastCreatedStoryId}", Method.Delete);
            var deleteResp = client.Execute(deleteReq);

            TestContext.Out.WriteLine("RAW RESPONSE (DELETE):");
            TestContext.Out.WriteLine(deleteResp.Content);

            // 2) ???????? 200 OK
            Assert.That(deleteResp.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"Expected 200 OK, got {(int)deleteResp.StatusCode} {deleteResp.StatusDescription}. Body: {deleteResp.Content}");

            // 3) ???????? "Deleted successfully!"
            var delJson = JsonSerializer.Deserialize<JsonElement>(deleteResp.Content!);
            string? msg = delJson.TryGetProperty("msg", out var msgEl) ? msgEl.GetString() : null;

            Assert.That(msg, Is.EqualTo("Deleted successfully!"),
                $"Expected 'Deleted successfully!' but got '{msg}'. Body: {deleteResp.Content}");
        }


        [Test, Order(5)]
        public void CreateStory_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            // ???? ?? ????????? ??? Title ? Description
            var invalidStory = new StorySpoiler.Models.StoryDTO
            {
                Title = null,
                Description = null,
                Url = "" // ??? URL ?? ? required, ????? ?? ?? ???????
            };

            var request = new RestRequest("/api/Story/Create", Method.Post)
                .AddJsonBody(invalidStory);

            var response = client.Execute(request);

            // ??? ?? ?????
            TestContext.Out.WriteLine("RAW RESPONSE (INVALID CREATE):");
            TestContext.Out.WriteLine(response.Content);

            // 1) ???????? ?????? 400 Bad Request
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                $"Expected 400 BadRequest, got {(int)response.StatusCode} {response.StatusDescription}. Body: {response.Content}");
        }

        [Test, Order(6)]
        public void Edit_NonExistingStory_ShouldReturnNotFound()
        {
            // ???????? StoryId (GUID, ????? ???? ??? ?? ?????????? ? ?????????)
            var nonExistingId = "00000000-0000-0000-0000-000000000000";

            var edited = new StorySpoiler.Models.StoryDTO
            {
                Title = "Edited title",
                Description = "Edited description",
                Url = "http://example.com/non-existing.bmp"
            };

            // PUT ??? endpoint ??? StoryId, ????? ?? ??????????
            var request = new RestRequest($"/api/Story/Edit/{nonExistingId}", Method.Put)
                .AddJsonBody(edited);

            var response = client.Execute(request);

            // ??? ?? ?????
            TestContext.Out.WriteLine("RAW RESPONSE (EDIT NON-EXISTING):");
            TestContext.Out.WriteLine(response.Content);

            // 1) ???????? 404 Not Found
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound),
                $"Expected 404 NotFound, got {(int)response.StatusCode} {response.StatusDescription}. Body: {response.Content}");

            // 2) ???????? ????????? "No spoilers..."
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
            string? msg = json.TryGetProperty("msg", out var msgEl) ? msgEl.GetString() : null;

            Assert.That(msg, Does.Contain("No spoilers"),
                $"Expected message to contain 'No spoilers', but got '{msg}'. Body: {response.Content}");
        }
        [Test, Order(7)]
        public void Delete_NonExistingStory_ShouldReturnBadRequest()
        {
            // ???????? StoryId (???? ??? ?? ?????????? ? ?????????)
            var nonExistingId = "00000000-0000-0000-0000-000000000000";

            var request = new RestRequest($"/api/Story/Delete/{nonExistingId}", Method.Delete);

            var response = client.Execute(request);

            // ??? ?? ?????
            TestContext.Out.WriteLine("RAW RESPONSE (DELETE NON-EXISTING):");
            TestContext.Out.WriteLine(response.Content);

            // 1) ???????? 400 Bad Request
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                $"Expected 400 BadRequest, got {(int)response.StatusCode} {response.StatusDescription}. Body: {response.Content}");

            // 2) ???????? ????????? "Unable to delete this story spoiler!"
            var json = JsonSerializer.Deserialize<JsonElement>(response.Content!);
            string? msg = json.TryGetProperty("msg", out var msgEl) ? msgEl.GetString() : null;

            Assert.That(msg, Is.EqualTo("Unable to delete this story spoiler!"),
                $"Expected 'Unable to delete this story spoiler!' but got '{msg}'. Body: {response.Content}");
        }















        [OneTimeTearDown]

        public void TearDown()
        {
            this.client?.Dispose();
        }


    }
}