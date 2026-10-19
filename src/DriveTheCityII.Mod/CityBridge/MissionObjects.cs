using System;
using System.Collections.Generic;
using DriveTheCityII.Diagnostics;
using Game.Common;
using Game.Objects;
using Unity.Entities;
using Unity.Mathematics;

namespace DriveTheCityII.CityBridge
{
    /// <summary>
    /// Presentation of mod-owned career passengers and similar mission objects (SPEC-ENTITY MissionObject). They are
    /// mod humans (not native citizens): they wait at a pickup point, vanish while aboard, and walk a few metres away at
    /// the destination before being removed. Records live in Career; these entities are disposable presentations.
    /// </summary>
    public sealed class MissionObjects
    {
        private sealed class Obj
        {
            public string Key;
            public Entity Entity = Entity.Null;
            public bool SpawnPending;
            public float3 Position;
            public float Yaw;
            public float3 WalkTarget;
            public bool Walking;
            public float RemoveAt = -1f;
            public int Seed;
        }

        private readonly Dictionary<string, Obj> m_Objects = new Dictionary<string, Obj>();
        private readonly PrefabCatalog m_Catalog;

        public MissionObjects(PrefabCatalog catalog)
        {
            m_Catalog = catalog;
        }

        public int Count => m_Objects.Count;

        /// <summary>Shows a waiting person at a point (idempotent per key).</summary>
        public void ShowWaiting(string key, float3 position, float yaw, int seed)
        {
            if (m_Objects.TryGetValue(key, out Obj o))
            {
                o.Position = position;
                o.Walking = false;
                o.RemoveAt = -1f;
                return;
            }
            o = new Obj { Key = key, Position = position, Yaw = yaw, Seed = seed };
            m_Objects[key] = o;
            Spawn(o);
        }

        /// <summary>The person boards: removed from the world (they are inside the vehicle).</summary>
        public void Board(string key) => Remove(key);

        /// <summary>The person gets out at <paramref name="from"/> and walks to <paramref name="to"/>, then disappears.</summary>
        public void Alight(string key, float3 from, float3 to, int seed, float now)
        {
            Remove(key);
            var o = new Obj { Key = key, Position = from, WalkTarget = to, Walking = true, RemoveAt = now + 8f, Seed = seed };
            o.Yaw = math.atan2(to.x - from.x, to.z - from.z);
            m_Objects[key] = o;
            Spawn(o);
        }

        public void Remove(string key)
        {
            if (!m_Objects.TryGetValue(key, out Obj o))
            {
                return;
            }
            m_Objects.Remove(key);
            Despawn(o);
        }

        public void RemoveAll()
        {
            foreach (Obj o in m_Objects.Values)
            {
                Despawn(o);
            }
            m_Objects.Clear();
        }

        /// <summary>Per-frame update: walking animation, timed removal, respawn after saves or invalidation.</summary>
        public void Update(EntityManager em, float dt, float now, bool frozen)
        {
            var expired = new List<string>();
            foreach (Obj o in m_Objects.Values)
            {
                if (o.RemoveAt > 0f && now >= o.RemoveAt)
                {
                    expired.Add(o.Key);
                    continue;
                }
                if (o.Entity == Entity.Null)
                {
                    if (!o.SpawnPending)
                    {
                        Spawn(o);
                    }
                    continue;
                }
                if (!em.Exists(o.Entity) || em.HasComponent<Deleted>(o.Entity))
                {
                    o.Entity = Entity.Null;
                    continue;
                }
                float speed = 0f;
                if (o.Walking && !frozen)
                {
                    float3 d = o.WalkTarget - o.Position;
                    d.y = 0f;
                    float len = math.length(d);
                    if (len > 0.3f)
                    {
                        float step = math.min(len, 1.3f * dt);
                        o.Position += d / len * step;
                        o.Yaw = math.atan2(d.x, d.z);
                        speed = 1.3f;
                    }
                }
                quaternion rot = quaternion.RotateY(o.Yaw);
                float3 vel = new float3(math.sin(o.Yaw), 0f, math.cos(o.Yaw)) * speed;
                ActorWriter.WritePose(em, o.Entity, o.Position, rot, vel, 0, speed > 0.1f ? TransformState.Move : TransformState.Idle);
                ActorWriter.PinHumanNavigation(em, o.Entity, o.Position, new float2(vel.x, vel.z), false);
            }
            foreach (string k in expired)
            {
                Remove(k);
            }
        }

        private void Spawn(Obj o)
        {
            Entity prefab = m_Catalog.PickHuman(o.Seed);
            if (prefab == Entity.Null)
            {
                return;
            }
            o.SpawnPending = true;
            float3 pos = o.Position;
            quaternion rot = quaternion.RotateY(o.Yaw);
            ActorOpsSystem.Enqueue(em =>
            {
                if (!m_Objects.TryGetValue(o.Key, out Obj current) || current != o)
                {
                    o.SpawnPending = false;
                    return; // removed before the spawn ran
                }
                o.Entity = ActorWriter.SpawnHuman(em, prefab, pos, rot, 0, (ushort)(o.Seed & 0xffff));
                em.SetComponentData(o.Entity, new DtcActor { Kind = ActorKind.MissionObject, RecordId = o.Seed });
                o.SpawnPending = false;
            });
        }

        private static void Despawn(Obj o)
        {
            Entity e = o.Entity;
            o.Entity = Entity.Null;
            if (e == Entity.Null)
            {
                return;
            }
            ActorOpsSystem.Enqueue(em =>
            {
                if (em.Exists(e) && !em.HasComponent<Deleted>(e))
                {
                    em.AddComponent<Deleted>(e);
                }
            });
        }
    }
}
