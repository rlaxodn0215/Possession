using UnityEngine;
using UnityEngine.Events;

namespace cowsins
{
    public class TurretProjectile : MonoBehaviour
    {
        [HideInInspector] public Vector3 dir;

        [HideInInspector] public float damage, speed, projectileDuration;

        public UnityEvent<TurretProjectile> destroyEvent;

        private void Start() => Invoke(nameof(DestroyTurretProjectile), projectileDuration);

        private void Update()
        {
            transform.Translate(dir * speed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                DestroyTurretProjectile();
                return;
            }

            PlayerStats player = other.GetComponent<PlayerStats>();
            player.Damage(damage, false);
            DestroyTurretProjectile();
        }

        private void DestroyTurretProjectile() => destroyEvent?.Invoke(this);
    }
}