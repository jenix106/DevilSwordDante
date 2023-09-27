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
                        EffectInstance instance = Catalog.GetData<EffectData>("DSDActivate").Spawn(grabbedItem.transform, null, false);
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
                        EffectInstance instance = Catalog.GetData<EffectData>("DSDActivate").Spawn(grabbedItem.transform, null, false);
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
                            EffectInstance instance = Catalog.GetData<EffectData>("DSDActivate").Spawn(item.transform, null, false);
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
                            EffectInstance instance = Catalog.GetData<EffectData>("DSDActivate").Spawn(item.transform, null, false);
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
        public Item item;
        public Item sword;
        public Creature user;
        public Color beamColor;
        public Color beamEmission;
        public Vector3 beamSize;
        public float despawnTime;
        public float beamSpeed;
        public float beamDamage;
        public bool dismember;
        public Vector3 beamScaleUpdate;
        List<RagdollPart> parts = new List<RagdollPart>();
        public Imbue imbue;
        public void Start()
        {
            item = GetComponent<Item>();
            imbue = item.colliderGroups[0].imbue;
            item.Despawn(despawnTime);
            item.disallowDespawn = true;
            item.renderers[0].material.SetColor("_BaseColor", beamColor);
            item.renderers[0].material.SetColor("_EmissionColor", beamEmission * 2f);
            item.renderers[0].gameObject.transform.localScale = beamSize;
            item.mainCollisionHandler.ClearPhysicModifiers();
            item.physicBody.useGravity = false;
            item.physicBody.drag = 0;
            item.IgnoreRagdollCollision(Player.currentCreature.ragdoll);
            item.RefreshCollision(true);
            item.Throw();
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
        public void Update()
        {
            item.gameObject.transform.localScale += beamScaleUpdate * (Time.deltaTime * 100);
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
            if (c.GetComponentInParent<Breakable>() is Breakable breakable)
            {
                if (item.physicBody.velocity.sqrMagnitude < breakable.neededImpactForceToDamage)
                    return;
                float sqrMagnitude = item.physicBody.velocity.sqrMagnitude;
                --breakable.hitsUntilBreak;
                if (breakable.canInstantaneouslyBreak && sqrMagnitude >= breakable.instantaneousBreakVelocityThreshold)
                    breakable.hitsUntilBreak = 0;
                breakable.onTakeDamage?.Invoke(sqrMagnitude);
                if (breakable.IsBroken || breakable.hitsUntilBreak > 0)
                    return;
                breakable.Break();
            }
            if (c.GetComponentInParent<ColliderGroup>() is ColliderGroup group && group.collisionHandler.isRagdollPart)
            {
                RagdollPart part = group.collisionHandler.ragdollPart;
                if (part.ragdoll.creature != user && part.ragdoll.creature.gameObject.activeSelf == true && !part.isSliced)
                {
                    CollisionInstance instance = new CollisionInstance(new DamageStruct(DamageType.Slash, beamDamage))
                    {
                        targetCollider = c,
                        targetColliderGroup = group,
                        sourceColliderGroup = item.colliderGroups[0],
                        sourceCollider = item.colliderGroups[0].colliders[0],
                        casterHand = sword?.lastHandler?.caster,
                        impactVelocity = item.physicBody.velocity,
                        contactPoint = c.transform.position,
                        contactNormal = -item.physicBody.velocity
                    };
                    instance.damageStruct.penetration = DamageStruct.Penetration.None;
                    instance.damageStruct.hitRagdollPart = part;
                    if (part.sliceAllowed && !part.ragdoll.creature.isPlayer && dismember)
                    {
                        Vector3 direction = part.GetSliceDirection();
                        float num1 = Vector3.Dot(direction, item.transform.up);
                        float num2 = 1f / 3f;
                        if (num1 < num2 && num1 > -num2 && !parts.Contains(part))
                        {
                            parts.Add(part);
                        }
                    }
                    if (imbue?.spellCastBase?.GetType() == typeof(SpellCastLightning))
                    {
                        part.ragdoll.creature.TryElectrocute(1, 2, true, true, (imbue.spellCastBase as SpellCastLightning).imbueHitRagdollEffectData);
                        imbue.spellCastBase.OnImbueCollisionStart(instance);
                    }
                    if (imbue?.spellCastBase?.GetType() == typeof(SpellCastProjectile))
                    {
                        instance.damageStruct.damage *= 2;
                        imbue.spellCastBase.OnImbueCollisionStart(instance);
                    }
                    if (imbue?.spellCastBase?.GetType() == typeof(SpellCastGravity))
                    {
                        imbue.spellCastBase.OnImbueCollisionStart(instance);
                        part.ragdoll.creature.TryPush(Creature.PushType.Hit, item.physicBody.velocity, 3, part.type);
                        part.physicBody.AddForce(item.physicBody.velocity, ForceMode.VelocityChange);
                    }
                    else
                    {
                        if (imbue?.spellCastBase != null && imbue.energy > 0)
                        {
                            imbue.spellCastBase.OnImbueCollisionStart(instance);
                        }
                        part.ragdoll.creature.TryPush(Creature.PushType.Hit, item.physicBody.velocity, 1, part.type);
                    }
                    part.ragdoll.creature.Damage(instance);
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
        bool right = false;
        bool up = false;
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
            item.physicBody.useGravity = true;
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
                    Vector3 velocity = item.physicBody.velocity;
                    item.physicBody.velocity = (creature.position - item.transform.position).normalized * velocity.magnitude;
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
                EffectInstance instance = Catalog.GetData<EffectData>("DSDActivate").Spawn(item.transform, null, false);
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
                right = !right;
                if (!right) up = !up;
                Catalog.GetData<ItemData>("DSDDaggers").SpawnAsync(ShootDagger,
                    Player.local.head.cam.transform.position + ((right ? Player.local.head.cam.transform.right : -Player.local.head.cam.transform.right) * 0.4f) +
                    (up ? Player.local.head.cam.transform.up * 0.25f : Vector3.zero),
                    Player.local.head.cam.transform.rotation);
                GameObject effect = new GameObject();
                effect.transform.position = Player.local.head.cam.transform.position + (right ? Player.local.head.cam.transform.right : -Player.local.head.cam.transform.right) +
                    (up ? Player.local.head.cam.transform.up * 0.5f : Vector3.zero);
                effect.transform.rotation = Quaternion.identity;
                EffectInstance instance = Catalog.GetData<EffectData>("DSDActivate").Spawn(effect.transform, null, false);
                instance.SetIntensity(1);
                instance.Play();
                Destroy(effect, 2);
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
                item.physicBody.rigidBody.detectCollisions = false;
                item.mainHandler.physicBody.rigidBody.detectCollisions = false;
                item.mainHandler.otherHand.physicBody.rigidBody.detectCollisions = false;
            }
            yield return new WaitForSeconds(DashTime);
            if (DisableGravity)
                Player.local.locomotion.rb.useGravity = true;
            if (DisableCollision)
            {
                Player.local.locomotion.rb.detectCollisions = true;
                item.physicBody.rigidBody.detectCollisions = true;
                item.mainHandler.physicBody.rigidBody.detectCollisions = true;
                item.mainHandler.otherHand.physicBody.rigidBody.detectCollisions = true;
            }
            if (StopOnEnd) Player.local.locomotion.rb.velocity = Vector3.zero;
            Player.fallDamage = fallDamage;
            dashing = false;
            yield break;
        }
        public void ShootDagger(Item spawnedItem)
        {
            Transform creature = GetEnemy()?.ragdoll?.targetPart?.transform;
            spawnedItem.physicBody.useGravity = false;
            spawnedItem.physicBody.drag = 0;
            if (creature != null)
                spawnedItem.physicBody.AddForce((creature.position - spawnedItem.transform.position).normalized * 45f, ForceMode.Impulse);
            else spawnedItem.physicBody.AddForce(Player.local.head.transform.forward * 45f, ForceMode.Impulse);
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
                if (creature != null && !creature.isPlayer && creature.ragdoll.isActiveAndEnabled && !creature.isKilled && Vector3.Angle(Player.local.head.cam.transform.forward.normalized, (creature.ragdoll.targetPart.transform.position - Player.local.head.cam.transform.position).normalized) <= 20 && closestCreature == null &&
                    Vector3.Distance(Player.local.transform.position, creature.ragdoll.targetPart.transform.position) <= 25)
                {
                    closestCreature = creature;
                }
                else if (creature != null && !creature.isPlayer && creature.ragdoll.isActiveAndEnabled && !creature.isKilled && Vector3.Angle(Player.local.head.cam.transform.forward.normalized, (creature.ragdoll.targetPart.transform.position - Player.local.head.cam.transform.position).normalized) <= 20 && closestCreature != null &&
                    Vector3.Distance(Player.local.transform.position, creature.ragdoll.targetPart.transform.position) <= 25)
                {
                    if (Vector3.Distance(Player.local.head.cam.transform.position, creature.ragdoll.targetPart.transform.position) < Vector3.Distance(Player.local.head.cam.transform.position, closestCreature.ragdoll.targetPart.transform.position)) closestCreature = creature;
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
                item.physicBody.useGravity = false;
                item.physicBody.AddForce(-(item.transform.position - lastHandler.transform.position).normalized * ReturnSpeed, ForceMode.Force);
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
                item.physicBody.useGravity = true;
                isThrown = false;
                startUpdate = false;
                spin = false;
            }
            if (lastHandler != null && Vector3.Dot(item.physicBody.velocity.normalized, (item.transform.position - lastHandler.transform.position).normalized) < 0 &&
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
                item.physicBody.useGravity = true;
                isThrown = false;
                startUpdate = false;
            }
            if (active) item.colliderGroups[0].imbue.Transfer(spell, 100);
            if (Time.time - cdH <= cooldown || !beam || item.physicBody.velocity.magnitude - Player.local.locomotion.rb.velocity.magnitude < swordSpeed)
            {
                return;
            }
            else
            {
                cdH = Time.time; 
                Catalog.GetData<ItemData>("DSDBeam").SpawnAsync(beam =>
                {
                    BeamCustomization beamCustomization = beam.GetComponent<BeamCustomization>();
                    beamCustomization.sword = item;
                    beamCustomization.user = item.mainHandler != null ? item.mainHandler?.creature : item.lastHandler?.creature;
                    if (beamCustomization.user?.player != null) beam.physicBody.AddForce(Player.local.head.transform.forward * beamCustomization.beamSpeed, ForceMode.Impulse);
                    else if (beamCustomization.user?.brain?.currentTarget is Creature target) beam.physicBody.AddForce(-(beam.transform.position - target.ragdoll.targetPart.transform.position).normalized * beamCustomization.beamSpeed, ForceMode.Impulse);
                    else beam.physicBody.AddForce(beamCustomization.user.ragdoll.headPart.transform.forward * beamCustomization.beamSpeed, ForceMode.Impulse);
                    beam.physicBody.angularVelocity = Vector3.zero;
                    if (item.colliderGroups[0].imbue is Imbue imbue && imbue.spellCastBase != null && imbue.energy > 0)
                        beam.colliderGroups[0].imbue.Transfer(imbue.spellCastBase, beam.colliderGroups[0].imbue.maxEnergy);
                }, item.flyDirRef.position, Quaternion.LookRotation(item.flyDirRef.forward, item.physicBody.GetPointVelocity(item.flyDirRef.position).normalized));
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
            EffectInstance instance = Catalog.GetData<EffectData>("DSDFire").Spawn(item.transform, null, false);
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
                item.flyDirRef.Rotate(new Vector3(0, 0, 1080) * Time.fixedDeltaTime);
                if (enemy != null)
                    item.physicBody.AddForce((item.transform.position - enemy.ragdoll.targetPart.transform.position).normalized * 5, ForceMode.Force);
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
