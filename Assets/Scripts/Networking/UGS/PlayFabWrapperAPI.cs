using PlayFab;
using System;
using System.Threading.Tasks;
#if ENABLE_PLAYFABSERVER_API
    using PlayFab.ServerModels;
#else
    using PlayFab.ClientModels;
#endif

namespace Networking.Playfab
{
    public static class PlayFabWrapperAPI
    {
#if ENABLE_PLAYFABSERVER_API
        public static Task<GetUserDataResult> GetUserDataAsync(GetUserDataRequest req)
        {
            var tcs = new TaskCompletionSource<GetUserDataResult>();
            PlayFabServerAPI.GetUserData(
                req,
                result => tcs.TrySetResult(result),
                error => tcs.TrySetException(new Exception(error.GenerateErrorReport()))
            );
            return tcs.Task;
        }

        public static Task<GetUserAccountInfoResult> GetAccountInfoAsync(GetUserAccountInfoRequest req)
        {
            var tcs = new TaskCompletionSource<GetUserAccountInfoResult>();
            PlayFabServerAPI.GetUserAccountInfo(
                req,
                result => tcs.TrySetResult(result),
                error => tcs.TrySetException(new Exception(error.GenerateErrorReport()))
            );
            return tcs.Task;
        }

        public static Task<UpdateUserDataResult> UpdateUserDataAsync(UpdateUserDataRequest req)
        {
            var tcs = new TaskCompletionSource<UpdateUserDataResult>();
            PlayFabServerAPI.UpdateUserData(
                req,
                result => tcs.TrySetResult(result),
                error => tcs.TrySetException(new Exception(error.GenerateErrorReport()))
            );
            return tcs.Task;
        }
#else
        public static Task<GetUserDataResult> GetUserDataAsync(GetUserDataRequest req)
        {
            var tcs = new TaskCompletionSource<GetUserDataResult>();
            PlayFabClientAPI.GetUserData(
                req,
                result => tcs.TrySetResult(result),
                error => tcs.TrySetException(new Exception(error.GenerateErrorReport()))
            );
            return tcs.Task;
        }

        public static Task<GetAccountInfoResult> GetAccountInfoAsync(GetAccountInfoRequest req)
        {
            var tcs = new TaskCompletionSource<GetAccountInfoResult>();
            PlayFabClientAPI.GetAccountInfo(
                req,
                result => tcs.TrySetResult(result),
                error => tcs.TrySetException(new Exception(error.GenerateErrorReport()))
            );
            return tcs.Task;
        }

        public static Task<ExecuteCloudScriptResult> ExecuteCloudScriptAsync(ExecuteCloudScriptRequest req)
        {
            var tcs = new TaskCompletionSource<ExecuteCloudScriptResult>();
            PlayFabClientAPI.ExecuteCloudScript(
                req,
                result =>
                {
                    tcs.TrySetResult(result);
                },
                error => tcs.TrySetException(new Exception(error.GenerateErrorReport()))
            );
            return tcs.Task;
        }
#endif
    }
}