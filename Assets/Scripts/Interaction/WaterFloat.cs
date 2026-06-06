using UnityEngine;

namespace RungTramTraSu
{
    public class WaterFloat : MonoBehaviour
    {
        [Header("Floating Offset")]
        [SerializeField] private float initialYOffset = 0.16f; // Adjustment for boat hull depth

        [Header("Rocking Parameters")]
        [SerializeField] private float rockXAmplitude = 1.5f; 
        [SerializeField] private float rockZAmplitude = 2.0f; 
        [SerializeField] private float rockFrequency = 0.8f;  

        private float initialY;
        private float initialYaw;
        private float randomOffset;

        private void Start()
        {
            initialY = transform.position.y;
            initialYaw = transform.rotation.eulerAngles.y; // Cache initial yaw
            randomOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            Vector3 pos = transform.position;
            float timeVal = Time.time + randomOffset;

            float waveHeight = 0f;
            float slopeLength = 0f;
            float slopeWidth = 0f;
            float delta = 0.3f;

            if (WaterWaveDeformer.Instance != null)
            {
                // Sample wave height at the boat center
                waveHeight = WaterWaveDeformer.Instance.GetWaveHeight(pos.x, pos.z);

                // Sample heights at four points in the boat's local reference frame (normalized to ignore scale)
                Vector3 localForward = transform.right.normalized;  // Boat length is along local X
                Vector3 localRight = transform.forward.normalized;  // Boat width is along local Z

                Vector3 posF = pos + localForward * delta;
                Vector3 posB = pos - localForward * delta;
                Vector3 posR = pos + localRight * delta;
                Vector3 posL = pos - localRight * delta;

                float hF = WaterWaveDeformer.Instance.GetWaveHeight(posF.x, posF.z);
                float hB = WaterWaveDeformer.Instance.GetWaveHeight(posB.x, posB.z);
                float hR = WaterWaveDeformer.Instance.GetWaveHeight(posR.x, posR.z);
                float hL = WaterWaveDeformer.Instance.GetWaveHeight(posL.x, posL.z);

                // Calculate slopes along local axes (delta is in world space, so distance is 2 * delta)
                slopeLength = (hF - hB) / (2f * delta); // Slope along boat length (pitch)
                slopeWidth = (hR - hL) / (2f * delta);  // Slope along boat width (roll)
            }
            else
            {
                // Fallback bobbing if deformer is missing
                waveHeight = initialY + Mathf.Sin(timeVal * 1.2f) * 0.04f;
            }

            // Set Y position
            float targetY = waveHeight + initialYOffset;
            transform.position = new Vector3(pos.x, targetY, pos.z);

            // Calculate local pitch and roll angles
            float pitch = slopeLength * 35f;   // Positive Z rotation tilts local +X up
            float roll = -slopeWidth * 35f;    // Negative X rotation tilts local +Z up

            // Clamp tilt angles to prevent unrealistic vertical or upside-down orientations
            pitch = Mathf.Clamp(pitch, -25f, 25f);
            roll = Mathf.Clamp(roll, -25f, 25f);

            // Add a gentle wind/water wobble
            pitch += Mathf.Sin(timeVal * rockFrequency) * rockXAmplitude;
            roll += Mathf.Cos(timeVal * rockFrequency * 1.3f) * rockZAmplitude;

            // Apply rotation relative to cached yaw:
            // - X-axis rotation is roll (tilts side-to-side)
            // - Z-axis rotation is pitch (tilts front-to-back)
            transform.rotation = Quaternion.Euler(0f, initialYaw, 0f) * Quaternion.Euler(roll, 0f, pitch);
        }
    }
}
