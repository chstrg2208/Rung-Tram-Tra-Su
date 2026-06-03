using UnityEngine;

namespace RungTramTraSu
{
    public class AnimalAI : MonoBehaviour
    {
        public enum AnimalType { Stork, Snake, Fish, Butterfly, Duck }

        [Header("Animal Settings")]
        [SerializeField] private AnimalType animalType;
        [SerializeField] private float speed = 2.0f;
        [SerializeField] private float range = 5.0f;

        private Vector3 startPos;
        private Transform player;
        private PlayerController playerController;
        private bool isFleeing = false;
        private float actionTimer = 0f;

        // Visual feedback when scared
        private bool hasFled = false;

        public AnimalType Type => animalType;

        private void Start()
        {
            startPos = transform.position;
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerController = playerObj.GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            switch (animalType)
            {
                case AnimalType.Stork:
                    HandleStork();
                    break;
                case AnimalType.Snake:
                    HandleSnake();
                    break;
                case AnimalType.Fish:
                    HandleFish();
                    break;
                case AnimalType.Butterfly:
                    HandleButterfly();
                    break;
                case AnimalType.Duck:
                    HandleDuck();
                    break;
            }
        }

        private void HandleStork()
        {
            if (isFleeing)
            {
                // Fly away upwards and forwards
                transform.Translate(new Vector3(0.3f, 1.2f, 1.0f) * speed * Time.deltaTime, Space.World);
                // Rotate to fly away
                transform.rotation = Quaternion.LookRotation(new Vector3(0.3f, 1.2f, 1.0f));
                
                // Destroy after flying far
                if (Vector3.Distance(transform.position, startPos) > 60f)
                {
                    Destroy(gameObject);
                }
                return;
            }

            if (player != null)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                bool playerIsCrouching = playerController != null && playerController.IsCrouching;
                float scareDist = playerIsCrouching ? 4.0f : 8.5f;

                if (dist < scareDist && !hasFled)
                {
                    isFleeing = true;
                    hasFled = true;
                    Debug.Log("[AnimalAI] Stork is scared and flies away!");
                    if (Phase4Manager.Instance != null)
                    {
                        Phase4Manager.Instance.NotifyStorkScared();
                    }
                }
            }
        }

        private void HandleSnake()
        {
            // Swim back and forth along X
            float offset = Mathf.PingPong(Time.time * speed, range) - (range / 2f);
            transform.position = startPos + new Vector3(offset, 0f, 0f);
            
            // Look direction
            float velocityX = Mathf.Cos(Time.time * speed);
            if (velocityX > 0.05f) transform.rotation = Quaternion.Euler(0, 90, 0);
            else if (velocityX < -0.05f) transform.rotation = Quaternion.Euler(0, -90, 0);
        }

        private void HandleFish()
        {
            // Fish swims under water, periodically leaps out
            actionTimer += Time.deltaTime;
            if (actionTimer > 4.5f)
            {
                actionTimer = 0f;
                StartCoroutine(JumpRoutine());
            }
        }

        private System.Collections.IEnumerator JumpRoutine()
        {
            float elapsed = 0f;
            float duration = 0.8f;
            Vector3 peakPos = startPos + Vector3.up * 1.5f; // Jump out of water
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                // Parabolic jump path
                float height = Mathf.Sin(t * Mathf.PI);
                transform.position = Vector3.Lerp(startPos, peakPos, height);
                yield return null;
            }
            transform.position = startPos;
        }

        private void HandleButterfly()
        {
            // Fly in circle path around start pos
            float angle = Time.time * speed;
            float x = startPos.x + Mathf.Cos(angle) * range;
            float z = startPos.z + Mathf.Sin(angle) * range;
            float y = startPos.y + Mathf.Sin(Time.time * 3f) * 0.3f;
            transform.position = new Vector3(x, y, z);
            
            // Face direction of circle path
            Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0.1f * Mathf.Cos(Time.time * 3f), Mathf.Cos(angle));
            transform.rotation = Quaternion.LookRotation(tangent);
        }

        private void HandleDuck()
        {
            // Swim in figure-eight
            float t = Time.time * speed * 0.5f;
            float x = startPos.x + Mathf.Sin(t) * range;
            float z = startPos.z + Mathf.Sin(2f * t) * (range / 2f);
            Vector3 nextPos = new Vector3(x, startPos.y, z);

            Vector3 direction = (nextPos - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 4.0f * Time.deltaTime);
            }
            transform.position = nextPos;
        }
    }
}
