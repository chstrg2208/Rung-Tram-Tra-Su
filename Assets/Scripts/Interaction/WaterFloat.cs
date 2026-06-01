using UnityEngine;

namespace RungTramTraSu
{
    public class WaterFloat : MonoBehaviour
    {
        [Header("Bobbing (Up/Down)")]
        [SerializeField] private float bobAmplitude = 0.04f; // Biên độ nhấp nhô (m)
        [SerializeField] private float bobFrequency = 1.2f;  // Tần số nhấp nhô

        [Header("Rocking (Tilt X/Z)")]
        [SerializeField] private float rockXAmplitude = 1.2f; // Độ nghiêng tối đa trục X (độ)
        [SerializeField] private float rockZAmplitude = 1.8f; // Độ nghiêng tối đa trục Z (độ)
        [SerializeField] private float rockFrequency = 0.8f;  // Tần số lắc lư

        private float initialY;
        private float randomOffset;

        private void Start()
        {
            // Lưu lại vị trí Y ban đầu
            initialY = transform.position.y;
            // Tạo một offset ngẫu nhiên để các thuyền khác nhau không bập bênh giống hệt nhau
            randomOffset = Random.Range(0f, 100f);
        }

        private void Update()
        {
            // 1. Nhấp nhô trục Y (Bobbing)
            Vector3 currentPosition = transform.position;
            float timeVal = Time.time + randomOffset;
            float newY = initialY + Mathf.Sin(timeVal * bobFrequency) * bobAmplitude;
            transform.position = new Vector3(currentPosition.x, newY, currentPosition.z);

            // 2. Lắc lư nghiêng ngả nhẹ (Rocking)
            float angleX = Mathf.Sin(timeVal * rockFrequency) * rockXAmplitude;
            float angleZ = Mathf.Cos(timeVal * rockFrequency * 1.1f) * rockZAmplitude;

            // Lấy góc xoay Y (Heading) hiện tại của thuyền để không khóa hướng xoay ngang
            float currentHeadingY = transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(angleX, currentHeadingY, angleZ);
        }
    }
}
