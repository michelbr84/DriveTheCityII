using System;
using System.Collections.Generic;
using DriveTheCityII.Core.Control;
using Xunit;

namespace DriveTheCityII.Core.Tests
{
    public class ControlStateMachineTests
    {
        [Fact]
        public void FullLoopFollowsLegalTransitions()
        {
            var sm = new ControlStateMachine();
            var ids = new List<string>();
            sm.StateChanged += r => ids.Add(r.TransitionId);
            Assert.Equal(TransitionResultCode.Ok, sm.Request(PlayerState.OnFoot, "enter"));
            Assert.Equal(TransitionResultCode.Ok, sm.Request(PlayerState.EnteringVehicle, "interact"));
            Assert.Equal(TransitionResultCode.Ok, sm.Request(PlayerState.Driving, "seated"));
            Assert.Equal(TransitionResultCode.Ok, sm.Request(PlayerState.ExitingVehicle, "exit"));
            Assert.Equal(TransitionResultCode.Ok, sm.Request(PlayerState.OnFoot, "placed"));
            Assert.Equal(TransitionResultCode.Ok, sm.Request(PlayerState.Building, "return"));
            Assert.Equal(new[] { "TR-01", "TR-03", "TR-04", "TR-06", "TR-07", "TR-02" }, ids);
        }

        [Theory]
        [InlineData(PlayerState.Building, PlayerState.Driving)]
        [InlineData(PlayerState.Building, PlayerState.Recovery)]
        [InlineData(PlayerState.OnFoot, PlayerState.Driving)]
        [InlineData(PlayerState.OnFoot, PlayerState.ExitingVehicle)]
        public void IllegalTransitionsAreRejected(PlayerState from, PlayerState to)
        {
            Assert.False(ControlStateMachine.IsLegal(from, to, out _));
        }

        [Fact]
        public void OverlayBlocksStartingTransitionButNotTeardown()
        {
            var sm = new ControlStateMachine();
            sm.Request(PlayerState.OnFoot, "enter");
            sm.SetOverlay(PlayerOverlay.Menu, true);
            Assert.Equal(TransitionResultCode.BlockedByOverlay, sm.Request(PlayerState.EnteringVehicle, "x"));
            sm.SetOverlay(PlayerOverlay.Loading, true);
            Assert.Equal(TransitionResultCode.BlockedByOverlay, sm.Request(PlayerState.Recovery, "x"));
            sm.ForceBuilding("loading");
            Assert.Equal(PlayerState.Building, sm.State);
        }

        [Fact]
        public void TransitionalStateTimesOut()
        {
            var sm = new ControlStateMachine();
            sm.Request(PlayerState.OnFoot, "a");
            sm.Request(PlayerState.EnteringVehicle, "b");
            Assert.False(sm.Tick(4.9f));
            Assert.True(sm.Tick(0.2f));
        }

        [Fact]
        public void PausedOverlayFreezesStateTimer()
        {
            var sm = new ControlStateMachine();
            sm.Request(PlayerState.OnFoot, "a");
            sm.Request(PlayerState.EnteringVehicle, "b");
            sm.SetOverlay(PlayerOverlay.Paused, true);
            Assert.False(sm.Tick(30f));
            sm.SetOverlay(PlayerOverlay.Paused, false);
            Assert.False(sm.Tick(1f));
        }

        [Fact]
        public void RecoveryLoopIsDetected()
        {
            var sm = new ControlStateMachine();
            sm.Request(PlayerState.OnFoot, "a");
            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(TransitionResultCode.Ok, sm.Request(PlayerState.Recovery, "stuck"));
                sm.Tick(1f);
                sm.Request(PlayerState.OnFoot, "placed");
            }
            Assert.True(sm.RecoveryLoopDetected);
            sm.Tick(61f);
            Assert.False(sm.RecoveryLoopDetected);
        }

        [Fact]
        public void CleanupReleasesInReverseOrderAndSurvivesFailures()
        {
            var order = new List<string>();
            var errors = new List<string>();
            var c = new CleanupStack();
            c.Push("speed", () => order.Add("speed"));
            c.Push("camera", () => throw new InvalidOperationException("boom"));
            c.Push("avatar", () => order.Add("avatar"));
            int released = c.ReleaseAll((n, e) => errors.Add(n));
            Assert.Equal(new[] { "avatar", "speed" }, order);
            Assert.Equal(new[] { "camera" }, errors);
            Assert.Equal(2, released);
            Assert.Equal(0, c.Count);
        }
    }
}
