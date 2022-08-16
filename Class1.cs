using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThunderRoad;
using UnityEngine;

namespace DevilSwordDante
{
    public class SummonSpell : SpellCastCharge
    {
        bool exists;
        public override void Fire(bool active)
        {
            base.Fire(active);
            if (active)
            {
                if (spellCaster.ragdollHand.grabbedHandle?.item is Item grabbedItem && spellCaster.ragdollHand.grabbedHandle?.item.GetComponent<DanteComponent>() is DanteComponent component)
                {
                    if (component.lastHolder != null && component.lastHolder.HasSlotFree())
                    {
                        foreach (Damager damager in grabbedItem.GetComponentsInChildren<Damager>())
                        {
                            damager.UnPenetrateAll();
                        }
                        Common.MoveAlign(grabbedItem.transform, grabbedItem.holderPoint, component.lastHolder.slots[0]);
                        component.lastHolder.Snap(grabbedItem);
                        EffectInstance instance = Catalog.GetData<EffectData>("DSDActivate").Spawn(grabbedItem.transform, false);
                        instance.SetIntensity(1);
                        instance.Play();
                    }
                    else if ((component.lastHolder == null || !component.lastHolder.HasSlotFree()) && Player.local.creature.equipment.GetFirstFreeHolder() != null)
                    {
                        foreach (Damager damager in grabbedItem.GetComponentsInChildren<Damager>())
                        {
                            damager.UnPenetrateAll();
                        }
                        Holder holder = Player.local.creature.equipment.GetFirstFreeHolder();
                        Common.MoveAlign(grabbedItem.transform, grabbedItem.holderPoint, holder.slots[0]);
                        holder.Snap(grabbedItem);
                        EffectInstance instance = Catalog.GetData<EffectData>("DSDActivate").Spawn(grabbedItem.transform, false);
                        instance.SetIntensity(1);
                        instance.Play();
                    }
                    else if ((component.lastHolder == null || !component.lastHolder.HasSlotFree()) && Player.local.creature.equipment.GetFirstFreeHolder() == null)
                    {
                        foreach (Damager damager in grabbedItem.GetComponentsInChildren<Damager>())
                        {
                            damager.UnPenetrateAll();
                        }
                        BackpackHolder.instance.StoreItem(grabbedItem);
                    }
                }
                else if (spellCaster.ragdollHand.grabbedHandle == null)
                {
                    foreach (Item item in Item.allActive)
                    {
                        if (item.GetComponent<DanteComponent>() != null)
                        {
                            if(item.mainHandler != null) item.mainHandler.UnGrab(false);
                            if (item.holder != null) item.holder.UnSnap(item, true);
                            foreach(Damager damager in item.GetComponentsInChildren<Damager>())
                            {
                                damager.UnPenetrateAll();
                            }
                            Common.MoveAlign(item.gameObject.transform, item.GetMainHandle(spellCaster.ragdollHand.side).GetDefaultOrientation(spellCaster.ragdollHand.side).gameObject.transform, spellCaster.ragdollHand.gameObject.transform);
                            spellCaster.ragdollHand.Grab(item.GetMainHandle(spellCaster.ragdollHand.side), true);
                            EffectInstance instance = Catalog.GetData<EffectData>("DSDActivate").Spawn(item.transform, false);
                            instance.SetIntensity(1);
                            instance.Play();
                            exists = true;
                            return;
                        }
                    }
                    if (!exists)
                    {
                        Catalog.GetData<ItemData>("DevilSwordDante").SpawnAsync(item =>
                        {
                            spellCaster.ragdollHand.Grab(item.GetMainHandle(spellCaster.ragdollHand.side), true);
                            EffectInstance instance = Catalog.GetData<EffectData>("DSDActivate").Spawn(item.transform, false);
                            instance.SetIntensity(1);
                            instance.Play();
                        }, spellCaster.ragdollHand.grip.position, spellCaster.ragdollHand.grip.rotation, null, false);
                    }
                }
                currentCharge = 0;
                spellCaster.isFiring = false;
                Fire(false);
            }
        }
    }
    public class BeamModule : ItemModule
    {
        public Color BeamColor;
        public Color BeamEmission;
        public Vector3 BeamSize;
        public float BeamSpeed;
        public float DespawnTime;
        public float BeamDamage;
        public bool BeamDismember;
        public Vector3 BeamScaleIncrease;
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<BeamCustomization>().Setup(BeamDismember, BeamSpeed, DespawnTime, BeamDamage, BeamColor, BeamEmission, BeamSize, BeamScaleIncrease);
        }
    }
    public class BeamCustomization : MonoBehaviour
    {
        Item item;
        public Color beamColor;
        public Color beamEmission;
        public Vector3 beamSize;
        float despawnTime;
        float beamSpeed;
        float beamDamage;
        bool dismember;
        Vector3 beamScaleUpdate;
        List<RagdollPart> parts = new List<RagdollPart>();
        public void Start()
        {
            item = GetComponent<Item>();
            item.renderers[0].material.SetColor("_BaseColor", beamColor);
            item.renderers[0].material.SetColor("_EmissionColor", beamEmission * 2f);
            item.renderers[0].gameObject.transform.localScale = beamSize;
            item.rb.useGravity = false;
            item.rb.drag = 0;
            item.rb.AddForce(Player.local.head.transform.forward * beamSpeed, ForceMode.Impulse);
            item.IgnoreRagdollCollision(Player.currentCreature.ragdoll);
            item.RefreshCollision(true);
            item.Throw();
            item.Despawn(despawnTime);
        }
        public void Setup(bool beamDismember, float BeamSpeed, float BeamDespawn, float BeamDamage, Color color, Color emission, Vector3 size, Vector3 scaleUpdate)
        {
            dismember = beamDismember;
            beamSpeed = BeamSpeed;
            despawnTime = BeamDespawn;
            beamDamage = BeamDamage;
            beamColor = color;
            beamEmission = emission;
            beamSize = size;
            beamScaleUpdate = scaleUpdate;
        }
        public void FixedUpdate()
        {
            item.gameObject.transform.localScale += beamScaleUpdate;
        }
        public void Update()
        {
            if (parts.Count > 0)
            {
                parts[0].gameObject.SetActive(true);
                parts[0].bone.animationJoint.gameObject.SetActive(true);
                parts[0].ragdoll.TrySlice(parts[0]);
                if (parts[0].data.sliceForceKill)
                    parts[0].ragdoll.creature.Kill();
                parts.RemoveAt(0);
            }
        }
        public void OnTriggerEnter(Collider c)
        {
            if (c.GetComponentInParent<ColliderGroup>() != null)
            {
                ColliderGroup enemy = c.GetComponentInParent<ColliderGroup>();
                if (enemy?.collisionHandler?.ragdollPart != null && enemy?.collisionHandler?.ragdollPart?.ragdoll?.creature != Player.currentCreature)
                {
                    RagdollPart part = enemy.collisionHandler.ragdollPart;
                    if (part.ragdoll.creature != Player.currentCreature && part?.ragdoll?.creature?.gameObject?.activeSelf == true && part != null && !part.isSliced)
                    {
                        if (part.sliceAllowed && dismember)
                        {
                            if (!parts.Contains(part))
                                parts.Add(part);
                        }
                        else if (!part.ragdoll.creature.isKilled)
                        {
                            CollisionInstance instance = new CollisionInstance(new DamageStruct(DamageType.Slash, beamDamage));
                            instance.damageStruct.hitRagdollPart = part;
                            part.ragdoll.creature.Damage(instance);
                            part.ragdoll.creature.TryPush(Creature.PushType.Hit, item.rb.velocity, 1);
                        }
                    }
                }
            }
        }
    }
    public class DanteModule : ItemModule
    {
        public float DashSpeed;
        public string DashDirection;
        public bool DisableGravity;
        public bool DisableCollision;
        public float DashTime;
        public float BeamCooldown;
        public float SwordSpeed;
        public float RotateDegreesPerSecond;
        public float ReturnSpeed;
        public bool StopOnEnd = false;
        public bool StopOnStart = false;
        public bool ThumbstickDash = false;
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<DanteComponent>().Setup(DashSpeed, DashDirection, DisableGravity, DisableCollision, DashTime, SwordSpeed, BeamCooldown, RotateDegreesPerSecond, ReturnSpeed, StopOnEnd, StopOnStart, ThumbstickDash);
        }
    }
    public class DanteComponent : MonoBehaviour
    {
        Item item;
        bool isThrown;
        public Holder lastHolder;
        RagdollHand lastHandler;
        bool startUpdate;
        public float DashSpeed;
        public string DashDirection;
        public bool DisableGravity;
        public bool DisableCollision;
        public float DashTime;
        public float RotationSpeed;
        public float ReturnSpeed;
        float cdH;
        float cooldown;
        float swordSpeed;
        bool beam;
        bool spin;
        Animation animation;
        bool active = false;
        SpellCastCharge spell;
        public bool StopOnEnd;
        public bool StopOnStart;
        bool ThumbstickDash;
        bool fallDamage;
        bool dashing;
        public void Start()
        {
            item = GetComponent<Item>();
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            item.OnUngrabEvent += Item_OnUngrabEvent;
            item.OnGrabEvent += Item_OnGrabEvent;
            item.OnTelekinesisReleaseEvent += Item_OnTelekinesisReleaseEvent;
            item.OnTelekinesisGrabEvent += Item_OnTelekinesisGrabEvent;
            item.OnUnSnapEvent += Item_OnUnSnapEvent;
            item.data.category = "Utilities";
            animation = GetComponent<Animation>();
            item.mainCollisionHandler.OnCollisionStartEvent += MainCollisionHandler_OnCollisionStartEvent;
            spell = Catalog.GetData<SpellCastCharge>("Fire");
        }

        private void MainCollisionHandler_OnCollisionStartEvent(CollisionInstance collisionInstance)
        {
            if (spin && collisionInstance.damageStruct.damager != null && collisionInstance.damageStruct.damageType != DamageType.Blunt)
            {
                collisionInstance.damageStruct.damager.UnPenetrateAll();
            }
        }

        private void Item_OnUnSnapEvent(Holder holder)
        {
            lastHolder = holder;
        }

        private void Item_OnTelekinesisGrabEvent(Handle handle, SpellTelekinesis teleGrabber)
        {
            if (teleGrabber.spellCaster.ragdollHand.playerHand.controlHand.castPressed)
            {
                StartCoroutine(Teleport(teleGrabber.spellCaster.ragdollHand));
            }
        }

        private void Item_OnTelekinesisReleaseEvent(Handle handle, SpellTelekinesis teleGrabber)
        {
            lastHandler = null;
            spin = false;
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            item.rb.useGravity = true;
            isThrown = false;
            startUpdate = false;
            spin = false;
        }

        private void Item_OnUngrabEvent(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            lastHandler = ragdollHand;
            beam = false; 
            if (throwing && PlayerControl.GetHand(ragdollHand.side).castPressed)
            {
                spin = true;
                Transform creature = GetEnemy()?.ragdoll?.targetPart?.transform;
                if (creature != null)
                {
                    Vector3 velocity = item.rb.velocity;
                    item.rb.velocity = (creature.position - item.transform.position).normalized * velocity.magnitude;
                }
            }
            if (active) ToggleActivate();
        }
        public void ToggleActivate()
        {
            active = !active;
            if (active)
            {
                animation.Play("Activate");
                EffectInstance instance = Catalog.GetData<EffectData>("DSDActivate").Spawn(item.transform, false);
                instance.SetIntensity(1);
                instance.Play();
            }
            else
            {
                animation.Play("Deactivate");
                item.colliderGroups[0].imbue.Stop();
            }
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.AlternateUseStart && !ragdollHand.playerHand.controlHand.usePressed)
            {
                StopCoroutine(Dash());
                StartCoroutine(Dash());
            }
            else if (action == Interactable.Action.AlternateUseStart && ragdollHand.playerHand.controlHand.usePressed)
            {
                ToggleActivate();
            }
            if (action == Interactable.Action.UseStart && active)
            {
                Vector3 v;
                v.x = UnityEngine.Random.Range(-0.15f, 0.15f);
                v.y = UnityEngine.Random.Range(-0.15f, 0.15f);
                v.z = UnityEngine.Random.Range(-0.15f, 0.15f);
                Catalog.GetData<ItemData>("DSDDaggers").SpawnAsync(ShootDagger, new Vector3(Player.local.head.transform.position.x + v.x, Player.local.head.transform.position.y + v.y, Player.local.head.transform.position.z + v.z), Player.local.head.transform.rotation);
            }
            if (action == Interactable.Action.UseStart)
            {
                beam = true;
            }
            if (action == Interactable.Action.UseStop)
            {
                beam = false;
            }
        }
        public IEnumerator Dash()
        {
            dashing = true;
            Player.fallDamage = false;
            if (StopOnStart) Player.local.locomotion.rb.velocity = Vector3.zero;
            if (Player.local.locomotion.moveDirection.magnitude <= 0 || !ThumbstickDash)
                if (DashDirection == "Item")
                {
                    Player.local.locomotion.rb.AddForce(item.mainHandler.grip.up * DashSpeed, ForceMode.Impulse);
                }
                else
                {
                    Player.local.locomotion.rb.AddForce(Player.local.head.transform.forward * DashSpeed, ForceMode.Impulse);
                }
            else
            {
                Player.local.locomotion.rb.AddForce(Player.local.locomotion.moveDirection.normalized * DashSpeed, ForceMode.Impulse);
            }
            if (DisableGravity)
                Player.local.locomotion.rb.useGravity = false;
            if (DisableCollision)
            {
                Player.local.locomotion.rb.detectCollisions = false;
                item.rb.detectCollisions = false;
                item.mainHandler.rb.detectCollisions = false;
                item.mainHandler.otherHand.rb.detectCollisions = false;
            }
            yield return new WaitForSeconds(DashTime);
            if (DisableGravity)
                Player.local.locomotion.rb.useGravity = true;
            if (DisableCollision)
            {
                Player.local.locomotion.rb.detectCollisions = true;
                item.rb.detectCollisions = true;
                item.mainHandler.rb.detectCollisions = true;
                item.mainHandler.otherHand.rb.detectCollisions = true;
            }
            if (StopOnEnd) Player.local.locomotion.rb.velocity = Vector3.zero;
            Player.fallDamage = fallDamage;
            dashing = false;
            yield break;
        }
        public void ShootDagger(Item spawnedItem)
        {
            Transform creature = GetEnemy()?.ragdoll?.targetPart?.transform;
            EffectInstance instance = Catalog.GetData<EffectData>("DSDActivate").Spawn(spawnedItem.transform, false);
            instance.SetIntensity(1);
            instance.Play();
            spawnedItem.rb.useGravity = false;
            spawnedItem.rb.drag = 0;
            if (creature != null)
                spawnedItem.rb.AddForce((creature.position - spawnedItem.transform.position).normalized * 45f, ForceMode.Impulse);
            else spawnedItem.rb.AddForce(Player.local.head.transform.forward * 45f, ForceMode.Impulse);
            spawnedItem.RefreshCollision(true);
            spawnedItem.IgnoreRagdollCollision(Player.currentCreature.ragdoll);
            spawnedItem.IgnoreObjectCollision(item);
            spawnedItem.gameObject.AddComponent<DaggerDespawn>();
            if(creature != null)
            spawnedItem.gameObject.GetComponent<DaggerDespawn>().enemy = creature.gameObject.GetComponent<Creature>();
            spawnedItem.Throw();
        }
        public Creature GetEnemy()
        {
            Creature closestCreature = null;
            if (Creature.allActive.Count <= 0) return null;
            foreach (Creature creature in Creature.allActive)
            {
                if (creature != null && !creature.isPlayer && creature.ragdoll.isActiveAndEnabled && !creature.isKilled && Vector3.Dot(Player.local.head.transform.forward.normalized, (creature.transform.position - Player.local.transform.position).normalized) >= 0.75f && closestCreature == null &&
                    Vector3.Distance(Player.local.transform.position, creature.transform.position) <= 25)
                {
                    closestCreature = creature;
                }
                else if (creature != null && !creature.isPlayer && creature.ragdoll.isActiveAndEnabled && !creature.isKilled && Vector3.Dot(Player.local.head.transform.forward.normalized, (creature.transform.position - Player.local.transform.position).normalized) >= 0.75f && closestCreature != null &&
                    Vector3.Distance(Player.local.transform.position, creature.transform.position) <= 25)
                {
                    if (Vector3.Dot(Player.local.head.transform.forward.normalized, (creature.transform.position - Player.local.transform.position).normalized) >
                    Vector3.Dot(Player.local.head.transform.forward.normalized, (closestCreature.transform.position - Player.local.transform.position).normalized))
                        closestCreature = creature;
                }
            }
            return closestCreature;
        }
        public void Setup(float speed, string direction, bool gravity, bool collision, float time, float SwordSpeed, float BeamCooldown, float rotationSpeed, float returnSpeed, bool stop, bool start, bool thumbstick)
        {
            DashSpeed = speed;
            DashDirection = direction;
            DisableGravity = gravity;
            DisableCollision = collision;
            DashTime = time;
            if (direction.ToLower().Contains("player") || direction.ToLower().Contains("head") || direction.ToLower().Contains("sight"))
            {
                DashDirection = "Player";
            }
            else if (direction.ToLower().Contains("item") || direction.ToLower().Contains("sheath") || direction.ToLower().Contains("flyref") || direction.ToLower().Contains("weapon"))
            {
                DashDirection = "Item";
            }
            swordSpeed = SwordSpeed;
            cooldown = BeamCooldown;
            RotationSpeed = rotationSpeed;
            ReturnSpeed = returnSpeed;
            StopOnEnd = stop;
            StopOnStart = start;
            ThumbstickDash = thumbstick;
        }
        public void FixedUpdate()
        {
            if (!dashing) fallDamage = Player.fallDamage;
            if (item.isTelekinesisGrabbed) lastHandler = null;
            if (item.isFlying && lastHandler != null && spin)
            {
                item.flyDirRef.Rotate(new Vector3(0, RotationSpeed, 0) * Time.fixedDeltaTime);
                item.rb.useGravity = false;
                item.rb.AddForce(-(item.transform.position - lastHandler.transform.position).normalized * ReturnSpeed, ForceMode.Force);
                item.IgnoreRagdollCollision(Player.local.creature.ragdoll);
                isThrown = true;
                startUpdate = true;
            }
            else if (isThrown && !item.IsHanded() && item.holder == null && !item.isTelekinesisGrabbed && lastHandler != null && spin)
            {
                item.Throw(1, Item.FlyDetection.Forced);
                item.IgnoreRagdollCollision(Player.local.creature.ragdoll);
            }
            else
            {
                item.flyDirRef.localRotation = Quaternion.identity;
                item.rb.useGravity = true;
                isThrown = false;
                startUpdate = false;
                spin = false;
            }
            if (lastHandler != null && Vector3.Dot(item.rb.velocity.normalized, (item.transform.position - lastHandler.transform.position).normalized) < 0 &&
                Vector3.Distance(item.GetMainHandle(lastHandler.side).transform.position, lastHandler.transform.position) <= 1 && !item.IsHanded() && isThrown && !item.isTelekinesisGrabbed &&
                startUpdate)
            {
                if (lastHandler.grabbedHandle == null)
                {
                    lastHandler.Grab(item.GetMainHandle(lastHandler.side), true);
                }
                else if (lastHandler.grabbedHandle != null && lastHolder != null && lastHolder.HasSlotFree())
                {
                    Common.MoveAlign(item.transform, item.holderPoint, lastHolder.slots[0]);
                    lastHolder.Snap(item);
                }
                else if (lastHandler.grabbedHandle != null && (lastHolder == null || !lastHolder.HasSlotFree()) && Player.local.creature.equipment.GetFirstFreeHolder() != null)
                {
                    Holder holder = Player.local.creature.equipment.GetFirstFreeHolder();
                    Common.MoveAlign(item.transform, item.holderPoint, holder.slots[0]);
                    holder.Snap(item);
                }
                else if (lastHandler.grabbedHandle != null && (lastHolder == null || !lastHolder.HasSlotFree()) && Player.local.creature.equipment.GetFirstFreeHolder() == null)
                {
                    BackpackHolder.instance.StoreItem(item);
                }
                item.rb.useGravity = true;
                isThrown = false;
                startUpdate = false;
            }
            if (active) item.colliderGroups[0].imbue.Transfer(spell, 100);
            if (Time.time - cdH <= cooldown || !beam || item.rb.velocity.magnitude - Player.local.locomotion.rb.velocity.magnitude < swordSpeed)
            {
                return;
            }
            else
            {
                cdH = Time.time;
                Catalog.GetData<ItemData>("DSDBeam").SpawnAsync(null, item.flyDirRef.position, Quaternion.LookRotation(item.flyDirRef.forward, item.rb.velocity.normalized - Player.local.locomotion.rb.velocity.normalized));
            }
        }
        public IEnumerator Teleport(RagdollHand hand)
        {
            yield return new WaitForEndOfFrame();
            hand.caster.telekinesis.TryRelease();
            foreach (Damager damager in item.GetComponentsInChildren<Damager>())
            {
                damager.UnPenetrateAll();
            }
            Common.MoveAlign(item.gameObject.transform, item.GetMainHandle(hand.side).GetDefaultOrientation(hand.side).gameObject.transform, hand.gameObject.transform);
            hand.Grab(item.GetMainHandle(hand.side), true);
        }
    }
    public class DaggerDespawn : MonoBehaviour
    {
        Item item;
        public Creature enemy;
        public void Start()
        {
            item = GetComponent<Item>();
            EffectInstance instance = Catalog.GetData<EffectData>("DSDFire").Spawn(item.transform, false);
            instance.SetRenderer(item.renderers[0], false);
            instance.SetIntensity(1);
            instance.Play();
            foreach(Item other in Item.allActive)
            {
                if(other.gameObject.GetComponent<DaggerDespawn>() != null)
                {
                    foreach(Collider collider in item.gameObject.GetComponentsInChildren<Collider>())
                    {
                        foreach(Collider otherCollider in other.gameObject.GetComponentsInChildren<Collider>())
                        {
                            Physics.IgnoreCollision(collider, otherCollider, true);
                        }
                    }
                }
            }
        }
        public void FixedUpdate()
        {
            if (!item.IsHanded() && item.isFlying)
            {
                //Quaternion deltaRotation = Quaternion.Euler(new Vector3(0, 0, 2160) * Time.fixedDeltaTime);
                //item.rb.MoveRotation(item.rb.rotation * deltaRotation);
                item.flyDirRef.Rotate(new Vector3(0, 0, 1080) * Time.fixedDeltaTime);
                if (enemy != null)
                    item.rb.AddForce((item.transform.position - enemy.ragdoll.targetPart.transform.position).normalized * 5, ForceMode.Force);
            }
        }
        public void OnCollisionEnter(Collision c)
        {
            if (c.collider.gameObject.GetComponentInParent<DanteComponent>() != null) item.IgnoreObjectCollision(c.collider.gameObject.GetComponentInParent<Item>());
            else if (!item.IsHanded())
            {
                StartCoroutine(BeginDespawn());
            }
        }
        public IEnumerator BeginDespawn()
        {
            if (enemy != null) yield return new WaitForSeconds(3);
            yield return new WaitForSeconds(0.3f);
            if (item.IsHanded()) yield break;
            foreach (Damager damager in item.GetComponentsInChildren<Damager>())
            {
                damager.UnPenetrateAll();
            }
            item.Despawn();
        }
    }
}
