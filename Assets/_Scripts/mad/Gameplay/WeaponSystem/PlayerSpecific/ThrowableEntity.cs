using UnityEngine;
using WGRF.AI;
using WGRF.Core;
using WGRF.Interactions;

namespace WGRF.Entities.BattleSystem
{
    public class ThrowableEntity : CoreBehaviour, IThrowable
    {
        [Header("Set in inspector")]
        [SerializeField] float throwForSeconds;
        [SerializeField] float throwSpeed;
        [SerializeField] LayerMask detectionLayers;

        bool wasThrown;
        bool abilityInUse;
        bool isFrozen;

        float rotationSpeed = 5f;
        float throwSpeedCache;

        float throwTimer;

        SpriteRenderer childSpriteTransform;

        protected override void PreAwake()
        {
            throwSpeedCache = throwSpeed;
        }

        private void Start()
        {
            ManagerHub.S.GameEventHandler.onAbilityUse += OnAbilityUse;
            ManagerHub.S.GameEventHandler.onAbilityEnd += OnAbilityEnd;
        }

        /// <summary>
        /// Call to get THIS throwables throw speed.
        /// </summary>
        public float GetThrowSpeed()
        {
            return throwSpeedCache;
        }

        /// <summary>
        /// Call to start the throwing weapon sequence by setting the wasThrown to true.
        /// <para>Sets throwTimer to throwForSeconds</para>
        /// </summary>
        public void InitiateThrow()
        {
            if (!abilityInUse)
            {
                rotationSpeed = 5f;
            }

            childSpriteTransform = GetComponentInChildren<SpriteRenderer>();

            throwTimer = throwForSeconds;
            wasThrown = true;
        }

        private void Update()
        {
            if (!wasThrown || isFrozen) return;

            CheckForCollisions();

            MoveForward(throwSpeedCache);

            CheckForCollisions();

            RotateChildSprite();
        }

        /// <summary>
        /// Call to check for enemy collisions on the local forward of the throwable object.
        /// <para>Calls ResetThrow() at the end.</para>
        /// </summary>
        void CheckForCollisions()
        {
            Ray forwardRay = new Ray(transform.position, transform.forward);
            RaycastHit hitInfo;

            if (Physics.Raycast(forwardRay, out hitInfo, 1f, detectionLayers))
            {
                IInteractable interaction = (IInteractable)hitInfo.collider.GetComponent<EnemyEntity>();

                if (interaction != null)
                {
                    interaction.StunInteraction();
                }

                ResetThrow();
            }
        }

        /// <summary>
        /// Call to move the gameObject forward by throwSpeed and subtract Time.deltaTime from throwTimer
        /// so the gameObject does not fly forever.
        /// <para>Calls ResetThrow() when the throw timer falls below 0.</para>
        /// </summary>
        void MoveForward(float throwSpeed)
        {
            transform.position += transform.forward * throwSpeed * Time.deltaTime;

            throwTimer -= Time.deltaTime;
            if (throwTimer <= 0)
            {
                ResetThrow();
            }
        }

        /// <summary>
        /// Call to rotate the sprite gameObject by rotationSpeed around it self.
        /// </summary>
        void RotateChildSprite()
        {
            float rY = -(rotationSpeed * Time.time * 360) % 360f;
            childSpriteTransform.transform.rotation = Quaternion.Euler(90f, rY, 0f);
        }

        #region ABILITY_SPECIFICS
        /// <summary>
        /// <para>*Subscribed to GameEventHandler.onAbilityUse*</para>
        /// Call to set the throwable values to the new passed values.
        /// <para>Used to simulate the time ability use.</para>
        /// </summary>
        /// <param name="newSpeed">The new speed of the throwable.</param>
        /// <param name="newRotSpeed">The new rotation speed of the throwable</param>
        /// <param name="isFrozen">Should the throwable be moving?</param>
        void OnAbilityUse(float newSpeed, float newRotSpeed, bool isFrozen)
        {
            throwSpeedCache = newSpeed;
            rotationSpeed = newRotSpeed;

            abilityInUse = true;

            this.isFrozen = isFrozen;
        }

        /// <summary>
        /// <para>*Subscribed to GameEventHandler.onAbilityEnd*</para>
        /// Call to set abilityInUse and isFrozen to false.
        /// <para>Calls ResetSpeedAndRotation().</para>
        /// </summary>
        void OnAbilityEnd()
        {
            abilityInUse = false;
            isFrozen = false;

            ResetSpeedAndRotation();
        }
        #endregion

        /// <summary>
        /// Call to reset the throwable values back to default.
        /// <para>Sets wasThrown to false, throwTimer to throwForSeconds and calls ResetSpeedAndRotation().</para>
        /// </summary>
        void ResetThrow()
        {
            wasThrown = false;

            throwTimer = throwForSeconds;

            ResetSpeedAndRotation();
        }

        /// <summary>
        /// Call to set the rotation speed to 5, and the throwSpeedCache to throwSpeed;
        /// </summary>
        public void ResetSpeedAndRotation()
        {
            rotationSpeed = 5f;
            throwSpeedCache = throwSpeed;
        }

        protected override void PreDestroy()
        {
            ManagerHub.S.GameEventHandler.onAbilityUse -= OnAbilityUse;
            ManagerHub.S.GameEventHandler.onAbilityEnd -= OnAbilityEnd;
        }
    }
}