using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace CPS.KitchenCatch.Tests
{
    public sealed class SerialConnectionTests
    {
        private GameObject root;
        private SerialConnection connection;
        private static string Frame(uint sequence = 12, uint tare = 1, uint boot = 17) =>
            File.ReadAllText(Path.Combine(Application.dataPath, "CPS/Tests/Fixtures/Usb/valid-v1.json")).Trim()
                .Replace("\"sequence\":12", "\"sequence\":" + sequence)
                .Replace("\"tareId\":1", "\"tareId\":" + tare)
                .Replace("\"bootId\":17", "\"bootId\":" + boot);

        [SetUp] public void SetUp() { root = new GameObject("USB test"); connection = root.AddComponent<SerialConnection>(); }
        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(root); }

        [Test] public void OpenDoesNotMeanFreshTelemetryAndOldCallbacksAreIgnored()
        {
            int generation = connection.BeginSession(); connection.TransportOpened(generation);
            Assert.That(connection.IsConnected, Is.True);
            Assert.That(connection.IsFresh(1), Is.False);
            connection.Receive(Frame() + "\n", generation, 1);
            Assert.That(connection.IsFresh(1.7), Is.True);
            Assert.That(connection.IsFresh(1.75), Is.False);
            connection.BeginSession();
            connection.Receive(Frame(13) + "\n", generation, 2);
            connection.TransportOpened(generation);
            Assert.That(connection.IsConnected, Is.False);
            Assert.That(connection.Packets.Latest, Is.Null);
        }
        [Test] public void TareNeedsANewerAcknowledgementNotOldValidForce()
        {
            int generation = connection.BeginSession(); connection.TransportOpened(generation);
            connection.Receive(Frame() + "\n", generation, 1);
            Assert.That(connection.LoadTareReady, Is.False);
            string sent = null; connection.SendCommand += command => sent = command;
            Assert.That(connection.RequestTare(), Is.True);
            Assert.That(sent, Is.EqualTo("t\n"));
            connection.Receive(Frame(13) + "\n", generation, 1.1);
            Assert.That(connection.LoadTareReady, Is.False);
            connection.Receive(Frame(14, 2) + "\n", generation, 1.2);
            Assert.That(connection.LoadTareReady, Is.True);
            connection.Receive(Frame(0, 0, 18) + "\n", generation, 1.3);
            Assert.That(connection.LoadTareReady, Is.False);
        }
        [Test] public void DisconnectClearsHeldStateAndPartialLineCannotCrossSessions()
        {
            int first = connection.BeginSession(); connection.TransportOpened(first);
            connection.Receive(Frame().Substring(0, 50), first, 1);
            connection.Disconnect();
            int next = connection.BeginSession(); connection.TransportOpened(next);
            connection.Receive(Frame() + "\n", next, 2);
            Assert.That(connection.IsFresh(2), Is.True);
            connection.Disconnect();
            Assert.That(connection.Packets.Latest, Is.Null);
            Assert.That(connection.LoadTareReady, Is.False);
        }
        [Test] public void InstalledMonoSerialTypeCanBeUsedWithoutAProfileOrPackageChange()
        {
            Type type = Type.GetType("System.IO.Ports.SerialPort, System");
            Assert.That(type, Is.Not.Null, "Installed Unity Mono SerialPort is unavailable.");
            Assert.That(type.GetMethod("Read", new[] { typeof(byte[]), typeof(int), typeof(int) }), Is.Not.Null);
            Assert.That(type.GetProperty("ReadTimeout"), Is.Not.Null);
        }
    }
}
