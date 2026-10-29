using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace CPS.KitchenCatch
{
    public sealed class VersusResultDelivery : MonoBehaviour
    {
        private const string RecordsKey = "Micro.Versus.Results.v1";
        private static VersusResultDelivery instance;
        private readonly HashSet<string> submitted = new HashSet<string>();

        public static void SaveAndSubmit(VersusResultUploadPayload payload, string endpoint, Action<string> status)
        {
            if (instance == null)
            {
                instance = new GameObject("Versus result delivery").AddComponent<VersusResultDelivery>();
                DontDestroyOnLoad(instance.gameObject);
            }
            if (!instance.submitted.Add(payload.MatchId)) return;
            bool saved = false;
            try
            {
                var records = JArray.Parse(PlayerPrefs.GetString(RecordsKey, "[]"));
                bool exists = false;
                foreach (var record in records)
                    if ((string)record["matchId"] == payload.MatchId) { exists = true; break; }
                if (!exists)
                {
                    records.Add(payload.Data.DeepClone());
                    PlayerPrefs.SetString(RecordsKey, records.ToString(Newtonsoft.Json.Formatting.None));
                    PlayerPrefs.Save();
                }
                saved = true;
            }
            catch (Exception error)
            {
                Debug.LogWarning("Versus local result save failed: " + error.Message);
            }
            status?.Invoke(saved ? "Saved on this device · Sending to Google Sheets..." : "Local save failed · Sending to Google Sheets...");
            instance.StartCoroutine(instance.Post(payload.ToJson(), endpoint, saved, status));
        }

        private IEnumerator Post(string json, string endpoint, bool saved, Action<string> status)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                status?.Invoke(saved ? "Saved on this device · Google Sheets is not configured" : "Result was not saved");
                yield break;
            }
            for (int attempt = 0; attempt < 2; attempt++)
            {
                using var request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST)
                {
                    uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                    downloadHandler = new DownloadHandlerBuffer(), timeout = 15
                };
                request.SetRequestHeader("Content-Type", "text/plain;charset=utf-8");
                yield return request.SendWebRequest();
                bool accepted = false;
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JObject.Parse(request.downloadHandler.text);
                        accepted = (bool?)response["ok"] == true &&
                            ((string)response["status"] == "created" || (string)response["status"] == "duplicate");
                    }
                    catch (Newtonsoft.Json.JsonException) { }
                }
                if (accepted)
                {
                    status?.Invoke(saved ? "Saved on this device and Google Sheets" : "Saved to Google Sheets · Local save failed");
                    yield break;
                }
                if (attempt == 0) yield return new WaitForSecondsRealtime(2);
            }
            status?.Invoke(saved ? "Saved on this device · Google Sheets unavailable" : "Result could not be saved");
            Debug.LogWarning("Versus Google Sheets upload failed after two attempts; the match ID is unchanged between attempts.");
        }
    }
}
