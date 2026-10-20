# Career events of the completed bus shift (session 20261019-212310, build 0.1.0-94004de, TEST AUTOPILOT, simulated input)

Harness steps that were not autopilot driving (both labelled in RUN-0003 §4):

- 22:32: the bus, stuck facing away from its route on a two-lane street, was driven about 9 m forward to the next
  junction with simulated input (`input throttle=0.6 t=7`) before the autopilot resumed.
- 23:01: to test whether the player's vehicle holds standing queues, the player returned to city building for 90 s,
  then re-entered character mode, called the operator's bus with the garage action "bring here" (`garage:call:SV2`)
  and boarded it; the shift continued unchanged.

```text
[2026-10-19 21:35:51,209] [INFO]  Career event: 706.5s MissionStateChanged DTC.Mission.Accepted Job accepted.
[2026-10-19 21:35:51,219] [INFO]  Career event: 706.5s ServiceVehicleProvided DTC.Garage.ServiceVehicle A service vehicle is waiting for you.
[2026-10-19 21:36:14,182] [INFO]  Career event: 729.6s MissionStateChanged DTC.Mission.Active Job started.
[2026-10-19 21:51:47,476] [INFO]  Career event: 1659.4s LegStateChanged DTC.Mission.Leg.Servicing Servicing NA Bus Stop Sign.
[2026-10-19 21:52:01,897] [INFO]  Career event: 1673.9s LegStateChanged DTC.Mission.Leg.Done Stop done: NA Bus Stop Sign.
[2026-10-19 21:54:34,184] [INFO]  Career event: 1825.9s LegStateChanged DTC.Mission.Leg.Servicing Servicing NA Bus Stop Sign.
[2026-10-19 21:54:45,008] [INFO]  Career event: 1836.7s LegStateChanged DTC.Mission.Leg.Done Stop done: NA Bus Stop Sign.
[2026-10-19 21:56:39,881] [INFO]  Career event: 1951.0s LegStateChanged DTC.Mission.Leg.Servicing Servicing NA Bus Stop Sign.
[2026-10-19 21:56:49,731] [INFO]  Career event: 1960.6s LegStateChanged DTC.Mission.Leg.Done Stop done: NA Bus Stop Sign.
[2026-10-19 22:07:15,311] [INFO]  Career event: 2584.1s LegStateChanged DTC.Mission.Leg.Servicing Servicing NA Bus Stop Sign.
[2026-10-19 22:07:21,316] [INFO]  Career event: 2590.1s LegStateChanged DTC.Mission.Leg.Done Stop done: NA Bus Stop Sign.
[2026-10-19 22:08:15,576] [INFO]  Career event: 2644.1s LegStateChanged DTC.Mission.Leg.Servicing Servicing NA Bus Stop Sign.
[2026-10-19 22:08:22,801] [INFO]  Career event: 2651.3s LegStateChanged DTC.Mission.Leg.Done Stop done: NA Bus Stop Sign.
[2026-10-19 22:10:46,760] [INFO]  Career event: 2788.3s LegStateChanged DTC.Mission.Leg.Servicing Servicing NA Bus Stop Sign.
[2026-10-19 22:10:58,790] [INFO]  Career event: 2800.3s LegStateChanged DTC.Mission.Leg.Done Stop done: NA Bus Stop Sign.
[2026-10-19 22:12:27,401] [INFO]  Career event: 2888.8s LegStateChanged DTC.Mission.Leg.Servicing Servicing NA Bus Stop Sign.
[2026-10-19 22:12:33,411] [INFO]  Career event: 2894.8s LegStateChanged DTC.Mission.Leg.Done Stop done: NA Bus Stop Sign.
[2026-10-19 22:13:55,747] [INFO]  Career event: 2976.9s LegStateChanged DTC.Mission.Leg.Servicing Servicing NA Bus Stop Sign.
[2026-10-19 22:14:10,166] [INFO]  Career event: 2991.3s LegStateChanged DTC.Mission.Leg.Done Stop done: NA Bus Stop Sign.
[2026-10-19 22:14:55,372] [INFO]  Career event: 3036.3s LegStateChanged DTC.Mission.Leg.Servicing Servicing NA Bus Stop Sign.
[2026-10-19 22:15:03,781] [INFO]  Career event: 3044.7s LegStateChanged DTC.Mission.Leg.Done Stop done: NA Bus Stop Sign.
[2026-10-19 22:16:29,480] [INFO]  Career event: 3130.4s LegStateChanged DTC.Mission.Leg.Servicing Servicing EU Bus Stop Sign.
[2026-10-19 22:16:48,719] [INFO]  Career event: 3149.6s LegStateChanged DTC.Mission.Leg.Done Stop done: EU Bus Stop Sign.
[2026-10-19 22:31:37,947] [INFO]  [bridge] < ERR [3318.06s] career drive 7200 :: aborted by request
[2026-10-19 22:50:11,746] [INFO]  [bridge] < ERR [1055.66s] career drive 4000 :: aborted by request
[2026-10-19 23:00:49,569] [INFO]  [bridge] < ERR [614.76s] career drive 4000 :: aborted by request
[2026-10-19 23:02:52,992] [INFO]  Career event: 5780.2s Notification DTC.Mission.WaitingForYou Your passengers or cargo are waiting in the vehicle. Return soon.
[2026-10-19 23:09:12,026] [INFO]  Career event: 6158.0s LegStateChanged DTC.Mission.Leg.Servicing Servicing NA Bus Stop Sign.
[2026-10-19 23:09:31,281] [INFO]  Career event: 6177.2s LegStateChanged DTC.Mission.Leg.Done Stop done: NA Bus Stop Sign.
[2026-10-19 23:20:22,848] [INFO]  Career event: 6825.1s LegStateChanged DTC.Mission.Leg.Servicing Servicing NA Bus Stop Sign.
[2026-10-19 23:20:47,372] [INFO]  Career event: 6849.1s LegStateChanged DTC.Mission.Leg.Done Stop done: NA Bus Stop Sign.
[2026-10-19 23:34:35,716] [INFO]  Career event: 7675.0s LegStateChanged DTC.Mission.Leg.Servicing Servicing NA Bus Stop Sign.
[2026-10-19 23:35:15,735] [INFO]  Career event: 7715.0s LegStateChanged DTC.Mission.Leg.Done Stop done: NA Bus Stop Sign.
[2026-10-19 23:37:19,774] [INFO]  Career event: 7837.9s LegStateChanged DTC.Mission.Leg.Servicing Servicing NA Bus Stop Sign.
[2026-10-19 23:37:57,253] [INFO]  Career event: 7875.2s LegStateChanged DTC.Mission.Leg.Done Stop done: NA Bus Stop Sign.
[2026-10-19 23:37:57,253] [INFO]  Career event: 7875.2s MissionStateChanged DTC.Mission.Completed Job completed.
[2026-10-19 23:37:57,254] [INFO]  Career event: 7875.2s Notification DTC.Mission.Completed Job completed.
[2026-10-19 23:37:57,254] [INFO]  Career event: 7875.2s VehicleReleased DTC.Garage.ServiceVehicleReturned The operator collects the service vehicle when you leave it.
[2026-10-19 23:37:57,256] [INFO]  Career event: 7875.2s Settled DTC.Mission.Settled Settled: 222.50 cr, +47 XP, reputation +0.
[2026-10-19 23:37:57,269] [INFO]  [bridge] < OK [2066.50s] career drive 4000 :: mission finished after 2067s; legs: Stop@NA Bus Stop Sign(0s) > recovery:stuck(110s) > recovery:turn-round did not succeed(173s) > recovery:turn-round did not succeed(216s) > recov
```
