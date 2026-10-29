using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace CPS.KitchenCatch.Tests
{
    public sealed class ResultUploaderTests
    {
        private GameObject controllerObject;
        private GameObject uploaderObject;
        private GameSessionController controller;
        private GoogleSheetsResultUploader uploader;

        [SetUp]
        public void Setup()
        {
            controllerObject = new GameObject("Controller");
            controller = controllerObject.AddComponent<GameSessionController>();
            controller.enabled = false;
            uploaderObject = new GameObject("Uploader");
            uploader = uploaderObject.AddComponent<GoogleSheetsResultUploader>();
        }

        [TearDown]
        public void Cleanup()
        {
            if (uploaderObject != null) Object.DestroyImmediate(uploaderObject);
            if (controllerObject != null) Object.DestroyImmediate(controllerObject);
        }

        [Test]
        public void Finished_StartsOneSubmissionAndDeduplicatesSession()
        {
            int submissions = 0;
            string json = null;
            uploader.Initialize(controller, "https://example.test/exec", body => { submissions++; json = body; });
            var result = Result("session-1");

            RaiseFinished(result);
            RaiseFinished(result);

            Assert.That(submissions, Is.EqualTo(1));
            StringAssert.Contains("\"sessionId\":\"session-1\"", json);
        }

        [Test]
        public void EmptyEndpointSkipsWithoutThrowing()
        {
            int submissions = 0;
            uploader.Initialize(controller, "", body => submissions++);
            LogAssert.Expect(LogType.Warning, "Google Sheets result upload skipped: endpoint is empty.");

            Assert.DoesNotThrow(() => RaiseFinished(Result("session-empty")));
            Assert.That(submissions, Is.Zero);
        }

        [Test]
        public void ReturnToMenuWithoutFinishedStartsNoSubmission()
        {
            int submissions = 0;
            uploader.Initialize(controller, "https://example.test/exec", body => submissions++);

            controller.ReturnToMenu();

            Assert.That(submissions, Is.Zero);
        }

        [Test]
        public void DisableAndDestroyUnsubscribeCleanly()
        {
            int submissions = 0;
            uploader.Initialize(controller, "https://example.test/exec", body => submissions++);
            uploader.enabled = false;
            RaiseFinished(Result("disabled"));
            Object.DestroyImmediate(uploaderObject);
            uploaderObject = null;
            RaiseFinished(Result("destroyed"));

            Assert.That(submissions, Is.Zero);
        }

        private void RaiseFinished(SessionResult result)
        {
            var field = typeof(GameSessionController).GetField("Finished", BindingFlags.Instance | BindingFlags.NonPublic);
            var listeners = (Action<SessionResult>)field.GetValue(controller);
            listeners?.Invoke(result);
        }

        private static SessionResult Result(string sessionId)
        {
            return new SessionResult
            {
                SchemaVersion = 1,
                SessionId = sessionId,
                PlayerId = "test-player",
                DisplayName = "Test Player",
                EndedAtUtc = "2026-10-20T02:00:00Z"
            };
        }
    }
}
