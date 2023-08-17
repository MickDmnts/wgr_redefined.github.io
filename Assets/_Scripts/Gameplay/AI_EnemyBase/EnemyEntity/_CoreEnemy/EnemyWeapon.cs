using System.Collections;
using UnityEngine;
using WGR.Gameplay.BattleSystem;
using WGR.Core;

namespace WGR.Gameplay.AI
{
    /* [CLASS DOCUMENTATION]
     * 
     * Private variables: These values change throughout the game.
     * 
     * Base Type:  AIEntityWeapon
     * 
     * This class is responsible for the attack mechanisms of the EnemyEntity.
     * 
     * [Must know]
     * 1.The ShootSequence() method gets passed as an Action callback in the attacking node creation of the Enemy BT.
     * 
     */
    public class EnemyWeapon : AIEntityWeapon
    {
        //Private variables
        EnemyEntity enemyEntity;
        bool currentlyAttacking;

        private void Awake()
        {
            CacheComponents();
        }

        /// <summary>
        /// Call cache the needed components.
        /// </summary>
        void CacheComponents()
        {
            //Component caching
            enemyEntity = GetComponentInParent<EnemyEntity>();

            if ((enemyWeaponRenderer = GetComponentInChildren<SpriteRenderer>(true)) == null)
            {
                Utils.MissingComponent("SpriteRenderer", this);
            }
        }

        private void Start()
        {
            SetStartDefaults();
            SetWeaponInfo(equipedWeapon);
        }

        /// <summary>
        /// Call to set the required startup default field values.
        /// </summary>
        protected override void SetStartDefaults()
        {
            canShoot = true;
            shootInterval = 0.4f;

            //cache attack timers
            shotBurstTimer = shotCooldownInterval;
            burstTimerCache = shotBurstTimer;
            projectilePerShotCache = projectilesPerShot;
        }

        //Base type summary
        public override void SetWeaponInfo(Weapon weapon, int cachedBulletCount = -1)
        {
            if (weapon == null) return;

            enemyWeaponRenderer.sprite = weapon.weaponEquipedSprite;
            shootInterval = weapon.IntervalBetweenShots;
            maxBulletSpread = weapon.MaxBulletSpread;
            canShoot = true;

            if (weapon.WeaponCategory.Equals(WeaponCategory.Ranged))
            {
                enemyEntity.EnemyAnimation.SetHoldingRangedWeaponState(true);
            }
        }

        /// <summary>
        /// Call to initiate the Enemy Shoot sequence.
        /// Acts as an update too when called from the enemyBT .Run()
        /// </summary>
        public override void ShootSequence()
        {
            if (!enemyEntity.GetIsAgentActive()) return;

            if (CanShoot())
            {
                TypeBasedAttack();
            }
        }

        /// <summary>
        /// Call to check if the enemy can shoot his next bullet again.
        /// </summary>
        protected override bool CanShoot()
        {
            if (Time.time >= shootDoneTime)
            {
                return true;
            }

            return false;
        }

        //Base type summary
        protected override void TypeBasedAttack()
        {
            switch (equipedWeapon.WeaponCategory)
            {
                case WeaponCategory.Unarmed:
                    MeleeAttack();
                    break;

                case WeaponCategory.Melee:
                    MeleeAttack();
                    break;

                case WeaponCategory.Ranged:
                    Shoot();

                    OnShootReset();
                    break;
            }
        }

        #region UNARMED_SPECIFIC
        /// <summary>
        /// Call to calculate the distance from THIS enemy to the PlayerEntity
        /// and check if the enemy can attack him based on an offset set from
        /// the equipedWeapon minimum shoot distance inspector field.
        /// </summary>
        void MeleeAttack()
        {
            if (currentlyAttacking) return;

            StartCoroutine(AttackThresshold());
        }

        /// <summary>
        /// Call to iniate the agent melee attack after 0.5f seconds.
        /// </summary>
        IEnumerator AttackThresshold()
        {
            currentlyAttacking = true;

            yield return new WaitForSecondsRealtime(0.5f);

            //Early exit if the enemy is dead or stunned.
            if (enemyEntity.IsDead || enemyEntity.IsStunned)
            {
                currentlyAttacking = false;
                yield break;
            }

            //Play an attack animation based on equiped weapon weapon type.
            enemyEntity.EnemyAnimation.PlayMeleeAnimation(equipedWeapon.WeaponType);

            Ray meleeRay = new Ray(transform.position, transform.forward);
            RaycastHit[] hits = new RaycastHit[10];
            int numOfHits = Physics.RaycastNonAlloc(meleeRay, hits, equipedWeapon.MinShootDistance, meleeDetectionLayers);

            if (numOfHits > 0)
            {
                for (int i = 0; i < numOfHits; i++)
                {
                    if (hits[i].transform.CompareTag("Player"))
                    {
                        if (!Physics.Linecast(transform.position, hits[i].transform.position, meleeLinecastLayers))
                        {
                            IInteractable interaction = hits[i].transform.GetComponent<IInteractable>();

                            if (interaction != null)
                            {
                                interaction.AttackInteraction();

                                GameManager.S.GameSoundsHandler.PlayOneShot(GameAudioClip.PunchSound);
                            }
                        }
                    }
                }
            }

            PlayWeaponSFX();

            currentlyAttacking = false;
            OnShootReset();
        }
        #endregion

        #region SHOOTING_SPECIFIC
        public override void Shoot()
        {
            //Shoot on cooldown update
            if (onCooldown)
            {
                DecreaseSpread();

                shotBurstTimer -= Time.deltaTime;
                if (shotBurstTimer <= 0f)
                {
                    onCooldown = false;
                    shotBurstTimer = burstTimerCache;
                    projectilesPerShot = projectilePerShotCache;
                }
                return;
            }

            //Shooting behaviour
            SetIsShooting(true);

            CalculateBulletSpread();

            if (!equipedWeapon.WeaponType.Equals(WeaponType.Shotgun))
            {
                EnableBullet(false);
            }
            else
            {
                EnableBullet(true);
            }

            //Weapon SFX.
            PlayWeaponSFX();
        }

        //Base type summary
        protected override void DecreaseSpread()
        {
            if (totalBulletSpread > 0)
            {
                totalBulletSpread -= accuracyIncreaseRate;
            }
        }

        /// <summary>
        /// Call to get a bullet gameObject from the BulletPool and move it in front of 
        /// firePoint.
        /// </summary>
        void EnableBullet(bool shotgunShot)
        {
            if (shotgunShot)
            {
                StartCoroutine(ShotgunShot());
                return;
            }

            GameObject tempBullet = GameManager.S.BulletPool.GetPooledBulletByType(BulletType.Enemy);

            float randomFloat = Random.Range(-totalBulletSpread, totalBulletSpread);

            if (tempBullet != null)
            {
                Quaternion bulletRotation = Quaternion.Euler(0f, randomFloat, 0);

                tempBullet.transform.position = firePoint.transform.position;
                tempBullet.transform.rotation = firePoint.rotation * bulletRotation;
                tempBullet.SetActive(true);
            }
            else
            {
                Debug.Log("BulletPool returned null");
            }

            projectilesPerShot--;
            if (projectilesPerShot <= 0)
            {
                onCooldown = true;
            }
        }

        /// <summary>
        /// Call to move 6 projectiles in front of the fire point.
        /// </summary>
        IEnumerator ShotgunShot()
        {
            GameObject[] pellets = new GameObject[6];

            for (int i = 0; i < 6; i++)
            {
                pellets[i] = GameManager.S.BulletPool.GetPooledBulletByType(BulletType.Enemy);
            }

            foreach (GameObject pellet in pellets)
            {
                float randomFloat = Random.Range(-equipedWeapon.MaxBulletSpread, equipedWeapon.MaxBulletSpread);

                if (pellet != null)
                {
                    Quaternion bulletRotation = Quaternion.Euler(0f, randomFloat, 0);

                    pellet.transform.position = firePoint.transform.position;
                    pellet.transform.rotation = firePoint.rotation * bulletRotation;
                    pellet.SetActive(true);
                }
                else
                {
                    Debug.Log("BulletPool returned null");
                }
            }

            yield return null;
        }

        /// <summary>
        /// Call to increase bulletSpread based on bulletSpreadRate
        /// </summary>
        void CalculateBulletSpread()
        {
            totalBulletSpread += bulletSpreadRate;
            if (totalBulletSpread >= maxBulletSpread)
            {
                totalBulletSpread = maxBulletSpread;
            }
        }
        #endregion

        #region UTILITIES
        //Base type summary
        public override AIEntity GetAIEntity()
        {
            return enemyEntity;
        }

        //Base type summary
        public override Transform GetFirepointTransform() => firePoint.transform;

        //Base type summary
        public override float GetWeaponRange()
        {
            return equipedWeapon.MinShootDistance;
        }

        //Base type summary
        public override void SetIsShooting(bool value)
        {
            isShooting = value;
        }

        //Base type summary
        public override void ClearWeaponSprite()
        {
            enemyWeaponRenderer.sprite = null;
        }

        //Base type summary
        protected override void OnShootReset()
        {
            shootDoneTime = shootInterval + Time.time;
            canShoot = false;
        }

        /// <summary>
        /// Call to play the equiped weapon SFX.
        /// <para>If the weapon has more than one SFX, plays a random one.</para>
        /// </summary>
        void PlayWeaponSFX()
        {
            if (equipedWeapon.gunShootSound.Length > 1)
            {
                int rndSfx = Random.Range(0, equipedWeapon.gunShootSound.Length);
                GameManager.S.GameSoundsHandler.PlayOneShot(equipedWeapon.gunShootSound[rndSfx]);
            }
            else
            {
                GameManager.S.GameSoundsHandler.PlayOneShot(equipedWeapon.gunShootSound[0]);
            }
        }
        #endregion
    }
}