using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace CPS.KitchenCatch
{
    public sealed class GoogleSheetsResultUploader : MonoBehaviour
    {
        [SerializeField] private GameSessionController controller;
        [SerializeField] private string endpoint;
        private readonly HashSet<string> startedSessions = new HashSet<string>();
        private Action<string> submitOverride;
        private GameSessionController subscribedController;

        public void Initialize(GameSessionController sessionController, string deploymentEndpoint)
        {
            Initialize(sessionController, deploymentEndpoint, null);
        }

        public void Initialize(GameSessionController sessionController, string deploymentEndpoint, Action<string> submission)
        {
            Unsubscribe();
            controller = sessionController;
            endpoint = deploymentEndpoint;
            submitOverride = submission;
            if (isActiveAndEnabled) Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (controller == null || subscribedController == controller) return;
            controller.Finished += OnFinished;
            subscribedController = controller;
        }

        private void Unsubscribe()
        {
            if (subscribedController == null) return;
            subscribedController.Finished -= OnFinished;
            subscribedController = null;
        }

        private void OnFinished(SessionResult result)
        {
            TryStartUpload(result);
        }

        private bool TryStartUpload(SessionResult result)
        {
            if (result == null || string.IsNullOrWhiteSpace(result.SessionId))
            {
                Debug.LogWarning("Google Sheets result upload skipped: result has no session ID.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                Debug.LogWarning("Google Sheets result upload skipped: endpoint is empty.");
                return false;
            }
            if (!startedSessions.Add(result.SessionId)) return false;

            string json = ResultUploadPayload.From(result).ToJson();
            if (submitOverride != null) submitOverride(json);
            else StartCoroutine(Post(json));
            return true;
        }

        private IEnumerator Post(string json)
        {
            using var request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 10
            };
            request.SetRequestHeader("Content-Type", "text/plain;charset=utf-8");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Google Sheets result upload failed: " + request.error);
                yield break;
            }

            string response = request.downloadHandler.text;
            if (!response.Contains("\"ok\":true") ||
                (!response.Contains("\"status\":\"created\"") && !response.Contains("\"status\":\"duplicate\"")))
                Debug.LogWarning("Google Sheets result upload returned an unexpected response.");
        }
    }
}
