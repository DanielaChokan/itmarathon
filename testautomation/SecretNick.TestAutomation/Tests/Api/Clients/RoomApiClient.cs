using Microsoft.Playwright;
using Serilog;
using Tests.Api.Models.Requests;
using Tests.Api.Models.Responses;

namespace Tests.Api.Clients
{
    public class RoomApiClient(IAPIRequestContext apiContext) : ApiClientBase(apiContext)
    {
        public IAPIRequestContext Context => ApiContext;

        public async Task<RoomCreationResponse> CreateRoomAsync(RoomCreationRequest request)
        {
            var url = "/api/rooms";
            var requestInfo = $"POST {url}\nBody: {SerializeRequest(request)}";

            Log.Information("Creating room via API: {RequestInfo}", requestInfo);

            var response = await ApiContext.PostAsync(url, new APIRequestContextOptions
            {
                DataObject = request
            });

            await ValidateResponseAsync(response, 201, requestInfo);

            var result = await DeserializeResponseAsync<RoomCreationResponse>(response);
            Log.Information("Room created: ID={RoomId}, InvitationCode={InvitationCode}, UserCode={UserCode}",
                result.Room.Id, result.Room.InvitationCode, result.UserCode);

            return result;
        }

        public async Task<RoomReadDto> GetRoomByUserCodeAsync(string userCode)
        {
            var url = $"/api/rooms?userCode={userCode}";
            var requestInfo = $"GET {url}";

            Log.Information("Getting room by user code: {RequestInfo}", requestInfo);

            var response = await ApiContext.GetAsync(url);

            await ValidateResponseAsync(response, 200, requestInfo);

            var result = await DeserializeResponseAsync<RoomReadDto>(response);
            Log.Information("Room retrieved: {RoomName} (ID={RoomId})",
                result.Name, result.Id);

            return result;
        }

        public async Task<RoomReadDto> GetRoomByInvitationCodeAsync(string roomCode)
        {
            var url = $"/api/rooms?roomCode={roomCode}";
            var requestInfo = $"GET {url}";

            Log.Information("Getting room by invitation code: {RequestInfo}", requestInfo);

            var response = await ApiContext.GetAsync(url);

            await ValidateResponseAsync(response, 200, requestInfo);

            var result = await DeserializeResponseAsync<RoomReadDto>(response);
            Log.Information("Room retrieved: {RoomName} (ID={RoomId})",
                result.Name, result.Id);

            return result;
        }

        public async Task<RoomReadDto> UpdateRoomAsync(string userCode, RoomPatchRequest request)
        {
            var url = $"/api/rooms?userCode={userCode}";
            var requestInfo = $"PATCH {url}\nBody: {SerializeRequest(request)}";

            Log.Information("Updating room: {RequestInfo}", requestInfo);

            var response = await ApiContext.PatchAsync(url,
                new APIRequestContextOptions
                {
                    DataObject = request
                });

            await ValidateResponseAsync(response, 200, requestInfo);

            var result = await DeserializeResponseAsync<RoomReadDto>(response);
            Log.Information("Room updated: {RoomName}", result.Name);

            return result;
        }

        public async Task<object> DrawRoomAsync(string userCode)
        {
            var url = $"/api/rooms/draw?userCode={userCode}";
            var requestInfo = $"POST {url}";

            Log.Information("Drawing names: {RequestInfo}", requestInfo);

            var response = await ApiContext.PostAsync(url, null);
            await ValidateResponseAsync(response, 200, requestInfo);

            var responseBody = await response.TextAsync();

            if (responseBody.TrimStart().StartsWith('['))
            {
                var result = await DeserializeResponseAsync<List<UserReadDto>>(response);
                Log.Information("Names drawn for {Count} users", result.Count);
                return result;
            }

            var user = await DeserializeResponseAsync<UserReadDto>(response);
            Log.Information("Names drawn, recipient assigned");
            return user;
        }

        public async Task DrawNamesAsync(string userCode)
        {
            await DrawRoomAsync(userCode);
        }
    }
}
