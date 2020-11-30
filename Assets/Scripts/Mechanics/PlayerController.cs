using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;

namespace Platformer.Mechanics
{
    /// <summary>
    /// This is the main class used to implement control of the player.
    /// It is a superset of the AnimationController class, but is inlined to allow for any kind of customisation.
    /// </summary>
    public class PlayerController : KinematicObject
    {
        public GameObject player;
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        public ParticleSystem attackParticle;

        /// <summary>
        /// Max horizontal speed of the player.
        /// </summary>
        public float maxSpeed = 7;
        /// <summary>
        /// Initial jump velocity at the start of a jump.
        /// </summary>
        public float jumpTakeOffSpeed = 7;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;
        /*internal new*/
        public Collider2D collider2d;
        /*internal new*/
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        bool jump;
        Vector2 move;
        float lastLookDirection = 0f;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        public Bounds Bounds => collider2d.bounds;

        //Double tap variables
        private bool isTapping;
        private float lastTap;
        private float tapTime = 0.3f;

        private int countL = 0;
        private int countR = 0;

        public bool isDoubleTap = false;

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        protected override void Update()
        {
            if (controlEnabled)
            {
                if (velocity.magnitude != 0)
                {
                    lastLookDirection = velocity.magnitude;
                }
                move.x = Input.GetAxis("Horizontal");
                if (jumpState == JumpState.Grounded && Input.GetButtonDown("Jump"))
                    jumpState = JumpState.PrepareToJump;
                else if (Input.GetButtonUp("Jump"))
                {
                    stopJump = true;
                    Schedule<PlayerStopJump>().player = this;
                }

                if (Input.GetMouseButtonDown(0))
                {
                    if (lastLookDirection > 0)
                    {
                        StartCoroutine(FinishAttack());
                        animator.SetTrigger("attackTrig");
                        attackParticle.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
                        attackParticle.Play();

                        this.transform.GetChild(0).GetComponent<BoxCollider2D>().enabled = true;
                    }
                    if (lastLookDirection < 0)
                    {
                        StartCoroutine(FinishAttack());
                        animator.SetTrigger("attackTrig");
                        attackParticle.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
                        attackParticle.Play();

                        this.transform.GetChild(1).GetComponent<BoxCollider2D>().enabled = true;
                    }
                }
            }
            else
            {
                move.x = 0;
            }
            UpdateJumpState();
            base.Update();
        }

        private IEnumerator FinishAttack()
        {
            yield return new WaitForSeconds(0.2f);
            this.transform.GetChild(1).GetComponent<BoxCollider2D>().enabled = false;
            this.transform.GetChild(0).GetComponent<BoxCollider2D>().enabled = false;
        }


        private IEnumerator SingleTapLeft()
        {
            yield return new WaitForSeconds(tapTime);

            if (isTapping)
            {
                isDoubleTap = false;
                Debug.Log("Single Tap Left!");
                isTapping = false;
                this.transform.GetChild(1).GetComponent<BoxCollider2D>().enabled = false;
            }
            if (isDoubleTap)
            {
                this.transform.GetChild(1).GetComponent<BoxCollider2D>().enabled = false;
            }
        }

        private IEnumerator SingleTapRight()
        {
            yield return new WaitForSeconds(tapTime);

            if (isTapping)
            {
                isDoubleTap = false;
                Debug.Log("Single Tap Right!");
                isTapping = false;
                this.transform.GetChild(0).GetComponent<BoxCollider2D>().enabled = false;
            }
            if (isDoubleTap)
            {
                this.transform.GetChild(0).GetComponent<BoxCollider2D>().enabled = false;
            }
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        protected override void ComputeVelocity()
        {
            if (jump && IsGrounded)
            {
                velocity.y = jumpTakeOffSpeed * model.jumpModifier;
                jump = false;
            }
            else if (stopJump)
            {
                stopJump = false;
                if (velocity.y > 0)
                {
                    velocity.y = velocity.y * model.jumpDeceleration;
                }
            }

            if (move.x > 0.01f)
                spriteRenderer.flipX = false;
            else if (move.x < -0.01f)
                spriteRenderer.flipX = true;

            animator.SetBool("grounded", IsGrounded);
            animator.SetFloat("velocityX", Mathf.Abs(velocity.x) / maxSpeed);

            targetVelocity = move * maxSpeed;
        }

        public enum JumpState
        {
            Grounded,
            PrepareToJump,
            Jumping,
            InFlight,
            Landed
        }
    }
}