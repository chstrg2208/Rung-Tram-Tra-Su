using UnityEngine;

namespace RungTramTraSu
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerFootsteps : MonoBehaviour
    {
        [Header("Audio Clips")]
        public AudioClip[] grassFootsteps;
        public AudioClip[] woodFootsteps; 

        [Header("Settings")]
        [SerializeField] private float walkInterval = 0.45f;
        [SerializeField] private float crouchInterval = 0.7f;
        [SerializeField] private float volume = 0.35f;

        private PlayerController playerController;
        private CharacterController characterController;
        private AudioSource audioSource;
        private float footstepTimer;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            characterController = GetComponent<CharacterController>();

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.spatialBlend = 0.1f; 
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }

        private void Update()
        {
            if (characterController == null || !characterController.enabled) return;

            if (!characterController.isGrounded)
            {
                return;
            }

            Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
            if (horizontalVelocity.magnitude < 0.15f)
            {
                footstepTimer = 0.1f;
                return;
            }

            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                string surface = DetectSurface();
                PlayFootstep(surface);
                footstepTimer = playerController.IsCrouching ? crouchInterval : walkInterval;
            }
        }

        private string DetectSurface()
        {
            // Raycast down from slightly above player feet
            if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 1.5f))
            {
                string hitName = hit.collider.gameObject.name.ToLower();
                
                // Climb up the hierarchy parent names to catch nested child objects
                Transform parent = hit.collider.transform.parent;
                while (parent != null)
                {
                    hitName += "/" + parent.gameObject.name.ToLower();
                    parent = parent.parent;
                }

                // If hit walkway, bridge, house structure, wood planks, or deck
                if (hitName.Contains("wood") || hitName.Contains("bridge") || 
                    hitName.Contains("house") || hitName.Contains("plank") || 
                    hitName.Contains("walkway") || hitName.Contains("deck"))
                {
                    return "wood";
                }
            }
            return "grass";
        }

        private void PlayFootstep(string surface)
        {
            AudioClip[] clips = grassFootsteps;
            bool isFallback = false;

            if (surface == "wood")
            {
                if (woodFootsteps != null && woodFootsteps.Length > 0)
                {
                    clips = woodFootsteps;
                }
                else
                {
                    // Fallback to grass but modify pitch/volume to sound like hollow wood thud
                    clips = grassFootsteps;
                    isFallback = true;
                }
            }

            if (clips == null || clips.Length == 0) return;

            int index = Random.Range(0, clips.Length);
            AudioClip clip = clips[index];
            if (clip != null)
            {
                if (isFallback)
                {
                    // Pitch shift lower and increase volume slightly for hollow wood fallback sound
                    audioSource.pitch = Random.Range(0.6f, 0.72f);
                    audioSource.PlayOneShot(clip, volume * 1.15f);
                }
                else
                {
                    audioSource.pitch = Random.Range(0.85f, 1.15f);
                    audioSource.PlayOneShot(clip, volume);
                }
            }
        }
    }
}
