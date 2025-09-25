using UnityEngine;

namespace cowsins
{
    public class Coin : Trigger
    {
        [SerializeField] private int minCoins, maxCoins;

        [SerializeField] private AudioClip collectCoinSFX;
        public override void TriggerEnter(Collider other)
        {
            int amountOfCoins = Random.Range(minCoins, maxCoins);
            CoinManager.Instance.AddCoins(amountOfCoins, true);
            UIController.instance.UpdateCoinsPanel();
            UIEvents.onCoinsChange?.Invoke(CoinManager.Instance.coins);
            SoundManager.Instance.PlaySound(collectCoinSFX, 0, 1, false);
            Destroy(this.gameObject);
        }


#if SAVE_LOAD_ADD_ON
        public override void LoadedState()
        {
            Destroy(this.gameObject);
        }
#endif
    }

}